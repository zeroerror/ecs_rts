using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeroHero;

public class UIBehavior : MonoBehaviour
{
    private void Awake()
    {
        GraphicRaycaster gr = transform.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    public object[] args;
    public UI ui;
    public struct UI
    {
        public string uiName;
        public string layer;
    }

    #region [Slider]
    protected void Slider_SetVal(string uiName, float val)
    {
        Slider slider = null;
        if (!_CheckComponent<Slider>(uiName, ref slider))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Slider Component Not Found!");
            return;
        };

        slider.value = val;
    }
    #endregion

    #region [Click]
    protected void SetOnClick(string uiName, Callback func, params object[] args)
    {
        Button button = null;
        Transform ui = null;
        if (!_TryGetChildUI(uiName, ref ui)) return;

        if (!_CheckComponent<Button>(uiName, ref button))
        {
            ui.gameObject.AddComponent<Button>();
        }

        button = ui.gameObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            func?.Invoke(args);
        });
    }
    #endregion

    #region [Text]
    protected void Text_SetFontSize(string uiName, int size)
    {
        Text text = null;
        if (!_CheckComponent<Text>(uiName, ref text))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Text Component Not Found!");
            return;
        };

        text.fontSize = size;
    }
    protected void Text_SetColor(string uiName, string color)
    {
        Text text = null;
        if (!_CheckComponent<Text>(uiName, ref text))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Text Component Not Found!");
            return;
        };

        ColorUtility.TryParseHtmlString(color, out Color nowColor);
        text.color = nowColor;
    }
    protected void Text_SetAlignment(string uiName, TextAnchor textAnchor)
    {
        Text text = null;
        if (!_CheckComponent<Text>(uiName, ref text))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Text Component Not Found!");
            return;
        };

        text.alignment = textAnchor;
    }
    protected void Text_SetText(string uiName, object content)
    {
        Text text = null;
        if (!_CheckComponent<Text>(uiName, ref text))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Text Component Not Found!");
            return;
        };

        text.text = content.ToString();
    }
    protected string Input_GetText(string uiName)
    {
        InputField inputField = null;
        if (!_CheckComponent<InputField>(uiName, ref inputField))
        {
            Debug.LogError(transform.name + ": " + uiName + ": InputField Component Not Found!");
            return "";
        }

        return inputField.text;
    }
    #endregion

    #region [Image]
    protected void Image_SetFillAmount(string uiName, float fill)
    {
        Image image = null;
        if (!_CheckComponent<Image>(uiName, ref image))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Image Component Not Found!");
            return;
        }

        image.fillAmount = fill;
    }
    protected void Image_SetImage(string uiName, int resourceID)
    {
        Image image = null;
        if (!_CheckComponent<Image>(uiName, ref image))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Image Component Not Found!");
            return;
        }

        image.sprite = ResourceMgr.LoadSprite(resourceID);
    }
    protected void Image_SetColor(string uiName, string color)
    {
        Image image = null;
        if (!_CheckComponent<Image>(uiName, ref image))
        {
            Debug.LogError(transform.name + ": " + uiName + ": Image Component Not Found!");
            return;
        }

        ColorUtility.TryParseHtmlString(color, out Color nowColor);
        image.color = nowColor;
    }

    protected void RawImage_SetImage(string uiName, int resourceID)
    {
        RawImage rawImage = null;
        if (!_CheckComponent<RawImage>(uiName, ref rawImage))
        {
            Debug.LogError(transform.name + ": " + uiName + ": RawImage Component Not Found!");
            return;
        }

        rawImage.texture = ResourceMgr.LoadTexture(resourceID);
    }
    #endregion

    #region [UI]
    protected bool AddChildUI(string uiName, string childName, string name = null)
    {
        GameObject child = ResourceMgr.LoadUI(childName);
        if (child == null)
        {
            Debug.LogError(transform.name + ": AddChildUI Failed! 不存在UI " + childName);
            return false;
        }
        child = Instantiate(child, transform.Find(uiName));
        child.name = name;
        return true;
    }

    protected void SetActive(string uiName, bool isActive)
    {
        Transform childUI = null;
        if (!_TryGetChildUI(uiName, ref childUI))
        {
            Debug.LogError(string.Format("{0}: childUI: {1} 不存在！", transform.name, uiName));
            return;
        }

        childUI.gameObject.SetActive(isActive);
    }
    #endregion



    #region PrivateMethod
    private bool _CheckComponent<T>(string uiName, ref T component)
    {
        Transform ui = null;
        if (uiName == transform.name) ui = transform;
        else _TryGetChildUI(uiName, ref ui);
        if (ui == null) return false;

        component = ui.GetComponent<T>();
        if (component == null)
        {
            //Debug.LogError(string.Format("{0}: {1} 组件 {2} 不存在！", transform.name, uiName, typeof(T)));
            return false;
        }
        return component != null;
    }

    private bool _TryGetChildUI(string uiName, ref Transform childUI)
    {
        childUI = transform.Find(uiName);
        if (childUI == null)
        {
            Debug.LogError(string.Format("{0}: uiName: {1} 不存在!", transform.name, uiName));
            return false;
        }
        return true;
    }
    #endregion

}
