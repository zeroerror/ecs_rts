using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Menu
{
    private static readonly TextEditor CopyTool = new TextEditor();

    [MenuItem("GameObject/CopyUIPath", priority = 5)]
    static void CopyPath()
    {
        Transform trans = Selection.activeTransform;
        if (!trans) return;
        CopyTool.text = GetPath(trans);
        CopyTool.SelectAll();
        CopyTool.Copy();
    }
    public static string GetPath(Transform trans)
    {
        if (!trans) return string.Empty;
        if (trans.GetComponent<Canvas>()) return string.Empty;
        if (!trans.parent.parent) return string.Empty;
        if (!trans.parent) return trans.name;

        return GetPath(trans.parent) + "/" + trans.name;
    }

}
