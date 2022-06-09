using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;

public class GridVisual : MonoBehaviour
{
    private  bool isInit = false;
    [SerializeField]
    private  Material material;
    [SerializeField]
    private  Mesh mesh;

    private void Start()
    {
    }
    private void Update()
    {
        if (GameController._gameState!=GameState.游戏中)
        {
            return;
        }
        MyGrid<GridNode> grid = GridSet.Instance.GetChosenGrid();
        if (!isInit)
        {
            isInit = true;
        }

        DrawMesh();
    }
    #region 自定义方法
    private void DrawMesh()
    {
        NativeArray<int> occupyArray = PathOccupySystem.occupyArray;
        GridBasicInfo info = GridSet.Instance.GetChosenGridInfo();
        for (int x = 0; x < info.width; x++)
        {
            for (int y = 0; y < info.height; y++)
            {
                int index = Common.CalculateIndex(x,y);
                if (occupyArray[index] == 1)
                {
                    Matrix4x4 m1 = Matrix4x4.TRS(Common.GetXYToWorldPosition_Center(x, y), Quaternion.Euler(-90, 0, 0), Vector3.one * 1);
                    Graphics.DrawMesh(mesh, m1, material, 1, Camera.main);
                }
            }
        }



    }
    #endregion

}
