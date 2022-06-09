using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Attribute : IComponentData
{
    [Header("角色类型")]
    public RoleType roleType;
    [Header("人物")]
    public CharacterType characterType;
    [Header("伤害类型（近战伤害判断）")]
    public DamageType damageType;
    [Header("子弹类型（远程伤害判断）")]
    public BulletType bulletType;
    [Header("友军伤害开启")]
    public bool friendlyFire;
    [Header("子弹实体")]
    public Entity bulletEntity;
    [Header("枪口实体")]
    public Entity atkPoint;
    [Header("子弹速度")]
    public float bulletSpeed;
    [Header("生命值")]
    public float health;
    [Header("能量值")]
    public float energy;
    [Header("攻击力")]
    public float atkStrength;
    [Header("攻击速度 times/S")]
    public float atkSpeed;
    [Header("攻击范围")]
    public float atkRange;
    [Header("伤害范围")]
    public float damageRange;
    [Header("子弹初速度")]
    public float3 bulletInitSpeed;
    [Header("移动速度")]
    public float moveSpeed;
    [Header("侦测距离")]
    public float searchRange;
    [Header("小技能冷却时间")]
    public float smallSkillCD;
    [Header("小技能消耗能量")]
    public float smallSkillNeedEnergy;
    [Header("大技能冷却时间")]
    public float bigSkillCD;
    [Header("大技能消耗能量")]
    public float bigSkillNeedEnergy;
    [Header("冷却缩减")]
    public float cdShrink;///  冷却时间 *（1-cdShrink）=真正冷却时间
}


