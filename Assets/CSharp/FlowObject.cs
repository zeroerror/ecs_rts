using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FlowObject : MonoBehaviour
{
    [Header("漂浮速度")]
    public float speed;
    [Header("漂浮位移")]
    public float3 offset;

    private void Start()
    {
        float3 startPos = transform.position;
        float3 endPos = startPos + offset;
        StartCoroutine(flowing(startPos, endPos));
    }

    private IEnumerator flowing(float3 startPos, float3 endPos)
    {
        float3 curFramePos = startPos;
        float curDistance = math.distance(curFramePos, endPos);
        float distance = curDistance;
        while (true)
        {
            curFramePos += offset / (speed / Time.deltaTime);
            float nexDistance = math.distance(curFramePos, endPos);
            if (nexDistance < curDistance)
            {
                transform.position = curFramePos;
                curDistance = nexDistance;
            }
            else
            {
                curFramePos = endPos;
                transform.position = curFramePos;
                curDistance = distance;

                endPos = startPos;
                startPos = curFramePos;
                offset = -offset;
            }
            yield return null;
        }

    }
}
