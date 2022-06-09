using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using ZeroHero;

public class GameInfo : UIBehavior
{
    private Dictionary<CharacterType, int> _allyInfo;//角色-数量
    private Dictionary<CharacterType, int> _enemyInfo;//角色-数量
    private Dictionary<CharacterType, string> ally_uiDictionary;//角色-uiName
    private Dictionary<CharacterType, string> enemy_uiDictionary;//角色-uiName
    private string _allyColumnName = "GameInfo/AllyColumn/Scroll View/Viewport/Content";
    private string _enemyColumnName = "GameInfo/EnemyColumn/Scroll View/Viewport/Content";
    private bool isInfoDicSet;
    private void Start()
    {
        EntityEventSystem.Instance.onGameInfoUpdate += _UpdateInfoDictionary;
        SetInfoDic();
    }
    private void OnDestroy()
    {
    }
    public void SetInfoDic()
    {
        _allyInfo = new Dictionary<CharacterType, int>();
        _enemyInfo = new Dictionary<CharacterType, int>();
        ally_uiDictionary = new Dictionary<CharacterType, string>();
        enemy_uiDictionary = new Dictionary<CharacterType, string>();
        int index = 0;
        var roleCfgList = RoleCfgMgr.GetConfigList();
        foreach (var kvp in roleCfgList)
        {
            string uiName;
            index++;
            string itemName = "InfoItem" + index;
            AddChildUI(_allyColumnName, "InfoItem", itemName);
            uiName = _allyColumnName + "/" + itemName;
            ally_uiDictionary.Add(kvp.CharacterType, uiName);
            AddChildUI(_enemyColumnName, "InfoItem", itemName);
            uiName = _enemyColumnName + "/" + itemName;
            enemy_uiDictionary.Add(kvp.CharacterType, uiName);
        }

        isInfoDicSet = true;
    }
    public void _UpdateInfoDictionary(params object[] args)
    {
        if (!isInfoDicSet) return;
        bool isNeedUpdate = false;
        Dictionary<CharacterType, int> allyInfo = (Dictionary<CharacterType, int>)args[0];
        Dictionary<CharacterType, int> enemyInfo = (Dictionary<CharacterType, int>)args[1];
        foreach (var kvp in allyInfo)
        {
            if (_allyInfo.ContainsKey(kvp.Key) && _allyInfo[kvp.Key] != kvp.Value)
            {
                _allyInfo[kvp.Key] = kvp.Value;
                isNeedUpdate = true;
            }
            else if (!_allyInfo.ContainsKey(kvp.Key) && kvp.Value != 0)
            {
                _allyInfo.Add(kvp.Key, kvp.Value);
                isNeedUpdate = true;
            }
        }
        foreach (var kvp in enemyInfo)
        {
            if (_enemyInfo.ContainsKey(kvp.Key) && _enemyInfo[kvp.Key] != kvp.Value)
            {
                _enemyInfo[kvp.Key] = kvp.Value;
                isNeedUpdate = true;
            }
            else if (!_enemyInfo.ContainsKey(kvp.Key) && kvp.Value != 0)
            {
                _enemyInfo.Add(kvp.Key, kvp.Value);
                isNeedUpdate = true;
            }
        }
        List<CharacterType> list = new List<CharacterType>();
        foreach (var kvp in _allyInfo)
        {
            if (!allyInfo.ContainsKey(kvp.Key) && kvp.Value != 0)
            {
                list.Add(kvp.Key);
                isNeedUpdate = true;
            }
        }
        foreach (var characterType in list)
        {
            _allyInfo[characterType] = 0;
        }
        list.Clear();
        foreach (var kvp in _enemyInfo)
        {
            if (!enemyInfo.ContainsKey(kvp.Key) && kvp.Value != 0)
            {
                list.Add(kvp.Key);
                isNeedUpdate = true;
            }
        }
        foreach (var characterType in list)
        {
            _enemyInfo[characterType] = 0;
        }
        if (isNeedUpdate) UpdateText();

    }
    private void UpdateText()
    {
        foreach (var kvp in _allyInfo)
        {
            string uiName1 = ally_uiDictionary[kvp.Key] + "/Num";
            string uiName2 = ally_uiDictionary[kvp.Key] + "/Label";
            Text_SetText(uiName1, kvp.Value.ToString());
            Text_SetText(uiName2, kvp.Key.ToString());
        }
        foreach (var kvp in _enemyInfo)
        {
            string uiName1 = enemy_uiDictionary[kvp.Key] + "/Num";
            string uiName2 = enemy_uiDictionary[kvp.Key] + "/Label";
            Text_SetText(uiName1, kvp.Value.ToString());
            Text_SetText(uiName2, kvp.Key.ToString());
        }
    }
}