using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SkillsTimerSystem : SystemBaseUnity
{
    private EntityQuery entityQuery;
    private EntityQueryDesc entityDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);
        entityDesc = new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<SkillsTimer>(),
            },
        };
    }
    protected override void OnInit()
    {
        base.OnInit();
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        entityQuery = GetEntityQuery(entityDesc);
        var job = new TimerJob();
        job.SkillsTimerAccessor = GetArchetypeChunkComponentType<SkillsTimer>(false);
        job.AttributeAccessor = GetArchetypeChunkComponentType<Attribute>(true);
        job.PathfindingParamsFromEntity = GetComponentDataFromEntity<PathfindingParams>(true);
        job.AttackComponentFromEntity = GetComponentDataFromEntity<AttackComponent>(true);
        job.ReleaseSkillFromEntity = GetComponentDataFromEntity<ReleaseSkill>(true);
        job.EntityAccessor = GetArchetypeChunkEntityType();
        job.deltaTime = Time.DeltaTime;
        job.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        Dependency = JobEntityBatchExtensions.ScheduleParallelBatched(job, entityQuery, 1, Dependency);
        Dependency.Complete();
    }

    [BurstCompile]
    private struct TimerJob : IJobEntityBatch
    {
        public ArchetypeChunkComponentType<SkillsTimer> SkillsTimerAccessor;
        [ReadOnly]
        public ArchetypeChunkComponentType<Attribute> AttributeAccessor;
        [ReadOnly]
        public ArchetypeChunkEntityType EntityAccessor;
        [ReadOnly]
        public ComponentDataFromEntity<PathfindingParams> PathfindingParamsFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<AttackComponent> AttackComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<ReleaseSkill> ReleaseSkillFromEntity;

        public EntityCommandBuffer.Concurrent commandBuffer;
        public float deltaTime;
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<SkillsTimer> skillsTimerArray = batchInChunk.GetNativeArray<SkillsTimer>(SkillsTimerAccessor);
            NativeArray<Attribute> AttributeArray = batchInChunk.GetNativeArray<Attribute>(AttributeAccessor);
            NativeArray<Entity> entityArray = batchInChunk.GetNativeArray(EntityAccessor);

            for (int i = 0; i < skillsTimerArray.Length; i++)
            {
                SkillsTimer st = skillsTimerArray[i];
                Attribute attribute = AttributeArray[i];
                Entity entity = entityArray[i];

                if (PathfindingParamsFromEntity.Exists(entity)) st.pathFindingTimer = 0;
                else if (st.pathFindingTimer + deltaTime <= 0.5f) st.pathFindingTimer += deltaTime;
                else st.pathFindingTimer = 0.5f;

                if (AttackComponentFromEntity.Exists(entity)) st.atkTimer = 0;
                else if (st.atkTimer + deltaTime <= 1f/attribute.atkSpeed) st.atkTimer += deltaTime;
                else st.atkTimer = 1f/attribute.atkSpeed;

                if (ReleaseSkillFromEntity.Exists(entity))
                {
                    ReleaseSkill releaseSkill = ReleaseSkillFromEntity[entity];
                    if (releaseSkill.skillType == SkillType.小技能) st.smallSkillTimer = 0;
                    else if (releaseSkill.skillType == SkillType.大技能) st.bigSkillTimer = 0;
                }
                else
                {
                    if (st.smallSkillTimer + deltaTime <= attribute.smallSkillCD) st.smallSkillTimer += deltaTime;
                    else st.smallSkillTimer = attribute.smallSkillCD;
                    if (st.bigSkillTimer + deltaTime <= attribute.bigSkillCD) st.bigSkillTimer += deltaTime;
                    else st.bigSkillTimer = attribute.bigSkillCD;
                }
                commandBuffer.SetComponent<SkillsTimer>(batchIndex, entity, st);
            }
        }
    }

}
