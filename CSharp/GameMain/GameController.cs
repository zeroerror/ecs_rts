using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using ZeroHero;

public class GameController
{
    public static void Init()
    {
        isInit = true;
        EntityEventSystem.Instance.onGameStateChanged += _GameStateChange;
    }

    #region [Public_Method]
    /// <summary>
    ///是否点击到生成区域
    /// </summary>
    /// <returns>返回点击处世界坐标</returns>
    public static bool IsClickRespawnArea(ref float3 spawnPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Perspective"))
            {
                spawnPos = hit.point;
                return true;
            }
        }

        return false;
    }
    /// <summary>
    /// 生成人物
    /// </summary>
    /// <param name="characterType"></param>
    /// <param name="spawnPos"></param>
    /// <param name="attributeBase">英雄基础属性</param>
    public static void SpawnCharacter(CampType campType, CharacterType characterType, float3 spawnPos)
    {
        if (_root == null)
        {
            _root = new GameObject("人物生成");
        }
        var roleCfg = RoleCfgMgr.GetByCharacterType(characterType);
        var cfg = ModleCfgMgr.GetById(roleCfg.ResourceId);
        GameObject obj;
        if (_spawnObjDic.ContainsKey(roleCfg))
        {
            obj = _spawnObjDic[roleCfg];
        }
        else
        {
            obj = Resources.Load(cfg.AssetPath + cfg.AssetName) as GameObject;
            _spawnObjDic.Add(roleCfg, obj);
        }
        obj = GameObject.Instantiate(obj, spawnPos, Quaternion.identity, _root.transform);

        EntityGameObject ego = obj.GetComponent<EntityGameObject>();
        ego.isNeedInitByConfig = true;
        ego.campType = campType;
    }
    public static void RespawnCharacter(GameObject character, AttributeBase savedAttributeBase)
    {
        if (!_InitEntityComponent(character)) return;

        EntityGameObject ego = character.GetComponent<EntityGameObject>();
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity entity = ego.entity;
        AttributeBase attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
        Attribute attribute = entityManager.GetComponentData<Attribute>(entity);
        SkillsTimer skillsTimer = entityManager.GetComponentData<SkillsTimer>(entity); ;

        #region [覆盖AttributeBase并拷贝到Attribute]
        attributeBase = savedAttributeBase;
        attribute.characterType = attributeBase.characterType;
        attribute.roleType = attributeBase.roleType;
        attribute.damageType = attributeBase.damageType;
        attribute.bulletType = attributeBase.bulletType;
        attribute.health = attributeBase.health;
        attribute.atkStrength = attributeBase.atkStrength;
        attribute.atkRange = attributeBase.atkRange;
        attribute.searchRange = attributeBase.searchRange;
        attribute.moveSpeed = attributeBase.moveSpeed;
        attribute.damageRange = attributeBase.damageRange;
        attribute.bulletSpeed = attributeBase.bulletSpeed;
        attribute.bulletInitSpeed = attributeBase.bulletInitSpeed;
        attribute.atkSpeed = attributeBase.atkSpeed;
        attribute.smallSkillCD = attributeBase.smallSkillCD;
        attribute.bigSkillCD = attributeBase.bigSkillCD;
        entityManager.SetComponentData<AttributeBase>(entity, attributeBase);
        entityManager.SetComponentData<Attribute>(entity, attribute);
        #endregion

        #region [技能计时器Component]
        skillsTimer.atkTimer = 0;
        skillsTimer.pathFindingTimer = 0;
        entityManager.AddComponentData<SkillsTimer>(entity, skillsTimer);
        #endregion
        #endregion

    }

    /// <summary>
    /// 初始化人物组件
    /// </summary>
    /// <param name="cfg">人物配置</param>
    /// <param name="character">人物对象</param>
    public static void InitCharacter(GameObject character, CharacterType characterType)
    {
        if (!_InitEntityComponent(character)) return;

        RoleCfg roleCfg = RoleCfgMgr.GetByCharacterType(characterType);
        SkillCfg skillCfg1 = roleCfg.IsHero ? SkillCfgMgr.GetBySkillID(roleCfg.SmallSkillID) : null;
        SkillCfg skillCfg2 = roleCfg.IsHero ? SkillCfgMgr.GetBySkillID(roleCfg.BigSkillID) : null;
        EntityGameObject ego = character.GetComponent<EntityGameObject>();
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity entity = ego.entity;
        AttributeBase attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
        Attribute attribute = entityManager.GetComponentData<Attribute>(entity);
        SkillsTimer skillsTimer = entityManager.GetComponentData<SkillsTimer>(entity);

        #region [初始化AttributeBase并拷贝到Attribute]
        m_float3 f3 = (m_float3)roleCfg.BulletInitSpeed;
        attributeBase.characterType = roleCfg.CharacterType;
        attributeBase.roleType = roleCfg.RoleType;
        attributeBase.health = roleCfg.Health;
        attributeBase.energy = roleCfg.Energy;
        attributeBase.damageType = roleCfg.DamageType;
        attributeBase.bulletType = roleCfg.BulletType;
        attributeBase.atkStrength = roleCfg.AtkStrength;
        attributeBase.atkRange = roleCfg.AtkRange;
        attributeBase.searchRange = roleCfg.SearchRange;
        attributeBase.moveSpeed = roleCfg.WalkSpeed;
        attributeBase.damageRange = roleCfg.DamageRange;
        attributeBase.bulletSpeed = roleCfg.BulletSpeed;
        attributeBase.bulletInitSpeed = new float3(f3.x, f3.y, f3.z);
        attributeBase.atkSpeed = roleCfg.AtkSpeed;
        attributeBase.isHero = roleCfg.IsHero;
        attributeBase.smallSkillCD = skillCfg1 != null ? skillCfg1.CD : float.MaxValue;
        attributeBase.smallSkillNeedEnergy = skillCfg1 != null ? skillCfg1.CostEnergy : float.MaxValue;
        attributeBase.bigSkillCD = skillCfg2 != null ? skillCfg2.CD : float.MaxValue;
        attributeBase.bigSkillNeedEnergy = skillCfg2 != null ? skillCfg2.CostEnergy : float.MaxValue;
        attributeBase.cdShrink = 0;
        entityManager.SetComponentData<AttributeBase>(entity, attributeBase);

        attribute.characterType = attributeBase.characterType;
        attribute.roleType = attributeBase.roleType;
        attribute.health = attributeBase.health;
        attribute.energy = attributeBase.energy;
        attribute.damageType = attributeBase.damageType;
        attribute.bulletType = attributeBase.bulletType;
        attribute.atkStrength = attributeBase.atkStrength;
        attribute.atkRange = attributeBase.atkRange;
        attribute.searchRange = attributeBase.searchRange;
        attribute.moveSpeed = attributeBase.moveSpeed;
        attribute.damageRange = attributeBase.damageRange;
        attribute.bulletSpeed = attributeBase.bulletSpeed;
        attribute.bulletInitSpeed = attributeBase.bulletInitSpeed;
        attribute.atkSpeed = attributeBase.atkSpeed;
        attribute.smallSkillCD = attributeBase.smallSkillCD;
        attribute.smallSkillNeedEnergy = attributeBase.smallSkillNeedEnergy;
        attribute.bigSkillCD = attributeBase.bigSkillCD;
        attribute.bigSkillNeedEnergy = attributeBase.bigSkillNeedEnergy;
        attribute.cdShrink = 0;
        entityManager.SetComponentData<Attribute>(entity, attribute);
        #endregion

        #region [初始化SkillsCD,SkillsTimer]
        entityManager.AddComponentData<SkillsTimer>(entity, new SkillsTimer());
        #endregion

    }
    /// <summary>
    /// 加载玩家存档
    /// </summary>
    public static void LoadPlayerInfo()
    {
        if (!Directory.Exists(_saveDir))
        {
            Directory.CreateDirectory(_saveDir);
        }

        isFirstPlay = !File.Exists(_savePath);
        if (isFirstPlay) StartNewGame();
        else ContinueGame();

    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public static void StartNewGame()
    {
        Debug.Log("~~~~~~~~~~~~开始新游戏~~~~~~~~~~~~");
        _gameState = GameState.加载中;
        Debug.Log("-----------------------------加载中");
        FileStream fileStream = new FileStream(_savePath, FileMode.OpenOrCreate);
        _playerInfo.Gold = 12000;
        _playerInfo.Level = 0;
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(PlayerInfo));
        xmlSerializer.Serialize(fileStream, _playerInfo);
        fileStream.Dispose();
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public static void ContinueGame()
    {
        Debug.Log("~~~~~~~~~~~~欢迎继续游戏~~~~~~~~~~~~");
        _gameState = GameState.加载中;
        Debug.Log("------------------------------------------------------加载中");
        var uri = new System.Uri(_savePath);
        var request = UnityWebRequest.Get(uri.AbsoluteUri);
        request.SendWebRequest();
        while (!request.isDone) { if (request.isNetworkError) { Debug.Log(request.error); return; }; };
        byte[] bytes = Encoding.UTF8.GetBytes(request.downloadHandler.text);
        MemoryStream stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(PlayerInfo));
        _playerInfo = (PlayerInfo)xmlSerializer.Deserialize(stream);
        _playerInfo.Gold = 0;
        stream.Dispose();
    }
    public static void SavePlayerInfo(PlayerInfo playerInfo)
    {
        Debug.Log("***************保存游戏***************");
        FileStream fileStream = new FileStream(_savePath, FileMode.Open);
        _playerInfo = playerInfo;
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(PlayerInfo));
        xmlSerializer.Serialize(fileStream, _playerInfo);
        fileStream.Dispose();
    }
    public static PlayerInfo GetPlayerInfo()
    {
        return _playerInfo;
    }
    /// <summary>
    /// 对击杀进行相应处理)
    /// </summary>
    /// <param name="killList"></param>
    public static void HandleKillList(CharacterType[] killList)
    {
        KillRewardCfg killRewardCfg;
        foreach (var characterType in killList)
        {
            var cfg = RoleCfgMgr.GetByCharacterType(characterType);
            killRewardCfg = KillRewardCfgMgr.GetByCharacterType(characterType);
            _playerInfo.Gold += killRewardCfg.Gold;
        }
        EntityEventSystem.Instance.OnGoldChange();
    }

    #region [Private_Method]
    private static bool _InitEntityComponent(GameObject character)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityGameObject ego = character.GetComponent<EntityGameObject>();
        if (ego == null)
        {
            return false;
        }
        AttributeBase attributeBase;
        Attribute attribute;
        SkillsTimer skillsTimer;
        Entity entity = ego.entity;
        if (entityManager.HasComponent<AttributeBase>(entity)) attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
        else attributeBase = new AttributeBase();
        if (entityManager.HasComponent<Attribute>(entity)) attribute = entityManager.GetComponentData<Attribute>(entity);
        else attribute = new Attribute();
        if (entityManager.HasComponent<SkillsTimer>(entity)) skillsTimer = entityManager.GetComponentData<SkillsTimer>(entity);
        else skillsTimer = new SkillsTimer();

        #region [根据EntityGameObject组件参数设置阵营]
        switch (ego.campType)
        {
            case CampType.友军:
                entityManager.RemoveComponent<EnemyComponent>(entity);
                entityManager.AddComponent<AllyComponent>(entity);
                entityManager.AddComponentData<QuadrantEntity>(entity, new QuadrantEntity { entity = entity, typeEnum = EntityType.Unit });
                break;
            case CampType.敌军:
                entityManager.RemoveComponent<AllyComponent>(entity);
                entityManager.AddComponent<EnemyComponent>(entity);
                entityManager.AddComponentData<QuadrantEntity>(entity, new QuadrantEntity { entity = entity, typeEnum = EntityType.Target });
                break;
            default:
                break;
        }
        #endregion

        return true;
    }
    private static GoldCostCfg _GetCostCfg(CharacterType characterType)
    {
        var list = GoldCostCfgMgr.GetConfigList();
        foreach (var costCfg in list)
        {
            if (characterType == costCfg.CharacterType)
            {
                return costCfg;
            }
        }
        return null;
    }
    private static void _GameStateChange(params object[] args)
    {
        GameState gameState = (GameState)args[0];
        Debug.Log("游戏状态=========> " + gameState.ToString());
        switch (gameState)
        {
            case GameState.加载中:
                break;
            case GameState.游戏中:
                isBreakRecord = false;
                break;
            case GameState.暂停:
                break;
            case GameState.结束:
                bool isWin = (bool)args[1];
                if (isWin)
                {
                    isBreakRecord = (bool)args[2];
                    if (isBreakRecord) Debug.Log("刷新记录！！！");
                    Debug.Log("=============任务完成 3s后回到大厅=============");
                    UIMgr.OpenUI("GameWinPanel");
                    TimerMgr.Instance.SetTimer(3f, () =>
                    {
                        SceneManager.LoadSceneAsync((int)SceneType.大厅, LoadSceneMode.Single);
                        UIMgr.CloseUI("GameWinPanel");
                    });
                    _playerInfo.Level++;
                    SavePlayerInfo(_playerInfo);
                    isFirstPlay = false;
                }
                else
                {
                    Debug.Log("=============任务失败 3s后回到大厅=============");
                    UIMgr.OpenUI("GameLostPanel");
                    TimerMgr.Instance.SetTimer(3f, () =>
                    {
                        SceneManager.LoadSceneAsync((int)SceneType.大厅, LoadSceneMode.Single);
                        UIMgr.CloseUI("GameLostPanel");
                    });
                }
                UIMgr.CloseUI("TopMenu");
                break;
            default:
                break;
        }
        _gameState = gameState;
    }
    #endregion

    #region Field
    private static Dictionary<RoleCfg, GameObject> _spawnObjDic = new Dictionary<RoleCfg, GameObject>();
    private static GameObject _root = null;
#if UNITY_EDITOR || UNITY_STANDALONE
    private static string _saveDir = Application.streamingAssetsPath;
#else
    private static string _saveDir = Application.persistentDataPath;
#endif
    private static string _savePath = _saveDir + "/info.xml";
    public static PlayerInfo _playerInfo = new PlayerInfo();
    public static GameState _gameState { get; private set; }
    public static bool isInit { get; private set; }
    public static bool isFirstPlay { get; private set; }
    public static bool isBreakRecord { get; private set; }
#endregion

}
