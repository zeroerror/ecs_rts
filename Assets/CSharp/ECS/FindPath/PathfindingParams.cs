using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PathfindingParams : IComponentData
{
    [Header("是否体积碰撞")]
    public bool hasCollision;
    public int2 startPosition;
    public int2 endPosition;
}
