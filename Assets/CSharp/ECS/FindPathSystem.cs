using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FindPathSystem : SystemBaseUnity
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    private static EntityManager entityManager;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        entityManager = World.EntityManager;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

    }
    public static Entity GetPathPositionBufferEntity(int2 startPosition, int2 endPosition)
    {
        Entity entity = entityManager.CreateEntity(typeof(PathPosition));
        DynamicBuffer<PathPosition> pathPositionBuffer = entityManager.GetBuffer<PathPosition>(entity);
        var job = new FindPathJob();
        job.mapNodeArray = MapSystem.mapNodeArray;
        job.occupyArray = new NativeArray<int>(PathOccupySystem.occupyArray.Length, Allocator.TempJob);
        job.startPosition = startPosition;
        job.endPosition = endPosition;
        job.pathPositionBuffer = pathPositionBuffer;
        job.info = GridSet.Instance.GetChosenGridInfo();
        JobHandle jh = job.Schedule();
        jh.Complete();
        return entity;
    }

    [BurstCompile]
    public struct FindPathJob : IJob
    {
        [ReadOnly]
        public NativeArray<MapNode> mapNodeArray;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> occupyArray;
        public int2 startPosition;
        public int2 endPosition;
        public GridBasicInfo info;
        public DynamicBuffer<PathPosition> pathPositionBuffer;
        public void Execute()
        {
            #region 特殊情况判定
            if (startPosition.Equals(endPosition))
            {
                //Debug.Log("起点和终点相同");
                pathPositionBuffer.Add(new PathPosition { position = new int2(endPosition.x, endPosition.y) });
                return;
            }

            if (!IsWalkable(endPosition))
            {
                //Debug.Log("终点不可走");
                return;
            }

            if (IsCanWalk(startPosition, endPosition))
            {
                pathPositionBuffer.Add(new PathPosition { position = new int2(endPosition.x, endPosition.y) });
                pathPositionBuffer.Add(new PathPosition { position = new int2(startPosition.x, startPosition.y) });
                return;
            }

            #endregion

            #region A*寻路
            int startNodeIndex = CalculateIndex(startPosition.x, startPosition.y);
            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y);
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

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentMapNode.x + neighbourOffset.x, currentMapNode.y + neighbourOffset.y);

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y);

                    if (!IsInGrid(neighbourPosition))//不在格子内
                        continue;
                    if (closedList.Contains(neighbourNodeIndex)) //Already searched this node
                        continue;

                    MapNode neighbourMapNode = mapNodeArray[neighbourNodeIndex];
                    if (!neighbourMapNode.isWalkable)
                    {
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    int2 currentNodePosition = new int2(currentMapNode.x, currentMapNode.y);
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

            if (endPosition.x == startPosition.x && endPosition.y == startPosition.y)
            {
                //Debug.Log("-留在原地");
                pathPositionBuffer.Add(new PathPosition { position = new int2(endPosition.x, endPosition.y) });
            }
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {
                //Debug.Log("-找不到路径");
            }
            else
            {
                //Debug.Log("-找到路径");
                MapNode endMapNode = mapNodeArray[endNodeIndex];
                CalculatePath(pathNodeArray, endNode, endMapNode, ref pathPositionBuffer);
            }
            #endregion
            #endregion

        }
        #region 自定义方法
        public void GetXYFloor(float3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
        public bool IsSurrounded(int2 pos, float range)
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
                bool b1 = IsCanReach(p1);
                bool b2 = IsCanReach(p2);
                bool b3 = IsCanReach(p3);
                bool b4 = IsCanReach(p4);
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
        public bool IsCanWalk(int2 startPosition, int2 endPosition)
        {
            //Debug.Log(frame + "  IsCanWalk： startPosition  " + startPosition + "  endPosition = " + endPosition);

            if (startPosition.x == endPosition.x || startPosition.y == endPosition.y)
            {
                return IsPointToPointCanWalk(startPosition, endPosition, true);
            }

            bool flag;
            bool flag1;
            bool flag2;
            float K = (float)(startPosition.y - endPosition.y) / (startPosition.x - endPosition.x);
            //Debug.Log(frame + "  IsCanWalk： K  " + (K > 0 ? "  大于0 = " : "小于0") + "  K = " + K);

            flag = IsPointToPointCanWalk(startPosition, endPosition, K > 0);
            if (K > 0)
            {
                //startPosition += new int2(1, 1);
                int2 startPosition1 = new int2(startPosition.x, startPosition.y + 1);
                int2 endPosition1 = new int2(endPosition.x, endPosition.y + 1);
                flag1 = IsPointToPointCanWalk(startPosition1, endPosition1, false);
                int2 startPosition2 = new int2(startPosition.x + 1, startPosition.y);
                int2 endPosition2 = new int2(endPosition.x + 1, endPosition.y);
                flag2 = IsPointToPointCanWalk(startPosition2, endPosition2, false);
                return flag && flag1 && flag2;
            }
            else
            {
                int2 startPosition1 = new int2(startPosition.x + 1, startPosition.y + 1);
                int2 endPosition1 = new int2(endPosition.x + 1, endPosition.y + 1);
                flag1 = IsPointToPointCanWalk(startPosition1, endPosition1, false);
                int2 startPosition2 = new int2(startPosition.x + 1, startPosition.y);
                int2 endPosition2 = new int2(endPosition.x + 1, endPosition.y);
                flag2 = IsPointToPointCanWalk(startPosition2, endPosition2, true);
                return flag && flag1 && flag2;
            }

        }
        public bool IsPointToPointCanWalk(int2 startPosition, int2 endPosition, bool isMiddle)
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
                        if (!IsCanReach(currentPos))
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
                        if (!IsCanReach(currentPos))
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
                if (!IsCanReach(currentPos) && !currentPos.Equals(startPosition))
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

                    if (!IsCanReach(currentPos))
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
        private bool IsCanReach(int2 pos)
        {
            int index = CalculateIndex(pos.x, pos.y);
            return IsInGrid(pos) && mapNodeArray[index].isWalkable && occupyArray[index] == 0;
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
        private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, MapNode endMapNode,ref DynamicBuffer<PathPosition> pathPositionBuffer)
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
                    else if (IsCanWalk((int2)lastPoint, (int2)curPathPosition))
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
        #endregion
    }
}
