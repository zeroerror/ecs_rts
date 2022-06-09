using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ABMgr : UnitySingleton<ABMgr>
{
    private AssetBundle _mainAB;
    private AssetBundleManifest _manifest;
    private Dictionary<string, AssetBundle> _abDic = new Dictionary<string, AssetBundle>();
    private string _MainABName
    {
        get
        {
#if UNITY_IOS
            return  "ios";
#elif UNITY_ANDROID
            return "android";
#else
            return "pc";
#endif
        }
    }
    public Object LoadRes(string abName, string resName)
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        Object obj = ab.LoadAsset(resName);
        //if (obj is GameObject)
        //{
        //    return GameObject.Instantiate(obj);
        //}
        //else
        return obj;
    }
    public Object LoadRes(string abName, string resName, System.Type type)
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        Object obj = ab.LoadAsset(resName, type);
        //if (obj is GameObject)
        //    return GameObject.Instantiate(obj);
        //else
        return obj;
    }
    public Object LoadRes<T>(string abName, string resName) where T : Object
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        T obj = ab.LoadAsset<T>(resName);
        //if (obj is GameObject)
        //    return GameObject.Instantiate(obj);
        //else
        return obj;
    }
    public void LoadResAsync(string abName, string resName, UnityAction<Object> callBack)
    {
        StartCoroutine(ReallyLoadResAsync(abName, resName, callBack));
    }
    public void LoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
    {
        StartCoroutine(ReallyLoadResAsync(abName, resName, type, callBack));
    }
    public void LoadResAsync<T>(string abName, string resName, UnityAction<Object> callBack)
    {
        StartCoroutine(ReallyLoadResAsync<T>(abName, resName, callBack));
    }
    public static void Director(string dir)
    {
        DirectoryInfo d = new DirectoryInfo(dir);
        FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
        foreach (FileSystemInfo fsinfo in fsinfos)
        {
            if (fsinfo is DirectoryInfo)     //判断是否为文件夹
            {
                Director(fsinfo.FullName);//递归调用
            }
            else
            {
                Debug.Log(fsinfo.Name);
            }
        }
    }

    private void LoadAB(string abName)
    {
        //if (_mainAB == null)
        //{
        //    _mainAB = AssetBundle.LoadFromFile(Application.dataPath + "!assets/android");
        //    if (!_mainAB)
        //    {
        //        Debug.LogError("main AB包失败！！！！！");
        //    }
        //    else
        //    {
        //        _manifest = _mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        //        AssetBundle ab = null;
        //        string[] strs = _manifest.GetAllDependencies(abName);
        //        for (int i = 0; i < strs.Length; i++)
        //        {
        //            if (!_abDic.ContainsKey(strs[i]))
        //            {
        //                ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, strs[i].ToLower()));
        //                _abDic.Add(strs[i], ab);
        //            }
        //        }
        //    }
        //}

        //加载目标包
        if (!_abDic.ContainsKey(abName))
        {
            AssetBundle ab = null;
#if UNITY_EDITOR || UNITY_STANDALONE
            var path = Path.Combine(Application.streamingAssetsPath, abName.ToLower());
#elif UNITY_ANDROID
            var path = Application.dataPath + "!assets/" + abName.ToLower();
#endif
            var url = new System.Uri(path);
            var request = UnityWebRequestAssetBundle.GetAssetBundle(url.AbsoluteUri);
            request.SendWebRequest();
            while (!request.isDone) { if (request.isNetworkError) { Debug.Log(request.error); return; } };
            ab = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
            if (!ab)
            {
                Debug.LogError("AB包-" + abName + " 加载失败！");
            }
            else
            {
                _abDic.Add(abName, ab);
                //Debug.Log("AB包-" + abName + " 成功！");
            }
        }
    }
    private void LoadALLAB()
    {
        StartCoroutine(ReallyLoadAllAB());
    }
    IEnumerator ReallyLoadResAsync(string abName, string resName, UnityAction<Object> callBack)
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        AssetBundleRequest abr = ab.LoadAssetAsync(resName);
        yield return abr;

        if (abr.asset is GameObject)
            callBack(Instantiate(abr.asset));
        else
            callBack(abr.asset);
    }
    IEnumerator ReallyLoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        AssetBundleRequest abr = ab.LoadAssetAsync(resName, type);
        yield return abr;

        if (abr.asset is GameObject)
        {
            callBack(Instantiate(abr.asset));
        }
        else
            callBack(abr.asset);
    }
    IEnumerator ReallyLoadResAsync<T>(string abName, string resName, UnityAction<Object> callBack)
    {
        LoadAB(abName);
        AssetBundle ab = _abDic[abName];
        AssetBundleRequest abr = ab.LoadAssetAsync<T>(resName);
        yield return abr;

        if (abr.asset is GameObject)
        {
            callBack(Instantiate(abr.asset));
        }
        else
            callBack(abr.asset);
    }
    IEnumerator ReallyLoadAllAB()
    {
        if (_mainAB == null)
        {
            _mainAB = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, _MainABName.ToLower()));
            _manifest = _mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        string[] allABName = _manifest.GetAllAssetBundles();
        for (int i = 0; i < allABName.Length; i++)
        {
            if (!_abDic.ContainsKey(allABName[i]))
            {
                //Debug.Log(Time.frameCount + ": 异步加载AB包--" + allABName[i]);
                AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, allABName[i].ToLower()));
                yield return abcr;
                _abDic.Add(abcr.assetBundle.name, abcr.assetBundle);
            }
        }
        Debug.Log(Time.frameCount + ": 异步加载所有AB包完成！");
    }

}
