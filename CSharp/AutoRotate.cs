using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Header("旋转轴")]
    public Vector3 rotateAxis;
    [Header("旋转速度(°/s)")]
    public float rotateSpeed = 90f;
    [Header("顺时针旋转")]
    public bool isClockwise;
    private Coroutine cor;
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (cor != null) StopCoroutine(cor);
        cor = StartCoroutine(_Rotate());
    }

    IEnumerator _Rotate()
    {
        rotateSpeed = isClockwise ? rotateSpeed : -rotateSpeed;
        while (true)
        {
            transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
