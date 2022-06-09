using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class ConvertedEntityHolder : MonoBehaviour, IConvertGameObjectToEntity
{
    private Entity entity;
    private EntityManager entityManager;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        this.entity = entity;
        this.entityManager = dstManager;
    }

    public Entity GetEntity()
    {
        return entity;
    }

    public EntityManager GetEntityManager()
    {
        return entityManager;
    }
}
