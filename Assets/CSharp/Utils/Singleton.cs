﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitySingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(T)) as T;
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    _instance = (T)obj.AddComponent(typeof(T));
                    obj.hideFlags = HideFlags.DontSave;
                    obj.name = typeof(T).Name;
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 在MonoBehaviour被创建时调用一次
    /// </summary>
    public virtual void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (_instance == null)
        {
            _instance = this as T;
        }
        else
        {
            GameObject.Destroy(this.gameObject);
        }
    }
}
public class Singleton<T> where T : new()
{
    private static T _instance;
    private static object mutex = new object();
    public static T instance
    {
        get
        {
            if (_instance == null)
            {
                lock (mutex)    //保证单例的线程安全性
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }
}