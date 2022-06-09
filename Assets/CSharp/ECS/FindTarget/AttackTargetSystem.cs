using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using ZeroHero;

public class AttackTargetSystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    private EntityQuery entityQuery;
    private EntityQueryDesc entityQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private int batchesPerChunk = 1;
    private static GridBasicInfo info;  //网格地图基本信息
    public struct RoleCfg
    {
        public CharacterType characterType;
        public BulletType bulletType;
    }
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<HasTarget>(),
                ComponentType.ReadOnly<SkillsTimer>(),
             },
            None = new ComponentType[]
            {

            }
        };
    }
    protected override void OnInit()
    {
        base.OnInit();
        info = GridSet.Instance.GetChosenGridInfo();
        isInit = true;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        entityQuery = this.GetEntityQuery(entityQueryDesc);
        var job = new AttackTargetJob();
        job.SkillsTimerAccessor = GetArchetypeChunkComponentType<SkillsTimer>(false);
        job.AttributeAccessor = GetArchetypeChunkComponentType<Attribute>(true);
        job.EntityAccessor = GetArchetypeChunkEntityType();
        job.TranslationAccessor = GetArchetypeChunkComponentType<Translation>(true);
        job.RotationTypeAccessor = GetArchetypeChunkComponentType<Rotation>(true);
        job.HasTargetTypeAccessor = GetArchetypeChunkComponentType<HasTarget>(true);
        job.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);
        job.LocalToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>(true);
        job.AllySelectedFromEntity = GetComponentDataFromEntity<AllySelected>(true);
        job.AllyComponentFromEntity = GetComponentDataFromEntity<AllyComponent>(true);
        job.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
        job.StaticObstacleFromEntity = GetComponentDataFromEntity<StaticObstacle>(true);
        job.AttackComponentFromEntity = GetComponentDataFromEntity<AttackComponent>(true);
        job.ToAttackablePlaceFromEntity = GetComponentDataFromEntity<ToAttackablePlace>(true);
        job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job.deltaTime = Time.DeltaTime;
        job.mapNodeArray = MapSystem.mapNodeArray;
        job.info = info;
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, batchesPerChunk, this.Dependency);
        Dependency.Complete();
    }

    [BurstCompile]
    private struct AttackTargetJob : IJobEntityBatch
    {
        public ArchetypeChunkComponentType<SkillsTimer> SkillsTimerAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Rotation> RotationTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<HasTarget> HasTargetTypeAccessor;
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<LocalToWorld> LocalToWorldFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllySelected> AllySelectedFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<StaticObstacle> StaticObstacleFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<ToAttackablePlace> ToAttackablePlaceFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AttackComponent> AttackComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllyComponent> AllyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;

        public EntityCommandBuffer.Concurrent commandBuffer;
        public NativeArray<MapNode> mapNodeArray;
        public GridBasicInfo info;
        public float deltaTime;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<SkillsTimer> skillsTimerArray = batchInChunk.GetNativeArray(SkillsTimerAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray(AttributeAccessor);
            NativeArray<Translation> translationArray = batchInChunk.GetNativeArray(TranslationAccessor);
            NativeArray<Rotation> rotationArray = batchInChunk.GetNativeArray(RotationTypeAccessor);
            NativeArray<HasTarget> hasTargetArray = batchInChunk.GetNativeArray(HasTargetTypeAccessor);
            NativeArray<Entity> entityArray = batchInChunk.GetNativeArray(EntityAccessor);
            for (int i = 0; i < hasTargetArray.Length; i++)
            {
                SkillsTimer skillsTimer = skillsTimerArray[i];
                Attribute attribute = attributeArray[i];
                Translation trans = translationArray[i];
                HasTarget target = hasTargetArray[i];
                Entity selfEntity = entityArray[i];
                if (!TranslationFromEntity.Exists(target.targetEntity))
                {
                    //攻击目标已不存在
                    commandBuffer.RemoveComponent<HasCommand>(batchIndex, selfEntity);
                    commandBuffer.RemoveComponent<HasTarget>(batchIndex, selfEntity);
                    commandBuffer.RemoveComponent<ToAttackablePlace>(batchIndex, selfEntity);//移除移动攻击命令
                    commandBuffer.SetComponent<PathFollow>(batchIndex, selfEntity, new PathFollow { pathIndex = -1 });//停下
                    continue;
                }

                float3 targetPos = TranslationFromEntity[target.targetEntity].Value;
                float distance = math.distance(trans.Value, targetPos);
                if (distance <= attribute.atkRange)
                {
                    float3 selfPos = trans.Value;
                    float3 endPoint = targetPos;

                    Vector3 direction = math.normalizesafe(endPoint - selfPos);

                    if (skillsTimer.atkTimer >= 1f / attribute.atkSpeed)
                    {
                        switch (attribute.roleType)
                        {
                            case RoleType.远程:
                                float3 startPoint = LocalToWorldFromEntity[attribute.atkPoint].Position;
                                int2 startPos, endPos;
                                GetXYFloor(startPoint, out startPos.x, out startPos.y);
                                GetXYFloor(endPoint, out endPos.x, out endPos.y);
                                if (!IsPassable(startPos, endPos))
                                {
                                    commandBuffer.AddComponent<ToAttackablePlace>(batchIndex, selfEntity, new ToAttackablePlace());
                                    break;
                                }
                                commandBuffer.RemoveComponent<ToAttackablePlace>(batchIndex, selfEntity);
                                CampType campType;
                                if (AllyComponentFromEntity.Exists(selfEntity))
                                {
                                    campType = CampType.友军;
                                }
                                else if (EnemyComponentFromEntity.Exists(selfEntity))
                                {
                                    campType = CampType.敌军;
                                }
                                else
                                {
                                    campType = CampType.中立;
                                }
                                BulletType bulletType = attribute.bulletType;
                                Entity bulletEntity = commandBuffer.Instantiate(batchIndex, attribute.bulletEntity);
                                commandBuffer.SetComponent<Translation>(batchIndex, bulletEntity, new Translation { Value = startPoint });
                                commandBuffer.AddComponent<BulletComponent>(batchIndex, bulletEntity, new BulletComponent { fromEntity = selfEntity, campType = campType, friendlyFire = attribute.friendlyFire, bulletType = bulletType, damage = attribute.atkStrength, range = attribute.damageRange, initSpeed = attribute.bulletInitSpeed });
                                commandBuffer.AddComponent<MoveComponent>(batchIndex, bulletEntity, new MoveComponent { speed = attribute.bulletSpeed, targetPos = endPoint });
                                if (attribute.damageType == DamageType.单体)
                                {
                                    commandBuffer.AddComponent<HasTarget>(batchIndex, bulletEntity, new HasTarget { targetEntity = target.targetEntity });
                                }

                                commandBuffer.AddComponent<AttackComponent>(batchIndex, selfEntity, new AttackComponent { });
                                if (!AllySelectedFromEntity.Exists(selfEntity))
                                {
                                    commandBuffer.SetComponent<PathFollow>(batchIndex, selfEntity, new PathFollow { pathIndex = -1 });
                                }

                                break;
                            case RoleType.近战:
                                if (AttackComponentFromEntity.Exists(selfEntity))
                                {
                                    AttackComponent attackComponent = AttackComponentFromEntity[selfEntity];
                                    attackComponent.delayTime = 0;
                                    commandBuffer.SetComponent<AttackComponent>(batchIndex, selfEntity, attackComponent);
                                    break;
                                }

                                commandBuffer.AddComponent<AttackComponent>(batchIndex, selfEntity, new AttackComponent
                                {
                                    atkEntity = selfEntity,
                                    targetEntity = target.targetEntity,
                                    damage = attribute.atkStrength,
                                    delayTime = 1f / attribute.atkSpeed / 2f,
                                    countTime = 0f
                                });
                                commandBuffer.SetComponent<PathFollow>(batchIndex, selfEntity, new PathFollow { pathIndex = -1 });
                                break;
                            default:
                                break;
                        }
                    }
                    direction.y = 0;
                    if (!direction.Equals(Vector3.zero))
                    {
                        Quaternion toQuaternion = Quaternion.LookRotation(direction);
                        Quaternion selfQuaternion = rotationArray[i].Value;
                        commandBuffer.SetComponent<Rotation>(batchIndex, selfEntity, new Rotation { Value = Quaternion.Lerp(selfQuaternion, toQuaternion, 1f) });
                    }
                }
                skillsTimerArray[i] = skillsTimer;
            }
        }
        #region 自定义方法
        public bool IsPassable(int2 startPosition, int2 endPosition)
        {
            return IsPointToPointCanWalk(startPosition, endPosition, true);
        }
        public bool IsPointToPointCanWalk(int2 startPosition, int2 endPosition, bool isMiddle)
        {
            //if (isMiddle && !IsCanReach(endPosition))
            //{
            //    return false;
            //}
            if (startPosition.Equals(endPosition)) return true;

            if (startPosition.x > endPosition.x)
            {
                int2 tempPos = startPosition;
                startPosition = endPosition;
                endPosition = tempPos;
            }
            if (startPosition.x == endPosition.x && startPosition.y > endPosition.y)
            {
                int2 tempPos = startPosition;
                startPosition = endPosition;
                endPosition = tempPos;
            }
            int x1, y1;
            int x2, y2;
            x1 = startPosition.x;
            y1 = startPosition.y;
            x2 = endPosition.x;
            y2 = endPosition.y;
            int A;
            int B;
            int2 currentPos = startPosition;
            if (x1 == x2 || y1 == y2)
            {
                if (x1 == x2)
                {
                    currentPos += new int2(0, 1);
                    while (!currentPos.Equals(endPosition))
                    {
                        if (!IsWalkable(currentPos))
                        {
                            //Debug.Log(frame + "    " + currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                            return false;
                        }
                        currentPos += new int2(0, 1);
                    }
                }
                else if (y1 == y2)
                {
                    currentPos += new int2(1, 0);
                    while (!currentPos.Equals(endPosition))
                    {
                        if (!IsWalkable(currentPos))
                        {
                            //Debug.Log(frame + "    " + currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                            return false;
                        }
                        currentPos += new int2(1, 0);
                    }
                }
            }
            else
            {
                A = y1 - y2;
                B = x2 - x1;
                int d = 0;
                int d1 = 0;
                int d2 = 0;
                int2 posAdd1 = new int2();
                int2 posAdd2 = new int2();
                float K;
                K = (float)-A / B;

                currentPos += new int2(0, K < 0 ? -1 : 0);//斜率为负时需要向下移动一格
                endPosition += new int2(0, K < 0 ? -1 : 0);
                if (isMiddle) endPosition += new int2(-1, K > 0 ? -1 : +1);   //如果是中间线检测，需要退一格
                if (!IsWalkable(currentPos))
                {
                    //Debug.Log(frame + "    " + currentPos + "不在网格内或不可走或已占用" + "  startPosition = " + startPosition + "  endPosition = " + endPosition);
                    return false;
                }

                if (Mathf.Abs(K) <= 1)
                {
                    posAdd1 = new int2(1, 0);
                    posAdd2 = new int2(1, K > 0 ? 1 : -1);

                    if (K > 0)
                    {
                        d = A + B;
                        d1 = A;
                        d2 = A + B;
                    }
                    else
                    {
                        d = A - B;
                        d1 = A;
                        d2 = A - B;
                    }
                }
                else
                {
                    //斜率大于1
                    posAdd1 = new int2(0, K > 0 ? 1 : -1);
                    posAdd2 = new int2(1, K > 0 ? 1 : -1);

                    if (K > 0)
                    {
                        d = A + B;
                        d1 = B;
                        d2 = A + B;
                    }
                    else
                    {
                        d = A - B;
                        d1 = -B;
                        d2 = A - B;
                    }
                }

                #region 开始遍历路经过的点
                while (!currentPos.Equals(endPosition))
                {
                    bool isXYBothChange;
                    if (Mathf.Abs(K) <= 1)
                    {
                        isXYBothChange = (K > 0 && d <= 0) || (K < 0 && d >= 0);
                    }
                    else
                    {
                        //斜率大于1
                        isXYBothChange = (K > 0 && d >= 0) || (K < 0 && d <= 0);
                    }

                    if (isXYBothChange)
                    {
                        currentPos += posAdd2;
                        d += d2;
                    }
                    else
                    {
                        currentPos += posAdd1;
                        d += d1;
                    }

                    if (currentPos.Equals(endPosition))
                    {
                        return true;
                    }

                    if (!IsWalkable(currentPos))
                    {
                        //Debug.Log("不在地图网格中");
                        return false;
                    }
                }
                if (currentPos.y != endPosition.y)
                {
                    //Debug.Log("currentPos.y != endPosition.y");
                    return false;
                }
                #endregion
            }
            return true;
        }

        private int CalculateIndex(int x, int y)
        {
            return x + y * info.width;
        }
        private bool IsInGrid(int2 position)
        {
            return position.x >= 0 && position.x < info.width && position.y >= 0 && position.y < info.height;
        }
        private bool IsWalkable(int2 pos)
        {
            int index = CalculateIndex(pos.x, pos.y);
            return IsInGrid(pos) && mapNodeArray[index].isWalkable;
        }
        private void GetXYFloor(float3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
        #endregion
    }
}
public struct AttackComponent : IComponentData
{
    public Entity atkEntity;//攻击者
    public Entity targetEntity;//被攻击目标
    public DamageType damageType;//伤害类型：单体、范围
    public float damage;
    public float delayTime;
    public float countTime;
}
public struct ToAttackablePlace : IComponentData { }//针对远程攻击应该被墙体挡住，需要走到指定可攻击地点
