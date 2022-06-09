using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

/*public class ShootingSystem : SystemBase
{
    private EntityManager entityManager;
    private Entity bulletPrefabEntity;
    private NativeArray<Entity> bulletArray;
    private int oneShotBullets;
    private BlobAssetStore blobAssetStore;

    protected override void OnStartRunning()
    {
        oneShotBullets = PlayerController.Instance.oneShotBullets;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        bulletPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(PlayerController.Instance.bulletPrefab, settings);
        base.OnStartRunning();
    }


    protected override void OnDestroy()
    {
        blobAssetStore.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {

        if (PlayerController.Instance == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, int.MaxValue))
            {
                float3 startPos = PlayerController.Instance.GetGunPointPosition();
                float3 endPos = raycastHit.point;
                float bulletSpeed = PlayerController.Instance.GetCurBulletSpeed();
                float bulletDamage = PlayerController.Instance.GetCurBulletDamage();

                //endPos += new float3(UnityEngine.Random.Range(-10, 10), 0, 0);

                //使用EntityManager创建子弹Entity
                bulletArray = new NativeArray<Entity>(oneShotBullets, Allocator.Temp);
                entityManager.Instantiate(bulletPrefabEntity, bulletArray);
                for (int i = 0; i < bulletArray.Length; i++)
                {
                    entityManager.AddComponentData<BulletFlying>(bulletArray[i], new BulletFlying { entity = bulletArray[i], starPosition = startPos, endPosition = endPos, speed = bulletSpeed, damage = bulletDamage });
                    entityManager.SetComponentData<Translation>(bulletArray[i], new Translation { Value = startPos });
                }
                bulletArray.Dispose();

            }

        }

    }

}
*/

