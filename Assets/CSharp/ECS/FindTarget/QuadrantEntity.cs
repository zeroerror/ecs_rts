using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct QuadrantEntity : IComponentData
{
    public Entity entity;
    public EntityType typeEnum;
}
