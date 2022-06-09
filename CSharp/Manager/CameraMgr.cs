using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMgr
{
    #region [InitRoot]
    public static void Init()
    {
        int layer = LayerMask.NameToLayer("UI");
        _uiCamera = new GameObject("UI相机", typeof(Camera)).transform;
        var uiCamera = _uiCamera.GetComponent<Camera>();
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << layer;
        uiCamera.orthographic = true;
        uiCamera.orthographicSize = 1f;
        uiCamera.nearClipPlane = -10;
        uiCamera.farClipPlane = 10;
        uiCamera.allowHDR = false;
        uiCamera.allowMSAA = false;
        uiCamera.allowDynamicResolution = false;
        uiCamera.useOcclusionCulling = false;

        //_worldCamera = new GameObject("世界相机", typeof(Camera)).transform;
        //var camera = _worldCamera.GetComponent<Camera>();
        //camera.clearFlags = CameraClearFlags.Skybox;
        //camera.cullingMask = 0 << layer;
        //camera.orthographic = false;
        //camera.orthographicSize = 1f;
        //camera.nearClipPlane = -10;
        //camera.farClipPlane = 10;
        //camera.allowHDR = false;
        //camera.allowMSAA = false;
        //camera.allowDynamicResolution = false;
        //camera.useOcclusionCulling = false;
        //UIMgr._worldUIRootCanvas.worldCamera = camera;
        //GameObject.DontDestroyOnLoad(_worldCamera);
    }
    #endregion

    #region Method
    public static void AddCullingMask(string layer)
    {
        _worldCamera.GetComponent<Camera>().cullingMask |= (1 << LayerMask.NameToLayer(layer));
    }

    public static void RemoveCullingMask(string layer)
    {
        _worldCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer(layer));
    }
    public static void SetWorldCameraActive(bool isActive)
    {
        if (_worldCamera == null) CreateWorldCamera();
        _worldCamera.gameObject.SetActive(isActive);
    }
    #endregion



    private static void CreateWorldCamera()
    {
        GameObject go = ResourceMgr.LoadCamera("地图相机") as GameObject;
        go = GameObject.Instantiate(go);
        _worldCamera = go.transform;
        _worldCamera.name = "地图相机";
    }
    #region Field
    public static Transform _uiCamera;
    public static Transform _worldCamera;
    #endregion
}
