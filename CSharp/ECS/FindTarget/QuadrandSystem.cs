using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
public class QuadrandSystem : SystemBaseUnity
{
  public const int quadrantYMultiplier = 1000;//象限宽度
    public const int quadrantCellSize = 30;//象限大小
    private static NativeMultiHashMap<int, QuadrantData> quadrantHashMap;
    private static NativeMultiHashMap<int, QuadrantData> unitHashMap;
    private static NativeMultiHashMap<int, QuadrantData> towerHashMap;
    private static NativeMultiHashMap<int, QuadrantData> targetHashMap;
    private static NativeMultiHashMap<int, QuadrantData> obstacleHashMap;
    private static NativeMultiHashMap<int, QuadrantInfo> quadrantInfoMultiHashMap;
    private static Dictionary<int, QuadrantInfo> quadrantDictionary;
    private EntityQuery entityQuery;
    private EntityQueryDesc entityQueryDesc;
    #region 自定义方法
    public static Dictionary<int, QuadrantInfo> GetQuadrantDictionary()
    {
        return quadrantDictionary;
    }
    public static NativeMultiHashMap<int, QuadrantData> GetQuadrantHashMap()
    {
        return quadrantHashMap;
    }
    public static NativeMultiHashMap<int, QuadrantData> GetUnitHashMap()
    {
        return unitHashMap;
    }
    public static NativeMultiHashMap<int, QuadrantData> GetTowerHashMap()
    {
        return towerHashMap;
    }
    public static NativeMultiHashMap<int, QuadrantData> GetTargetHashMap()
    {
        return targetHashMap;
    }
    public static NativeMultiHashMap<int, QuadrantData> GetObstacleHashMap()
    {
        return obstacleHashMap;
    }
    public static NativeMultiHashMap<int, QuadrantInfo> GetQuadrantInfoHashMap()
    {
        return quadrantInfoMultiHashMap;
    }
    public static int GetPositionHashMapKey(float3 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.z / quadrantCellSize)));
    }
    private static void DebugDrawQuadrant(float3 position)
    {
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize, 0, math.floor(position.z / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, 0, 0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(0, 0, +1) * quadrantCellSize, lowerLeft + new Vector3(+1, 0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(+1, 0, 0) * quadrantCellSize, lowerLeft + new Vector3(+1, 0, +1) * quadrantCellSize);
        //Debug.Log(GetPositionHashMapKey(position) + "" + position);
    }
    //获取象限内Entity数量
    private static int GetEntityCountInHashMap(int hashMapKey)
    {
        int quandrantCount = quadrantHashMap.CountValuesForKey(hashMapKey);
        Debug.Log("象限内Entity数量 = " + quandrantCount);
        return quandrantCount;
    }
    //根据对象类型分别计算Enity数量
    private static int GetEntityCountInHashMapByEntityType(int hashMapKey, EntityType entityTypeEnum)
    {
        return 0;
    }
    //初始化象限字典
    private static void SetQuadrantDictionary()
    {
        NativeKeyValueArrays<int, QuadrantData> hashMapKeyValueArrays = quadrantHashMap.GetKeyValueArrays(Allocator.Temp);
        NativeArray<int> keyArray = hashMapKeyValueArrays.Keys;
        NativeArray<QuadrantData> quadrantDataArray = hashMapKeyValueArrays.Values;
        int length = Common.GetEntityTypeLength();
        QuadrantInfo quadrantInfo = default(QuadrantInfo);
        for (int i = 0; i < keyArray.Length; i++)
        {
            int hashMapKey = keyArray[i];
            QuadrantData quadrantData = quadrantDataArray[i];
            EntityType entityTypeEnum = quadrantData.quadrantEntity.typeEnum;

            if (quadrantDictionary.TryGetValue(hashMapKey, out quadrantInfo))
            {
                if (entityTypeEnum == EntityType.Target) quadrantInfo.targetEntityCount++;
                else if (entityTypeEnum == EntityType.Unit) quadrantInfo.unitEntityCount++;
                quadrantDictionary.Remove(hashMapKey);
                quadrantDictionary.Add(hashMapKey, quadrantInfo);
            }
            else
            {
                quadrantInfo = new QuadrantInfo();
                quadrantInfo.key = hashMapKey;
                if (entityTypeEnum == EntityType.Target)
                {
                    quadrantInfo.targetEntityCount = 1;
                }
                else if (entityTypeEnum == EntityType.Unit)
                {
                    quadrantInfo.unitEntityCount = 1;
                }
                quadrantDictionary.Add(hashMapKey, quadrantInfo);
            }
        }
        if (quadrantInfoMultiHashMap.Capacity < quadrantDictionary.Count)
        {
            quadrantInfoMultiHashMap.Capacity = quadrantDictionary.Count;
        }


        Dictionary<int, QuadrantInfo>.Enumerator enumerator = quadrantDictionary.GetEnumerator();
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator = new NativeMultiHashMapIterator<int>();
        while (enumerator.MoveNext())
        {
            int key = enumerator.Current.Key;
            QuadrantInfo info = enumerator.Current.Value;
            if (!quadrantInfoMultiHashMap.TryGetFirstValue(key, out quadrantInfo, out nativeMultiHashMapIterator))
            {
                quadrantInfoMultiHashMap.Add(key, info);
                //Debug.Log("key = " + key + " info友军数量 = " + info.unitEntityCount + " info敌军数量 = " + info.targetEntityCount);
            }
        }

        hashMapKeyValueArrays.Dispose();
        keyArray.Dispose();
        quadrantDataArray.Dispose();
    }

    #endregion
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { 
                ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Translation>(), 
            },
        };
        quadrantHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        unitHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        towerHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        targetHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        obstacleHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        quadrantInfoMultiHashMap = new NativeMultiHashMap<int, QuadrantInfo>(0, Allocator.Persistent);
    }
    protected override void OnInit()
    {
        base.OnInit();
        GridBasicInfo info = GridSet.Instance.GetChosenGridInfo();
        quadrantDictionary = new Dictionary<int, QuadrantInfo>(info.width * info.height);
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        quadrantDictionary.Clear();
        quadrantHashMap.Clear();
        unitHashMap.Clear();
        towerHashMap.Clear();
        targetHashMap.Clear();
        obstacleHashMap.Clear();
        quadrantInfoMultiHashMap.Clear();

        entityQuery = GetEntityQuery(entityQueryDesc);

        if (quadrantHashMap.Capacity < entityQuery.CalculateEntityCount())
        {
            quadrantHashMap.Capacity = entityQuery.CalculateEntityCount();
            unitHashMap.Capacity = entityQuery.CalculateEntityCount();
            towerHashMap.Capacity = entityQuery.CalculateEntityCount();
            targetHashMap.Capacity = entityQuery.CalculateEntityCount();
            obstacleHashMap.Capacity = entityQuery.CalculateEntityCount();
        }

        var job = new SetQuadrantDataHashMapJob();
        job.quadrantHashMap = quadrantHashMap.AsParallelWriter();
        job.unitHashMap = unitHashMap.AsParallelWriter();
        job.towerHashMap = towerHashMap.AsParallelWriter();
        job.targetHashMap = targetHashMap.AsParallelWriter();
        job.obstacleHashMap = obstacleHashMap.AsParallelWriter();
        job.TranslationTypeAccessor = this.GetArchetypeChunkComponentType<Translation>(true);
        job.QuadrantEntityTypeAccessor = this.GetArchetypeChunkComponentType<QuadrantEntity>(true);
        int batchesPerChunk = 1;
        JobHandle jobHandle = JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, batchesPerChunk, this.Dependency);

        jobHandle.Complete();
        SetQuadrantDictionary();
        #region 查看象限详细信息
        DebugDrawQuadrant(Common.GetMouseWorldPosition());
        if (Input.GetKeyDown(KeyCode.F2))
        {
            GetEntityCountInHashMap(GetPositionHashMapKey(Common.GetMouseWorldPosition()));
        }
        #endregion
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        quadrantHashMap.Dispose();
        unitHashMap.Dispose();
        targetHashMap.Dispose();
        obstacleHashMap.Dispose();
        towerHashMap.Dispose();
        quadrantInfoMultiHashMap.Dispose();
        base.OnDestroy();
    }

    [BurstCompile]
    private struct SetQuadrantDataHashMapJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationTypeAccessor;

        [ReadOnly]
        public ArchetypeChunkComponentType<QuadrantEntity> QuadrantEntityTypeAccessor;

        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantHashMap;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter unitHashMap;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter towerHashMap;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter targetHashMap;
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter obstacleHashMap;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> quadrandTranslations = batchInChunk.GetNativeArray<Translation>(TranslationTypeAccessor);
            NativeArray<QuadrantEntity> quadrantEntityArray = batchInChunk.GetNativeArray<QuadrantEntity>(QuadrantEntityTypeAccessor);

            for (int i = 0; i < quadrantEntityArray.Length; i++)
            {
                EntityType typeEnum = quadrantEntityArray[i].typeEnum;
                int hashMapKey = GetPositionHashMapKey(quadrandTranslations[i].Value);

                quadrantHashMap.Add(hashMapKey, new QuadrantData { quadrantEntity = quadrantEntityArray[i], position = quadrandTranslations[i].Value });

                switch (typeEnum)
                {
                    case EntityType.Unit:
                        unitHashMap.Add(hashMapKey, new QuadrantData { quadrantEntity = quadrantEntityArray[i], position = quadrandTranslations[i].Value });
                        break;
                    case EntityType.Target:
                        targetHashMap.Add(hashMapKey, new QuadrantData { quadrantEntity = quadrantEntityArray[i], position = quadrandTranslations[i].Value });
                        break;
                    case EntityType.Tower:
                        towerHashMap.Add(hashMapKey, new QuadrantData { quadrantEntity = quadrantEntityArray[i], position = quadrandTranslations[i].Value });
                        break;
                    case EntityType.可摧毁的:
                        obstacleHashMap.Add(hashMapKey, new QuadrantData { quadrantEntity = quadrantEntityArray[i], position = quadrandTranslations[i].Value });
                        break;
                }
            }
        }
    }
}

public struct QuadrantData
{
    public QuadrantEntity quadrantEntity;
    public float3 position;
}

public struct QuadrantInfo
{
    public int key;
    public int unitEntityCount;
    public int targetEntityCount;
}
