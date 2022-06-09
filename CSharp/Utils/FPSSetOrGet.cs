using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSSetOrGet : UIBehavior
{
    [Header("FPS刷新间隔")]
    [SerializeField]
    private float fpsShowInteval = 1f;
    [Header("FPS最大帧数")]
    [SerializeField]
    private int fpsLimit;
    private float passedDeltaTime = 0;
    private int passedframeCount = 0;
    private float fps = 0;


    private void Start()
    {
        SetFPS();
    }

    private void Update()
    {
        GetFPS();
    }

    private void SetFPS()
    {
        fpsLimit = fpsLimit == 0 ? 144 : fpsLimit;
        Application.targetFrameRate = fpsLimit;
    }

    private void GetFPS()
    {
        passedDeltaTime += Time.deltaTime;
        passedframeCount++;

        if (passedDeltaTime >= fpsShowInteval)
        {
            fps = passedframeCount/passedDeltaTime;
            Text_SetText("", "FPS: " + fps.ToString("#.#"));
            passedDeltaTime = 0;
            passedframeCount = 0;
        }
    }
}
