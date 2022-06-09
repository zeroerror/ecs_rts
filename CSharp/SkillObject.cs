using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Create SkillObject")]
public class SkillObject : ScriptableObject
{
    [Header("----------作用类型")]
    public DamageType damageType;
    [Header("----------作用时间")]
    public float totalTime;
    [Header("----------生命值")]
    public float health;
    public BuffInfoClass healthBuffInfo = new BuffInfoClass();
    [Header("----------能量值")]
    public float energy;
    public BuffInfoClass energyBuffInfo = new BuffInfoClass();
    [Header("----------攻击力")]
    public float atkStrength;
    public BuffInfoClass atkStrengthBuffInfo = new BuffInfoClass();
    [Header("----------攻击速度")]
    public float atkSpeed;
    public BuffInfoClass atkSpeedBuffInfo = new BuffInfoClass();
    [Header("----------移动速度")]
    public float moveSpeed;
    public BuffInfoClass moveSpeedBuffInfo = new BuffInfoClass();
    [Header("----------攻击范围")]
    public float atkRange;
    public BuffInfoClass atkRangeBuffInfo = new BuffInfoClass();
    [Header("----------伤害范围")]
    public float damageRange;
    public BuffInfoClass damageRangeBuffInfo = new BuffInfoClass();
    [Header("----------子弹初速度")]
    public float3 bulletInitSpeed;
    public BuffInfoClass bulletInitSpeedBuffInfo = new BuffInfoClass();
    [Header("----------侦测距离")]
    public float searchRange;
    public BuffInfoClass searchRangeBuffInfo = new BuffInfoClass();
    [Header("----------小技能冷却时间")]
    public float smallSkillCD;
    [Header("----------小技能消耗能量")]
    public float smallSkillNeedEnergy;
    [Header("----------大技能冷却时间")]
    public float bigSkillCD;
    [Header("----------大技能消耗能量")]
    public float bigSkillNeedEnergy;
    [Header("----------冷却缩减")]
    public float cdShrink;///  冷却时间 *（1-cdShrink）=真正冷却时间
    public BuffInfoClass cdShrinkBuffInfo = new BuffInfoClass();
}

[Serializable]
public class BuffInfoClass
{
    [Header("是否可叠加")]
    public bool isStackable;
    [Header("叠加间隔时间")]
    public float stackInterval;
    [Header("是否需要恢复差值")]
    public bool needReset;
    public BuffInfo ToBuffInfo()
    {
        BuffInfo buffInfo = new BuffInfo();
        buffInfo.isStackable = isStackable;
        buffInfo.stackInterval = stackInterval;
        buffInfo.needReset = needReset;
        return buffInfo;
    }
}