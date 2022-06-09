using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using Unity.Mathematics;
public class GridSet : MonoBehaviour
{
    [SerializeField] private int maxGridCount = 4;
    [SerializeField] private int curGridCount = 0;
    [SerializeField] private GridBasicInfo[] gridsBasicInfo;
    [SerializeField] private MyGrid<GridNode>[] myAllGrids;
    [SerializeField] public Mesh gridNodeMesh;
    [SerializeField] public Material gridNodeMaterial;
    [SerializeField] private int chosenGridIndex = -1;
    public static GridSet Instance { private set; get; }
    public bool isInit = false;
    public void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        #region 事件添加
        //EventSystem.Event_PressMouseDown_0 += SetGrid;
        //EventSystem.Event_PressKeyDown_G += SetGridNodeWalkable;
        //EventSystem.Event_PressKeyDown_Space += SetChosenGridIndex;
        #endregion
    }


    private void Update()
    {
        if (GameSetting.Instance == null) return;

        #region 数据初始化
        if (!isInit)
        {
            gridsBasicInfo = new GridBasicInfo[maxGridCount];
            myAllGrids = new MyGrid<GridNode>[maxGridCount];
            chosenGridIndex = -1;

            Transform floor = GameSetting.Instance.floor.transform;
            int cellSize = 1;
            int width = (int)(floor.localScale.x * 10 / cellSize);
            int height = (int)(floor.localScale.z * 10 / cellSize);
            GridBasicInfo info = new GridBasicInfo(curGridCount, width, height, cellSize, (float3)floor.position + new float3(-width / 2f, 0, -height / 2f));
            gridsBasicInfo[curGridCount] = info;
            chosenGridIndex = curGridCount;
            SetGrid();
            isInit = true;
            Common.info = info;
        }
        #endregion

    }
    #region 事件定义
    private void SetGrid()
    {
        GridBasicInfo info = gridsBasicInfo[curGridCount];
        if (info.width == 0 || info.height == 0 || info.cellSize == 0)
        {
            //Debug.Log("没有输入参数！设置随机网格");
            int width = UnityEngine.Random.Range(5, 5);
            int height = UnityEngine.Random.Range(5, 5);
            int cellSize = UnityEngine.Random.Range(2, 2);
            Vector3 worldPosition = Common.GetMouseWorldPosition();
            if (worldPosition != Vector3.zero)
            {
                Vector3 originPosition = new Vector3(worldPosition.x, 0, worldPosition.z);
                info = new GridBasicInfo(curGridCount, width, height, cellSize, originPosition);
            }
        }
        else
        {
            info = new GridBasicInfo(curGridCount, info.width, info.height, info.cellSize, info.originPosition);
        }
        gridsBasicInfo[curGridCount] = info;

        MyGrid<GridNode> myGrid = new MyGrid<GridNode>(curGridCount, info.width, info.height, info.cellSize, info.originPosition, gridNodeMesh, gridNodeMaterial);
        myAllGrids[curGridCount] = myGrid;

        for (int x = 0; x < myGrid.width; x++)
        {
            for (int y = 0; y < myGrid.height; y++)
            {
                GridNode gridNode = new GridNode(myGrid, x, y);
                myGrid.SetGridNodeObject(gridNode, x, y);
            }
        }
        //myGrid.SetGridLine();
        //GridVisual.Instance.SetGridVisual(myGrid);
        curGridCount++;
        if (chosenGridIndex == -1) SetChosenGridIndex(myGrid.GetGridIndex());
    }
    private void SetChosenGridIndex()
    {
        Vector3 worldPosition = Common.GetMouseWorldPosition();
        for (int i = 0; i < curGridCount; i++)
        {
            GridBasicInfo info = gridsBasicInfo[i];
            float minX = info.originPosition.x;
            float maxX = minX + info.width * info.cellSize;
            float minZ = info.originPosition.z;
            float maxZ = minZ + info.height * info.cellSize;
            if (worldPosition.x >= minX && worldPosition.x <= maxX && worldPosition.z >= minZ && worldPosition.z <= maxZ)
            {
                //Debug.Log("当前选中网格：" + info.index);
                this.chosenGridIndex = info.index;
                break;
            }
        }

    }
    #endregion

    #region 自定义方法
    public MyGrid<GridNode> GetGridByIndex(int gridIndex)
    {
        return myAllGrids[gridIndex];
    }
    public GridBasicInfo GetGridInfo(int gridIndex)
    {
        return gridsBasicInfo[gridIndex];
    }
    public GridBasicInfo GetChosenGridInfo()
    {
        GridBasicInfo info = new GridBasicInfo();
        if (this.chosenGridIndex != -1) info = gridsBasicInfo[this.chosenGridIndex];
        return info;
    }
    public MyGrid<GridNode> GetChosenGrid()
    {
        MyGrid<GridNode> grid = default(MyGrid<GridNode>);
        if (this.chosenGridIndex != -1) grid = myAllGrids[this.chosenGridIndex];
        return grid;
    }
    public int GetIndex(int x, int y, int width)
    {
        return x + y * width;
    }
    public void SetChosenGridIndex(int index)
    {
        this.chosenGridIndex = index;
    }
    public bool IsInChosenGrid(Vector3 worldPosition)
    {
        if (this.chosenGridIndex == -1) return false;
        GridBasicInfo info = gridsBasicInfo[this.chosenGridIndex];
        float minX = info.originPosition.x;
        float maxX = minX + info.width * info.cellSize;
        float minZ = info.originPosition.z;
        float maxZ = minZ + info.height * info.cellSize;
        if (worldPosition.x >= minX && worldPosition.x <= maxX && worldPosition.z >= minZ && worldPosition.z <= maxZ)
        {
            return true;
        }
        return false;
    }
    #endregion







}

