using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using ZeroHero;

//拖塔建塔
public class TopMenu : UIBehavior
{
    private List<GoldCostCfg> cfgList;
    private string _shopMenuContent = "ShopMenu/Scroll View/Viewport/Content";
    private CharacterType _curCharacter;
    private string _curItemMask;
    private bool _hasRoleChosen;
    private float3 _curSpawnPos;
    private PlayerInfo _playerInfo;
    private Dictionary<CharacterType, int> _allyInfo;//角色-数量
    private Dictionary<CharacterType, int> _enemyInfo;//角色-数量
    private Dictionary<CharacterType, string> ally_uiDictionary;//角色-uiName
    private Dictionary<CharacterType, string> enemy_uiDictionary;//角色-uiName
    private string _allyColumnName = "GameInfo/AllyColumn/Scroll View/Viewport/Content";
    private string _enemyColumnName = "GameInfo/EnemyColumn/Scroll View/Viewport/Content";
    private bool isInfoDicSet;
    private ShowType curShowType = ShowType.Shop;
    private enum ShowType
    {
        Shop = 1,
        GameInfo,
    }
    private void Start()
    {
        cfgList = GoldCostCfgMgr.GetConfigList();
        int index = 0;
        for (int i = 0; i < cfgList.Count; i++)
        {
            var cfg = cfgList[i];
            index++;
            string itemName = "ShopItem" + index;
            AddChildUI(_shopMenuContent, "ShopItem", itemName);
            string uiName = _shopMenuContent + "/" + itemName;
            SetOnClick(uiName, new Callback(SetChosenRole), uiName + "/_Selected", cfg);
        }
        _ResetShoeMenuItem();
        _SetInfoDic();
        SetActive("ShopMenu", ShowType.Shop == curShowType);
        SetActive("GameInfo", ShowType.Shop != curShowType);
        SetOnClick("ClickArea", new Callback(SpawnCharacter));
        SetOnClick("SwitchMenuBtn", new Callback(_SwitchMenu));

    }
    private void OnEnable()
    {
        _playerInfo = GameController.GetPlayerInfo();
        Text_SetText("Gold/Num", _playerInfo.Gold);
        _ResetShoeMenuItem();
        CameraMgr.RemoveCullingMask("Perspective");
        EntityEventSystem.Instance.onGoldChange -= _UpdateGold;
        EntityEventSystem.Instance.onGameInfoUpdate -= _UpdateInfoDictionary;
        InputMgr.Instance.SwitchTopMenu -= _SwitchMenu;
        EntityEventSystem.Instance.onGoldChange += _UpdateGold;
        EntityEventSystem.Instance.onGameInfoUpdate += _UpdateInfoDictionary;
        InputMgr.Instance.SwitchTopMenu += _SwitchMenu;
    }
    private void OnDisable()
    {
        EntityEventSystem.Instance.onGoldChange -= _UpdateGold;
        EntityEventSystem.Instance.onGameInfoUpdate -= _UpdateInfoDictionary;
        InputMgr.Instance.SwitchTopMenu -= _SwitchMenu;
    }
    private void OnDestroy()
    {

    }
    private void SetChosenRole(params object[] args)
    {
        //Debug.Log(string.Format("args长度: {0}  args : {1}", args.Length, args.ToString()));
        var maskName = (string)args[0];
        var cfg = (GoldCostCfg)args[1];
        if (_hasRoleChosen && _curCharacter == cfg.CharacterType)
        {
            _hasRoleChosen = false;         //点击了同一个item
        }
        else
        {
            if (_hasRoleChosen)
            {
                //还原上一个被选中的item状态
                SetActive(_curItemMask, false);
            }
            _hasRoleChosen = true;
            _curCharacter = cfg.CharacterType;
            _curItemMask = maskName;
        }

        SetActive(maskName, _hasRoleChosen);
        SetActive("ClickArea", _hasRoleChosen);
        if (!_hasRoleChosen)
        {
            CameraMgr.RemoveCullingMask("Perspective");
        }
        else
        {
            CameraMgr.AddCullingMask("Perspective");
        }
    }
    private void SpawnCharacter(params object[] args)
    {
        if (!GameController.IsClickRespawnArea(ref _curSpawnPos))
        {
            //Debug.LogError("生成位置不在区域内！");
            return;
        }
        foreach (var costCfg in cfgList)
        {
            if (costCfg.CharacterType == _curCharacter)
            {
                if (_playerInfo.Gold >= costCfg.Cost)
                {
                    //扣除金币,更新显示金币数量
                    _playerInfo.Gold -= costCfg.Cost;
                    _UpdateGold();
                    //游戏控制器=》生成人物
                    GameController.SpawnCharacter(CampType.友军, _curCharacter, _curSpawnPos);
                }
                else
                {
                    Debug.Log("金币不足！！");
                }
                break;
            }
        }

    }
    private void _UpdateShopMenuItem(params object[] args)
    {
        Transform content = transform.Find(_shopMenuContent);
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            var cfg = cfgList[i];
            var characterType = cfg.CharacterType;
            var iconID = RoleCfgMgr.GetByCharacterType(characterType).IconId;
            Text_SetText(_shopMenuContent + "/" + child.name + "/Cost", cfg.Cost);
            Image_SetImage(_shopMenuContent + "/" + child.name + "/Icon", iconID);
            SetActive(_shopMenuContent + "/" + child.name + "/_Layer", cfg.Cost > _playerInfo.Gold);
        }
    }
    private void _ResetShoeMenuItem()
    {
        _UpdateShopMenuItem();
        _hasRoleChosen = false;
        SetActive("ClickArea", false);
        Transform content = transform.Find(_shopMenuContent);
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            SetActive(_shopMenuContent + "/" + child.name + "/_Selected", false);
        }
    }
    private void _UpdateGold(params object[] args)
    {
        _playerInfo = GameController.GetPlayerInfo();
        Text_SetText("Gold/Num", _playerInfo.Gold);
        _UpdateShopMenuItem();
    }
    private void _SetInfoDic()
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
        if (!isInfoDicSet || curShowType == ShowType.Shop) return;
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
        if (isNeedUpdate) _UpdateText();

    }
    private void _SwitchMenu(params object[] args)
    {
        if (curShowType == ShowType.Shop)
        {
            curShowType = ShowType.GameInfo;
            CameraMgr.RemoveCullingMask("Perspective");
            _ResetShoeMenuItem();
        }
        else
        {
            curShowType = ShowType.Shop;
        }
        SetActive("ShopMenu", ShowType.Shop == curShowType);
        SetActive("GameInfo", ShowType.Shop != curShowType);
    }
    private void _UpdateText()
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