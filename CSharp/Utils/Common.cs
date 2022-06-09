using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using System;
public enum EntityType    //实体类型
{
    Unit,
    Target,
    Tower,
    可摧毁的
}
public enum RoleType    //角色类型
{
    远程,
    近战,
}
public enum CampType    //角色类型
{
    敌军,
    友军,
    中立
}
public enum CharacterType
{
    步兵,
    枪手,
    炮车,
    战士,
    坦克,
    机枪人,
    中立障碍物,
    防御塔,
    礼物炮车,
    自由骑士,
}
public enum BuffType    //Buff类型
{
    速度,
    生命,
}
public enum DamageType    //攻击类型
{
    单体,
    范围,
}
public enum BulletType    //子弹类型
{
    默认 = 0,
    子弹,
    炮弹,
}

public enum GameState
{
    加载中 = 0,
    游戏中,
    暂停,
    结束,
    退出
}
public enum SceneType
{
    Main = 0,
    大厅,
    游戏场景
}
public static class Common
{
    public static GridBasicInfo info;

    #region 通用方法
    #region 寻路所用
    public static bool IsSurrounded(int2 pos, float range, int2 gridSize, NativeArray<MapNode> mapNodeArray, NativeArray<int> occupyArray)
    {
        //1+4+8+16
        NativeList<int2> searchedList = new NativeList<int2>(Allocator.Temp);
        searchedList.Add(pos);
        int curIndex = 0;
        while (curIndex < searchedList.Length)
        {
            int2 curPos = searchedList[curIndex++];
            int2 p1 = curPos + new int2(-1, 0);
            int2 p2 = curPos + new int2(+1, 0);
            int2 p3 = curPos + new int2(0, +1);
            int2 p4 = curPos + new int2(0, -1);
            bool b1 = IsCanReach(p1, gridSize, mapNodeArray, occupyArray);
            bool b2 = IsCanReach(p2, gridSize, mapNodeArray, occupyArray);
            bool b3 = IsCanReach(p3, gridSize, mapNodeArray, occupyArray);
            bool b4 = IsCanReach(p4, gridSize, mapNodeArray, occupyArray);
            if (!b1 && !b2 && !b3 && !b4)
            {
                searchedList.Dispose();
                return true;
            }
            else
            {
                if (b1 && !searchedList.Contains(p1)) searchedList.Add(p1);
                if (b2 && !searchedList.Contains(p2)) searchedList.Add(p2);
                if (b3 && !searchedList.Contains(p3)) searchedList.Add(p3);
                if (b4 && !searchedList.Contains(p4)) searchedList.Add(p4);
            }
            if (math.distance(pos, p1) > range
                || math.distance(pos, p2) > range
                || math.distance(pos, p3) > range
                || math.distance(pos, p4) > range)
            {
                searchedList.Dispose();
                return false;
            }
        }
        searchedList.Dispose();
        return false;
    }
    private static bool IsCanReach(int2 pos, int2 gridSize, NativeArray<MapNode> mapNodeArray, NativeArray<int> occupyArray)
    {
        int index = CalculateIndex(pos.x, pos.y);
        return IsInGrid(pos, gridSize) && mapNodeArray[index].isWalkable && occupyArray[index] == 0;
    }
    private static bool IsInGrid(int2 position, int2 gridSize)
    {
        return position.x >= 0 && position.x < gridSize.x && position.y >= 0 && position.y < gridSize.y;
    }
    #endregion

    public static int GetEntityTypeLength()
    {
        return System.Enum.GetNames(typeof(EntityType)).Length;
    }
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 worldPosition = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000))
        {
            return raycastHit.point;
        }
        return worldPosition;
    }
    public static Vector3 GetScreenPointWorldPosition(Vector3 point)
    {
        Vector3 worldPosition = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(point);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000))
        {
            return raycastHit.point;
        }
        return worldPosition;
    }
    /// <summary>
    /// 获取当前屏幕射线击中的场景物件
    /// </summary>
    /// <returns></returns>
    public static GameObject GetSceneObjectByRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000))
        {
            return raycastHit.transform.gameObject;
        }
        return null;
    }
    public static Entity GetClickEntity()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000))
        {
            EntityGameObject ego = raycastHit.transform.GetComponent<EntityGameObject>();
            if (ego)
            {
                return ego.entity;
            }
        }
        return Entity.Null;
    }
    public static float3 GetRandomFloat3(float xRange, float yRange, float zRange)
    {
        float3 originPosition = info.originPosition;
        return new float3(UnityEngine.Random.Range(-xRange, xRange) + originPosition.x, UnityEngine.Random.Range(-yRange, yRange) + originPosition.y, UnityEngine.Random.Range(-zRange, zRange) + originPosition.z);
    }
    //获取转换后的x,y
    public static void GetXYFloor(float3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
        y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
    }
    public static void GetXYFloor(GridBasicInfo info, float3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
        y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
    }
    public static void GetWorldPositioToXY(out int x, out int y)
    {
        float3 position = GetMouseWorldPosition();
        GetXYFloor(position, out x, out y);
    }
    public static float3 GetXYToWorldPosition(int x, int y)
    {
        int cellSize = info.cellSize;
        float3 worldPosition = info.originPosition + new float3(x * cellSize, 0, y * cellSize);
        return worldPosition;
    }
    public static float3 GetXYToWorldPosition_Center(int x, int y)
    {
        int cellSize = info.cellSize;
        float3 worldPosition = info.originPosition + new float3(x * cellSize, 0, y * cellSize) + new float3(cellSize / 2f, 0, cellSize / 2f);
        return worldPosition;
    }
    public static float3 GetXYToWorldPosition_Center(GridBasicInfo info, int x, int y)
    {
        int cellSize = info.cellSize;
        float3 worldPosition = info.originPosition + new float3(x * cellSize, 0, y * cellSize) + new float3(cellSize / 2f, 0, cellSize / 2f);
        return worldPosition;
    }
    public static int CalculateIndex(int x, int y)
    {
        int index = -1;
        if (x < info.width && x >= 0 && y < info.height && y >= 0) index = x + y * info.width;
        if (index >= info.width * info.height) index = -1;
        return index;

    }
    public static int CalculateIndex(GridBasicInfo info, int x, int y)
    {
        return x + y * info.width;
    }
    public static bool IsWalkable(int2 pos)
    {
        int index = CalculateIndex(pos.x, pos.y);
        return pos.x >= 0 && pos.x < info.width && pos.y >= 0 && pos.y < info.height && MapSystem.mapNodeArray[index].isWalkable;
    }
    public static float3 GetWalkableWorldPosition()
    {
        GetWalkableXY(out int randomX, out int randomY);
        float3 randomPos = Common.GetXYToWorldPosition_Center(randomX, randomY);
        return randomPos;
    }
    public static void GetWalkableXY(out int x, out int y)
    {
        x = UnityEngine.Random.Range(0, info.width - 1);
        y = UnityEngine.Random.Range(0, info.height - 1);
        while (!Common.IsWalkable(new int2(x, y)))
        {
            x = UnityEngine.Random.Range(0, info.width - 1);
            y = UnityEngine.Random.Range(0, info.width - 1);
        }
    }
    public static void FormatFloat3(ref float3 f, int save)
    {
        int num = (int)math.pow(10, save);
        f.x = (float)math.round(f.x * num) / num;
        f.y = (float)math.round(f.y * num) / num;
        f.z = (float)math.round(f.z * num) / num;
    }
    public static void FormatFloat(ref float f, int save)
    {
        int num = (int)math.pow(10, save);

        f = (float)math.round(f * num) / num;
    }
    #endregion

}