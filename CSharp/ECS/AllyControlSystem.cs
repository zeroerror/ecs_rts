using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ZeroHero;

[UpdateAfter(typeof(FindTargetSystem))]
public class AllyControlSystem : SystemBaseUnity
{
    private EntityCommandBufferSystem commandBufferSystem;
    private float3 mouseStartPosition;
    private float3 lowerLeftPosition;
    private float3 upperRightPosition;
    private EntityQuery memberDieEventQuery;
    private EntityQueryDesc memberDieEventQueryDesc;
    private NativeArray<MemberDieEvent> memberDieEventArray;
    public static NativeMultiHashMap<int, MemberInfo> teamMulHashMap;

    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        commandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        memberDieEventQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<MemberDieEvent>(),
            }
        };
        teamMulHashMap = new NativeMultiHashMap<int, MemberInfo>(11, Allocator.Persistent);
    }
    protected override void OnInit()
    {
        base.OnInit();
    }
    protected override void OnEnable()
    {
        if (InputMgr.Instance)
        {
            InputMgr.Instance.DoubleClickDown += _DoubleClickDown;
            InputMgr.Instance.OneClickDown += _OneClickDown;
            InputMgr.Instance.OneClickUp += _OneClickUp;
            InputMgr.Instance.SetTeam += _OnSetTeam;
            InputMgr.Instance.ChoseTeam += _OnChoseTeam;
        }

        base.OnEnable();
    }
    protected override void OnDisable()
    {
        if (InputMgr.Instance)
        {
            InputMgr.Instance.OneClickDown -= _OneClickDown;
            InputMgr.Instance.OneClickUp -= _OneClickUp;
            InputMgr.Instance.DoubleClickDown -= _DoubleClickDown;
        }

        base.OnDisable();
    }
    protected override void OnDestroy()
    {
        teamMulHashMap.Dispose();
        base.OnDestroy();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        memberDieEventQuery = GetEntityQuery(memberDieEventQueryDesc);
        memberDieEventArray = memberDieEventQuery.ToComponentDataArray<MemberDieEvent>(Allocator.Temp);
        if (memberDieEventArray.Length > 0)
        {
            for (int i = 0; i <= 10; i++)
            {
                UpdateTeamPosition(i);
            }
        }
        EntityManager.DestroyEntity(memberDieEventQuery);
        memberDieEventArray.Dispose();
    }

    #region [Method]
    private void UpdateTeamPosition(int key)//尝试获取人员死亡名单并重新填充队伍位置
    {
        NativeList<int> deadPosList = new NativeList<int>(Allocator.Temp);
        NativeList<Entity> deadList = new NativeList<Entity>(Allocator.Temp);
        //获取死亡名单中属于key编队信息的人员
        if (teamMulHashMap.TryGetFirstValue(key, out MemberInfo info, out NativeMultiHashMapIterator<int> iterator))
        {
            do
            {
                for (int i = 0; i < memberDieEventArray.Length; i++)
                {
                    MemberDieEvent mde = memberDieEventArray[i];
                    if (mde.entity.Equals(info.entity))
                    {
                        deadPosList.Add(info.posIndex);
                        deadList.Add(info.entity);
                        //Debug.Log(UnityEngine.Time.frameCount + " 队伍" + key + "  位置：" + info.posIndex + " 死亡");
                    }
                }
            } while (teamMulHashMap.TryGetNextValue(out info, ref iterator));
            //if (deadPosList.Length == teamMulHashMap.CountValuesForKey(key) && key != 10) Debug.Log("队伍 " + key + "  全军覆没！");
        }
        //队伍重新编队
        if (teamMulHashMap.TryGetFirstValue(key, out info, out iterator))
        {
            NativeList<MemberInfo> tempInfo = new NativeList<MemberInfo>(Allocator.Temp);
            do
            {
                if (!deadList.Contains(info.entity))//非死者本人
                {
                    int offset = 0;
                    for (int i = 0; i < deadList.Length; i++)
                    {
                        if (info.posIndex > deadPosList[i]) offset++;
                    }
                    info.posIndex -= offset;
                    tempInfo.Add(info);
                }
            } while (teamMulHashMap.TryGetNextValue(out info, ref iterator));
            teamMulHashMap.Remove(key);
            for (int i = 0; i < tempInfo.Length; i++)
            {
                teamMulHashMap.Add(key, tempInfo[i]);
            }
            tempInfo.Dispose();
            deadPosList.Dispose();
            deadList.Dispose();
        }
    }
    private void SelectAlly(CharacterType type)
    {
        SelectAlly(0, 0, false, type);
    }
    private void SelectAlly(float3 lowerLeftPosition, float3 upperRightPosition, bool isSingleSelected)
    {
        SelectAlly(lowerLeftPosition, upperRightPosition, isSingleSelected, default(CharacterType));
    }
    private void SelectAlly(float3 lowerLeftPosition, float3 upperRightPosition, bool isSingleSelected, CharacterType type)//选中范围内友军
    {
        //把上次储存的即时队伍信息移除，重新编排即时队伍
        teamMulHashMap.Remove(10);

        #region 重新选取队伍人员
        int curSelectedNum = 0;
        NativeList<int> oldPosIndexList = new NativeList<int>(Allocator.Temp);
        NativeList<Entity> entityList = new NativeList<Entity>(Allocator.Temp);
        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
        Entity clickEntity = Entity.Null;
        clickEntity = Common.GetClickEntity();
        if (clickEntity != Entity.Null && isSingleSelected && !EntityManager.HasComponent<AllyComponent>(clickEntity)) return;
        Entities.WithAll<AllyComponent>().ForEach((Entity entity, Attribute attribute) =>
        {
            float3 entityPosition = EntityManager.GetComponentData<Translation>(entity).Value;
            bool isChosen = false;
            if (lowerLeftPosition.Equals(upperRightPosition) && !isSingleSelected)
            {
                //单选双击的情况选中所有同类型兵种
                isChosen = (attribute.characterType == type);
            }
            else if (isSingleSelected)
            {
                //单选一个单位
                isChosen = clickEntity.Equals(entity);
            }
            else
            {
                isChosen = IsInSelectedArea(entityPosition);
            }

            FinalGoal finalGoal = EntityManager.GetComponentData<FinalGoal>(entity);
            bool hasGoal = !finalGoal.place_1.Equals(float3.zero) || !finalGoal.place_2.Equals(float3.zero) || !finalGoal.place_3.Equals(float3.zero) || !finalGoal.place_4.Equals(float3.zero);
            if (!hasGoal && attribute.characterType != CharacterType.防御塔)          //对于拥有自身攻击路线目标的小兵不进行选取操作
            {
                if (EntityManager.HasComponent<AllySelected>(entity) && isChosen)//选中的老兵
                {
                    AllySelected allySelected = EntityManager.GetComponentData<AllySelected>(entity);
                    oldPosIndexList.Add(allySelected.posIndex);
                    entityList.Add(entity);
                    curSelectedNum++;
                }
                else if (isChosen)//选中的新兵
                {
                    entityList.Add(entity);
                    curSelectedNum++;
                }
                else if (EntityManager.HasComponent<AllySelected>(entity))//未选中的老兵
                {
                    commandBuffer.RemoveComponent<AllySelected>(entity);
                }
                if (isSingleSelected && isChosen)
                {
                    commandBuffer.AddComponent<AllySelected>(entity, new AllySelected { teamIndex = 10, posIndex = 0 });
                    teamMulHashMap.Add(10, new MemberInfo { entity = entity, posIndex = 0 });
                }
            }

        }).WithoutBurst().Run();
        #endregion
        #region 重新编制队伍
        int curIndex = 0;
        NativeList<int> newPosIndexList = new NativeList<int>(Allocator.Temp);
        for (int i = 0; i < curSelectedNum; i++)
        {
            if (!oldPosIndexList.Contains(i)) newPosIndexList.Add(i);
        }
        for (int i = 0; i < entityList.Length; i++)
        {
            Entity entity = entityList[i];
            if (EntityManager.HasComponent<AllySelected>(entity))
            {
                AllySelected allySelected = EntityManager.GetComponentData<AllySelected>(entity);
                if (allySelected.posIndex >= entityList.Length)
                {
                    //(新队伍成员比原来少)老成员重新编制
                    int posIndex = newPosIndexList[curIndex];
                    curIndex++;
                    commandBuffer.AddComponent<AllySelected>(entity, new AllySelected { teamIndex = 10, posIndex = posIndex });
                }
                else
                {
                    teamMulHashMap.Add(10, new MemberInfo { entity = entity, posIndex = allySelected.posIndex });
                }
            }
            else
            {
                //新人入队
                int posIndex = newPosIndexList[curIndex];
                curIndex++;
                commandBuffer.AddComponent<AllySelected>(entity, new AllySelected { teamIndex = 10, posIndex = posIndex });
                teamMulHashMap.Add(10, new MemberInfo { entity = entity, posIndex = posIndex });
            }
            if (isSingleSelected) break;
        }
        #endregion

        oldPosIndexList.Dispose();
        newPosIndexList.Dispose();
        entityList.Dispose();
    }
    private void _OnSetTeam(params object[] args)//设置编队
    {
        int teamIndex = (int)args[0];
        if (teamIndex != -1)
        {
            teamMulHashMap.Remove(10);
            if (teamMulHashMap.TryGetFirstValue(teamIndex, out MemberInfo info, out NativeMultiHashMapIterator<int> iterator))
            {
                Debug.Log("覆盖原有编队" + teamIndex);
                teamMulHashMap.Remove(teamIndex);
            }
            else
            {
                Debug.Log("保存新编队" + teamIndex);
            }
            int count = 0;
            Entities.ForEach((Entity entity, ref AllySelected allySelected) =>
            {
                teamMulHashMap.Add(teamIndex, new MemberInfo { entity = entity, posIndex = allySelected.posIndex });
                allySelected.teamIndex = teamIndex;
                count++;
            }).WithoutBurst().Run();
            if (count != 0)
            {
                EntityEventSystem.Instance.OnSetTeamSuccess(teamIndex);
            }
        }
    }
    private void _OnChoseTeam(params object[] args)//选择编队
    {
        int teamIndex = (int)args[0];
        if (teamIndex != -1)
        {
            NativeList<MemberInfo> infoList = new NativeList<MemberInfo>(Allocator.Temp);
            if (teamMulHashMap.TryGetFirstValue(teamIndex, out MemberInfo info, out NativeMultiHashMapIterator<int> iterator))
            {
                Debug.Log("切换至队伍" + teamIndex);
                do
                {
                    infoList.Add(info);
                } while (teamMulHashMap.TryGetNextValue(out info, ref iterator));
            }
            if (infoList.Length > 0)
            {
                EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
                Entities.WithAll<AllyComponent>().ForEach((Entity entity) =>
                {
                    bool isInCurTeam = false;
                    int posIndex = -1;
                    for (int i = 0; i < infoList.Length; i++)//判断当前选中的友军是否在指定队伍内
                    {
                        if (infoList[i].entity == entity)
                        {
                            isInCurTeam = true;
                            posIndex = infoList[i].posIndex;
                            break;
                        }
                    }
                    if (isInCurTeam)
                    {
                        commandBuffer.AddComponent<AllySelected>(entity, new AllySelected { teamIndex = teamIndex, posIndex = posIndex });
                    }
                    else if (EntityManager.HasComponent<AllySelected>(entity))
                    {
                        commandBuffer.RemoveComponent<AllySelected>(entity);
                    }
                }).WithoutBurst().Run();
            }

        }
    }
    private bool IsInSelectedArea(float3 entityPosition)
    {
        if (entityPosition.x >= lowerLeftPosition.x &&
            entityPosition.z >= lowerLeftPosition.z &&
            entityPosition.x <= upperRightPosition.x &&
            entityPosition.z <= upperRightPosition.z)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region [监听事件]
    private void _OneClickDown(params object[] args)
    {
        mouseStartPosition = Input.mousePosition;
    }
    private void _OneClickUp(params object[] args)
    {
        float3 mouseEndPosition = Input.mousePosition;
        bool isSingleSelected = false;
        if (!mouseStartPosition.Equals(mouseEndPosition))
        {
            mouseStartPosition = Common.GetScreenPointWorldPosition(mouseStartPosition);
            mouseEndPosition = Common.GetScreenPointWorldPosition(mouseEndPosition);
            lowerLeftPosition = new float3(math.min(mouseStartPosition.x, mouseEndPosition.x), 0, math.min(mouseStartPosition.z, mouseEndPosition.z));
            upperRightPosition = new float3(math.max(mouseStartPosition.x, mouseEndPosition.x), 0, math.max(mouseStartPosition.z, mouseEndPosition.z));
        }
        else
        {
            isSingleSelected = true;
        }
        SelectAlly(lowerLeftPosition, upperRightPosition, isSingleSelected);

        if (isSingleSelected)
        {
            Entity entity = Common.GetClickEntity();
            CampType campType = EntityManager.HasComponent<AllyComponent>(entity) ? CampType.友军 : CampType.敌军;
            if (entity != Entity.Null && EntityManager.HasComponent<AttributeBase>(entity))
            {
                AttributeBase attributeBase = EntityManager.GetComponentData<AttributeBase>(entity);
                Attribute attribute = EntityManager.GetComponentData<Attribute>(entity);
                SkillsTimer skillsTimer = EntityManager.GetComponentData<SkillsTimer>(entity);
                EntityEventSystem.Instance.OnClickCharacter(attribute, attributeBase, skillsTimer, campType);
            }
            else
            {
                EntityEventSystem.Instance.OnClickCharacter();
            }
        }
    }
    private void _DoubleClickDown(params object[] args)
    {
        Entity clickEntity = Common.GetClickEntity();
        if (clickEntity != Entity.Null && EntityManager.HasComponent<AllyComponent>(clickEntity))
        {
            Attribute attribute = EntityManager.GetComponentData<Attribute>(clickEntity);
            SelectAlly(attribute.characterType);
        }
    }
    #endregion

}



[UpdateAfter(typeof(AllyControlSystem))]
public class AllyCommandSystem : SystemBaseUnity
{
    private EntityCommandBufferSystem commandBufferSystem;
    private EntityQueryDesc allyDesc;
    protected override void OnCreate()
    {
        base.OnCreate();
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        allyDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<AllySelected>(),
            }
        };
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        InputMgr.Instance.PointerDown1 -= _SetTarget;
        InputMgr.Instance.BombTargetPos -= _OnBombTargetPos;

        InputMgr.Instance.PointerDown1 += _SetTarget;
        InputMgr.Instance.BombTargetPos += _OnBombTargetPos;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        InputMgr.Instance.PointerDown1 -= _SetTarget;
        InputMgr.Instance.BombTargetPos -= _OnBombTargetPos;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
    /// <summary>
    /// 范围类攻击可手动指定地点
    /// </summary>
    private void _OnBombTargetPos(params object[] args)
    {
        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();
        float3 clickWorldPos = Common.GetMouseWorldPosition();
        Entity clickEntity = Common.GetClickEntity();
        if (clickEntity != Entity.Null && EntityManager.HasComponent<Translation>(clickEntity))
            clickWorldPos = EntityManager.GetComponentData<Translation>(clickEntity).Value;

        Entities.WithAll<AllySelected>().ForEach((Entity entity, SkillsTimer skillsTimer, Translation translation, Attribute attribute, Rotation rotation) =>
        {
            if (attribute.damageType == DamageType.范围 && skillsTimer.atkTimer >= 1f / attribute.atkSpeed && math.distance(translation.Value, clickWorldPos) <= attribute.atkRange)
            {
                Entity bulletEntity = commandBuffer.Instantiate(attribute.bulletEntity);
                CampType campType = CampType.中立;
                if (EntityManager.HasComponent<AllyComponent>(entity))
                {
                    campType = CampType.友军;
                }
                else if (EntityManager.HasComponent<EnemyComponent>(entity))
                {
                    campType = CampType.敌军;
                }

                float3 startPos = GetComponentDataFromEntity<LocalToWorld>(true)[attribute.atkPoint].Position;
                Vector3 direction = math.normalizesafe(clickWorldPos - startPos);
                commandBuffer.SetComponent<Translation>(bulletEntity, new Translation { Value = startPos });
                commandBuffer.AddComponent<BulletComponent>(bulletEntity, new BulletComponent { campType = campType, bulletType = attribute.bulletType, friendlyFire = attribute.friendlyFire, damage = attribute.atkStrength, range = attribute.damageRange, initSpeed = attribute.bulletInitSpeed });
                commandBuffer.AddComponent<MoveComponent>(bulletEntity, new MoveComponent { speed = attribute.bulletSpeed, targetPos = clickWorldPos });
                commandBuffer.AddComponent<AttackComponent>(entity, new AttackComponent { });
                commandBuffer.SetComponent<PathFollow>(entity, new PathFollow { pathIndex = -1 });
                if (!direction.Equals(Vector3.zero))
                {
                    Quaternion toQuaternion = Quaternion.LookRotation(direction);
                    commandBuffer.SetComponent<Rotation>(entity, new Rotation { Value = Quaternion.Lerp(rotation.Value, toQuaternion, 1f) });
                }
            }
        }).WithoutBurst().Run();
    }
    private void _SetTarget(params object[] args)
    {
        GameObject rayCastObj = Common.GetSceneObjectByRay();
        if (rayCastObj.layer == LayerMask.NameToLayer("Building"))
        {
            return;
        }
        int entityCount = GetEntityQuery(allyDesc).CalculateEntityCount();
        #region 点击处存在敌人，设置为目标 
        Entity targetEntity = Common.GetClickEntity();
        if (EntityManager.HasComponent<QuadrantEntity>(targetEntity))
        {
            QuadrantEntity quadrantEntity = EntityManager.GetComponentData<QuadrantEntity>(targetEntity);
            if (quadrantEntity.typeEnum == EntityType.Unit) targetEntity = Entity.Null;
        }
        else
        {
            targetEntity = Entity.Null;
        }
        float3 clickWorldPos = Common.GetMouseWorldPosition();
        #endregion

        EntityQuery entityQuery = GetEntityQuery(allyDesc);
        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
        MapSystem.GetMultiplePostionArray(clickWorldPos, entityCount, out NativeList<int2> endPositionList);
        if (endPositionList.Length > 0)
        {
            for (int i = 0; i < entityArray.Length; i++)
            {
                Entity entity = entityArray[i];
                Translation translation = EntityManager.GetComponentData<Translation>(entity);
                AllySelected allySelected = EntityManager.GetComponentData<AllySelected>(entity);
                if (targetEntity != Entity.Null)
                {
                    EntityManager.AddComponentData<HasTarget>(entity, new HasTarget { targetEntity = targetEntity });
                    EntityManager.AddComponentData<HasCommand>(entity, new HasCommand { });
                }
                else
                {
                    EntityManager.RemoveComponent<HasTarget>(entity);
                }
                //添加寻路组件
                int2 startPosition;
                float3 entityPosition = translation.Value;
                int posIndex = -1;
                NativeMultiHashMap<int, MemberInfo> teamMulHashMap = AllyControlSystem.teamMulHashMap;
                if (teamMulHashMap.TryGetFirstValue(allySelected.teamIndex, out MemberInfo info, out NativeMultiHashMapIterator<int> iterator))
                {
                    do
                    {
                        if (info.entity.Equals(entity) && !entity.Equals(Entity.Null))
                        {
                            posIndex = info.posIndex;
                            break;
                        }
                    } while (teamMulHashMap.TryGetNextValue(out info, ref iterator));

                    int2 endPosition = endPositionList[posIndex];
                    Common.GetXYFloor(entityPosition, out startPosition.x, out startPosition.y);
                    allySelected.posIndex = posIndex;
                    EntityManager.SetComponentData<AllySelected>(entity, allySelected);
                    EntityManager.AddComponentData<PathfindingParams>(entity, new PathfindingParams { startPosition = startPosition, endPosition = endPosition });
                    EntityManager.AddComponentData<HasCommand>(entity, new HasCommand { });
                    EntityManager.RemoveComponent<AttackComponent>(entity);
                }
            }
        }
        entityArray.Dispose();
        endPositionList.Dispose();
    }
}



public struct AllySelected : IComponentData
{
    public int teamIndex;
    public int posIndex;
}
public struct HasCommand : IComponentData { }
public struct MemberDieEvent : IComponentData
{
    public Entity entity;
}
public struct MemberInfo
{
    public Entity entity;
    public int posIndex;
}