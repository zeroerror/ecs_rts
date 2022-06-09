using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ZeroHero;
/// <summary>
/// 近战攻击伤害计算
/// </summary>
public class MeleeDamageSystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    private EntityQuery entityQuery;
    private EntityQueryDesc entityQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private NativeMultiHashMap<int, float3> effectMultiHashMap;
    private NativeList<CharacterType> killedEnemyList;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
             },
        };
    }
    protected override void OnInit()
    {
        base.OnInit();

        effectMultiHashMap = new NativeMultiHashMap<int, float3>(1000, Allocator.Persistent);
        killedEnemyList = new NativeList<CharacterType>(Allocator.Persistent);
        isInit = true;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isInit)
        {
            effectMultiHashMap.Dispose();
            killedEnemyList.Dispose();
        }
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!MapSystem.isInit) return;
        entityQuery = this.GetEntityQuery(entityQueryDesc);
        effectMultiHashMap.Clear();
        killedEnemyList.Clear();
        var job = new DamageCauseJob();
        job.AttackComponentTypeAccessor = GetArchetypeChunkComponentType<AttackComponent>(false);
        job.EntityAccessor = GetArchetypeChunkEntityType();
        job.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);
        job.AllyComponentFromEntity = GetComponentDataFromEntity<AllyComponent>(true);
        job.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
        job.AttributeFromEntity = GetComponentDataFromEntity<Attribute>(true);
        job.AttributeBaseFromEntity = GetComponentDataFromEntity<AttributeBase>(true);
        job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job.KillRewardList = LevelUpSystem.KillRewardList;
        job.effectMultiHashMap = effectMultiHashMap.AsParallelWriter();
        job.killedEnemyList = killedEnemyList.AsParallelWriter();
        job.deltaTime = Time.DeltaTime;
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, 1, this.Dependency);
        Dependency.Complete();

        NativeKeyValueArrays<int, float3> kvArray = effectMultiHashMap.GetKeyValueArrays(Allocator.Temp);
        float3 offset = new float3(0, 3f, 0);
        for (int i = 0; i < kvArray.Keys.Length; i++)
        {
            int effectID = kvArray.Keys[i];
            float3 pos = kvArray.Values[i];
            EffectMgr.Instance.PlayEffect(effectID, pos + offset);
        }
        if (killedEnemyList.Length > 0) GameController.HandleKillList(killedEnemyList.ToArray());
    }
    [BurstCompile]
    private struct DamageCauseJob : IJobEntityBatch
    {
        public ArchetypeChunkComponentType<AttackComponent> AttackComponentTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllyComponent> AllyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Attribute> AttributeFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AttributeBase> AttributeBaseFromEntity;
        [ReadOnly]
        public NativeList<KillRewardStruct> KillRewardList;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public NativeMultiHashMap<int, float3>.ParallelWriter effectMultiHashMap;
        public NativeList<CharacterType>.ParallelWriter killedEnemyList;
        public float deltaTime;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<AttackComponent> atkComponentArray = batchInChunk.GetNativeArray<AttackComponent>(AttackComponentTypeAccessor);
            NativeArray<Entity> entityArray = batchInChunk.GetNativeArray(EntityAccessor);
            for (int i = 0; i < atkComponentArray.Length; i++)
            {
                AttackComponent atkComponent = atkComponentArray[i];
                Entity selfEntity = entityArray[i];
                Entity targetEntity = atkComponent.targetEntity;
                Entity atkEntity = atkComponent.atkEntity;

                if (!TranslationFromEntity.Exists(targetEntity) || !AttributeFromEntity.Exists(targetEntity))
                {

                    commandBuffer.RemoveComponent<AttackComponent>(batchIndex, selfEntity);
                    continue;
                }
                float3 targetPos = TranslationFromEntity[targetEntity].Value;
                if (atkComponent.countTime <= atkComponent.delayTime)
                {
                    atkComponent.countTime += deltaTime;
                    commandBuffer.AddComponent<AttackComponent>(batchIndex,selfEntity,atkComponent);
                    continue;
                }

                float damage = atkComponent.damage;
                Attribute attribute = AttributeFromEntity[targetEntity];
                AttributeBase attributeBase = AttributeBaseFromEntity[targetEntity];
                if (damage > 0)
                {
                    float health = attribute.health - damage;
                    attribute.health = health > 0 ? health : 0;
                    #region [击杀单位添加经验]
                    if (health <= 0)
                    {
                        //Conditions：1.攻击者必须为英雄 2.非击杀友军
                        AttributeBase atktor = AttributeBaseFromEntity[atkEntity];
                        if (true)
                        {
                            bool isCount = false;
                            bool isAttacktorAlly = false;
                            if (AllyComponentFromEntity.Exists(atkEntity) && EnemyComponentFromEntity.Exists(targetEntity))
                            {
                                isCount = true;
                                isAttacktorAlly = true;
                            }
                            else if (AllyComponentFromEntity.Exists(targetEntity) && EnemyComponentFromEntity.Exists(atkEntity))
                            {
                                isCount = true;
                            }
                            if (isCount)
                            {
                                KillRewardStruct krs = new KillRewardStruct();
                                for (int index = 0; index < KillRewardList.Length; index++)
                                {
                                    var cfg = KillRewardList[index];
                                    if (cfg.CharacterType == attribute.characterType)
                                    {
                                        krs = cfg;
                                        break;
                                    }
                                }
                                float addExp = krs.Exp;
                                atktor.exp += addExp;
                                //Debug.Log("增加经验值: "+addExp);
                                int effectID;
                                if (isAttacktorAlly)
                                {
                                    effectID = 8;
                                    killedEnemyList.AddNoResize(attribute.characterType);
                                }
                                else
                                {
                                    effectID = 7;
                                }
                                effectMultiHashMap.Add(effectID, targetPos);

                                commandBuffer.SetComponent<AttributeBase>(batchIndex, atkEntity, atktor);
                            }
                        }

                    }

                    #endregion

                }
                else
                {
                    float health = attribute.health - damage;
                    attribute.health = health > attributeBase.health ? attributeBase.health : health;
                }

                commandBuffer.SetComponent<Attribute>(batchIndex, targetEntity, attribute);
                commandBuffer.RemoveComponent<AttackComponent>(batchIndex, selfEntity);
            }
        }
    }
}
