using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PathFollowInfo : IComponentData
{
    public int2 latestGridNodePos;//上一个网格节点的位置
}