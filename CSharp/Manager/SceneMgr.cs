using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroHero;
public class SceneMgr : UnitySingleton<SceneMgr>
{
    public override void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += SceneLoadCompleted;
        DontDestroyOnLoad(new GameObject("EffectMgr", typeof(EffectMgr)));
    }

    #region Method
    private void SceneLoadCompleted(Scene scene, LoadSceneMode sceneType)
    {
        if (scene.buildIndex == (int)SceneType.大厅)
        {
            Debug.Log("[SceneMgr]: --------------------------大厅界面--------------------------");
            currentScene = SceneType.大厅;
            if (!GameController.isInit) GameController.Init();
            UIMgr.CloseUI("GameWinPanel");
            UIMgr.CloseUI("GameLostPanel");
#if UNITY_ANDROID || UNITY_IOS
            UIMgr.CloseUI("MobilePanel");
#endif
            UIMgr.OpenUI("Home");
            //TimerMgr.Instance.SetTimer(1f,()=> { UIMgr.OpenUI("Home"); });
            CameraMgr.SetWorldCameraActive(false);
        }
        else if (scene.buildIndex == (int)SceneType.游戏场景)
        {
            Debug.Log("[SceneMgr]: --------------------------游戏场景--------------------------");
            currentScene = SceneType.游戏场景;
            TimerMgr.Instance.RemoveTimer(_timerId);
            TimerMgr.Instance.SetTimer(1.5f, _StartGameTimer);
            CameraMgr.SetWorldCameraActive(true);
#if UNITY_ANDROID || UNITY_IOS
            UIMgr.OpenUI("MobilePanel");
#endif
            EntityEventSystem.Instance.onClickCharacter += _OnClickCharacter;
        }
    }
    private void _OnClickCharacter(params object[] args)
    {
        UIMgr.CloseUI("AttributePanel");
        if (args.Length > 0) UIMgr.OpenUI("AttributePanel", args);
    }
    private void _StartGameTimer()
    {
        EntityEventSystem.Instance.OnGameStateChanged(GameState.游戏中);
        UIMgr.OpenUI("TopMenu");
    }
    #endregion

    #region Property
    public SceneType currentScene { get; private set; }
    #endregion

    #region Field
    private int _timerId;
    #endregion

}
