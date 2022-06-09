using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ZeroHero;
/// <summary>
/// 用于实时监控游戏情况
/// </summary>
public class GameInfoSystem : SystemBaseUnity
{
    private static Dictionary<CharacterType, int> _allyInfo;
    private static Dictionary<CharacterType, int> _enemyInfo;
    private static List<RoleCfg> _cfgList;
    private static EntityQuery entityQuery;
    private static EntityQueryDesc allyQueryDesc;
    private static EntityQueryDesc enemyQueryDesc;
    private static EntityQueryDesc allySelectedQueryDesc;
    private static Attribute lastSelectAttribute;
    private static AttributeBase lastSelectAttributeBase;
    private static SkillsTimer lastSelectSkillsTimer;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
    }
    protected override void OnInit()
    {
        base.OnInit();
        _cfgList = RoleCfgMgr.GetConfigList();
        _allyInfo = new Dictionary<CharacterType, int>();
        _enemyInfo = new Dictionary<CharacterType, int>();
        allyQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
            ComponentType.ReadOnly<AllyComponent>(),
            },
        };
        allySelectedQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
            ComponentType.ReadOnly<AllyComponent>(),
            ComponentType.ReadOnly<AllySelected>(),
            },
        };
        enemyQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
            ComponentType.ReadOnly<EnemyComponent>(),
            },
        };

    }
    private int costTime;
    protected override void OnEnable()
    {
        base.OnEnable();
        TimerMgr.Instance.RemoveTimer(_timerId);
        costTime = 0;
        TimerMgr.Instance.SetTimer(2f, () => { _timerId = TimerMgr.Instance.SetLoopTimer(1f, 0f, _CheckGameOverTimer); });
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        TimerMgr.Instance.RemoveTimer(_timerId);
        TimerMgr.Instance.RemoveTimer(_timerUI);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        _allyInfo.Clear();
        _enemyInfo.Clear();
        Entities.ForEach((Entity entity, Attribute attribute, AllyComponent AllyComponent) =>
        {
            if (_allyInfo.ContainsKey(attribute.characterType)) _allyInfo[attribute.characterType]++;
            else _allyInfo.Add(attribute.characterType, 1);
        }).WithoutBurst().Run();
        Entities.ForEach((Entity entity, Attribute attribute, EnemyComponent enemyComponent) =>
        {
            if (_enemyInfo.ContainsKey(attribute.characterType)) _enemyInfo[attribute.characterType]++;
            else _enemyInfo.Add(attribute.characterType, 1);
        }).WithoutBurst().Run();
        if (_timerUI == 0)
        {
            _timerUI = TimerMgr.Instance.SetLoopTimer(1, 0, _UpdateGameInfoUI);

        }
        entityQuery = GetEntityQuery(allySelectedQueryDesc);
        if (entityQuery.CalculateEntityCount() == 1)
        {
            NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
            Entity selectedEntity = entityArray[0];
            CampType campType = EntityManager.HasComponent<AllyComponent>(selectedEntity) ? CampType.友军 : CampType.敌军;
            Attribute attribute = EntityManager.GetComponentData<Attribute>(selectedEntity);
            AttributeBase attributeBase = EntityManager.GetComponentData<AttributeBase>(selectedEntity);
            SkillsTimer skillsTimer = EntityManager.GetComponentData<SkillsTimer>(selectedEntity);
            if (!attribute.Equals(lastSelectAttribute) || !attributeBase.Equals(lastSelectAttributeBase) || !skillsTimer.Equals(lastSelectSkillsTimer))
            {
                if (!UIMgr.IsActive("AttributePanel")) UIMgr.OpenUI("AttributePanel", attribute, attributeBase, skillsTimer, campType);
                EntityEventSystem.Instance.OnChosenCharacterUpdate(attribute, attributeBase, skillsTimer, campType);
                lastSelectAttribute = attribute;
                lastSelectAttributeBase = attributeBase;
                lastSelectSkillsTimer = skillsTimer;
            }
            entityArray.Dispose();
        }
        else
        {
            if (UIMgr.IsActive("AttributePanel")) UIMgr.CloseUI("AttributePanel");
            lastSelectAttribute = default(Attribute);
            lastSelectAttributeBase = default(AttributeBase);
            lastSelectSkillsTimer = default(SkillsTimer);
        }
    }
    private int _timerUI;
    private int _timerId;
    private void _UpdateGameInfoUI()
    {
        EntityEventSystem.Instance.OnGameInfoUpdate(_allyInfo, _enemyInfo);
    }
    private void _CheckGameOverTimer()
    {
        costTime++;

        #region PVP模式
        if (!_enemyInfo.ContainsKey(CharacterType.防御塔))
        {
            PlayerInfo info = GameController._playerInfo;
            info.ThisSecond = costTime;
            bool isBreakRecord = false;
            if (info.ShortestSecond == 0 || info.ThisSecond < info.ShortestSecond)
            {
                //破纪录
                info.ShortestSecond = costTime;
                isBreakRecord = true;
            }
            EntityEventSystem.Instance.OnGameStateChanged(GameState.结束, true, isBreakRecord);
            TimerMgr.Instance.RemoveTimer(_timerId);
        }
        else if (!_allyInfo.ContainsKey(CharacterType.防御塔))
        {
            EntityEventSystem.Instance.OnGameStateChanged(GameState.结束, false, false);
            TimerMgr.Instance.RemoveTimer(_timerId);
        }
        #endregion

        #region 大乱斗模式
        //EntityQuery eqe = GetEntityQuery(enemyQueryDesc);
        //EntityQuery eqa = GetEntityQuery(allyQueryDesc);
        //if (eqe.CalculateEntityCount() == 0)
        //{
        //    #region 计算回收金币
        //    int gold = 0;
        //    var costList = GoldCostCfgMgr.GetConfigList();
        //    NativeArray<Attribute> attributeArray = eqa.ToComponentDataArray<Attribute>(Allocator.TempJob);
        //    foreach (var attribute in attributeArray)
        //    {
        //        foreach (var costCfg in costList)
        //        {
        //            if (costCfg.CharacterType == attribute.characterType)
        //            {
        //                gold += costCfg.Cost / 2;
        //                break;
        //            }
        //        }
        //    }
        //    attributeArray.Dispose();
        //    Debug.Log("战斗结束,回收友军金币数量: " + gold);
        //    #endregion

        //    PlayerInfo info = GameController._playerInfo;
        //    info.Gold += gold;
        //    info.ThisSecond = costTime;
        //    bool isBreakRecord = false;
        //    if (info.ShortestSecond == 0 || info.ThisSecond < info.ShortestSecond)
        //    {
        //        //破纪录
        //        info.ShortestSecond = costTime;
        //        isBreakRecord = true;
        //    }
        //    EntityEventSystem.Instance.OnGameStateChanged(GameState.结束, true, isBreakRecord);
        //    TimerMgr.Instance.RemoveTimer(_timerId);
        //}
        #endregion
    }
}
