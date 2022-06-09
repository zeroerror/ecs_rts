using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ZeroHero;
/// <summary>
/// 等级系统
/// </summary>
public class LevelUpSystem : SystemBaseUnity
{
    public static bool isInit;
    public static NativeList<KillRewardStruct> KillRewardList;
    public static NativeList<LevelStruct> LevelCfgList;
    public static NativeList<RoleAttributeStruct> RoleAttributeCfgList;
    private EntityQuery entityQuery;
    private EntityQueryDesc entityQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private static float3 effectOffset = new float3(0, 2f, 0);
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<AttributeBase>(),
             },
        };
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isInit)
        {
            KillRewardList.Dispose();
            LevelCfgList.Dispose();
            RoleAttributeCfgList.Dispose();
        }
    }
    protected override void OnInit()
    {
        base.OnInit();
        var list1 = KillRewardCfgMgr.GetConfigList();
        var list2 = LevelCfgMgr.GetConfigList();
        var list3 = RoleAttributeCfgMgr.GetConfigList();
        KillRewardList = new NativeList<KillRewardStruct>(Allocator.Persistent);
        LevelCfgList = new NativeList<LevelStruct>(Allocator.Persistent);
        RoleAttributeCfgList = new NativeList<RoleAttributeStruct>(Allocator.Persistent);
        foreach (var item in list1)
        {
            var krs = new KillRewardStruct();
            krs.CharacterType = item.CharacterType;
            krs.Gold = item.Gold;
            krs.Exp = item.Exp;
            KillRewardList.Add(krs);
        }
        foreach (var item in list2)
        {
            var ls = new LevelStruct();
            ls.Level = item.Level;
            ls.Exp = item.Exp;
            LevelCfgList.Add(ls);
        }
        foreach (var item in list3)
        {
            var ra = new RoleAttributeStruct();
            ra.ID = item.ID;
            ra.Health = item.Health;
            ra.AtkStrength = item.AtkStrength;
            ra.AtkSpeed = item.AtkSpeed;
            ra.MoveSpeed = item.MoveSpeed;
            RoleAttributeCfgList.Add(ra);
        }
        isInit = true;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Entities.ForEach((Entity entity, ref Attribute attribute, ref AttributeBase attributeBase, ref Translation translation) =>
        {
            if (attributeBase.isHero)
            {
                for (int index = 0; index < LevelCfgList.Length; index++)
                {
                    var cfg = LevelCfgList[index];
                    if (cfg.Level == attributeBase.level + 1 && attributeBase.exp >= cfg.Exp)
                    {
                        attributeBase.exp -= cfg.Exp;
                        attributeBase.level++;
                        int id = 1000 + attributeBase.level;
                        for (int index2 = 0; index2 < RoleAttributeCfgList.Length; index2++)
                        {
                            var attributeCfg = RoleAttributeCfgList[index];
                            if (attributeCfg.ID == id)
                            {
                                float offset = attributeCfg.Health - attributeBase.health;
                                offset = offset > 0 ? offset : 0;
                                attribute.health += offset;

                                offset = attributeCfg.AtkStrength - attributeBase.atkStrength;
                                offset = offset > 0 ? offset : 0;
                                attribute.atkStrength += offset;

                                offset = attributeCfg.AtkSpeed - attributeBase.atkSpeed;
                                offset = offset > 0 ? offset : 0;
                                attribute.atkSpeed += offset;

                                offset = attributeCfg.MoveSpeed - attributeBase.moveSpeed;
                                offset = offset > 0 ? offset : 0;
                                attribute.moveSpeed += offset;

                                attributeBase.health = attributeCfg.Health;
                                attributeBase.atkStrength = attributeCfg.AtkStrength;
                                attributeBase.atkSpeed = attributeCfg.AtkSpeed;
                                attributeBase.moveSpeed = attributeCfg.MoveSpeed;
                                break;
                            }
                        }
                        EntityManager.SetComponentData<Attribute>(entity, attribute);
                        EntityManager.SetComponentData<AttributeBase>(entity, attributeBase);
                        EffectMgr.Instance.PlayEffect("升级", translation.Value + effectOffset);
                        break;
                    }
                }
            }

        }).WithoutBurst().Run();

    }
}
