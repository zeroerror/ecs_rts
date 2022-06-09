using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

[UpdateAfter(typeof(PathFollowSystem))]
public class AISystem : SystemBase
{
    private EntityQueryDesc entityQueryDesc;
    private EntityQueryDesc entityAutoQueryDesc;
    private EntityQuery entityQuery;
    private EntityCommandBufferSystem commandBufferSystem;


    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<SkillsTimer>(),
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<HasCommand>(),   ///PS:AI系统只在没有指令的情况下才生效
            }
        };
        base.OnCreate();
    }


    protected override void OnUpdate()
    {
        if (!MapSystem.isInit || !PathOccupySystem.isInit)
        {
            return;
        }
        entityQuery = this.GetEntityQuery(entityQueryDesc);
        var job = new ChaseTargetJob();
        job.TranslationAccessor = GetArchetypeChunkComponentType<Translation>(true);
        job.PathFollowAccessor = GetArchetypeChunkComponentType<PathFollow>(true);
        job.AttributeAccessor = GetArchetypeChunkComponentType<Attribute>(true);
        job.SkillsTimerAccessor = GetArchetypeChunkComponentType<SkillsTimer>(false);
        job.QuadrantEntityTypeAccessor = GetArchetypeChunkComponentType<QuadrantEntity>(true);
        job.TranslationDataFromEntity = GetComponentDataFromEntity<Translation>(true);
        job.HasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
        job.FinalGoalFromEntity = GetComponentDataFromEntity<FinalGoal>(true);
        job.pathPositionBufferFromEntity = GetBufferFromEntity<PathPosition>(true);
        job.mapNodeArray = MapSystem.mapNodeArray;
        job.occupyArray = PathOccupySystem.occupyArray;
        job.entityCommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job.info = GridSet.Instance.GetChosenGridInfo();
        job.deltaTime = Time.DeltaTime;
        job.frameCount = UnityEngine.Time.frameCount;

        Dependency = (JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, 1, this.Dependency));
        Dependency.Complete();
    }

    [BurstCompile]
    private struct ChaseTargetJob : IJobEntityBatch
    {
        public ArchetypeChunkComponentType<SkillsTimer> SkillsTimerAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<PathFollow> PathFollowAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<QuadrantEntity> QuadrantEntityTypeAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationDataFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<HasTarget> HasTargetFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<FinalGoal> FinalGoalFromEntity;
        [ReadOnly]
        public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;
        [ReadOnly]
        public NativeArray<MapNode> mapNodeArray;
        [ReadOnly]
        public NativeArray<int> occupyArray;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        public GridBasicInfo info;
        public float deltaTime;
        public int frameCount;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<PathFollow> pathFollowArray = batchInChunk.GetNativeArray<PathFollow>(PathFollowAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray<Attribute>(AttributeAccessor);
            NativeArray<SkillsTimer> skillsTimerArray = batchInChunk.GetNativeArray<SkillsTimer>(SkillsTimerAccessor);
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray<Translation>(TranslationAccessor);

            NativeArray<QuadrantEntity> quadrantEntityArray = batchInChunk.GetNativeArray<QuadrantEntity>(QuadrantEntityTypeAccessor);
            for (int i = 0; i < quadrantEntityArray.Length; i++)
            {
                bool isNeedFindPath = false;
                Entity selfEntity = quadrantEntityArray[i].entity;
                Attribute attribute = attributeArray[i];
                SkillsTimer skillsTimer = skillsTimerArray[i];
                Translation selfTranslation = translationArray[i];
                PathFollow pathFollow = pathFollowArray[i];
                float3 targetPos = float3.zero;
                if (HasTargetFromEntity.Exists(selfEntity))
                {
                    Entity targetEntity = HasTargetFromEntity[selfEntity].targetEntity;
                    if (TranslationDataFromEntity.Exists(targetEntity)) targetPos = TranslationDataFromEntity[targetEntity].Value;
                }
                else if (FinalGoalFromEntity.Exists(selfEntity))
                {
                    FinalGoal finalGoal = FinalGoalFromEntity[selfEntity];
                    float3 place1 = finalGoal.place_1;
                    float3 place2 = finalGoal.place_2;
                    float3 place3 = finalGoal.place_3;
                    float3 place4 = finalGoal.place_4;
                    if (!place1.Equals(float3.zero))
                    {
                        targetPos = place1;
                        if (math.distance(selfTranslation.Value, place1) < 5f)
                        {
                            //第一次到达目的地1
                            targetPos = place2;
                            finalGoal.place_1 = float3.zero;
                            isNeedFindPath = true;
                        }
                    }
                    else if (!place2.Equals(float3.zero))
                    {
                        targetPos = place2;
                        if (math.distance(selfTranslation.Value, place2) < 5f)
                        {
                            //第一次到达目的地2
                            targetPos = place3;
                            finalGoal.place_2 = float3.zero;
                            isNeedFindPath = true;
                        }
                    }
                    else if (!place3.Equals(float3.zero))
                    {
                        targetPos = place3;
                        if (math.distance(selfTranslation.Value, place3) < 5f)
                        {
                            //第一次到达目的地3
                            targetPos = place4;
                            finalGoal.place_3 = float3.zero;
                            isNeedFindPath = true;
                        }
                    }
                    else if (!place4.Equals(float3.zero))
                    {
                        targetPos = place4;
                        if (math.distance(selfTranslation.Value, place4) < 5f)
                        {
                            //第一次到达目的地4
                            finalGoal.place_4 = float3.zero;
                        }
                    }
                    entityCommandBuffer.SetComponent<FinalGoal>(batchIndex, selfEntity, finalGoal);
                }

                #region [自动寻路控制]
                if (!targetPos.Equals(float3.zero))
                {
                    float3 worldPosition;
                    worldPosition = selfTranslation.Value;
                    int2 startPosition;
                    int2 endPosition;
                    GetXYFloor(worldPosition, out startPosition.x, out startPosition.y, info);
                    GetXYFloor(targetPos, out endPosition.x, out endPosition.y, info);
                    if (pathPositionBufferFromEntity.Exists(selfEntity))
                    {
                        DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[selfEntity];
                        bool isMoving = pathFollow.pathIndex > 0;
                        bool isSameEnd = pathPositionBuffer.Length > 0 && pathPositionBuffer[0].position.Equals(endPosition);
                        bool isHasTarget = HasTargetFromEntity.Exists(selfEntity);
                        if (!isSameEnd) isNeedFindPath = true;
                        if (!isMoving) isNeedFindPath = true;
                    }

                    if (isNeedFindPath)
                    {
                        if (skillsTimer.pathFindingTimer >= 0.5f)
                        {
                            //添加寻路组件
                            entityCommandBuffer.AddComponent<PathfindingParams>(batchIndex, selfEntity, new PathfindingParams { startPosition = startPosition, endPosition = endPosition });
                        }
                    }
                }
                #endregion


            }
        }
        #region 自定义方法
        public static void GetXYFloor(Vector3 worldPosition, out int x, out int y, GridBasicInfo info)
        {
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
        public static bool IsEqual(int2 pos1, int2 pos2)
        {
            return pos1.x == pos2.x && pos1.y == pos2.y;
        }
        public bool IsTargetSurrounded(int2 targetPos)
        {
            int2 p1 = targetPos + new int2(-1, 0);
            int2 p2 = targetPos + new int2(+1, 0);
            int2 p3 = targetPos + new int2(0, +1);
            int2 p4 = targetPos + new int2(0, -1);
            return !IsWalkable(p1) && !IsWalkable(p2) && !IsWalkable(p3) && !IsWalkable(p4);
        }
        private bool IsWalkable(int2 pos)
        {
            int index = CalculateIndex(pos.x, pos.y);
            return IsInGrid(pos) && mapNodeArray[index].isWalkable && occupyArray[index] == 0;
        }
        private int CalculateIndex(int x, int y)
        {
            return x + y * info.width;
        }
        private bool IsInGrid(int2 position)
        {
            return position.x >= 0 && position.x < info.width && position.y >= 0 && position.y < info.height;
        }
        #endregion
    }
}