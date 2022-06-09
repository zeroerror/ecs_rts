using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class PathOccupySystem : SystemBaseUnity
{
    public static bool isInit { get; private set; }
    private EntityQueryDesc entityQueryDesc;
    private EntityQuery entityQuery;
    private EntityCommandBufferSystem commandBufferSystem;
    public static NativeArray<int> occupyArray;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Attribute>() }
        };
    }
    protected override void OnInit()
    {
        base.OnInit();
        CreateOccupyArray();
        isInit = true;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isInit) occupyArray.Dispose();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!MapSystem.isInit) return;
    
        entityQuery = GetEntityQuery(entityQueryDesc);
        CreateOccupyArray();
        var job = new OccupyJob();
        job.info = GridSet.Instance.GetChosenGridInfo();
        job.TranslationAccessor = GetArchetypeChunkComponentType<Translation>(true);
        job.occupyArray = occupyArray;
        job.frameCount = UnityEngine.Time.frameCount;
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, 1, this.Dependency);
        Dependency.Complete();
    }
    private void CreateOccupyArray()
    {
        if (isInit) occupyArray.Dispose();
        GridBasicInfo info = GridSet.Instance.GetChosenGridInfo();
        occupyArray = new NativeArray<int>(info.width * info.height, Allocator.Persistent);
    }
    private struct OccupyJob : IJobEntityBatch
    {
        [ReadOnly]
        public ArchetypeChunkComponentType<Translation> TranslationAccessor;
        public GridBasicInfo info;
        public NativeArray<int> occupyArray;
        public float frameCount;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Translation> transArray = batchInChunk.GetNativeArray<Translation>(TranslationAccessor);
            for (int i = 0; i < transArray.Length; i++)
            {
                float3 position = transArray[i].Value;
                int x, y;
                GetXYFloor(position, out x, out y);
                int index = Common.CalculateIndex(x, y);
                occupyArray[index] = 1;
            }
        }

        public static void GetXYFloor(Vector3 worldPosition, out int x, out int y)
        {
            GridBasicInfo info = GridSet.Instance.GetChosenGridInfo();
            x = Mathf.FloorToInt((worldPosition.x - info.originPosition.x) / info.cellSize);
            y = Mathf.FloorToInt((worldPosition.z - info.originPosition.z) / info.cellSize);
        }
    }

}
