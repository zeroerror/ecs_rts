using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceToCamera : MonoBehaviour
{
    private Camera _mainCam;
    private void Start()
    {
        _mainCam = Camera.main;
    }
    private void Update()
    {
        gameObject.transform.forward = -_mainCam.transform.forward;
    }
}
