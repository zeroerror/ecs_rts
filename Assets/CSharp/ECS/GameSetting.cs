using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;
using ZeroHero;

public class GameSetting : UnitySingleton<GameSetting>
{
    [Header("地面")]
    [SerializeField] public GameObject floor;
    [System.NonSerialized]
    public bool isInit = false;

    private Dictionary<RoleCfg, GameObject> spawnObjDic = new Dictionary<RoleCfg, GameObject>();
    private GridBasicInfo info;
    private int cellSize = 1;
    private int floorWidth;
    private int floorHeight;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private Entity blockPrefabEntity;
    private Entity towerPrefabEntity;
    private Transform _environment;
    private Transform _charactors;

    public Transform allyArea;
    public Transform enemyArea;
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _environment = GameObject.Find("环境").transform;
        _charactors = new GameObject("角色生成").transform;
        //blobAssetStore = new BlobAssetStore();
        //GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
    }

    private void OnDestroy()
    {
        //blobAssetStore.Dispose();
    }

    #region 自定义方法
    public void SpawnPrefab_Random(int number, GameObject prefab, Transform area)
    {
        //获得实际区域大小
        float3 size = area.GetComponent<Renderer>().bounds.size;
        float3 center = area.position;
        float3 lowerLeft = center - new float3(size.x / 2.0f, 0f, size.z / 2.0f);
        float3 upperRight= center + new float3(size.x / 2.0f, 0f, size.z / 2.0f);
        float3 randomPos;
        randomPos.x = UnityEngine.Random.Range(lowerLeft.x, upperRight.x);
        randomPos.y = center.y;
        randomPos.z = UnityEngine.Random.Range(lowerLeft.z, upperRight.z);
        for (int i = 0; i < number; i++)
        {
            randomPos.x = UnityEngine.Random.Range(lowerLeft.x, upperRight.x);
            randomPos.y = center.y;
            randomPos.z = UnityEngine.Random.Range(lowerLeft.z, upperRight.z);
            var go = GameObject.Instantiate(prefab, randomPos, Quaternion.identity);
            go.name = prefab.name + i;
            go.transform.SetParent(_charactors, true);
        }
    }

    public void SpawnBlock(int count)
    {
        NativeArray<Entity> blockEntityArray = new NativeArray<Entity>(count, Allocator.Temp);
        entityManager.Instantiate(blockPrefabEntity, blockEntityArray);
        for (int i = 0; i < blockEntityArray.Length; i++)
        {
            Entity blockEntity = blockEntityArray[i];
            Common.GetWalkableXY(out int randomX, out int randomY);
            int index = randomX + randomY * GridSet.Instance.GetChosenGridInfo().width;
            NativeArray<MapNode> mapNodeArray = MapSystem.mapNodeArray;
            MapNode mapNode = mapNodeArray[index];
            mapNode.isWalkable = false;
            mapNodeArray[index] = mapNode;

            float3 randomPosition = Common.GetXYToWorldPosition_Center(randomX, randomY);
            randomPosition += new float3(0, 1f, 0);
            entityManager.SetComponentData<Translation>(blockEntity, new Translation { Value = randomPosition });
        }


    }

    #endregion



}
