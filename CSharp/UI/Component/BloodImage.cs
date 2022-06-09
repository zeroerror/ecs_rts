using System.Collections;

namespace UnityEngine.UI
{
    public sealed class BloodImage : RawImage
    {
        protected override void Start()
        {
            base.OnEnable();
            base.Awake();
            //获取血条  
            if (_bloodSlider == null)
            {
                _bloodSlider = transform.GetComponentInParent<Slider>();
                _width = _bloodSlider.GetComponent<RectTransform>().rect.width;
                _height = _bloodSlider.GetComponent<RectTransform>().rect.height;
                uvRect = new Rect(0, 0, _bloodSlider.maxValue, 1);
            }
            Transform fade = _bloodSlider.transform.Find("Background/Fade");
            if (fade == null)
            {
                fade = new GameObject("Fade", typeof(Image)).transform;
                fade.SetParent(_bloodSlider.transform.Find("Background"));
            }
            _fadeImage = fade.GetComponent<RectTransform>();
            _SetRectTransform(ref _fadeImage);
        }
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //获取血条的值  
            if (_bloodSlider != null&&gameObject.activeInHierarchy)
            {
                //刷新血条的显示  
                float value = _bloodSlider.value;
                float maxValue = _bloodSlider.maxValue;
                float num = (value / maxValue) * _width;
                uvRect = new Rect(0, 0, value, 1);
                //刷新渐变
                if (_cor!=null) StopCoroutine(_cor);
                Vector2 rt = _fadeImage.offsetMax;
                float targetValue = num - _width;
                if (rt.x < targetValue)
                {
                    rt.x = targetValue;
                    _fadeImage.offsetMax = rt;
                }
                else
                {
                    _cor = StartCoroutine(_UpdateFadeImage(targetValue));
                }
            }
        }

        #region Method
        private static void _SetRectTransform(ref RectTransform rct)
        {
            rct.anchorMin = Vector2.zero;
            rct.anchorMax = Vector2.one;
            rct.offsetMin = Vector2.zero;
            rct.offsetMax = Vector2.zero;
        }
        IEnumerator _UpdateFadeImage(float targetValue)
        {
            Vector2 rt = _fadeImage.offsetMax;
            float fadeValue = (rt.x - targetValue) / (_fadeTime / Time.deltaTime);
            while (rt.x > targetValue)
            {
                rt.x -= fadeValue;
                //rt.x = rt.x < targetValue ? targetValue : rt.x;
                _fadeImage.offsetMax = rt;
                yield return null;
            }
        }
        #endregion

        #region SerializeField
        [SerializeField]
        private RectTransform _fadeImage;
        [SerializeField]
        [Header("褪去时间")]
        private float _fadeTime = 0.2f;
        #endregion

        #region Field
        private Slider _bloodSlider;
        private float _width;
        private float _height;
        private Coroutine _cor;
        #endregion

    }
}
