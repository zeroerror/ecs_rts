using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroHero;
public class MainInit : UnitySingleton<MainInit>
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#endif
    private static void Init()
    {
        SceneManager.sceneLoaded -= SceneLoadCompleted;
        SceneManager.sceneLoaded += SceneLoadCompleted;
        Application.wantsToQuit -= OnApplicationQuit;
        Application.wantsToQuit += OnApplicationQuit;
    }
#if UNITY_EDITOR
#else  
    protected override void OnAwake()
    {
        Init();
    }
#endif
    private static void SceneLoadCompleted(Scene scene, LoadSceneMode sceneType)
    {
        if (scene.buildIndex == (int)SceneType.Main)
        {
            Debug.Log("--------------------------Main--------------------------");
            IconCfgMgr.Init();
            ModleCfgMgr.Init();
            EffectCfgMgr.Init();
            RoleCfgMgr.Init();
            ItemCfgMgr.Init();
            UICfgMgr.Init();
            GoldCostCfgMgr.Init();
            KillRewardCfgMgr.Init();
            CameraCfgMgr.Init();
            CampCfgMgr.Init();
            ResourceCfgMgr.Init();
            LevelCfgMgr.Init();
            SkillCfgMgr.Init();
            RoleAttributeCfgMgr.Init();
            Debug.Log("配置管理器初始化完成--------------------------");

            CameraMgr.Init();
            CameraMgr.SetWorldCameraActive(true);
            GameController.LoadPlayerInfo();
            UIMgr.InitRoot();
            DontDestroyOnLoad(new GameObject("EntityEventSystem", typeof(EntityEventSystem)));
            DontDestroyOnLoad(new GameObject("TimerManager", typeof(TimerMgr)));
            DontDestroyOnLoad(new GameObject("ABMgr", typeof(ABMgr)));
            DontDestroyOnLoad(new GameObject("SceneMgr", typeof(SceneMgr)));
            DontDestroyOnLoad(new GameObject("CursorHandler", typeof(CursorHandler)));
            DontDestroyOnLoad(new GameObject("InputMgr", typeof(InputMgr)));
            DontDestroyOnLoad(new GameObject("控制台", typeof(ChinarViewConsole)));
            SceneManager.sceneLoaded -= SceneLoadCompleted;
            SceneManager.LoadSceneAsync((int)SceneType.大厅, LoadSceneMode.Single);
            UIMgr.OpenUI("FPS");
        }
    }
    private static bool OnApplicationQuit()
    {
        if (EntityEventSystem.Instance) EntityEventSystem.Instance.OnGameStateChanged(GameState.退出);
        return true; //return true表示可以关闭unity编辑器
    }
}
