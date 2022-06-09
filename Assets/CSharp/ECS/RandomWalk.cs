using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class RandomWalk : MonoBehaviour
{
    [SerializeField] private ConvertedEntityHolder convertedEntityHolder;
    private Entity entity = Entity.Null;
    private EntityManager entityManager;

    void Update()
    {
        if (GameController._gameState != GameState.游戏中) return;
        //初始化实体
        if (entity == Entity.Null)
        {
            entity = convertedEntityHolder.GetEntity();
            entityManager = convertedEntityHolder.GetEntityManager();
            return;
        }
        if (!entityManager.HasComponent<Translation>(entity)) return;
        #region 定时自动随机寻路
        SkillsTimer timer = entityManager.GetComponentData<SkillsTimer>(entity);
        timer.pathFindingTimer += Time.deltaTime;
        if (timer.pathFindingTimer >=0.5f&& !entityManager.HasComponent<HasTarget>(entity) && !entityManager.HasComponent<AllySelected>(entity))
        {
            timer.pathFindingTimer = 0;
            int2 startPosition;
            int2 curEndPosition;
            Vector3 worldPosition = transform.position - new Vector3(0.5f, 0, 0.5f);
            if (!GridSet.Instance.IsInChosenGrid(worldPosition))
            {
                //运动对象不在当前选中网格内
                startPosition = new int2(0, 0);
            }
            else
            {
                Common.GetXYFloor(worldPosition, out startPosition.x, out startPosition.y);
            }

            Common.GetXYFloor(Common.GetWalkableWorldPosition(), out curEndPosition.x, out curEndPosition.y);
            entityManager.AddComponentData(entity, new PathfindingParams
            {
                startPosition = startPosition,
                endPosition = curEndPosition
            });
            entityManager.SetComponentData(entity, new PathFollow { pathIndex = -1 });
        }
        entityManager.SetComponentData<SkillsTimer>(entity, timer);
        #endregion
    }
}
