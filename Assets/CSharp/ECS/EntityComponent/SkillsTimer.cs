using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct SkillsTimer : IComponentData
{
    [Header("寻路计时")]
    public float pathFindingTimer;
    [Header("攻击计时")]
    public float atkTimer;
    [Header("小技能计时")]
    public float smallSkillTimer;
    [Header("大技能计时")]
    public float bigSkillTimer;
}
