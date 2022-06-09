using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
[System.Serializable]
public struct GridBasicInfo
{
    public int index;
    public int width;
    public int height;
    public int cellSize;
    public int2 rangeX;
    public int2 rangeY;


    public float3 originPosition;

    public GridBasicInfo(int index, int width = 0, int height = 0, int cellSize = 0, float3 originPosition = default(float3))
    {
        this.index = index;
        if (width <= 0 || height <= 0 || cellSize <= 0)
        {
            this.width = 2;
            this.height = 2;
            this.cellSize = 1;
        }
        else
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
        }
        this.rangeX = new int2(Mathf.RoundToInt(originPosition.x), Mathf.FloorToInt((originPosition.x + width) * cellSize));
        this.rangeY = new int2(Mathf.RoundToInt(originPosition.z), Mathf.FloorToInt((originPosition.z + height) * cellSize));
        this.originPosition = originPosition;
    }
}
public class MyGrid<T>
{
    public int gridIndex;
    public int width;
    public int height;
    public float cellSize;
    public float3 originPosition;
    private Mesh mesh;
    private Material material;
    private T[,] gridArray;


    public MyGrid(int gridIndex, int width, int height, float cellSize, float3 originPosition, Mesh mesh, Material material)
    {
        this.gridIndex = gridIndex;
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.mesh = mesh;
        this.material = material;
        gridArray = new T[width, height];
    }

    public int GetGridIndex()
    {
        return this.gridIndex;
    }

    public Mesh GetMesh()
    {
        return this.mesh;
    }

    public Material GetMaterial()
    {
        return this.material;
    }

    public void SetGridLine()   //设置边界线
    {
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                Debug.DrawLine(Common.GetXYToWorldPosition(x, y), Common.GetXYToWorldPosition(x + 1, y), Color.white, 999f);
                Debug.DrawLine(Common.GetXYToWorldPosition(x, y), Common.GetXYToWorldPosition(x, y + 1), Color.white, 999f);
            }
        }
        Debug.DrawLine(Common.GetXYToWorldPosition(width, 0), Common.GetXYToWorldPosition(width, height), Color.white, 999f);
        Debug.DrawLine(Common.GetXYToWorldPosition(0, height), Common.GetXYToWorldPosition(width, height), Color.white, 999f);
    }
    public void SetGridNodeObject(T gridNodeObject, int x, int y)
    {
        gridArray[x, y] = gridNodeObject;
    }
    //获得网格节点对象
    public T GetGridNodeObject(int x, int y)
    {
        if (x >= width || y >= height || x < 0 || y < 0) return default(T);
        return gridArray[x, y];
    }
    public T GetGridNodeObject(float3 worldPosition)
    {
        Common.GetXYFloor(worldPosition, out int x, out int y);
        if (x >= width || y >= height || x < 0 || y < 0) return default(T);
        return gridArray[x, y];
    }
    public T[,] GetAllGridNodeObject()
    {
        return gridArray;
    }
    //The gridNode in the position of (x,y) has changed,then do this.(Change it's apearance or something)

    public void SetValue(Vector3 worldPosition, T value)
    {
        Common.GetXYFloor(worldPosition, out int x, out int y);
        SetValue(x, y, value);
    }
    public void SetValue(int x, int y, T value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
        }
    }
}
