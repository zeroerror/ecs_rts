using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ZeroHero;

[UpdateAfter(typeof(BuffMapSystem))]
public class LifeSystem : SystemBaseUnity
{
    private EntityQuery lifeQuery;
    private EntityQueryDesc lifeQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        lifeQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<Translation>(),
            }
        };

    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        lifeQuery = GetEntityQuery(lifeQueryDesc);
        var job = new LifeControlJob();
        job.EntityAccessor = GetArchetypeChunkEntityType();
        job.AttributeAccessor = GetArchetypeChunkComponentType<Attribute>(true);
        job.AttributeBaseAccessor = GetArchetypeChunkComponentType<AttributeBase>(true);
        job.TranslationAccessor = GetArchetypeChunkComponentType<Translation>(true);
        job.LocalToWorldAccessor = GetArchetypeChunkComponentType<LocalToWorld>(true);
        job.StaticObstacleFromEntity = GetComponentDataFromEntity<StaticObstacle>(true);
        job.AllyComponentFromEntity = GetComponentDataFromEntity<AllyComponent>(true);
        job.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
        job.AllySelectedFromEntity = GetComponentDataFromEntity<AllySelected>(true);
        job.mapNodeArray = MapSystem.mapNodeArray;
        job.info = GridSet.Instance.GetChosenGridInfo();
        job.memberDieEvent = EntityEventSystem.Instance.memberDieEvent;
        job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, lifeQuery, 1, Dependency);
        Dependency.Complete();
    }

    [BurstCompile]
    private struct LifeControlJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<AttributeBase> AttributeBaseAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<StaticObstacle> StaticObstacleFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllyComponent> AllyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllySelected> AllySelectedFromEntity;
        [ReadOnly]
        public GridBasicInfo info;
        public NativeArray<MapNode> mapNodeArray;
        public EntityArchetype memberDieEvent;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Entity> entityArray = batchInChunk.GetNativeArray(EntityAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray(AttributeAccessor);
            NativeArray<AttributeBase> attributeBaseArray = batchInChunk.GetNativeArray(AttributeBaseAccessor);
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray(TranslationAccessor);
            NativeArray<LocalToWorld> localToWorldArray = batchInChunk.GetNativeArray(LocalToWorldAccessor);
            for (int i = 0; i < entityArray.Length; i++)
            {
                Entity selfEntity = entityArray[i];
                Attribute attribute = attributeArray[i];
                AttributeBase attributeBase = attributeBaseArray[i];
                float3 selfPos = translationArray[i].Value;
                selfPos = localToWorldArray[i].Position;
                if (attribute.health <= 0)
                {
                    if (StaticObstacleFromEntity.Exists(selfEntity))
                    {
                        Common.GetXYFloor(info, selfPos, out int x, out int y);
                        int index = Common.CalculateIndex(info, x, y);
                        MapNode mapNode = mapNodeArray[index];
                        mapNode.isWalkable = true;
                        mapNodeArray[index] = mapNode;
                    }
                    if (AllyComponentFromEntity.Exists(selfEntity))
                    {
                        //友军死亡，通知刷新队伍信息
                        Entity entity = commandBuffer.CreateEntity(batchIndex, memberDieEvent);
                        MemberDieEvent mde = new MemberDieEvent();
                        mde.entity = selfEntity;
                        commandBuffer.SetComponent<MemberDieEvent>(batchIndex, entity, mde);
                    }
                    commandBuffer.DestroyEntity(batchIndex, selfEntity);
                }else if(attribute.health > attributeBase.health)
                {
                    //防止血量溢出最大血量
                    attribute.health = attributeBase.health;
                    commandBuffer.SetComponent(batchIndex, selfEntity, attribute);
                }
            }
        }
    }
}
