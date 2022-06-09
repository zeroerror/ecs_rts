using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct MoveComponent : IComponentData
{
    public float speed;
    public float3 targetPos;
    public int curMoveFrame;
}