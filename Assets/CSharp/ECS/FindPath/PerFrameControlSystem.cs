using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class PerFrameControlSystem : SystemBaseUnity
{
    private EntityQuery entityQuery;
    private EntityQueryDesc allyAutoQueryDesc;
    private EntityQueryDesc allyCommandedQueryDesc;
    private EntityQueryDesc enemyQueryDesc;
    private EntityCommandBufferSystem commandBufferSystem;
    private int divideNum = 20;//分帧执行
    private int minCalPerFrame = 10;
    private NativeArray<Entity> entityArray;
    private int length;

    protected override void OnCreate()
    {
        base.OnCreate();
        SetGameStatesOnly(GameState.游戏中);

        allyAutoQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<PathfindingParams>(), ComponentType.ReadOnly<AllyComponent>() },
            None = new ComponentType[] { ComponentType.ReadOnly<CanFindPath>(), ComponentType.ReadOnly<AttackComponent>(), ComponentType.ReadOnly<HasCommand>() }
        };
        allyCommandedQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<PathfindingParams>(), ComponentType.ReadOnly<AllyComponent>(), ComponentType.ReadOnly<HasCommand>() },
            None = new ComponentType[] { ComponentType.ReadOnly<CanFindPath>(), ComponentType.ReadOnly<AttackComponent>() }
        };
        enemyQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<PathfindingParams>(), ComponentType.ReadOnly<EnemyComponent>() },
            None = new ComponentType[] { ComponentType.ReadOnly<CanFindPath>(), ComponentType.ReadOnly<AttackComponent>() }
        };
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!MapSystem.isInit)
        {
            Debug.Log("!MapSystem.isInit");
            return;
        }

        EntityCommandBuffer commandBuffer = commandBufferSystem.CreateCommandBuffer();

        #region ally寻路工作分配
        entityQuery = GetEntityQuery(allyAutoQueryDesc);
        entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
        if (entityArray.Length < divideNum * minCalPerFrame)
        {
            length = minCalPerFrame < entityArray.Length ? minCalPerFrame : entityArray.Length;
        }
        else
        {
            length = Mathf.FloorToInt(entityArray.Length / divideNum + 0.5f);
        }
        for (int i = 0; i < length; i++)
        {
            Entity entity = entityArray[i];
            commandBuffer.AddComponent<CanFindPath>(entity);
        }
        entityArray.Dispose();

        entityQuery = GetEntityQuery(allyCommandedQueryDesc);
        entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
        if (entityArray.Length < divideNum * minCalPerFrame)
        {
            length = minCalPerFrame < entityArray.Length ? minCalPerFrame : entityArray.Length;
        }
        else
        {
            length = Mathf.FloorToInt(entityArray.Length / divideNum + 0.5f);
        }
        for (int i = 0; i < length; i++)
        {
            Entity entity = entityArray[i];
            commandBuffer.AddComponent<CanFindPath>(entity);
        }
        entityArray.Dispose();
        #endregion
        #region enemy寻路工作分配
        entityQuery = GetEntityQuery(enemyQueryDesc);
        entityArray = entityQuery.ToEntityArray(Allocator.TempJob);
        if (entityArray.Length < divideNum * minCalPerFrame)
        {
            length = minCalPerFrame < entityArray.Length ? minCalPerFrame : entityArray.Length;
        }
        else
        {
            length = Mathf.FloorToInt(entityArray.Length / divideNum + 0.5f);
        }
        for (int i = 0; i < length; i++)
        {
            Entity entity = entityArray[i];
            commandBuffer.AddComponent<CanFindPath>(entity);
        }
        entityArray.Dispose();
        #endregion

    }

    protected override void OnInit()
    {
        base.OnInit();
    }
}
public struct CanFindPath : IComponentData
{

}