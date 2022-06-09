using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Physics;

[UpdateAfter(typeof(QuadrandSystem))]
public class PathFollowSystem : SystemBaseUnity
{
    private EntityQuery pathFollowQuery;
    private EntityQueryDesc pathFollowQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private static int batchesPerChunk = 1;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        pathFollowQueryDesc = new EntityQueryDesc
        {
            //所有需要赶路的entity
            All = new ComponentType[] {
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PathPosition>(),
                ComponentType.ReadOnly<PathFollow>(),
            },
            None = new ComponentType[]
            {
                ComponentType.ReadOnly<AttackComponent>(),
            }
        };
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!MapSystem.isInit || !PathOccupySystem.isInit) return;

        pathFollowQuery = this.GetEntityQuery(pathFollowQueryDesc);
        int count = pathFollowQuery.CalculateEntityCount();
        var job = new PathFollowJob();
        job.TranslationTypeAccessor = GetArchetypeChunkComponentType<Translation>(true);
        job.RotationTypeAccessor = GetArchetypeChunkComponentType<Rotation>(true);
        job.AttributeTypeAccessor = GetArchetypeChunkComponentType<Attribute>(true);
        job.QuadrantEntityTypeAccessor = GetArchetypeChunkComponentType<QuadrantEntity>(true);
        job.PathFollowTypeAccessor = GetArchetypeChunkComponentType<PathFollow>();
        job.pathPositionBufferFromEntity = GetBufferFromEntity<PathPosition>();
        job.HasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
        job.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);
        job.HasCommandFromEntity = GetComponentDataFromEntity<HasCommand>(true);
        job.ToAttackablePlaceFromEntity = GetComponentDataFromEntity<ToAttackablePlace>(true);
        job.PathfindingParamsFromEntity = GetComponentDataFromEntity<PathfindingParams>(true);
        job.occupyArray = PathOccupySystem.occupyArray;
        job.frameCount = UnityEngine.Time.frameCount;
        job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job.deltaTime = Time.DeltaTime;
        job.info = GridSet.Instance.GetChosenGridInfo();
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, pathFollowQuery, batchesPerChunk, this.Dependency);
        Dependency.Complete();
    }

    [BurstCompile]
    private struct PathFollowJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Rotation> RotationTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<QuadrantEntity> QuadrantEntityTypeAccessor;
        [ReadOnly]
        public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;
        [ReadOnly]
        public ArchetypeChunkComponentType<PathFollow> PathFollowTypeAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<HasTarget> HasTargetFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<HasCommand> HasCommandFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<ToAttackablePlace> ToAttackablePlaceFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<PathfindingParams> PathfindingParamsFromEntity;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public NativeArray<int> occupyArray;
        public float deltaTime;
        public int frameCount;
        public GridBasicInfo info;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray(TranslationTypeAccessor);
            NativeArray<Rotation> rotationArray = batchInChunk.GetNativeArray(RotationTypeAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray(AttributeTypeAccessor);
            NativeArray<QuadrantEntity> quadrantEntityArray = batchInChunk.GetNativeArray(QuadrantEntityTypeAccessor);
            NativeArray<PathFollow> pathFollowArray = batchInChunk.GetNativeArray(PathFollowTypeAccessor);

            for (int i = 0; i < translationArray.Length; i++)
            {
                Entity selfEntity = quadrantEntityArray[i].entity;
                float3 curFramePosition = translationArray[i].Value;
                EntityType entityType = quadrantEntityArray[i].typeEnum;
                #region 特殊情况停止行走   
                if (!pathPositionBufferFromEntity.Exists(selfEntity)) continue;
                //攻击目标距离达到攻击距离、且攻击没有收到阻隔的情况下
                if (HasTargetFromEntity.Exists(selfEntity) && !ToAttackablePlaceFromEntity.Exists(selfEntity))
                {
                    Entity targetEntity = HasTargetFromEntity[selfEntity].targetEntity;
                    if (TranslationFromEntity.Exists(targetEntity))
                    {
                        float3 targetPos = TranslationFromEntity[targetEntity].Value;
                        if (math.distance(targetPos, curFramePosition) <= attributeArray[i].atkRange)
                        {
                            continue;
                        }
                    }
                }
                #endregion

                DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[selfEntity];
                if (pathPositionBuffer.Length <= 0) continue;
                Attribute attribute = attributeArray[i];
                PathFollow pathFollow = pathFollowArray[i];
                float3 targetPosition;
                float3 moveDir;
                float moveSpeed = attribute.moveSpeed;
                if (pathFollow.pathIndex >= 0)
                {
                    int2 pathPosition;
                    if (pathFollow.pathIndex >= 1)
                    {
                        pathPosition = pathPositionBuffer[pathFollow.pathIndex - 1].position;
                    }
                    else
                    {
                        pathPosition = pathPositionBuffer[pathFollow.pathIndex].position;
                    }
                    targetPosition = GetXYToWorldPosition_Center(pathPosition.x, pathPosition.y);
                    moveDir = math.normalizesafe(targetPosition - curFramePosition);
                    if (!moveDir.Equals(float3.zero))
                    {
                        //设置朝向
                        Quaternion rotation = rotationArray[i].Value;
                        commandBuffer.SetComponent(batchIndex, selfEntity, new Rotation { Value = Quaternion.Lerp(rotation, Quaternion.LookRotation(moveDir), moveSpeed / 40f) });
                        //控制位移
                        float3 moveOffset = moveDir * moveSpeed * deltaTime;
                        float remianDistance = math.distance(curFramePosition, targetPosition);
                        float addedDistance = math.distance(curFramePosition, curFramePosition + moveOffset);
                        int curFrameX, curFrameY;
                        GetXYFloor(curFramePosition, out curFrameX, out curFrameY);
                        float3 nextFramePosition;
                        if (addedDistance < remianDistance)
                        {
                            nextFramePosition = curFramePosition + moveOffset;
                        }
                        else
                        {
                            nextFramePosition = targetPosition;
                            if (pathFollow.pathIndex == 1) commandBuffer.SetComponent(batchIndex, selfEntity, new PathFollow { pathIndex = -1 });
                            else commandBuffer.SetComponent(batchIndex, selfEntity, new PathFollow { pathIndex = pathFollow.pathIndex - 1 });
                        }

                        #region 判断下一帧是否移动到不同格子,处理单位碰撞
                        int nextFrameX, nextFrameY;
                        GetXYFloor(nextFramePosition, out nextFrameX, out nextFrameY);
                        if (curFrameX != nextFrameX || curFrameY != nextFrameY)
                        {
                            if (occupyArray[CalculateIndex(nextFrameX, nextFrameY)] != 0)
                            {
                                if (HasCommandFromEntity.Exists(selfEntity) && !HasTargetFromEntity.Exists(selfEntity))
                                {
                                    //有任务指令，且没有指定攻击目标时 不考虑碰撞（单纯的编队）
                                }
                                else if (!PathfindingParamsFromEntity.Exists(selfEntity))
                                {
                                    commandBuffer.SetComponent(batchIndex, selfEntity, new PathFollow { pathIndex = -1 });
                                    commandBuffer.AddComponent<PathfindingParams>(batchIndex, selfEntity,
                                        new PathfindingParams
                                        {
                                            startPosition = new int2(curFrameX, curFrameY),
                                            endPosition = pathPositionBuffer[0].position,
                                            hasCollision = true
                                        }
                                    );
                                    continue;
                                }
                            }
                        }
                        #endregion
                        commandBuffer.SetComponent(batchIndex, selfEntity, new Translation { Value = nextFramePosition });
                    }
                }
                else if (HasCommandFromEntity.Exists(selfEntity) && !PathfindingParamsFromEntity.Exists(selfEntity))
                {
                    commandBuffer.RemoveComponent<HasCommand>(batchIndex, selfEntity);
                }
            }
        }

        public void GetXYFloor(float3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
        public int CalculateIndex(int x, int y)
        {
            return x + y * info.width;
        }
        public float3 GetXYToWorldPosition_Center(int x, int y)
        {
            int cellSize = info.cellSize;
            float3 worldPosition = info.originPosition + new float3(x * cellSize, 0, y * cellSize) + new float3(cellSize / 2f, 0, cellSize / 2f);
            return worldPosition;
        }
    }
}
