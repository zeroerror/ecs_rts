using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ZeroHero;

public abstract class SystemBaseUnity : SystemBase
{
    #region [Public]
    public void SetEnable(bool isEnable)
    {
        _enable = isEnable;
        if (_enable) OnEnable();
        else OnDisable();
    }
    #endregion

    #region [生命周期]
    /// <summary>
    /// SystemBaseUnity: OnInit 基于SystemBase的自定义Init函数
    /// </summary>
    protected virtual void OnInit()
    {
        //Debug.Log("[SystemBaseUnity]: ----------------" + this.GetType().ToString() + " OnInit.");
        if (IsInRunningState(GameController._gameState)) SetEnable(true);
        EntityEventSystem.Instance.onGameStateChanged += _OnGameStateChange;
    }

    /// <summary>
    /// SystemBaseUnity: 基于SystemBase的自定义OnEnable函数
    /// </summary>
    protected virtual void OnEnable()
    {
        //Debug.Log("[SystemBaseUnity]: ----------------" + this.GetType().ToString() + " OnEnable.");
    }
    ///     /// <summary>
    /// SystemBaseUnity: FixedUpdate 基于SystemBase的Update控制
    /// </summary>
    protected virtual void FixedUpdate()
    {
    }
    /// <summary>
    /// SystemBaseUnity: OnDisable 基于SystemBase的自定义OnDisable函数
    /// </summary>
    protected virtual void OnDisable()
    {
        //Debug.Log("[SystemBaseUnity]: ----------------" + this.GetType().ToString() + " OnDisable.");
    }
    /// <summary>
    /// SystemBaseUnity: 基于SystemBase的OnCreate,添加本SystemBase到SystemBaseGroup
    /// </summary>
    protected override void OnCreate()
    {
        //Debug.Log("[SystemBaseUnity]: ----------------" + this.GetType().ToString() + " OnCreate.");
        AddToSystemBaseGroup(this.GetType().ToString(), this);
        SetGameStatesToAll();
    }
    /// <summary>
    /// SystemBaseUnity: OnDisable 基于SystemBase的OnDestroy控制
    /// </summary>
    protected override void OnDestroy()
    {
    }
    protected sealed override void OnUpdate()
    {
        //游戏状态
        bool isCanUpdate = false;
        foreach (GameState gameState in runOnGameStateList)
        {
            if (gameState == GameController._gameState)
            {
                isCanUpdate = true;
                break;
            }
        }
        if (!isCanUpdate) return;
        //是否初始化
        if (!_isInit)
        {
            OnInit();
            _isInit = true;
            return;
        }
        //是否激活
        if (!_enable) return;
        //Update
        FixedUpdate();
    }
    #endregion

    #region [Protected_Method]
    /// <summary>
    /// SystemBaseUnity: 基于SystemBase的自定义SetEnable函数
    /// </summary>
    protected void SetGameStatesToAll()
    {
        runOnGameStateList.Clear();
        foreach (GameState gameState in Enum.GetValues(typeof(GameState)))
        {
            runOnGameStateList.Add(gameState);
        }
    }
    protected void SetGameStatesExcept(GameState gameState)
    {
        runOnGameStateList.Clear();
        foreach (GameState _gameState in Enum.GetValues(typeof(GameState)))
        {
            if (_gameState != gameState) runOnGameStateList.Add(gameState);
        }
    }
    protected void SetGameStatesOnly(GameState gameState)
    {
        runOnGameStateList.Clear();
        foreach (GameState _gameState in Enum.GetValues(typeof(GameState)))
        {
            if (_gameState == gameState)
            {
                runOnGameStateList.Add(gameState);
                break;
            }
        }
    }

    #endregion

    #region [Protected_Field]
    protected List<GameState> runOnGameStateList = new List<GameState>();
    #endregion

    #region Property
    private bool _enable;
    private bool _isInit;
    #endregion

    #region [Private]
    private static Dictionary<string, SystemBase> _systemBaseGroup = new Dictionary<string, SystemBase>();
    private static void AddToSystemBaseGroup(string systemName, SystemBaseUnity systemBaseUnity)
    {
        _systemBaseGroup.Add(systemName, systemBaseUnity);
    }
    private bool IsInRunningState(GameState gameState)
    {
        foreach (GameState state in runOnGameStateList)
        {
            if (state == gameState)
            {
                return true;
            }
        }
        return false;
    }
    private void _OnGameStateChange(params object[] args)
    {
        GameState state = (GameState)args[0];
        if (IsInRunningState(state)&&_enable==false) SetEnable(true);
        if (!IsInRunningState(state) && _enable == true) SetEnable(false);
    }
    #endregion

}
