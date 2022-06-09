using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SelectedMesh : MonoBehaviour
{
    [Header("实体")]
    [SerializeField] private ConvertedEntityHolder convertedEntityHolder;
    private Entity entity = Entity.Null;
    private EntityManager entityManager;
    private MeshRenderer _meshRenderer;
    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }
    private void Update()
    {
        //初始化实体
        if (entity == Entity.Null)
        {
            Init();
            return;
        }

        _meshRenderer.enabled = entityManager.HasComponent<AllySelected>(entity);

    }

    #region Method
    private void Init()
    {
        entity = convertedEntityHolder.GetEntity();
        entityManager = convertedEntityHolder.GetEntityManager();
    }
    #endregion
}
