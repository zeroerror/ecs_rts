using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using ZeroHero;

[UpdateAfter(typeof(QuadrandSystem))]
public class BulletFlySystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    private EntityQuery bulletQuery;
    private EntityQueryDesc bulletQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private NativeList<float3> hitfleshPosList;
    private NativeList<float3> hitSteelPosList;
    private NativeList<float3> explodePosList;
    private NativeList<CharacterType> killedEnemyList;
    private NativeMultiHashMap<int, float3> effectMultiHashMap;

    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        bulletQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<BulletComponent>(),
                ComponentType.ReadOnly<MoveComponent>()
            }
        };
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnInit()
    {
        base.OnInit();
        isInit = true;
        killedEnemyList = new NativeList<CharacterType>(Allocator.Persistent);
        hitfleshPosList = new NativeList<float3>(1000, Allocator.TempJob);
        hitSteelPosList = new NativeList<float3>(1000, Allocator.TempJob);
        explodePosList = new NativeList<float3>(1000, Allocator.TempJob);
        effectMultiHashMap = new NativeMultiHashMap<int, float3>(1000, Allocator.Persistent);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        bulletQuery = GetEntityQuery(bulletQueryDesc);
        NativeArray<Entity> entityArray = bulletQuery.ToEntityArray(Allocator.TempJob);
        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
        foreach (Entity entity in entityArray)
        {
            commandBuffer.DestroyEntity(entity);
        }
        entityArray.Dispose();
    }
    protected override void OnDestroy()
    {
        if (isInit)
        {
            hitfleshPosList.Dispose();
            hitSteelPosList.Dispose();
            explodePosList.Dispose();
            killedEnemyList.Dispose();
            effectMultiHashMap.Dispose();
        }
        base.OnDestroy();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        bulletQuery = GetEntityQuery(bulletQueryDesc);
        if (bulletQuery.CalculateEntityCount() != 0)
        {
            hitfleshPosList.Clear();
            hitSteelPosList.Clear();
            explodePosList.Clear();
            killedEnemyList.Clear();
            effectMultiHashMap.Clear();
            var job = new BulletFlyJob();
            job.EntityAccessor = GetArchetypeChunkEntityType();
            job.BulletComponentTypeAccessor = GetArchetypeChunkComponentType<BulletComponent>(true);
            job.TranslationTypeAccessor = GetArchetypeChunkComponentType<Translation>(true);
            job.RotationAccessor = GetArchetypeChunkComponentType<Rotation>(false);
            job.MoveComponentTypeAccessor = GetArchetypeChunkComponentType<MoveComponent>(false);
            job.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);
            job.AttributeFromEntity = GetComponentDataFromEntity<Attribute>(true);
            job.AttributeBaseFromEntity = GetComponentDataFromEntity<AttributeBase>(true);
            job.AllyComponentFromEntity = GetComponentDataFromEntity<AllyComponent>(true);
            job.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
            job.HasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
            job.allyMap = QuadrandSystem.GetUnitHashMap();
            job.enemyMap = QuadrandSystem.GetTargetHashMap();
            job.quadrantMap = QuadrandSystem.GetQuadrantHashMap();
            job.killedEnemyList = killedEnemyList.AsParallelWriter();
            job.effectMultiHashMap = effectMultiHashMap.AsParallelWriter();
            job.hitfleshPosList = hitfleshPosList;
            job.hitSteelPosList = hitSteelPosList;
            job.explodePosList = explodePosList;
            job.buffArchetype = EntityEventSystem.Instance.buffArchetype;
            job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, bulletQuery, 1, this.Dependency);
            Dependency.Complete();

            #region [处理特效]
            float3 offset = new float3(0, 2f, 0);
            for (int i = 0; i < hitfleshPosList.Length; i++)
            {
                float3 hitPos = hitfleshPosList[i];
                EffectMgr.Instance.PlayEffect("喷血", hitPos);
            }
            for (int i = 0; i < hitSteelPosList.Length; i++)
            {
                float3 hitPos = hitSteelPosList[i];
                EffectMgr.Instance.PlayEffect("火花", hitPos);
            }

            for (int i = 0; i < explodePosList.Length; i++)
            {
                float3 explodePos = explodePosList[i];
                EffectMgr.Instance.PlayEffect("炮弹爆炸", explodePos + offset);
            }
            //人物死亡标记
            NativeKeyValueArrays<int, float3> kvArray = effectMultiHashMap.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < kvArray.Keys.Length; i++)
            {
                int effectID = kvArray.Keys[i];
                float3 pos = kvArray.Values[i];
                EffectMgr.Instance.PlayEffect(effectID, pos + offset);
            }
            if (killedEnemyList.Length > 0) GameController.HandleKillList(killedEnemyList.ToArray());
            #endregion
        }
    }

    [BurstCompile]
    private struct BulletFlyJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<BulletComponent> BulletComponentTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationTypeAccessor;
        public ArchetypeChunkComponentType<Rotation> RotationAccessor;
        public ArchetypeChunkComponentType<MoveComponent> MoveComponentTypeAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Attribute> AttributeFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AttributeBase> AttributeBaseFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllyComponent> AllyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<HasTarget> HasTargetFromEntity;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> allyMap;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> enemyMap;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> quadrantMap;
        [NativeDisableParallelForRestriction]
        public NativeList<float3> hitfleshPosList;
        [NativeDisableParallelForRestriction]
        public NativeList<float3> hitSteelPosList;
        [NativeDisableParallelForRestriction]
        public NativeList<float3> explodePosList;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public EntityArchetype buffArchetype;
        public NativeList<CharacterType>.ParallelWriter killedEnemyList;
        public NativeMultiHashMap<int, float3>.ParallelWriter effectMultiHashMap;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray<Translation>(TranslationTypeAccessor);
            NativeArray<Rotation> rotationArray = batchInChunk.GetNativeArray<Rotation>(RotationAccessor);
            NativeArray<BulletComponent> bulletComponentArray = batchInChunk.GetNativeArray<BulletComponent>(BulletComponentTypeAccessor);
            NativeArray<MoveComponent> moveComponentArray = batchInChunk.GetNativeArray<MoveComponent>(MoveComponentTypeAccessor);
            NativeArray<Entity> bulletEntityArray = batchInChunk.GetNativeArray(EntityAccessor);
            for (int i = 0; i < bulletComponentArray.Length; i++)
            {
                BulletComponent bulletComponent = bulletComponentArray[i];
                MoveComponent moveComponent = moveComponentArray[i];
                Translation translation = translationArray[i];
                Entity bulletEntity = bulletEntityArray[i];
                float speed = moveComponent.speed;
                int curMoveFrame = moveComponent.curMoveFrame;
                //if (curMoveFrame >= 500)
                //{
                //    commandBuffer.DestroyEntity(batchIndex, bulletEntity);
                //    continue;
                //}
                float3 targetPos = moveComponent.targetPos;
                Entity target = Entity.Null;
                if (HasTargetFromEntity.Exists(bulletEntity))
                {
                    target = HasTargetFromEntity[bulletEntity].targetEntity;
                    if (TranslationFromEntity.Exists(target)) targetPos = TranslationFromEntity[target].Value;
                }
                float3 curFramePosition = translation.Value;
                float3 moveDir = math.normalizesafe(targetPos - curFramePosition);
                float3 extraSpeed = bulletComponent.initSpeed;
                float distance = speed * 0.02f;
                extraSpeed.x = extraSpeed.x > distance ? distance : extraSpeed.x;
                extraSpeed.y = extraSpeed.y > distance ? distance : extraSpeed.y;
                extraSpeed.z = extraSpeed.z > distance ? distance : extraSpeed.z;
                moveDir += extraSpeed;
                if (!moveDir.Equals(float3.zero))
                {
                    float3 moveOffset = moveDir * distance;

                    float remianDistance = math.distance(curFramePosition, targetPos);
                    float addedDistance = math.distance(curFramePosition, curFramePosition + moveOffset);
                    float3 nextFramePosition = curFramePosition + moveOffset;
                    if (addedDistance < remianDistance)
                    {
                        //尚未到达射击目标点
                        commandBuffer.SetComponent<Translation>(batchIndex, bulletEntity, new Translation { Value = nextFramePosition });
                        Rotation rotation = rotationArray[i];
                        rotation.Value = Quaternion.LookRotation(moveDir);
                        commandBuffer.SetComponent<Rotation>(batchIndex, bulletEntity, rotation);
                    }
                    else //造成伤害
                    {
                        NativeList<Entity> damagedEntityList = new NativeList<Entity>(Allocator.Temp);
                        float damageRange = bulletComponent.range;
                        float damage = bulletComponent.damage;
                        if (bulletComponent.bulletType == BulletType.子弹) //造成单体伤害
                        {
                            if (AttributeFromEntity.Exists(target) && math.distance(targetPos, TranslationFromEntity[target].Value) <= bulletComponent.range)
                            {
                                Attribute targetAttribute = AttributeFromEntity[target];
                                targetAttribute.health -= bulletComponent.damage;
                                targetAttribute.health = targetAttribute.health > 0 ? targetAttribute.health : 0;
                                commandBuffer.SetComponent<Attribute>(batchIndex, target, targetAttribute);
                                damagedEntityList.Add(target);
                            }
                        }
                        else if (bulletComponent.bulletType == BulletType.炮弹) //造成群体伤害
                        {
                            int hashMapKey = QuadrandSystem.GetPositionHashMapKey(targetPos);
                            NativeArray<int> allKey = new NativeArray<int>(9, Allocator.Temp);
                            allKey[0] = hashMapKey;
                            allKey[1] = hashMapKey + 1;
                            allKey[2] = hashMapKey - 1;
                            allKey[3] = hashMapKey + QuadrandSystem.quadrantYMultiplier;
                            allKey[4] = hashMapKey - QuadrandSystem.quadrantYMultiplier;
                            allKey[5] = hashMapKey + QuadrandSystem.quadrantYMultiplier + 1;
                            allKey[6] = hashMapKey + QuadrandSystem.quadrantYMultiplier - 1;
                            allKey[7] = hashMapKey - QuadrandSystem.quadrantYMultiplier + 1;
                            allKey[8] = hashMapKey - QuadrandSystem.quadrantYMultiplier - 1;
                            for (int index = 0; index < allKey.Length; index++)
                            {
                                if (bulletComponent.friendlyFire)
                                {
                                    FindTargetInQuadrant(quadrantMap, allKey[index], translation.Value, damageRange, ref damagedEntityList);
                                }
                                else if (bulletComponent.campType == CampType.友军)
                                {
                                    FindTargetInQuadrant(damage > 0 ? enemyMap : allyMap, allKey[index], translation.Value, damageRange, ref damagedEntityList);
                                }
                                else if (bulletComponent.campType == CampType.敌军)
                                {
                                    FindTargetInQuadrant(damage > 0 ? allyMap : enemyMap, allKey[index], translation.Value, damageRange, ref damagedEntityList);
                                }
                            }
                        }
                        #region [处理 伤害列表、特效列表]
                        for (int index = 0; index < damagedEntityList.Length; index++)
                        {
                            Entity targetEntity = damagedEntityList[index];
                            Entity atkEntity = bulletComponent.fromEntity;
                            Attribute targetAttribute = AttributeFromEntity[targetEntity];
                            float health = targetAttribute.health - damage;
                            targetAttribute.health = health > 0 ? health : 0;
                            if (damage > 0)
                            {
                                if (health <= 0)
                                {
                                    //Conditions：1.攻击者必须为英雄 2.击杀对立阵营
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
                                            //KillRewardStruct krs = new KillRewardStruct();
                                            //for (int index = 0; index < KillRewardList.Length; index++)
                                            //{
                                            //    var cfg = KillRewardList[index];
                                            //    if (cfg.CharacterType == attribute.characterType)
                                            //    {
                                            //        krs = cfg;
                                            //        break;
                                            //    }
                                            //}
                                            //float addExp = krs.Exp;
                                            //atktor.exp += addExp;
                                            //Debug.Log("增加经验值: "+addExp);
                                            int effectID;
                                            if (isAttacktorAlly)
                                            {
                                                effectID = 8;
                                                killedEnemyList.AddNoResize(targetAttribute.characterType);
                                            }
                                            else
                                            {
                                                effectID = 7;
                                            }
                                            effectMultiHashMap.Add(effectID, targetPos);//死亡特效

                                            if (AttributeBaseFromEntity.Exists(atkEntity))
                                            {
                                                AttributeBase atktorAttributeBase = AttributeBaseFromEntity[atkEntity];
                                                commandBuffer.SetComponent<AttributeBase>(batchIndex, atkEntity, atktorAttributeBase);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                #region [针对礼物炮车炮弹的暂时Buff]
                                Buff buff = new Buff();
                                buff.buffID = 111;
                                buff.moveSpeed = 20f;
                                buff.moveSpeed_BuffInfo.needReset = true;
                                buff.health = +500;
                                buff.health_BuffInfo.needReset = false;
                                buff.targetEntity = targetEntity;
                                buff.totalTime = 10f;
                                Entity buffEntity = commandBuffer.CreateEntity(batchIndex, buffArchetype);
                                commandBuffer.SetComponent(batchIndex, buffEntity, buff);
                                #endregion
                            }
                            commandBuffer.SetComponent<Attribute>(batchIndex, targetEntity, targetAttribute);
                        }
                        if (bulletComponent.bulletType == BulletType.炮弹) explodePosList.Add(targetPos);
                        #endregion

                        moveComponent.curMoveFrame = curMoveFrame + 1;
                        moveComponentArray[i] = moveComponent;
                        commandBuffer.DestroyEntity(batchIndex, bulletEntity);
                    }
                }
            }
        }
        public void FindTargetInQuadrant(NativeMultiHashMap<int, QuadrantData> quadrantMap, int hashMapKey, float3 selfPos, float damageRange, ref NativeList<Entity> damagedEntityList)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (quadrantMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
            {
                do
                {
                    Entity entity = quadrantData.quadrantEntity.entity;
                    if (!TranslationFromEntity.HasComponent(entity))
                        continue;

                    Translation targetTranslation = TranslationFromEntity[entity];
                    if (math.distance(targetTranslation.Value, selfPos) < damageRange)
                    {
                        AddToEffectPosList(AttributeFromEntity[entity], targetTranslation.Value);
                        damagedEntityList.Add(entity);
                    }

                } while (quadrantMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
            }
        }
        public void AddToEffectPosList(Attribute attribute, float3 pos)
        {
            switch (attribute.characterType)
            {
                case CharacterType.步兵:
                    hitfleshPosList.Add(pos);
                    break;
                case CharacterType.枪手:
                    hitfleshPosList.Add(pos);
                    break;
                case CharacterType.战士:
                    hitfleshPosList.Add(pos);
                    break;
                case CharacterType.坦克:
                    hitSteelPosList.Add(pos);
                    break;
                default:
                    break;
            }
        }

    }
}