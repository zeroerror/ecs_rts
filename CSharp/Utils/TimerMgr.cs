using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroHero;

public class TimerMgr : UnitySingleton<TimerMgr>
{

    #region [公用方法]
    public int SetTimer(float second, Action action = null)
    {
        Coroutine coroutine = StartCoroutine(utils.WaitForSecondsRealtime(second, action));
        _timerDictionary[_curCoroutineId] = coroutine;
        return _curCoroutineId++;
    }
    public int SetLoopTimer(float second, float delaySec, Action action = null)
    {
        Coroutine coroutine = StartCoroutine(utils.WaitForSecondsRealtimeLoop(second, delaySec, action));
        _timerDictionary[_curCoroutineId] = coroutine;
        return _curCoroutineId++;
    }
    public void RemoveTimer(int coroutineId)
    {
        if (_timerDictionary.ContainsKey(coroutineId))
        {
            Coroutine cor = _timerDictionary[coroutineId];
            StopCoroutine(cor);
            _timerDictionary.Remove(coroutineId);
        }
    }
    #endregion
    public override void Awake()
    {
        _timerDictionary = new Dictionary<int, Coroutine>();
        base.Awake();
    }



    #region Method
    private void RemoveAllTimer()
    {
        foreach (var item in _timerDictionary)
        {
            StopCoroutine(item.Value);
        }
        _curCoroutineId = 1;
    }
    #endregion

    #region Field
    private Dictionary<int, Coroutine> _timerDictionary;
    private int _curCoroutineId = 1;
    #endregion

}
