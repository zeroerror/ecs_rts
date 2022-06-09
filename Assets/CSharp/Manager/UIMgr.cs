using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZeroHero;

public class UIMgr
{
    #region [InitRoot]
    public static void InitRoot()
    {
        int layer = LayerMask.NameToLayer("UI");

        #region UI
        _uiRoot = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler));
        _uiRootCanvas = _uiRoot.GetComponent<Canvas>();
        _uiRootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        _uiRootCanvas.sortingLayerID = SortingLayer.NameToID("Billbo");
        _uiRoot.layer = layer;
        _uiRootRt = _uiRoot.GetComponent<RectTransform>();
        GameObject.DontDestroyOnLoad(_uiRoot);
        CanvasScaler tempScale = _uiRoot.GetComponent<CanvasScaler>();
        tempScale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        tempScale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        tempScale.referenceResolution = UIDef.UIResolution;
        _uiRootRt.position = Vector3.zero;//CanvasScaler 会进行重置坐标 等其加载完成
        CameraMgr._uiCamera.transform.SetParent(_uiRoot.transform, false);
        UIMgr._uiRootCanvas.worldCamera = CameraMgr._uiCamera.GetComponent<Camera>();
        #endregion

        //#region WorldUI
        //_worldUIRoot = new GameObject("WorldUICanvas", typeof(Canvas), typeof(CanvasScaler));
        //_worldUIRootCanvas = _worldUIRoot.GetComponent<Canvas>();
        //_worldUIRootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        //_worldUIRoot.layer = layer;
        //_worldUIRootRt = _worldUIRoot.GetComponent<RectTransform>();
        //GameObject.DontDestroyOnLoad(_worldUIRoot);
        //#endregion


        _uiMainView = new GameObject("UIMainView", typeof(RectTransform)).GetComponent<RectTransform>();
        _SetRectTransform(ref _uiMainView);
        _uiMainView.transform.SetParent(_uiRoot.transform, false);

        foreach (var item in SortingLayer.layers)
        {
            if (item.value == 0) continue;
            var layerGO = new GameObject(item.name, typeof(Canvas));
            var layerRct = layerGO.GetComponent<RectTransform>();
            layerRct.transform.SetParent(_uiMainView.transform, false);
            layerRct.gameObject.layer = layer;
            Canvas canvas = layerGO.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = item.id;
            canvas.sortingOrder = _curSortingOrder;
            _layerSortingDic.Add(item.name, new int2(_curSortingOrder, 0));
            _curSortingOrder += 10;
            _SetRectTransform(ref layerRct);
            _uiLayerDic.Add(item.name, layerRct.transform);
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule), typeof(BaseInput));
        eventSystem.transform.SetParent(_uiRoot.transform, false);
    }
    #endregion

    #region Method
    public static bool IsActive(string uiName)
    {
        Transform ui = null;
        return _CheckUI(uiName, ref ui) && ui.gameObject.activeInHierarchy;
    }
    public static void OpenUI(string uiName, params object[] args)
    {
        Transform ui = null;
        if (!_CheckUI(uiName, ref ui))
        {
            if (!_TryCreateUI(uiName, ref ui))
            {
                Debug.LogError(string.Format("UI: {0} 不存在！", uiName));
                return;
            }
            else
            {
                Debug.Log(new StringBuilder("UI: ").Append(uiName).Append(" Loaded------"));
            }
        }
        if (ui.gameObject.activeInHierarchy) return;


        Canvas canvas;
        if (!ui.GetComponent<Canvas>()) canvas = ui.gameObject.AddComponent<Canvas>();
        else canvas = ui.GetComponent<Canvas>();
        UIBehavior uiBehavior = (UIBehavior)ui.gameObject.GetComponent("UIBehavior");
        UICfg uiCfg = UICfgMgr.GetByUIName(uiName);
        int2 value = _layerSortingDic[uiCfg.Layer];

        if (!_uiDic.ContainsKey(uiName))
        {
            if (uiBehavior)
            {
                uiBehavior.args = args;
                uiBehavior.ui.uiName = uiName;
                uiBehavior.ui.layer = uiCfg.Layer;
            }
            ui.gameObject.SetActive(true);
            _uiDic.Add(uiName, ui);
            value.y++;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = SortingLayer.NameToID(uiCfg.Layer);
            canvas.sortingOrder = value.x + value.y;
        }
        else
        {
            Debug.Log(new StringBuilder("UI: ").Append(uiName).Append(" ReOpened---"));
            uiBehavior.args = args;
            ui.gameObject.SetActive(true);
            value.y++;
            canvas.sortingOrder = value.x + value.y;
        }
        _layerSortingDic[uiCfg.Layer] = value;
    }
    public static void CloseUI(string uiName)
    {
        Transform ui = null;
        if (!_CheckUI(uiName, ref ui))
        {
            return;
        }
        if (!ui.gameObject.activeInHierarchy)
        {
            return;
        }

        UIBehavior uiBehavior = (UIBehavior)ui.GetComponent("UIBehavior");
        Canvas canvas = ui.GetComponent<Canvas>();
        string layer = uiBehavior.ui.layer;
        Transform layerTrans = _uiLayerDic[layer];
        Canvas[] allCanvas = layerTrans.GetComponentsInChildren<Canvas>();
        var cfg = UICfgMgr.GetByUIName(uiName);
        for (int i = 1; i < allCanvas.Length; i++)
        {
            Canvas otherCanvas = allCanvas[i];
            if (otherCanvas.sortingOrder > canvas.sortingOrder)
            {
                otherCanvas.sortingOrder--;
            }
        }
        int2 value = _layerSortingDic[cfg.Layer];
        value.y--;
        _layerSortingDic[cfg.Layer] = value;
        ui.gameObject.SetActive(false);
    }
    public static void DestoryUI(string uiName)
    {
        Transform ui = null;
        if (!_CheckUI(uiName, ref ui))
        {
            return;
        }
        GameObject.Destroy(ui.gameObject);
        var cfg = UICfgMgr.GetByUIName(uiName);
        int2 value = _layerSortingDic[cfg.Layer];
        value.y -= 1;
        _layerSortingDic[cfg.Layer] = value;
    }


    private static bool _TryCreateUI(string uiName, ref Transform ui)
    {
        //Debug.Log("_TryCreateUI " + uiName);
        var cfg = UICfgMgr.GetByUIName(uiName);
        GameObject go;
#if UNITY_EDITOR
        go = Resources.Load(cfg.AssetPath + cfg.AssetName) as GameObject;
#else
        go = ABMgr.Instance.LoadRes("ui",cfg.AssetName.ToLower()) as GameObject;
#endif
        if (!go) return false;
        go.SetActive(false);
        go = GameObject.Instantiate(go);
        ui = go.transform;
        ui.name = uiName;
        ui.SetParent(_uiLayerDic[cfg.Layer], false);
        return true;
    }
    private static bool _CheckUI(string uiName, ref Transform ui)
    {
        if (UIMgr._uiDic.ContainsKey(uiName))
        {
            ui = UIMgr._uiDic[uiName];
            return true;
        }

        return false;
    }
    private static void _SetRectTransform(ref RectTransform rct)
    {
        rct.pivot = new Vector2(0.5f, 0.5f);
        rct.anchorMin = Vector2.zero;
        rct.anchorMax = Vector2.one;
        rct.offsetMax = Vector2.zero;
        rct.offsetMin = Vector2.zero;
    }
    #endregion

    #region [Fields]
    private static GameObject _uiRoot;
    private static Canvas _uiRootCanvas;
    private static RectTransform _uiRootRt;
    private static GameObject _worldUIRoot;
    private static Canvas _worldUIRootCanvas;
    private static RectTransform _worldUIRootRt;
    private static RectTransform _uiMainView;
    private static Dictionary<string, Transform> _uiLayerDic = new Dictionary<string, Transform>();
    private static Dictionary<string, Transform> _uiDic = new Dictionary<string, Transform>();
    private static int _curSortingOrder = 0;
    private static Dictionary<string, int2> _layerSortingDic = new Dictionary<string, int2>();
    #endregion
}
