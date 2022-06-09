using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZeroHero;

public class UIHome : UIBehavior
{
    private int _allyNum = 0;
    private int _enemyNum = 0;
    private string _allyColumnName = "AllyColumn/Scroll View/Viewport/Content";
    private string _enemyColumnName = "EnemyColumn/Scroll View/Viewport/Content";
    public Dictionary<RoleCfg, int> spawnDic;
    private Dictionary<RoleCfg, string> spawnUIDic;
    private void Start()
    {
        spawnDic = new Dictionary<RoleCfg, int>();
        spawnUIDic = new Dictionary<RoleCfg, string>();
        var configList = RoleCfgMgr.GetConfigList();
        foreach (var role in configList)
        {
            spawnDic.Add(role, 0);
        }
        SetOnClick("Menu/StartBtn", new Callback(ClickStartBtn));
        SetOnClick("Menu/QuitBtn", new Callback(ClickQuitBtn));
    }
    private void OnEnable()
    {
        if (!GameController.isFirstPlay)
        {
            SetOnClick("Menu/ContinueBtn", new Callback(ClickContinueBtn));
        }
        SetActive("Menu/ContinueBtn", !GameController.isFirstPlay);
        Text_SetText("Lv/Num", GameController._playerInfo.Level);
    }
    public void ClickStartBtn(params object[] args)
    {
        GameController.StartNewGame();
        SceneManager.LoadSceneAsync((int)SceneType.游戏场景, LoadSceneMode.Single);
        SceneManager.sceneLoaded -= GameStart;
        SceneManager.sceneLoaded += GameStart;
    }
    public void ClickContinueBtn(params object[] args)
    {
        GameController.ContinueGame();
        SceneManager.LoadSceneAsync((int)SceneType.游戏场景, LoadSceneMode.Single);
        SceneManager.sceneLoaded -= GameStart;
        SceneManager.sceneLoaded += GameStart;
    }
    public void ClickQuitBtn(params object[] args)
    {
#if UNITY_EDITOR
        //UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void GameStart(Scene scene, LoadSceneMode sceneType)
    {
        if (scene.buildIndex == (int)SceneType.游戏场景)
        {
            //GameSetting.Instance.SpawnEntity(spawnDic);
            SceneManager.sceneLoaded -= GameStart;
            UIMgr.CloseUI("Home");
        }
    }
}
