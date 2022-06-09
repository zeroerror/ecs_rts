using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
public struct GridNodeParams : IComponentData
{
    public int cameFromGridIndex;
    public int2 Position;
}
