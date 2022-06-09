using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using ZeroHero;

/// <summary>
/// 指针控制器
/// </summary>
public class CursorHandler : UnitySingleton<CursorHandler>
{
    private Texture2D normal;
    private Texture2D ally;
    private Texture2D enemy;
    private EntityManager entityManager;
    private  void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        normal = ResourceMgr.LoadTexture(1012) as Texture2D;
        ally = ResourceMgr.LoadTexture(1013) as Texture2D;
        enemy = ResourceMgr.LoadTexture(1014) as Texture2D;
    }
    private void OnDisable()
    {
        Cursor.SetCursor(null,new Vector2(),CursorMode.Auto);
    }
    private void Update()
    {
        if (Time.frameCount % 10 == 0)
        {
            if (SceneMgr.Instance.currentScene != SceneType.游戏场景)
            {
                Cursor.SetCursor(null, new Vector2(), CursorMode.Auto);
                return;
            }
            Entity entity = Common.GetClickEntity();
            if (entity == Entity.Null)
            {
                Cursor.SetCursor(normal, new Vector2(), CursorMode.Auto);
            }
            else if (entityManager.HasComponent<AllyComponent>(entity))
            {
                Cursor.SetCursor(ally, new Vector2(), CursorMode.Auto);
            }
            else if (entityManager.HasComponent<EnemyComponent>(entity))
            {
                Cursor.SetCursor(enemy, new Vector2(), CursorMode.Auto);
            }
        }
    }
}
