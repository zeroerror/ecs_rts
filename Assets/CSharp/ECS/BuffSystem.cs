using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using System;

[UpdateBefore(typeof(QuadrandSystem))]
public class BuffSystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    public static NativeMultiHashMap<int, Buff> entityBuffMap;
    public static NativeList<int> buffIdList;
    private EntityQuery buffQuery;
    private EntityQueryDesc buffDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private static int batchesPerChunk = 1;
    private NativeMultiHashMap<int, Buff> buffMap;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
    }
    protected override void OnInit()
    {
        base.OnInit();

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        buffDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(Buff),
             },
        };
        buffMap = new NativeMultiHashMap<int, Buff>(0, Allocator.Persistent);
        entityBuffMap = new NativeMultiHashMap<int, Buff>(0, Allocator.Persistent);
        buffIdList = new NativeList<int>(Allocator.Persistent);
        isInit = true;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isInit)
        {
            buffMap.Dispose();
            entityBuffMap.Dispose();
            buffIdList.Dispose();
        }
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();

        //ps:没有buff entity存在时 这个Update都不会执行 所以也就不会clear buffIdList  entityBuffMap
        buffQuery = this.GetEntityQuery(buffDesc);
        int count = buffQuery.CalculateEntityCount();
        #region [获取entityBuffMap]
        ///首先获取当前帧存在的 buff Entity 存储在buffMap
        if (buffMap.Capacity < count) buffMap.Capacity = count;
        if (entityBuffMap.Capacity < buffQuery.CalculateEntityCount()) entityBuffMap.Capacity = buffQuery.CalculateEntityCount();
        buffMap.Clear();
        var job = new BuffJob();
        job.AttributeFromEntity = GetComponentDataFromEntity<Attribute>(true);
        job.EntityAccessor = GetArchetypeChunkEntityType();
        job.BuffTypeAccessor = GetArchetypeChunkComponentType<Buff>();
        job.buffMap = buffMap.AsParallelWriter();
        job.commandBuffer = commandBuffer.ToConcurrent();
        job.deltaTime = Time.DeltaTime;

        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, buffQuery, batchesPerChunk, this.Dependency);
        Dependency.Complete();
        ///再获取所有buff的buffId存储在buffIdList
        NativeArray<int> keyArray = buffMap.GetKeyArray(Allocator.Temp);
        NativeMultiHashMapIterator<int> iterator1 = new NativeMultiHashMapIterator<int>();
        NativeMultiHashMapIterator<int> iterator2 = new NativeMultiHashMapIterator<int>();
        foreach (int key in keyArray)
        {
            if (!buffIdList.Contains(key)) buffIdList.Add(key);
        }
        ///对buffMap进行一些buff刷新覆盖等操作，并且存储到新的entityBuffMap
        foreach (int buffID in buffIdList)
        {
            if (buffMap.TryGetFirstValue(buffID, out Buff buff, out iterator1))
            {
                do
                {
                    if (!entityBuffMap.ContainsKey(buffID)) entityBuffMap.Add(buffID, buff);
                    else if (entityBuffMap.TryGetFirstValue(buffID, out Buff bf, out iterator2))
                    {
                        bool isExit = false;
                        do
                        {
                            if (buff.targetEntity == bf.targetEntity)
                            {
                                bf.timer = 0;
                                bf.atkRange_BuffInfo.timer = 0;
                                bf.atkSpeed_BuffInfo.timer = 0;
                                bf.atkStrength_BuffInfo.timer = 0;
                                bf.bulletInitSpeed_BuffInfo.timer = 0;
                                bf.bulletSpeed_BuffInfo.timer = 0;
                                bf.cdShrink_BuffInfo.timer = 0;
                                bf.damageRange_BuffInfo.timer = 0;
                                bf.health_BuffInfo.timer = 0;
                                bf.moveSpeed_BuffInfo.timer = 0;
                                bf.searchRange_BuffInfo.timer = 0;
                                entityBuffMap.Remove(iterator2);
                                entityBuffMap.Add(bf.buffID, bf);
                                isExit = true;
                                break;
                            }
                        } while (entityBuffMap.TryGetNextValue(out bf, ref iterator2));
                        if (!isExit) entityBuffMap.Add(buff.buffID, buff);
                    }
                } while (buffMap.TryGetNextValue(out buff, ref iterator1));
            }
        }
        keyArray.Dispose();
        ///重新对存储buffId的buffIdList赋值
        buffIdList.Clear();
        keyArray = entityBuffMap.GetKeyArray(Allocator.Temp);
        foreach (int key in keyArray)
        {
            if (!buffIdList.Contains(key)) buffIdList.Add(key);
        }
        keyArray.Dispose();
        #endregion
        Entities.WithAll<Buff>().ForEach((Entity entity) =>
        {
            commandBuffer.DestroyEntity(entity);
        }).Run();

    }

    [BurstCompile]
    private struct BuffJob : IJobEntityBatch
    {
        [ReadOnly]
        public ComponentDataFromEntity<Attribute> AttributeFromEntity;
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        public ArchetypeChunkComponentType<Buff> BuffTypeAccessor;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public NativeMultiHashMap<int, Buff>.ParallelWriter buffMap;
        public float deltaTime;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Buff> buffArray = batchInChunk.GetNativeArray<Buff>(BuffTypeAccessor);
            NativeArray<Entity> entityArray = batchInChunk.GetNativeArray(EntityAccessor);
            for (int i = 0; i < buffArray.Length; i++)
            {
                Buff buff = buffArray[i];
                if (!AttributeFromEntity.Exists(buff.targetEntity)) continue;
                buffMap.Add(buff.buffID, buff);
            }
        }
    }
}

[UpdateAfter(typeof(BuffSystem))]
public class BuffMapSystem : SystemBaseUnity
{
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!BuffSystem.isInit) return;
        #region [Buff生效]
        NativeMultiHashMap<int, Buff> entityBuffMap = BuffSystem.entityBuffMap;
        if (entityBuffMap.Count() == 0) return;
        NativeList<int> buffIdList = BuffSystem.buffIdList;
        for (int i = 0; i < buffIdList.Length; i++)
        {
            int buffID = buffIdList[i];
            if (entityBuffMap.TryGetFirstValue(buffID, out Buff buff, out NativeMultiHashMapIterator<int> iterator))
            {
                do
                {
                    if (EntityManager.HasComponent<Attribute>(buff.targetEntity))
                    {
                        entityBuffMap.Remove(iterator);
                        Attribute targetAttribute = EntityManager.GetComponentData<Attribute>(buff.targetEntity);
                        if (buff.timer >= buff.totalTime)    /// Buff作用时间到期  还原状态
                        {
                            if (buff.bulletSpeed_BuffInfo.needReset) targetAttribute.bulletSpeed -= buff.bulletSpeed_BuffInfo.offset;
                            if (buff.health_BuffInfo.needReset) targetAttribute.health -= buff.health_BuffInfo.offset;
                            if (buff.atkStrength_BuffInfo.needReset) targetAttribute.atkStrength -= buff.atkStrength_BuffInfo.offset;
                            if (buff.atkSpeed_BuffInfo.needReset) targetAttribute.atkSpeed -= buff.atkSpeed_BuffInfo.offset;
                            if (buff.moveSpeed_BuffInfo.needReset) targetAttribute.moveSpeed -= buff.moveSpeed_BuffInfo.offset;
                            if (buff.atkRange_BuffInfo.needReset) targetAttribute.atkRange -= buff.atkRange_BuffInfo.offset;
                            if (buff.damageRange_BuffInfo.needReset) targetAttribute.damageRange -= buff.damageRange_BuffInfo.offset;
                            if (buff.searchRange_BuffInfo.needReset) targetAttribute.searchRange -= buff.searchRange_BuffInfo.offset;
                            if (buff.cdShrink_BuffInfo.needReset) targetAttribute.cdShrink -= buff.cdShrink_BuffInfo.offset;
                            EntityManager.SetComponentData(buff.targetEntity, targetAttribute);
                        }
                        else    /// Buff持续作用
                        {
                            #region [子弹初速度]
                            if (buff.bulletInitSpeed_BuffInfo.isStackable)
                            {
                                if (buff.bulletInitSpeed_BuffInfo.timer==0|| buff.bulletInitSpeed_BuffInfo.timer >= buff.bulletInitSpeed_BuffInfo.stackInterval)
                                {
                                    targetAttribute.bulletSpeed += buff.bulletSpeed;
                                    buff.bulletInitSpeed_BuffInfo.offset += buff.bulletSpeed;
                                    buff.bulletInitSpeed_BuffInfo.timer = 0;
                                }
                                buff.bulletInitSpeed_BuffInfo.timer += Time.DeltaTime;
                            }
                            else if (!buff.bulletInitSpeed_BuffInfo.isSet)
                            {
                                targetAttribute.bulletSpeed += buff.bulletSpeed;
                                buff.bulletInitSpeed_BuffInfo.isSet = true;
                                buff.bulletInitSpeed_BuffInfo.offset += buff.bulletSpeed;
                            }

                            #endregion

                            #region [生命值]
                            if (buff.health_BuffInfo.isStackable)
                            {
                                if (buff.health_BuffInfo.timer == 0 || buff.health_BuffInfo.timer >= buff.health_BuffInfo.stackInterval)
                                {
                                    targetAttribute.health += buff.health;
                                    buff.health_BuffInfo.offset += buff.health;
                                    buff.health_BuffInfo.timer = 0;
                                }
                                buff.health_BuffInfo.timer += Time.DeltaTime;
                            }
                            else if (!buff.health_BuffInfo.isSet)
                            {
                                targetAttribute.health += buff.health;
                                buff.health_BuffInfo.isSet = true;
                                buff.health_BuffInfo.offset += buff.health;
                            }

                            #endregion

                            #region [攻击力]
                            if (buff.atkStrength_BuffInfo.isStackable)
                            {
                                buff.atkStrength_BuffInfo.timer += Time.DeltaTime;
                                if (buff.atkStrength_BuffInfo.timer >= buff.atkStrength_BuffInfo.stackInterval)
                                {
                                    targetAttribute.atkStrength += buff.atkStrength;
                                    buff.atkStrength_BuffInfo.offset += buff.atkStrength;
                                    buff.atkStrength_BuffInfo.timer = 0;
                                }
                            }
                            else if (!buff.atkStrength_BuffInfo.isSet)
                            {
                                targetAttribute.atkStrength += buff.atkStrength;
                                buff.atkStrength_BuffInfo.isSet = true;
                                buff.atkStrength_BuffInfo.offset += buff.atkStrength;
                            }
                            #endregion

                            #region [攻击间隔]
                            if (buff.atkSpeed_BuffInfo.isStackable)
                            {
                                buff.atkSpeed_BuffInfo.timer += Time.DeltaTime;
                                if (buff.atkSpeed_BuffInfo.timer >= buff.atkSpeed_BuffInfo.stackInterval)
                                {
                                    targetAttribute.atkSpeed += buff.atkSpeed;
                                    buff.atkSpeed_BuffInfo.offset += buff.atkSpeed;
                                    buff.atkSpeed_BuffInfo.timer = 0;
                                }
                            }
                            else if (!buff.atkSpeed_BuffInfo.isSet)
                            {
                                targetAttribute.atkSpeed += buff.atkSpeed;
                                buff.atkSpeed_BuffInfo.isSet = true;
                                buff.atkSpeed_BuffInfo.offset += buff.atkSpeed;
                            }
                            #endregion

                            #region [攻击距离]
                            if (buff.atkRange_BuffInfo.isStackable)
                            {
                                buff.atkRange_BuffInfo.timer += Time.DeltaTime;
                                if (buff.atkRange_BuffInfo.timer >= buff.atkRange_BuffInfo.stackInterval)
                                {
                                    targetAttribute.atkRange += buff.atkRange;
                                    buff.atkRange_BuffInfo.offset += buff.atkRange;
                                    buff.atkRange_BuffInfo.timer = 0;
                                }
                            }
                            else if (!buff.atkRange_BuffInfo.isSet)
                            {
                                targetAttribute.atkRange += buff.atkRange;
                                buff.atkRange_BuffInfo.isSet = true;
                                buff.atkRange_BuffInfo.offset += buff.atkRange;
                            }
                            #endregion

                            #region [伤害范围]
                            if (buff.damageRange_BuffInfo.isStackable)
                            {
                                buff.damageRange_BuffInfo.timer += Time.DeltaTime;
                                if (buff.damageRange_BuffInfo.timer >= buff.damageRange_BuffInfo.stackInterval)
                                {
                                    targetAttribute.damageRange += buff.damageRange;
                                    buff.damageRange_BuffInfo.offset += buff.damageRange;
                                    buff.damageRange_BuffInfo.timer = 0;
                                }
                            }
                            else if (!buff.damageRange_BuffInfo.isSet)
                            {
                                targetAttribute.damageRange += buff.damageRange;
                                buff.damageRange_BuffInfo.isSet = true;
                                buff.damageRange_BuffInfo.offset += buff.damageRange;
                            }

                            #endregion

                            #region [移动速度]
                            if (buff.moveSpeed_BuffInfo.isStackable)
                            {
                                buff.moveSpeed_BuffInfo.timer += Time.DeltaTime;
                                if (buff.moveSpeed_BuffInfo.timer >= buff.moveSpeed_BuffInfo.stackInterval)
                                {
                                    targetAttribute.moveSpeed += buff.moveSpeed;
                                    buff.moveSpeed_BuffInfo.offset += buff.moveSpeed;
                                    buff.moveSpeed_BuffInfo.timer = 0;
                                }
                            }
                            else if (!buff.moveSpeed_BuffInfo.isSet)
                            {
                                targetAttribute.moveSpeed += buff.moveSpeed;
                                buff.moveSpeed_BuffInfo.isSet = true;
                                buff.moveSpeed_BuffInfo.offset += buff.moveSpeed;
                            }
                            #endregion

                            #region [搜索范围]
                            if (buff.searchRange_BuffInfo.isStackable)
                            {
                                if (buff.searchRange_BuffInfo.timer == 0 || buff.searchRange_BuffInfo.timer >= buff.searchRange_BuffInfo.stackInterval)
                                {
                                    targetAttribute.searchRange += buff.searchRange;
                                    buff.searchRange_BuffInfo.offset += buff.searchRange;
                                    buff.searchRange_BuffInfo.timer = 0;
                                }
                                buff.searchRange_BuffInfo.timer += Time.DeltaTime;
                            }
                            else if (!buff.searchRange_BuffInfo.isSet)
                            {
                                targetAttribute.searchRange += buff.searchRange;
                                buff.searchRange_BuffInfo.isSet = true;
                                buff.searchRange_BuffInfo.offset += buff.searchRange;
                            }
                            #endregion

                            #region [冷却缩减]
                            if (buff.cdShrink_BuffInfo.isStackable)
                            {
                                if (buff.cdShrink_BuffInfo.timer == 0 || buff.cdShrink_BuffInfo.timer >= buff.cdShrink_BuffInfo.stackInterval)
                                {
                                    targetAttribute.cdShrink += buff.cdShrink;
                                    buff.cdShrink_BuffInfo.offset += buff.cdShrink;
                                    buff.cdShrink_BuffInfo.timer = 0;
                                }
                                buff.cdShrink_BuffInfo.timer += Time.DeltaTime;
                            }
                            else if (!buff.cdShrink_BuffInfo.isSet)
                            {
                                targetAttribute.cdShrink += buff.cdShrink;
                                buff.cdShrink_BuffInfo.isSet = true;
                                buff.cdShrink_BuffInfo.offset += buff.cdShrink;
                            }
                            #endregion

                            EntityManager.SetComponentData(buff.targetEntity, targetAttribute);
                            buff.timer += Time.DeltaTime;
                            entityBuffMap.Add(buffID, buff);
                        }
                    }
                    else
                    {
                        entityBuffMap.Remove(iterator);
                    }
                } while (entityBuffMap.TryGetNextValue(out buff, ref iterator));
            }
        }

        #endregion
    }


}
public struct Buff : IComponentData
{
    public Entity targetEntity;
    public int buffID;//相同ID的Buff，代表同一个角色所施加的buff，效果不可叠加

    [Header("子弹速度")]
    public float bulletSpeed;
    public BuffInfo bulletSpeed_BuffInfo;

    [Header("生命值")]
    public float health;
    public BuffInfo health_BuffInfo;

    [Header("攻击力")]
    public float atkStrength;
    public BuffInfo atkStrength_BuffInfo;

    [Header("攻击间隔")]
    public float atkSpeed;
    public BuffInfo atkSpeed_BuffInfo;

    [Header("移动速度")]
    public float moveSpeed;
    public BuffInfo moveSpeed_BuffInfo;

    [Header("侦测距离")]
    public float searchRange;
    public BuffInfo searchRange_BuffInfo;

    [Header("攻击范围")]
    public float atkRange;
    public BuffInfo atkRange_BuffInfo;

    [Header("伤害范围")]
    public float damageRange;
    public BuffInfo damageRange_BuffInfo;

    [Header("子弹初速度")]
    public float3 bulletInitSpeed;
    public BuffInfo bulletInitSpeed_BuffInfo;

    [Header("冷却缩减")]
    public float cdShrink;
    public BuffInfo cdShrink_BuffInfo;

    public float timer;
    public float totalTime;
}
public struct BuffInfo
{
    public bool isStackable;//是否可叠加
    public bool needReset;//是否需要恢复差值
    public float offset;
    public bool isSet;

    //控制buff的生效间隔（只有可叠加时才判断）
    public float timer;
    public float stackInterval;//持续生效 间隔
}