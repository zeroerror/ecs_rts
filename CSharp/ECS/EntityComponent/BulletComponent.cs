using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct BulletComponent : IComponentData
{
    [Header("子弹所属阵营")]
    public CampType campType;
    [Header("子弹所属Entity")]
    public Entity fromEntity;
    [Header("友军伤害开启")]
    public bool friendlyFire;
    [Header("子弹类型")]
    public BulletType bulletType;
    [Header("子弹伤害")]
    public float damage;
    [Header("伤害半径")]
    public float range;
    [Header("初速度")]
    public float3 initSpeed;
}