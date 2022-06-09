/*using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

public class TriggerEventSystem : SystemBase
{
    private BuildPhysicsWorld buildPhysicsWorld;
    private StepPhysicsWorld stepPhysicsWorld;
    private EntityCommandBuffer commandBuffer;
    private EntityCommandBufferSystem commandBufferSystem;
    private EntityArchetype buffArchetype;
    protected override void OnCreate()
    {
        buffArchetype = EntityManager.CreateArchetype(
                typeof(Buff)
            );
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        base.OnCreate();
    }
    protected override void OnUpdate()
    {
        var triggerJob = new triggerJob();
        triggerJob.BulletComponentFromEntity = GetComponentDataFromEntity<BulletComponent>(true);
        triggerJob.MoveComponentFromEntity = GetComponentDataFromEntity<MoveComponent>(true);
        triggerJob.EnemyComponentFromEntity = GetComponentDataFromEntity<EnemyComponent>(true);
        triggerJob.AttributeFromEntity = GetComponentDataFromEntity<Attribute>(true);
        triggerJob.BuffFromEntity = GetComponentDataFromEntity<Buff>(true);
        triggerJob.buffArchetype = buffArchetype;
        triggerJob.commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();
        this.Dependency = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, this.Dependency);
        this.Dependency.Complete();
    }

    //[BurstCompile]
    private struct triggerJob : ITriggerEventsJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<BulletComponent> BulletComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<MoveComponent> MoveComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyComponent> EnemyComponentFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Attribute> AttributeFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Buff> BuffFromEntity;
        [ReadOnly]
        public EntityArchetype buffArchetype;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(TriggerEvent triggerEvent)
        {
            GetEntity_Bullet(triggerEvent, out Entity bulletEntity);
            GetEntity_Enemy(triggerEvent, out Entity enemyEntity);
            GetEntity_Buff(triggerEvent, out Entity attachedBuffEntity);

            #region 子弹
            //子弹和敌人
            if (!bulletEntity.Equals(Entity.Null) && !enemyEntity.Equals(Entity.Null))
            {
                BulletComponent bulletComponent = BulletComponentFromEntity[bulletEntity];
                MoveComponent moveComponent = MoveComponentFromEntity[bulletEntity];
                float bulletSpeed = moveComponent.speed;
                float damage = bulletComponent.damage;
                Attribute enemyAttribute = AttributeFromEntity[enemyEntity];
                Debug.Log("damage = " + damage);
                enemyAttribute.health -= damage;
                commandBuffer.SetComponent<Attribute>(enemyEntity.Index, enemyEntity, enemyAttribute);
            }
            //摧毁子弹
            if (!bulletEntity.Equals(Entity.Null))
            {
                commandBuffer.DestroyEntity(bulletEntity.Index, bulletEntity);
            }
            #endregion

            #region 接触碰撞Buff
            //减速带和敌人
            if (!attachedBuffEntity.Equals(Entity.Null) && !enemyEntity.Equals(Entity.Null))
            {
                Buff buff = BuffFromEntity[attachedBuffEntity];
                buff.effectTarget = enemyEntity;
                Entity entity = commandBuffer.CreateEntity(attachedBuffEntity.Index, buffArchetype);
                commandBuffer.SetComponent<Buff>(attachedBuffEntity.Index, entity, buff);
            }
            #endregion
        }

        #region 自定义方法
        private void GetEntity_Bullet(TriggerEvent triggerEvent, out Entity bullet)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;
            bullet = BulletComponentFromEntity.HasComponent(entityA) ? entityA : (BulletComponentFromEntity.HasComponent(entityB) ? entityB : Entity.Null);
        }
        private void GetEntity_Enemy(TriggerEvent triggerEvent, out Entity enemy)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;
            enemy = EnemyComponentFromEntity.HasComponent(entityA) ? entityA : (EnemyComponentFromEntity.HasComponent(entityB) ? entityB : Entity.Null);
        }
        private void GetEntity_Buff(TriggerEvent triggerEvent, out Entity buff)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;
            buff = BuffFromEntity.HasComponent(entityA) ? entityA : (BuffFromEntity.HasComponent(entityB) ? entityB : Entity.Null);
        }
        #endregion
    }

}
*/