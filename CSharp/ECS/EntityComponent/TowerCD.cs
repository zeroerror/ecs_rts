using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TowerCD : IComponentData
{
    #region 所有技能CD
    [Header("上次攻击时间")]
    public int lastAtkFrameCount;
    #endregion

}
