using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EnemyBornPoint : MonoBehaviour
{
    [Header("预制体")]
    [SerializeField] public GameObject prefab;
    [Header("每波生成数量")]
    [SerializeField] public int spawnCount;
    [Header("生成速度")]
    [SerializeField] public float spawnSec;
    [Header("每波间隔")]
    [SerializeField] public int spawnInterval;
    [Header("阵营")]
    [SerializeField] public CampType campType;
    [Header("目的地1")]
    public Transform place1;
    private float3 place_1;
    [Header("目的地2")]
    public Transform place2;
    private float3 place_2;
    [Header("目的地3")]
    public Transform place3;
    private float3 place_3;
    [Header("目的地4")]
    public Transform place4;
    private float3 place_4;
    private float curTime;//当前距上一波单位生成时间
    private float curSpawnDealtaTime;//当前距上次单位生成时间

    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private Entity prefabEntity;
    private int curCount = 0;
    private Entity pathPositionBufferEntity;
    private bool isPathPositionBufferEntitySet;
    private void Awake()
    {
        if (place1) place_1 = place1.position;
        if (place2) place_2 = place2.position;
        if (place3) place_3 = place3.position;
        if (place4) place_4 = place4.position;
        curTime = 0;
        curSpawnDealtaTime = 0;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
    }
    private void OnDestroy()
    {
        if (blobAssetStore != null) blobAssetStore.Dispose();
    }
    private void Update()
    {
        if (!GridSet.Instance.isInit) return;
        if (ObstacleSystem.isStaticObstacleSet && !isPathPositionBufferEntitySet)
        {
            int2 startPosition = new int2();
            int2 endPosition = new int2();
            Common.GetXYFloor(transform.position, out startPosition.x, out startPosition.y);
            Common.GetXYFloor(place_1, out endPosition.x, out endPosition.y);
            pathPositionBufferEntity = FindPathSystem.GetPathPositionBufferEntity(startPosition, endPosition);
            isPathPositionBufferEntitySet = true;
        }

        curTime += Time.deltaTime;
        if (curTime >= spawnInterval)
        {
            if (curCount < spawnCount)
            {
                curSpawnDealtaTime += Time.deltaTime;
                if (curSpawnDealtaTime >= spawnSec)
                {
                    curSpawnDealtaTime = 0;
                    SpawnEntity();
                    if (curCount == spawnCount)
                    {
                        curCount = 0;
                        curTime = 0;
                    }
                }
            }
        }
    }
    public void SpawnEntity()
    {
        curCount++;
        EntityGameObject ego = GameObject.Instantiate(prefab, transform.position, transform.rotation).GetComponent<EntityGameObject>();
        ego.campType = campType;
        ego.place_1 = place_1;
        ego.place_2 = place_2;
        ego.place_3 = place_3;
        ego.place_4 = place_4;
        ego.isNeedInitByConfig = true;
        ego.pathPositionBufferEntity = pathPositionBufferEntity;
    }
}
