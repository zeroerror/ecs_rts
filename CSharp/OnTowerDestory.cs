using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 当防御塔被摧毁时，需要触发事件
/// </summary>
public class OnTowerDestory : MonoBehaviour
{
    public GameObject awakeBornPoint;
    [Header("落地区域")]
    public GameObject linkLandArea;
    private void Awake()
    {
        if (linkLandArea) linkLandArea.SetActive(false);
    }
    private void OnDestroy()
    {
        if (awakeBornPoint)
        {
            awakeBornPoint.SetActive(true);
        }
        if (linkLandArea) linkLandArea.SetActive(true);
    }
}
