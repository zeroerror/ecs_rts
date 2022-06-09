using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;

[UpdateAfter(typeof(PathFollowSystem))]
public class FindTargetSystem : SystemBaseUnity
{
    private EntityCommandBufferSystem commandBufferSystem;

    private EntityQuery entityQuery;
    private EntityQueryDesc entityQueryDesc;

    private static int batchesPerChunk = 1;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Attribute>(),
                ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Translation>(),
            },
            Any = new ComponentType[]
            {
                ComponentType.ReadOnly<AllyComponent>(),
                ComponentType.ReadOnly<EnemyComponent>()
            },
            None = new ComponentType[] {
                ComponentType.ReadOnly<HasCommand>(),
            }
        };
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        entityQuery = this.GetEntityQuery(entityQueryDesc);
        #region 寻找Target
        var job1 = new FindTargetQuadrantSystemJob();
        job1.TranslationAccessor = this.GetArchetypeChunkComponentType<Translation>(true);
        job1.QuadrantEntityTypeAccessor = this.GetArchetypeChunkComponentType<QuadrantEntity>(true);
        job1.AttributeAccessor = this.GetArchetypeChunkComponentType<Attribute>(true);
        job1.allyHashMap = QuadrandSystem.GetUnitHashMap();
        job1.enemyHashMap = QuadrandSystem.GetTargetHashMap();
        job1.obstacleHashMap = QuadrandSystem.GetObstacleHashMap();
        job1.quadrantInfoMultiHashMap = QuadrandSystem.GetQuadrantInfoHashMap();
        job1.entityCommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job1.TranslationDataFromEntity = GetComponentDataFromEntity<Translation>(true);
        job1.HasTargetFromEntity = GetComponentDataFromEntity<HasTarget>(true);
        job1.AllyComponentFromEntity = GetComponentDataFromEntity<AllyComponent>(true);
        job1.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
        job1.AttributeFromEntity = GetComponentDataFromEntity<Attribute>(true);
        job1.AttributeBaseFromEntity = GetComponentDataFromEntity<AttributeBase>(true);
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job1, entityQuery, batchesPerChunk, this.Dependency);
        Dependency.Complete();
        #endregion
    }

    [BurstCompile]
    private struct FindTargetQuadrantSystemJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<QuadrantEntity> QuadrantEntityTypeAccessor;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> allyHashMap;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> enemyHashMap;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> obstacleHashMap;
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantInfo> quadrantInfoMultiHashMap;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationDataFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<HasTarget> HasTargetFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AllyComponent> AllyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Attribute> AttributeFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AttributeBase> AttributeBaseFromEntity;

        public EntityCommandBuffer.Concurrent entityCommandBuffer;


        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> selfTranslationArray = batchInChunk.GetNativeArray<Translation>(TranslationAccessor);
            NativeArray<Attribute> attributeArray = batchInChunk.GetNativeArray<Attribute>(AttributeAccessor);
            NativeArray<QuadrantEntity> QuadrantEntityArray = batchInChunk.GetNativeArray<QuadrantEntity>(QuadrantEntityTypeAccessor);

            for (int i = 0; i < QuadrantEntityArray.Length; i++)
            {
                Entity selfEntity = QuadrantEntityArray[i].entity;
                Translation selfTranslation = selfTranslationArray[i];
                Attribute attribute = attributeArray[i];
                float closestDistance = float.PositiveInfinity;
                float lowestHP = float.PositiveInfinity;
                Entity closestEntity = Entity.Null;
                if (HasTargetFromEntity.Exists(selfEntity))
                {
                    Entity targetEntty = HasTargetFromEntity[selfEntity].targetEntity;
                    if (TranslationDataFromEntity.Exists(targetEntty))
                    {
                        closestEntity = targetEntty;
                        float3 targetPos = TranslationDataFromEntity[closestEntity].Value;
                        closestDistance = math.distance(targetPos, selfTranslation.Value);
                        if (closestDistance <= attribute.atkRange) continue; 
                    }
                }
                int hashMapKey = QuadrandSystem.GetPositionHashMapKey(selfTranslation.Value);
                NativeArray<int> allKey = new NativeArray<int>(9, Allocator.Temp);
                allKey[0] = hashMapKey;
                allKey[1] = hashMapKey + 1;
                allKey[2] = hashMapKey - 1;
                allKey[3] = hashMapKey + QuadrandSystem.quadrantYMultiplier;
                allKey[4] = hashMapKey - QuadrandSystem.quadrantYMultiplier;
                allKey[5] = hashMapKey + QuadrandSystem.quadrantYMultiplier + 1;
                allKey[6] = hashMapKey + QuadrandSystem.quadrantYMultiplier - 1;
                allKey[7] = hashMapKey - QuadrandSystem.quadrantYMultiplier + 1;
                allKey[8] = hashMapKey - QuadrandSystem.quadrantYMultiplier - 1;
                for (int index = 0; index < allKey.Length; index++)
                {
                    int key = allKey[index];
                    NativeMultiHashMapIterator<int> iterator;
                    if (AllyComponentFromEntity.Exists(selfEntity))
                    {
                        if (attribute.characterType != CharacterType.礼物炮车)
                        {
                            //友军需找攻击的敌军
                            if (quadrantInfoMultiHashMap.TryGetFirstValue(key, out QuadrantInfo quadrantInfo, out iterator) && quadrantInfo.targetEntityCount != 0)
                            {
                                FindTargetInQuadrant(enemyHashMap, key, selfTranslation, ref closestEntity, ref closestDistance);
                            }
                        }
                        else
                        {
                            //友军需找治愈的友军
                            if (quadrantInfoMultiHashMap.TryGetFirstValue(key, out QuadrantInfo quadrantInfo, out iterator) && quadrantInfo.unitEntityCount != 0)
                            {
                                FindLowestHPInQuadrant(allyHashMap, key, attribute.atkRange, selfEntity, selfTranslation, ref closestEntity, ref closestDistance, ref lowestHP);
                            }
                        }
                    }
                    if (EnemyComponentFromEntity.Exists(selfEntity))
                    {
                        if (attribute.characterType != CharacterType.礼物炮车)
                        {
                            //敌军需找攻击的友军
                            if (quadrantInfoMultiHashMap.TryGetFirstValue(key, out QuadrantInfo quadrantInfo, out iterator) && quadrantInfo.unitEntityCount != 0)
                            {
                                FindTargetInQuadrant(allyHashMap, key, selfTranslation, ref closestEntity, ref closestDistance);
                            }
                        }
                        else
                        {
                            //敌军需找治愈的敌军
                            if (quadrantInfoMultiHashMap.TryGetFirstValue(key, out QuadrantInfo quadrantInfo, out iterator) && quadrantInfo.targetEntityCount != 0)
                            {
                                FindLowestHPInQuadrant(enemyHashMap, key, attribute.atkRange, selfEntity, selfTranslation, ref closestEntity, ref closestDistance, ref lowestHP);
                            }
                        }

                    }

                }
                if (closestEntity != Entity.Null)
                {
                    if(attribute.searchRange >= closestDistance) entityCommandBuffer.AddComponent<HasTarget>(batchIndex, selfEntity, new HasTarget { targetEntity = closestEntity });
                }
                else
                {
                    entityCommandBuffer.RemoveComponent<HasTarget>(batchIndex, selfEntity);
                }
                allKey.Dispose();
            }
        }

        public void FindTargetInQuadrant(NativeMultiHashMap<int, QuadrantData> targetHashMap, int hashMapKey, Translation selfTranslation, ref Entity closestEntity, ref float closestDistance)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (targetHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
            {
                do
                {
                    if (!TranslationDataFromEntity.HasComponent(quadrantData.quadrantEntity.entity))
                    {
                        Debug.Log("not has component");
                        continue;
                    }

                    Translation targetTranslation = TranslationDataFromEntity[quadrantData.quadrantEntity.entity];
                    float distance = math.distance(targetTranslation.Value, selfTranslation.Value);
                    if (distance < closestDistance)
                    {
                        closestEntity = quadrantData.quadrantEntity.entity;
                        closestDistance = distance;
                    }
                } while (targetHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
            }
        }
        public void FindLowestHPInQuadrant(NativeMultiHashMap<int, QuadrantData> targetHashMap, int hashMapKey, float atkRange, Entity selfEntity, Translation selfTranslation, ref Entity lowestEntity, ref float closestDistance, ref float lowestHP)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (targetHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
            {
                do
                {
                    if (!TranslationDataFromEntity.HasComponent(quadrantData.quadrantEntity.entity))
                    {
                        Debug.Log("not has component");
                        continue;
                    }
                    Entity targetEntity = quadrantData.quadrantEntity.entity;
                    float targetHP = AttributeFromEntity[targetEntity].health;
                    if (targetHP < AttributeBaseFromEntity[targetEntity].health)
                    {
                        //非满血
                        float distance = math.distance(TranslationDataFromEntity[quadrantData.quadrantEntity.entity].Value, selfTranslation.Value);
                        if (targetHP < lowestHP)
                        {
                            lowestEntity = quadrantData.quadrantEntity.entity;
                            lowestHP = targetHP;
                            closestDistance = distance;
                        }
                        else if (targetHP == lowestHP)
                        {
                            //同样的低血量情况下比较距离最近的
                            if (distance < closestDistance)
                            {
                                lowestEntity = quadrantData.quadrantEntity.entity;
                                closestDistance = distance;
                            }
                        }
                    }
                } while (targetHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));

                //自身非满血的情况下对比
                float selfHealth = AttributeFromEntity[selfEntity].health;
                float selfBaseHealth = AttributeBaseFromEntity[selfEntity].health;
                if (selfHealth < selfBaseHealth && selfHealth < lowestHP)
                {
                    lowestHP = selfHealth;
                    lowestEntity = selfEntity;
                    closestDistance = 0;
                }
                if (lowestEntity != Entity.Null && AttributeFromEntity[lowestEntity].health >= AttributeBaseFromEntity[lowestEntity].health)
                {
                    lowestEntity = Entity.Null;
                }
            }
        }
        public void FindObstacleInQuadrant(int hashMapKey, Translation selfTranslation, ref Entity closestEntity, ref float closestDistance)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (obstacleHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
            {
                do
                {
                    if (quadrantData.quadrantEntity.typeEnum == EntityType.可摧毁的)
                    {
                        if (!TranslationDataFromEntity.HasComponent(quadrantData.quadrantEntity.entity))
                        {
                            Debug.Log("not has component");
                            continue;
                        }

                        Translation targetTranslation = TranslationDataFromEntity[quadrantData.quadrantEntity.entity];
                        float distance = math.distance(targetTranslation.Value, selfTranslation.Value);
                        if (distance < closestDistance)
                        {
                            closestEntity = quadrantData.quadrantEntity.entity;
                            closestDistance = distance;
                        }
                    }
                } while (obstacleHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
            }
        }

    }
}

public struct HasTarget : IComponentData
{
    public Entity targetEntity;
}