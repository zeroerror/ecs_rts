/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct Buff : IComponentData
{
    [Header("Buff类型")]
    public BuffType buffType;
    [Header("影响时间")]
    public float effectTime;
    [Header("影响数值")]
    public float effectValue;
    [Header("默认")]
    public Entity effectTarget;
    [Header("默认")]
    public float cd;

    public Attribute SetBuff(Attribute life, int frame, out bool isFinished)
    {
        Attribute lifeAuthoring = life;
        if (cd == 0)
        {
            cd = frame;
        }
        if (frame - cd >= (effectTime * 60))
        {
            if (buffType == BuffType.速度)
            {
                lifeAuthoring.curSpeed = lifeAuthoring.normalSpeed;
            }
            isFinished = true;
        }
        else
        {
            switch (buffType)
            {
                case BuffType.速度:
                    lifeAuthoring.curSpeed = lifeAuthoring.normalSpeed + effectValue;
                    break;
                case BuffType.生命:
                    lifeAuthoring.health += effectValue;
                    break;
                default:
                    break;
            }
            isFinished = false;
        }
        return lifeAuthoring;
    }

}
*/