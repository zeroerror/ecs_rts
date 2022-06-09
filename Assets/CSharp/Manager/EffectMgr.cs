using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using MiniExcelLibs;
using System.Linq;
using System;
using System.Xml.Serialization;
using ZeroHero;
using UnityEngine.SceneManagement;
using System.Text;

public class EffectMgr : UnitySingleton<EffectMgr>
{

    #region [Public_Method]
    public GameObject PlayEffect(int effectID, float3 pos)
    {
        EffectCfg effectCfg = EffectCfgMgr.GetById(effectID);
        if (effectCfg == null)
        {
            Debug.LogError("特效ID：" + effectID + "不存在！");
            return null;
        }
        GameObject effectGO = _GetEffectGameObj(effectCfg);
        effectGO.transform.position = pos;
        return effectGO;
    }
    public GameObject PlayEffect(string effectName, float3 pos)
    {
        EffectCfg effectCfg = GetCfgByName(effectName);
        if (effectCfg == null)
        {
            Debug.LogError("特效名：" + effectName + "不存在！");
            return null;
        }

        GameObject effectGO = _GetEffectGameObj(effectCfg);
        effectGO.transform.position = pos;
        return effectGO;
    }
    public EffectCfg GetCfgByName(string effectName)
    {
        EffectCfg effect = null;
        List<EffectCfg> configList = EffectCfgMgr.GetConfigList();
        foreach (var eff in configList)
        {
            if (eff.Name == effectName)
            {
                effect = eff;
            }
        }

        return effect;
    }
    #endregion

    private void Start()
    {
        _effectPool = new GameObject("特效对象池").transform;
        DontDestroyOnLoad(_effectPool);
        _effectMap = new NativeMultiHashMap<int, int>(0, Allocator.Persistent);
        _effectGOs = new Dictionary<int, GameObject>();
        SceneManager.sceneLoaded -= _ControlrEffectPool;
        SceneManager.sceneLoaded += _ControlrEffectPool;
    }
    private void OnDestroy()
    {
        _effectMap.Dispose();
        _effectGOs.Clear();
    }

    #region [Private_Method]
    private GameObject _GetEffectGameObj(EffectCfg effectCfg)
    {
        GameObject effectGO = null;
        int count = _effectMap.CountValuesForKey(effectCfg.Id);
        if (count >= _limit)
        {
            if (_effectMap.TryGetFirstValue(effectCfg.Id, out int id, out NativeMultiHashMapIterator<int> iterator))
            {
                _effectGOs.TryGetValue(id, out effectGO);
                do
                {
                    _effectGOs.TryGetValue(id, out effectGO);
                    if (!effectGO.activeInHierarchy)
                    {
                        break;
                    }
                } while (_effectMap.TryGetNextValue(out id, ref iterator));
            }
        }
        else
        {
#if UNITY_EDITOR
            effectGO = Resources.Load(effectCfg.AssetPath + effectCfg.AssetName) as GameObject;
#else
            effectGO = ABMgr.Instance.LoadRes(_abName, effectCfg.AssetName) as GameObject;
#endif
            if (effectGO == null)
            {
                Debug.LogError(new StringBuilder("特效预制体路径：").Append(effectCfg.AssetPath).Append(effectCfg.AssetName).Append("  不存在！！！"));
                return null;
            }

            effectGO.SetActive(false);
            effectGO = Instantiate(effectGO);
            int instanceID = effectGO.GetInstanceID();
            effectGO.transform.SetParent(_effectPool, false);
            _effectGOs.Add(instanceID, effectGO);
            if (_effectMap.Capacity < _effectGOs.Count) _effectMap.Capacity = _effectGOs.Count;
            _effectMap.Add(effectCfg.Id, instanceID);
        }

        if (effectGO.activeInHierarchy)
        {
            //特效超出上限数量，且当前没有可利用的特效
#if UNITY_EDITOR
            effectGO = Resources.Load(effectCfg.AssetPath + effectCfg.AssetName) as GameObject;
#else
            effectGO = ABMgr.Instance.LoadRes(_abName, effectCfg.AssetName) as GameObject;
#endif
            if (effectGO == null) return null;

            effectGO.SetActive(false);
            effectGO = Instantiate(effectGO);
            int instanceID = effectGO.GetInstanceID();
            effectGO.transform.SetParent(_effectPool, false);
            _effectGOs.Add(instanceID, effectGO);
            if (_effectMap.Capacity < _effectGOs.Count) _effectMap.Capacity = _effectGOs.Count;
            _effectMap.Add(effectCfg.Id, instanceID);
        }

        ParticleSystem ps;
        ps = effectGO.GetComponent<ParticleSystem>();
        ps.Play();
        effectGO.SetActive(true);
        return effectGO;
    }
    private void _ControlrEffectPool(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == (int)SceneType.大厅)
        {
            _effectPool.gameObject.SetActive(false);
            TimerMgr.Instance.RemoveTimer(_timerId);
            _timerId = TimerMgr.Instance.SetTimer(20, _ClearEffectPool);
        }
        else if (scene.buildIndex == (int)SceneType.游戏场景)
        {
            if (!_effectPool)
            {
                _effectPool = new GameObject("特效对象池").transform;
                DontDestroyOnLoad(_effectPool);
            }
            _effectPool.gameObject.SetActive(true);
            TimerMgr.Instance.RemoveTimer(_timerId);
        }
    }

    private void _ClearEffectPool()
    {
        Destroy(_effectPool.gameObject);
        if (_effectMap.IsCreated) _effectMap.Clear();
        else _effectMap = new NativeMultiHashMap<int, int>(0, Allocator.Persistent);
    }
    #endregion



    #region Field
    private string _abName = "effect";
    private int _limit = 50;
    private NativeMultiHashMap<int, int> _effectMap;//Key => effectID  Value => Object.GetInstanceID 
    private Dictionary<int, GameObject> _effectGOs;//Key => Object.GetInstanceID  Value => GameObject
    private Transform _effectPool;
    private int _timerId;
    #endregion

}
