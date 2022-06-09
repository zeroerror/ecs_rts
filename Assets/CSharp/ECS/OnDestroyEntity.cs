using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

public class OnDestroyEntity : MonoBehaviour
{
    public ConvertedEntityHolder entityHolder;
    private Entity entity;
    private EntityManager entityManager;
    private void OnDestroy()
    {
        entity = entityHolder.GetEntity();
        entityManager = entityHolder.GetEntityManager();
        if (entity != Entity.Null&& GameController._gameState != GameState.退出)
        {
            _TryClearObstacle();
            entityManager.DestroyEntity(entity);
        }
    }

    private void _TryClearObstacle()
    {
        if (entity != Entity.Null && entityManager.HasComponent<StaticObstacle>(entity))
        {
            GridBasicInfo info = GridSet.Instance.GetChosenGridInfo();
            Common.GetXYFloor(info, entityManager.GetComponentData<LocalToWorld>(entity).Position, out int x, out int y);
            int index = Common.CalculateIndex(info, x, y);
            NativeArray<MapNode> mapNodeArray = MapSystem.mapNodeArray;
            if (!mapNodeArray.IsCreated) return;
            MapNode mapNode = mapNodeArray[index];
            mapNode.isWalkable = true;
            mapNodeArray[index] = mapNode;
            //Debug.Log("ClearObstacle");
        }
    }

}
