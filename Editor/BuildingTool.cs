using UnityEngine;
using UnityEditor;

public class BuildinTool
{
    [MenuItem("Tools/BuildingSetting", false, 100)]
    public static void Buidld()
    {
        Debug.Log("BuildingSetting Complete!");
        PlayerSettings.Android.forceSDCardPermission = true;//设置sdcard读取权限
    }
}