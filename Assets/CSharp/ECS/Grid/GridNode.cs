using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
public class GridNode : IComponentData
{
    private MyGrid<GridNode> cameFromGrid;
    private int x;
    private int y;

    private bool isWalkable;
    private bool isLanded;

    public GridNode()
    {

    }
    public GridNode(MyGrid<GridNode> grid, int x, int y)
    {
        this.cameFromGrid = grid;
        this.x = x;
        this.y = y;
        this.isWalkable = true;
        this.isLanded = false;
    }

    public bool IsWalkable()
    {
        return this.isWalkable;
    }

    public bool IsLanded()
    {
        return this.isLanded;
    }

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }

    public void SetisLanded(bool isLanded)
    {
        //通过PathFollowSystem自动控制
        this.isLanded = isLanded;
    }
}
