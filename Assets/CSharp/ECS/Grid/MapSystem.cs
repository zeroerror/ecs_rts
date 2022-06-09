using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public struct MapNode
{
    public int camefromGridIndex;
    public int x;
    public int y;

    public int index;
    public bool isWalkable;
}
public class MapSystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    public static NativeArray<MapNode> mapNodeArray;
    public static NativeArray<int> reachableArray;//0为可走
    private static GridBasicInfo info;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
    }
    protected override void OnInit()
    {
        base.OnInit();
        CreateMapNodeArray();
        //CreateReachableArray();
        isInit = true;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
    protected override void OnStopRunning()
    {
        if (isInit)
        {
            mapNodeArray.Dispose();
            //reachableArray.Dispose();
        }
        base.OnStopRunning();
    }

    //获取指定位置周边一定数量的可行走位置
    public static void GetMultiplePostionArray(float3 mousePosition, int count, out NativeList<int2> positionList)
    {
        positionList = new NativeList<int2>(Allocator.Temp);
        int2 endPosition;
        Common.GetXYFloor(mousePosition, out endPosition.x, out endPosition.y);
        if (!Common.IsWalkable(endPosition))
        {
            //Debug.Log("指定地点不可走");
            return;
        }

        int curCount = 0;
        int curListIndex = 0;
        NativeList<int2> tempPosList = new NativeList<int2>(Allocator.Temp);
        tempPosList.Add(endPosition);
        positionList.Add(endPosition);
        curCount++;
        while (curCount != count)
        {
            int2 curPos = tempPosList[curListIndex++];
            int2 pos1 = new int2(curPos.x - 1, curPos.y);
            int2 pos2 = new int2(curPos.x + 1, curPos.y);
            int2 pos3 = new int2(curPos.x - 1, curPos.y + 1);
            int2 pos4 = new int2(curPos.x, curPos.y + 1);
            int2 pos5 = new int2(curPos.x + 1, curPos.y + 1);
            int2 pos6 = new int2(curPos.x - 1, curPos.y - 1);
            int2 pos7 = new int2(curPos.x, curPos.y - 1);
            int2 pos8 = new int2(curPos.x + 1, curPos.y - 1);
            if (AddToPosList(curPos, ref tempPosList))
            {
                curCount++;
                positionList.Add(curPos);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos1, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos1);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos2, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos2);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos3, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos3);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos4, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos4);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos5, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos5);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos6, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos6);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos7, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos7);
                if (curCount >= count) break;
            }
            if (AddToPosList(pos8, ref tempPosList))
            {
                curCount++;
                positionList.Add(pos8);
                if (curCount >= count) break;
            }
        }
    }
    private static bool AddToPosList(int2 pos, ref NativeList<int2> posList)
    {
        bool isCanAdd = true;
        if (Common.IsWalkable(pos))
        {
            for (int i = 0; i < posList.Length; i++)
            {
                if (posList[i].Equals(pos))
                {
                    isCanAdd = false;
                    break;
                }
            }
        }
        else
        {
            isCanAdd = false;
        }
        if (isCanAdd)
        {
            posList.Add(pos);
        }
        return isCanAdd;
    }
    private void CreateMapNodeArray()
    {
        info = GridSet.Instance.GetChosenGridInfo();
        mapNodeArray = new NativeArray<MapNode>(info.width * info.height, Allocator.Persistent);
        MyGrid<GridNode> myGrid = GridSet.Instance.GetGridByIndex(info.index);

        for (int x = 0; x < info.width; x++)
        {
            for (int y = 0; y < info.height; y++)
            {
                MapNode node = new MapNode();
                node.camefromGridIndex = info.index;
                node.x = x;
                node.y = y;
                node.index = (x + y * info.width);
                node.isWalkable = myGrid.GetGridNodeObject(x, y).IsWalkable();
                mapNodeArray[node.index] = node;
            }
        }
    }
    private void CreateReachableArray()
    {
        info = GridSet.Instance.GetChosenGridInfo();
        reachableArray = new NativeArray<int>(info.width * info.height, Allocator.Persistent);
        for (int i = 0; i < reachableArray.Length; i++)
        {
            reachableArray[i] = 0;
        }
    }
    private void ChangeNodeWalkable(int x, int y)
    {
        int index = Common.CalculateIndex(x, y);
        MapNode node = mapNodeArray[index];
        node.isWalkable = !node.isWalkable;
        mapNodeArray[index] = node;
    }

}