using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using Unity.Rendering;
[UpdateAfter(typeof(MapSystem))]
public class ObstacleSystem : SystemBaseUnity
{
    private EntityQuery staticObstacleQuery;
    private EntityQueryDesc staticObstacleQueryDesc;
    public static bool isStaticObstacleSet = false;
    private EntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        staticObstacleQueryDesc = new EntityQueryDesc
        {
            //所有静态障碍物
            All = new ComponentType[] {
                ComponentType.ReadOnly<StaticObstacle>(),
                ComponentType.ReadOnly<Translation>()
            },
        };
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        isStaticObstacleSet = false;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isStaticObstacleSet)
        {
            staticObstacleQuery = GetEntityQuery(staticObstacleQueryDesc);
            var job = new SetStaticObstacleJob();
            job.info = GridSet.Instance.GetChosenGridInfo();
            job.mapNodeArray = MapSystem.mapNodeArray;
            job.TranslationTypeAccessor = this.GetArchetypeChunkComponentType<Translation>();
            job.StaticObstacleTypeAccessor = this.GetArchetypeChunkComponentType<StaticObstacle>();
            job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            this.Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, staticObstacleQuery, 1, this.Dependency);
            this.Dependency.Complete();
            isStaticObstacleSet = true;
            Debug.Log("障碍物Entity生成数量:"+ staticObstacleQuery.CalculateEntityCount());
        }
    }

    [BurstCompile]
    private struct SetStaticObstacleJob : IJobEntityBatch
    {
        public GridBasicInfo info;
        public NativeArray<MapNode> mapNodeArray;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationTypeAccessor;

        public ArchetypeChunkComponentType<StaticObstacle> StaticObstacleTypeAccessor;

        public EntityCommandBuffer.Concurrent commandBuffer;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray<Translation>(TranslationTypeAccessor);
            NativeArray<StaticObstacle> staticObstacleArray = batchInChunk.GetNativeArray<StaticObstacle>(StaticObstacleTypeAccessor);
            for (int i = 0; i < translationArray.Length; i++)
            {
                float3 position = translationArray[i].Value;
                Entity entity = staticObstacleArray[i].entity;
                Common.GetXYFloor(info, position, out int x, out int y);
                int index = x + y * info.width;
                MapNode mapNode = mapNodeArray[index];
                mapNode.isWalkable = false;
                mapNodeArray[index] = mapNode;
                float3 centerPos = Common.GetXYToWorldPosition_Center(info, x, y);
                centerPos.y = position.y;
                commandBuffer.SetComponent<Translation>(batchIndex, entity, new Translation { Value = centerPos });
            }
        }

    }

}
