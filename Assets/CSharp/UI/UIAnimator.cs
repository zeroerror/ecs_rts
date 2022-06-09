using System.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace ZeroHero
{
    public class UIAnimator : MonoBehaviour
    {
        [Header("过渡时间")]
        public float time;
        [Header("起始位置")]
        public float2 startPos;
        private RectTransform rct;
        private float2 endPos;
        private Coroutine _cor = null;
        private void Awake()
        {
            rct = GetComponent<RectTransform>();
            endPos = rct.anchoredPosition;
        }
        private void OnEnable()
        {
            if (_cor != null) StopCoroutine(_cor);
            _cor = StartCoroutine(enumerator());
        }
        private void OnDisable()
        {
            if (_cor != null) StopCoroutine(_cor);
        }
        private IEnumerator enumerator()
        {
            Vector2 offset = (endPos - startPos) / (time / Time.deltaTime);
            rct.anchoredPosition = startPos;
            if (endPos.x == startPos.x)
            {
                bool toUp = endPos.y > startPos.y;
                while (rct.anchoredPosition.y != endPos.y)
                {
                    rct.anchoredPosition += offset;
                    if (toUp && rct.anchoredPosition.y >= endPos.y)
                    {
                        rct.anchoredPosition = endPos;
                        break;
                    }
                    if (!toUp && rct.anchoredPosition.y <= endPos.y)
                    {
                        rct.anchoredPosition = endPos;
                        break;
                    }
                    offset = (endPos - startPos) / (time / Time.deltaTime);
                    yield return null;
                }
            }
            else
            {
                bool toRight = endPos.x > startPos.x;
                while (rct.anchoredPosition.x != endPos.x)
                {
                    rct.anchoredPosition += offset;
                    if (toRight && rct.anchoredPosition.x >= endPos.x)
                    {
                        rct.anchoredPosition = endPos;
                        break;
                    }
                    if (!toRight && rct.anchoredPosition.x <= endPos.x)
                    {
                        rct.anchoredPosition = endPos;
                        break;
                    }
                    offset = (endPos - startPos) / (time / Time.deltaTime);
                    yield return null;
                }
            }


        }
    }
}

