using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StaticObstacle : IComponentData
{
    public Entity entity;
}
