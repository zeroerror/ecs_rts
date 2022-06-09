using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
[GenerateAuthoringComponent]
public struct TowerComponent : IComponentData
{    
    [Header("自身实体")]
    public Entity entity;
    [Header("枪口实体")]
    public Entity atkPoint;
    [Header("子弹实体")]
    public Entity bulletEntity;
    [Header("盾牌实体")]
    public Entity shieldEntity;

    [Header("等级")]
    public int level;
    [Header("攻击速度（次/秒）")]
    public int atkSpeed;
    [Header("子弹速度")]
    public float bulletSpeed;


    //[Header("1星级外观")]
    //public RenderBounds renderMesh_1;

    //[Header("2星级外观")]
    //public RenderMesh renderMesh_2;

    //[Header("3星级外观")]
    //public RenderMesh renderMesh_3;

}
