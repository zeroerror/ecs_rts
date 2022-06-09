using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Entities;
using ZeroHero;
using Unity.Transforms;

[UpdateAfter(typeof(PathOccupySystem))]
public class PathfindingSystem : SystemBaseUnity
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    private EntityCommandBufferSystem commandBufferSystem;
    private static GridBasicInfo info;  //网格地图基本信息

    private EntityQuery entityQuery;
    private EntityQueryDesc enemyQueryDesc;
    private EntityQueryDesc enemyAutoyQueryDesc;
    private EntityQueryDesc allyQueryDesc;
    private EntityQueryDesc allyAutoQueryDesc;
    private EntityQueryDesc allyAutoSelectedQueryDesc;
    private EntityQueryDesc allySelectedQueryDesc;
    private EntityQueryDesc commandedQueryDesc;
    private EntityQueryDesc commandedWithTargetQueryDesc;
    private int batchesPerChunk;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        //ps:因为batchesPerChunk设置通常大于1，所以需要保证每一个query内的entity都会在同一个batchChunk内否则会报错
        #region 所有寻路EntityQuery分类

        #region 自动寻路的enenmy
        //1.有目标的寻路
        enemyQueryDesc = new EntityQueryDesc
        {

            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<HasTarget>(),
                ComponentType.ReadOnly<EnemyComponent>() },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>()
            }
        };
        //2.无目标 固定路线 寻路
        enemyAutoyQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<EnemyComponent>(),
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasTarget>(),
            }
        };
        #endregion

        #region 自动寻路的ally  
        //1.未被选中状态
        allyQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<HasTarget>(),
                ComponentType.ReadOnly<AllyComponent>(),
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasCommand>(),
                ComponentType.ReadOnly<AllySelected>(),
            }
        };
        //2.被选中状态
        allySelectedQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<HasTarget>(),
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<AllySelected>(),
            },

            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasCommand>()
            }
        };
        //3.未被选中状态下的 无目标 固定路线 寻路
        allyAutoQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<AllyComponent>(),
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasCommand>(),
                ComponentType.ReadOnly<AllySelected>(),
                ComponentType.ReadOnly<HasTarget>(),
            }
        };
        //4.被选中状态下的 无目标 固定路线 寻路
        allyAutoSelectedQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<AllySelected>(),
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasCommand>(),
                ComponentType.ReadOnly<HasTarget>(),
            }
        };
        #endregion

        #region 收到指令的寻路
        commandedQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<HasCommand>()
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
            }
        };
        commandedWithTargetQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PathfindingParams>(),
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<CanFindPath>(),
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<HasCommand>()
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<AttackComponent>(),
                ComponentType.ReadOnly<ToAttackablePlace>(),
                ComponentType.ReadOnly<HasTarget>()
            }
        };
        #endregion

        #endregion
    }
    protected override void OnInit()
    {
        base.OnInit();
        info = GridSet.Instance.GetChosenGridInfo();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        #region enemy自动寻路
        CreateJob(enemyQueryDesc);
        CreateJob(enemyAutoyQueryDesc);
        #endregion

        #region ally自动寻路
        CreateJob(allyQueryDesc);
        CreateJob(allySelectedQueryDesc);
        CreateJob(allyAutoQueryDesc);
        CreateJob(allyAutoSelectedQueryDesc);
        #endregion

        #region 收到指令的寻路
        NativeArray<int> emptyOccupyArray = new NativeArray<int>(PathOccupySystem.occupyArray.Length, Allocator.TempJob);
        CreateJob(commandedQueryDesc);
        CreateJob(commandedWithTargetQueryDesc);
        emptyOccupyArray.Dispose();
        #endregion

    }

    private void CreateJob(EntityQueryDesc entityQueryDesc)
    {
        entityQuery = GetEntityQuery(entityQueryDesc);
        batchesPerChunk = entityQuery.CalculateEntityCount();
        if (batchesPerChunk >= 0)
        {
            var organizeJob = new FindPathJobEntityBatch();
            organizeJob.TranslationAccessor = this.GetArchetypeChunkComponentType<Translation>(true);
            organizeJob.PathFindingParamsAccessor = this.GetArchetypeChunkComponentType<PathfindingParams>(true);
            organizeJob.AttributeAccessor = this.GetArchetypeChunkComponentType<Attribute>(true);
            organizeJob.QuadrantEntityAccessor = this.GetArchetypeChunkComponentType<QuadrantEntity>(false);
            organizeJob.info = info;
            organizeJob.mapNodeArray = MapSystem.mapNodeArray;
            organizeJob.occupyArray = PathOccupySystem.occupyArray;
            organizeJob.pathPositionBufferFromEntity = GetBufferFromEntity<PathPosition>();
            organizeJob.HasCommandFromEntity = GetComponentDataFromEntity<HasCommand>(true);
            organizeJob.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);
            organizeJob.HasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
            organizeJob.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            batchesPerChunk = batchesPerChunk > 10 ? 10 : batchesPerChunk;
            this.Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(organizeJob, entityQuery, batchesPerChunk, this.Dependency);
            this.Dependency.Complete();
        }
    }
    [BurstCompile]
    public struct FindPathJobEntityBatch : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkComponentType<PathfindingParams> PathFindingParamsAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<QuadrantEntity> QuadrantEntityAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<HasCommand> HasCommandFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<HasTarget> HasTargetFromEntity;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;
        [ReadOnly]
        public NativeArray<MapNode> mapNodeArray;
        [ReadOnly]
        public NativeArray<int> occupyArray;
        public GridBasicInfo info;

        public EntityCommandBuffer.Concurrent commandBuffer;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<PathfindingParams> pathfindingParamsArray = batchInChunk.GetNativeArray<PathfindingParams>(PathFindingParamsAccessor);
            NativeArray<Translation> TranslationArray = batchInChunk.GetNativeArray<Translation>(TranslationAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray<Attribute>(AttributeAccessor);
            NativeArray<QuadrantEntity> quadrantEntityArray = batchInChunk.GetNativeArray<QuadrantEntity>(QuadrantEntityAccessor);
            for (int index = 0; index < pathfindingParamsArray.Length; index++)
            {
                #region 初始化参数  
                PathfindingParams pathfindingParams = pathfindingParamsArray[index];
                QuadrantEntity quadrantEntity = quadrantEntityArray[index];
                Attribute attribute = attributeArray[index];
                Entity entity = quadrantEntity.entity;
                float3 selfPos = TranslationArray[index].Value;
                if (entity == Entity.Null) continue;

                commandBuffer.RemoveComponent<PathfindingParams>(batchIndex, entity);
                commandBuffer.RemoveComponent<CanFindPath>(batchIndex, entity);
                if (!pathPositionBufferFromEntity.Exists(entity)) continue;
                DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[entity];
                pathPositionBuffer.Clear();
                int2 startPosition = pathfindingParams.startPosition;
                int2 sp;
                GetXYFloor(selfPos, out sp.x, out sp.y);
                if (startPosition.x != sp.x || startPosition.y != sp.y)
                {
                    startPosition = sp;
                }

                int2 endPosition = pathfindingParams.endPosition;
                if (HasTargetFromEntity.Exists(entity))
                {
                    Entity target = HasTargetFromEntity[entity].targetEntity;
                    if (TranslationFromEntity.Exists(target))
                    {
                        int2 ep;
                        GetXYFloor(TranslationFromEntity[target].Value, out ep.x, out ep.y);
                        if (ep.x != endPosition.x || ep.y != endPosition.y)
                        {
                            endPosition = ep;
                        }
                    }

                }
                int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y);
                int startNodeIndex = CalculateIndex(startPosition.x, startPosition.y);
                #endregion
                #region 特殊情况判定
                if (startPosition.Equals(endPosition))
                {
                    //Debug.Log("起点和终点相同");
                    pathPositionBuffer.Add(new PathPosition { position = new int2(endPosition.x, endPosition.y) });
                    commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = -11 });
                    continue;
                }

                if (!IsWalkable(endPosition))
                {
                    //Debug.Log("终点不可走");
                    commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = -111 });
                    continue;
                }
                //float range = attribute.atkRange;
                //float distance = math.distance(startPosition, endPosition);
                //range = math.min(distance, range);
                if (IsSurrounded(startPosition, 3, pathfindingParams.hasCollision))
                {
                    continue;
                }
                if (IsSurrounded(endPosition, 3, pathfindingParams.hasCollision))
                {
                    continue;
                }

                if (IsCanWalk(startPosition, endPosition, pathfindingParams.hasCollision))
                {
                    pathPositionBuffer.Add(new PathPosition { position = new int2(endPosition.x, endPosition.y) });
                    pathPositionBuffer.Add(new PathPosition { position = new int2(startPosition.x, startPosition.y) });
                    commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = 1 });
                    continue;
                }

                #endregion

                //Debug.Log("a*");
                #region A*寻路
                NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(info.width * info.height, Allocator.Temp);
                for (int i = 0; i < pathNodeArray.Length; i++)
                {
                    PathNode pathNode = pathNodeArray[i];
                    MapNode mapNode = mapNodeArray[i];
                    pathNode.hCost = CalculateDistanceCost(new int2(mapNode.x, mapNode.y), endPosition);
                    pathNode.gCost = int.MaxValue;
                    pathNode.cameFromNodeIndex = -1;
                    pathNodeArray[i] = pathNode;
                }
                NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
                neighbourOffsetArray[0] = new int2(-1, 0); //left
                neighbourOffsetArray[1] = new int2(+1, 0); //right
                neighbourOffsetArray[2] = new int2(0, +1); //up
                neighbourOffsetArray[3] = new int2(0, -1); //down
                #region  开始对pathNodeArray操作
                //定义起点
                PathNode startNode = pathNodeArray[startNodeIndex];
                MapNode startMapNode = mapNodeArray[startNodeIndex];
                startNode.gCost = 0;
                startNode.CalculateFCost();
                pathNodeArray[startMapNode.index] = startNode;

                NativeList<int> openList = new NativeList<int>(Allocator.Temp);
                NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

                openList.Add(startMapNode.index);
                while (openList.Length > 0)
                {
                    int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                    PathNode currentNode = pathNodeArray[currentNodeIndex];
                    MapNode currentMapNode = mapNodeArray[currentNodeIndex];

                    if (currentNodeIndex == endNodeIndex)
                    {
                        //到达地点
                        break;
                    }

                    //remove current node from OpenList
                    for (int i = 0; i < openList.Length; i++)
                    {
                        if (openList[i] == currentNodeIndex)
                        {
                            openList.RemoveAtSwapBack(i);
                            break;
                        }
                    }

                    closedList.Add(currentNodeIndex);
                
                    int2 currentNodePosition = new int2(currentMapNode.x, currentMapNode.y);
                    for (int i = 0; i < neighbourOffsetArray.Length; i++)
                    {
                        int2 neighbourOffset = neighbourOffsetArray[i];
                        int2 neighbourPosition = new int2(currentNodePosition.x + neighbourOffset.x, currentNodePosition.y + neighbourOffset.y);

                        int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y);

                        if (!IsInGrid(neighbourPosition))//不在格子内
                            continue;
                        if (!HasCommandFromEntity.Exists(entity) && !IsInSearchingArea(endPosition, neighbourPosition, Mathf.RoundToInt(attribute.searchRange * 1f)) && HasTargetFromEntity.Exists(entity)) //角色超出寻路范围
                            continue;
                        if (pathfindingParams.hasCollision && occupyArray[neighbourNodeIndex] != 0 && !neighbourPosition.Equals(endPosition))//格子已被占用
                        {
                            //Debug.Log("格子已被占用");
                            continue;
                        }
                        if (closedList.Contains(neighbourNodeIndex)) //Already searched this node
                            continue;

                        PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                        MapNode neighbourMapNode = mapNodeArray[neighbourNodeIndex];
                        if (!neighbourMapNode.isWalkable)
                        {
                            continue;
                        }

                        int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                        if (tentativeGCost < neighbourNode.gCost)
                        {
                            //更新节点
                            neighbourNode.cameFromNodeIndex = currentNodeIndex;
                            neighbourNode.gCost = tentativeGCost;
                            neighbourNode.CalculateFCost();
                            pathNodeArray[neighbourNodeIndex] = neighbourNode;

                            if (!openList.Contains(neighbourNodeIndex))
                            {
                                openList.Add(neighbourNodeIndex);
                            }
                        }

                    }
                }

                openList.Dispose();
                closedList.Dispose();
                neighbourOffsetArray.Dispose();
                #endregion
                #region 开始对存储路径的DynamicBuffer进行修改

                if (pathfindingParams.endPosition.x == pathfindingParams.startPosition.x && pathfindingParams.endPosition.y == pathfindingParams.startPosition.y)
                {
                    //Debug.Log("-留在原地");
                    pathPositionBuffer.Add(new PathPosition { position = new int2(pathfindingParams.endPosition.x, pathfindingParams.endPosition.y) });
                    commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = pathPositionBuffer.Length - 1 });
                }
                PathNode endNode = pathNodeArray[endNodeIndex];
                if (endNode.cameFromNodeIndex == -1)
                {
                    //Debug.Log("-找不到路径");
                    //commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = -1 });
                    //reachableArray[endNodeIndex] = 1;
                }
                else
                {
                    //Debug.Log("-找到路径");
                    MapNode endMapNode = mapNodeArray[endNodeIndex];
                    CalculatePath(pathNodeArray, endNode, endMapNode, pathPositionBuffer, pathfindingParams.hasCollision);
                    commandBuffer.SetComponent<PathFollow>(batchIndex, entity, new PathFollow { pathIndex = pathPositionBuffer.Length - 1 });
                }
                #endregion
                #endregion
            }
        }
        #region 自定义方法
        public void GetXYFloor(float3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
        public bool IsSurrounded(int2 pos, float range, bool hasCollision = false)
        {
            NativeList<int2> searchedList = new NativeList<int2>(Allocator.Temp);
            searchedList.Add(pos);
            for (int i = 0; i < searchedList.Length; i++)
            {
                int2 curPos = searchedList[i];
                int2 p1 = curPos + new int2(-1, 0);
                int2 p2 = curPos + new int2(+1, 0);
                int2 p3 = curPos + new int2(0, +1);
                int2 p4 = curPos + new int2(0, -1);
                bool b1 = IsCanReach(p1, hasCollision);
                bool b2 = IsCanReach(p2, hasCollision);
                bool b3 = IsCanReach(p3, hasCollision);
                bool b4 = IsCanReach(p4, hasCollision);
                if (math.distance(pos, p1) < range
                    && math.distance(pos, p2) < range
                     && math.distance(pos, p3) < range
                      && math.distance(pos, p4) < range)
                {
                    if (b1 && !searchedList.Contains(p1)) searchedList.Add(p1);
                    if (b2 && !searchedList.Contains(p2)) searchedList.Add(p2);
                    if (b3 && !searchedList.Contains(p3)) searchedList.Add(p3);
                    if (b4 && !searchedList.Contains(p4)) searchedList.Add(p4);
                }
                else
                {
                    searchedList.Dispose();
                    return false;
                }
            }
            searchedList.Dispose();
            return true;
        }

        //判断网格点与网格点之间是否可直走
        public bool IsCanWalk(int2 startPosition, int2 endPosition, bool hasCollision = false)
        {
            //Debug.Log(frame + "  IsCanWalk： startPosition  " + startPosition + "  endPosition = " + endPosition);

            if (startPosition.x == endPosition.x || startPosition.y == endPosition.y)
            {
                return IsPointToPointCanWalk(startPosition, endPosition, true, hasCollision);
            }

            bool flag;
            bool flag1;
            bool flag2;
            float K = (float)(startPosition.y - endPosition.y) / (startPosition.x - endPosition.x);
            //Debug.Log(frame + "  IsCanWalk： K  " + (K > 0 ? "  大于0 = " : "小于0") + "  K = " + K);

            flag = IsPointToPointCanWalk(startPosition, endPosition, K > 0, hasCollision);
            if (K > 0)
            {
                //startPosition += new int2(1, 1);
                int2 startPosition1 = new int2(startPosition.x, startPosition.y + 1);
                int2 endPosition1 = new int2(endPosition.x, endPosition.y + 1);
                flag1 = IsPointToPointCanWalk(startPosition1, endPosition1, false, hasCollision);
                int2 startPosition2 = new int2(startPosition.x + 1, startPosition.y);
                int2 endPosition2 = new int2(endPosition.x + 1, endPosition.y);
                flag2 = IsPointToPointCanWalk(startPosition2, endPosition2, false, hasCollision);
                return flag && flag1 && flag2;
            }
            else
            {
                int2 startPosition1 = new int2(startPosition.x + 1, startPosition.y + 1);
                int2 endPosition1 = new int2(endPosition.x + 1, endPosition.y + 1);
                flag1 = IsPointToPointCanWalk(startPosition1, endPosition1, false, hasCollision);
                int2 startPosition2 = new int2(startPosition.x + 1, startPosition.y);
                int2 endPosition2 = new int2(endPosition.x + 1, endPosition.y);
                flag2 = IsPointToPointCanWalk(startPosition2, endPosition2, true, hasCollision);
                return flag && flag1 && flag2;
            }

        }
        public bool IsPointToPointCanWalk(int2 startPosition, int2 endPosition, bool isMiddle, bool hasCollision = false)
        {
            if (isMiddle && !IsWalkable(endPosition))
            {
                return false;
            }
            if (startPosition.Equals(endPosition)) return true;

            if (startPosition.x > endPosition.x)
            {
                int2 tempPos = startPosition;
                startPosition = endPosition;
                endPosition = tempPos;
            }
            if (startPosition.x == endPosition.x && startPosition.y > endPosition.y)
            {
                int2 tempPos = startPosition;
                startPosition = endPosition;
                endPosition = tempPos;
            }
            int x1, y1;
            int x2, y2;
            x1 = startPosition.x;
            y1 = startPosition.y;
            x2 = endPosition.x;
            y2 = endPosition.y;
            int A;
            int B;
            int2 currentPos = startPosition;
            if (x1 == x2 || y1 == y2)
            {
                if (x1 == x2)
                {
                    currentPos += new int2(0, 1);
                    while (!currentPos.Equals(endPosition))
                    {
                        if (!IsCanReach(currentPos, hasCollision))
                        {
                            //Debug.Log(frame + "    " + currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                            return false;
                        }
                        currentPos += new int2(0, 1);
                    }
                }
                else if (y1 == y2)
                {
                    currentPos += new int2(1, 0);
                    while (!currentPos.Equals(endPosition))
                    {
                        if (!IsCanReach(currentPos, hasCollision))
                        {
                            //Debug.Log(frame + "    " + currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                            return false;
                        }
                        currentPos += new int2(1, 0);
                    }
                }
            }
            else
            {
                A = y1 - y2;
                B = x2 - x1;
                int d = 0;
                int d1 = 0;
                int d2 = 0;
                int2 posAdd1 = new int2();
                int2 posAdd2 = new int2();
                float K;
                K = (float)-A / B;

                currentPos += new int2(0, K < 0 ? -1 : 0);//斜率为负时需要向下移动一格
                endPosition += new int2(0, K < 0 ? -1 : 0);
                if (isMiddle) endPosition += new int2(-1, K > 0 ? -1 : +1);   //如果是中间线检测，需要退一格
                if (!IsCanReach(currentPos, hasCollision) && !currentPos.Equals(startPosition))
                {
                    //Debug.Log(currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                    return false;
                }

                if (Mathf.Abs(K) <= 1)
                {
                    posAdd1 = new int2(1, 0);
                    posAdd2 = new int2(1, K > 0 ? 1 : -1);

                    if (K > 0)
                    {
                        d = A + B;
                        d1 = A;
                        d2 = A + B;
                    }
                    else
                    {
                        d = A - B;
                        d1 = A;
                        d2 = A - B;
                    }
                }
                else
                {
                    //斜率大于1
                    posAdd1 = new int2(0, K > 0 ? 1 : -1);
                    posAdd2 = new int2(1, K > 0 ? 1 : -1);

                    if (K > 0)
                    {
                        d = A + B;
                        d1 = B;
                        d2 = A + B;
                    }
                    else
                    {
                        d = A - B;
                        d1 = -B;
                        d2 = A - B;
                    }
                }

                #region 开始遍历路经过的点
                while (!currentPos.Equals(endPosition))
                {
                    bool isXYBothChange;
                    if (Mathf.Abs(K) <= 1)
                    {
                        isXYBothChange = (K > 0 && d <= 0) || (K < 0 && d >= 0);
                    }
                    else
                    {
                        //斜率大于1
                        isXYBothChange = (K > 0 && d >= 0) || (K < 0 && d <= 0);
                    }

                    if (isXYBothChange)
                    {
                        currentPos += posAdd2;
                        d += d2;
                    }
                    else
                    {
                        currentPos += posAdd1;
                        d += d1;
                    }

                    if (currentPos.Equals(endPosition))
                    {
                        return true;
                    }

                    if (!IsCanReach(currentPos, hasCollision))
                    {
                        //Debug.Log("不在地图网格中");
                        return false;
                    }
                }
                if (currentPos.y != endPosition.y)
                {
                    //Debug.Log("currentPos.y != endPosition.y");
                    return false;
                }
                #endregion
            }
            return true;
        }
        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            int xDistance = math.abs(aPosition.x - bPosition.x);
            int yDistance = math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }
        private int CalculateIndex(int x, int y)
        {
            return x + y * info.width;
        }

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
        {
            PathNode lowestCostPathNode = pathNodeArray[openList[0]];
            MapNode lowestCostMapNode = mapNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; i++)
            {
                PathNode testPathNode = pathNodeArray[openList[i]];
                MapNode testMapNode = mapNodeArray[openList[i]];
                if (testPathNode.fCost < lowestCostPathNode.fCost)
                {
                    lowestCostPathNode = testPathNode;
                    lowestCostMapNode = testMapNode;
                }
            }
            return lowestCostMapNode.index;
        }
        private bool IsCanReach(int2 pos, bool hasCollision = false)
        {
            int index = CalculateIndex(pos.x, pos.y);
            bool isOccupy;
            if (!hasCollision) isOccupy = false;
            else isOccupy = occupyArray[index] != 0;
            return IsInGrid(pos) && mapNodeArray[index].isWalkable && !isOccupy;
        }
        private bool IsWalkable(int2 pos)
        {
            int index = CalculateIndex(pos.x, pos.y);
            return IsInGrid(pos) && mapNodeArray[index].isWalkable;
        }
        private bool IsInGrid(int2 position)
        {
            return position.x >= 0 && position.x < info.width && position.y >= 0 && position.y < info.height;
        }
        private bool IsInSearchingArea(int2 endPosition, int2 curPosition, int range)
        {
            //当前遍历的点的位置需要在半径为range的圆内
            return math.distance(endPosition, curPosition) < range && IsInGrid(curPosition);
        }
        private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, MapNode endMapNode, DynamicBuffer<PathPosition> pathPositionBuffer, bool hasCollision = false)
        {
            if (endNode.cameFromNodeIndex == -1)
            {
                //Didnt find a path
            }
            else
            {
                //Found a path
                pathPositionBuffer.Add(new PathPosition { position = new int2(endMapNode.x, endMapNode.y) });

                PathNode currentNode = endNode;

                while (currentNode.cameFromNodeIndex != -1)
                {
                    PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    MapNode cameFromMapNode = mapNodeArray[currentNode.cameFromNodeIndex];
                    pathPositionBuffer.Add(new PathPosition { position = new int2(cameFromMapNode.x, cameFromMapNode.y) });
                    currentNode = cameFromNode;
                }
                #region 优化路径点
                float2 firstPosition = pathPositionBuffer[0].position;
                float2 secPosition = pathPositionBuffer[1].position;
                float2 lastLineDir = math.normalizesafe(secPosition - firstPosition);
                float2 lastPoint = firstPosition;
                for (int i = 2; i < pathPositionBuffer.Length; i++)
                {
                    float2 curPathPosition = pathPositionBuffer[i].position;
                    float2 lastPathPosition = pathPositionBuffer[i - 1].position;
                    float2 curLineDir = math.normalizesafe(curPathPosition - lastPathPosition);

                    if (curLineDir.Equals(lastLineDir))
                    {
                        pathPositionBuffer.RemoveAt(i - 1);
                        i--;
                    }
                    else if (IsCanWalk((int2)curPathPosition, (int2)lastPoint, hasCollision))
                    {

                        pathPositionBuffer.RemoveAt(i - 1);
                        i--;
                        lastLineDir = math.normalizesafe(curPathPosition - lastPoint);
                    }
                    else
                    {
                        lastLineDir = curLineDir;
                        lastPoint = lastPathPosition;
                    }
                }
                #endregion



            }
        }
        #endregion\
    }
}
public struct PathNode
{
    public int gCost;
    public int hCost;
    public int fCost;
    public int cameFromNodeIndex;
    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }
}