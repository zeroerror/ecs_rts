using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ZeroHero;

public struct ReleaseSkill : IComponentData
{
    public int skillID;
    public SkillType skillType;
    public Entity releaser;
    public Entity target;
}
public enum SkillType
{
    小技能 = KeyCode.Q,
    大技能 = KeyCode.R,
}
/// <summary>
/// 负责处理 技能释放 后
/// </summary>
public class SkillReleaseHandleSystem : SystemBaseUnity
{
    private EntityQuery entityQuery;
    private EntityQueryDesc releaseSkillDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
    }
    protected override void OnInit()
    {
        base.OnInit();

        releaseSkillDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<ReleaseSkill>(),
            }
        };
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
        #region [清理人物身上挂载的技能释放状态]

        Entities.WithAll<Attribute>().WithAll<ReleaseSkill>().ForEach((Entity entity) =>
        {
            commandBuffer.RemoveComponent<ReleaseSkill>(entity);
        }).Run();
        #endregion

        #region [对技能释放"事件"处理]
        NativeList<Entity> entityList = new NativeList<Entity>(Allocator.TempJob);
        NativeList<Attribute> attributeList = new NativeList<Attribute>(Allocator.TempJob);
        Entities.WithNone<Attribute>().ForEach((Entity entity, ref ReleaseSkill releaseSkill) =>
        {
            Entity releaser = releaseSkill.releaser;
            Entity target = releaseSkill.target;
            SkillCfg skillCfg = SkillCfgMgr.GetBySkillID(releaseSkill.skillID);
            //1.技能配置存在 2.释放角色仍存活 3.存在技能CD计时器
            if (skillCfg != null && EntityManager.HasComponent<Attribute>(releaser) && EntityManager.HasComponent<SkillsTimer>(releaser))
            {
                AttributeBase attributeBase = EntityManager.GetComponentData<AttributeBase>(releaser);
                Attribute attribute = EntityManager.GetComponentData<Attribute>(releaser);
                SkillsTimer skillsTimer = EntityManager.GetComponentData<SkillsTimer>(releaser);
                SkillType skillType = releaseSkill.skillType;
                SkillObject skillObject = ResourceMgr.LoadSkillObject(releaseSkill.skillID);
                if (attribute.energy >= skillCfg.CostEnergy)//能量足够
                {
                    float cd = float.MaxValue;
                    float timer = 0;
                    if (skillType == SkillType.小技能)
                    {
                        cd = attribute.smallSkillCD;
                        timer = skillsTimer.smallSkillTimer;
                    }
                    else if (skillType == SkillType.大技能)
                    {
                        cd = attribute.bigSkillCD;
                        timer = skillsTimer.bigSkillTimer;
                    }
                    cd *= (1 - attribute.cdShrink);
                    if (timer >= cd)//cd冷却
                    {
                        //设置Buff
                        Buff buff = new Buff();
                        buff.buffID = releaseSkill.skillID;
                        buff.totalTime = skillObject.totalTime;
                        buff.targetEntity = target;
                        buff.health = skillObject.health;
                        buff.health_BuffInfo = skillObject.healthBuffInfo.ToBuffInfo();
                        buff.atkSpeed = skillObject.atkSpeed;
                        buff.atkSpeed_BuffInfo = skillObject.atkSpeedBuffInfo.ToBuffInfo();
                        buff.moveSpeed = skillObject.moveSpeed;
                        buff.moveSpeed_BuffInfo = skillObject.moveSpeedBuffInfo.ToBuffInfo();
                        buff.cdShrink = skillObject.cdShrink;
                        buff.cdShrink_BuffInfo = skillObject.cdShrinkBuffInfo.ToBuffInfo();
                        Entity buffEntity = commandBuffer.CreateEntity(EntityEventSystem.Instance.buffArchetype);
                        commandBuffer.SetComponent<Buff>(buffEntity, buff);
                        //扣除能量任务添加
                        attribute.energy -= skillCfg.CostEnergy;
                        entityList.Add(releaser);
                        attributeList.Add(attribute);
                        //添加释放技能组件==》为了实现EntityGameObject组件内对动画状态机操作的判断
                        commandBuffer.AddComponent<ReleaseSkill>(releaser, new ReleaseSkill { skillType = skillType });
                        Debug.Log(releaser + "======>释放技能：" + skillType + "  =====>释放目标: " + target);
                    }
                }
            }

            commandBuffer.DestroyEntity(entity);
        }).WithoutBurst().Run();
        for (int i = 0; i < entityList.Length; i++)
        {
            Entity entity = entityList[i];
            Attribute attribute = attributeList[i];
            EntityManager.SetComponentData<Attribute>(entity, attribute);
        }
        entityList.Dispose();
        attributeList.Dispose();
        #endregion
    }
}

public class SkillReleaseSystem : SystemBaseUnity
{
    private EntityQuery entityQuery;
    private EntityQueryDesc selectedEntityDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        selectedEntityDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<AllySelected>(),
            }
        };
    }
    protected override void OnInit()
    {
        base.OnInit();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        InputMgr.Instance.ReleaseSmallSkill += _ReleaseSmallSkill;
        InputMgr.Instance.ReleaseBigSkill += _ReleaseBigSkill;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        InputMgr.Instance.ReleaseSmallSkill -= _ReleaseSmallSkill;
        InputMgr.Instance.ReleaseBigSkill -= _ReleaseBigSkill;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
    private void _ReleaseSmallSkill(object[] args)
    {
        //Debug.Log("Try _ReleaseSmallSkill");
        _ReleaseSkill(SkillType.小技能);
    }
    private void _ReleaseBigSkill(object[] args)
    {
        //Debug.Log("Try _ReleaseBigSkill");
        _ReleaseSkill(SkillType.大技能);
    }

    private void _ReleaseSkill(SkillType skillType)
    {
        entityQuery = GetEntityQuery(selectedEntityDesc);
        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
        if (entityQuery.CalculateEntityCount() == 1)//只允许单选英雄情况下释放技能
        {
            Entity selectedEntity = entityArray[0];
            AttributeBase attributeBase = EntityManager.GetComponentData<AttributeBase>(selectedEntity);
            if (attributeBase.isHero)
            {
                RoleCfg roleCfg = RoleCfgMgr.GetByCharacterType(attributeBase.characterType);
                int skillID = (skillType == SkillType.小技能) ? roleCfg.SmallSkillID : roleCfg.BigSkillID;
                EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
                ReleaseSkill releaseSkill = new ReleaseSkill();
                releaseSkill.releaser = selectedEntity;
                releaseSkill.target = selectedEntity;
                releaseSkill.skillID = skillID;
                releaseSkill.skillType = skillType;
                Entity entity = commandBuffer.CreateEntity(EntityEventSystem.Instance.releaseSkillArchetype);
                commandBuffer.SetComponent<ReleaseSkill>(entity, releaseSkill);
            }
        }
        entityArray.Dispose();
    }
}