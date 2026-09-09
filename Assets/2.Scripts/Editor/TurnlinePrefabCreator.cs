#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TurnlinePrefabCreator
{
    [MenuItem("Tools/Create Turnline Prefab")]
    public static void Create()
    {
        GameObject obj = new GameObject("Turnline", typeof(TurnBannerUI));

        const string path = "Assets/3.Prefabs/UI/Turnline.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/3.Prefabs/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/3.Prefabs"))
                AssetDatabase.CreateFolder("Assets", "3.Prefabs");
            AssetDatabase.CreateFolder("Assets/3.Prefabs", "UI");
        }

        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);

        Debug.Log($"Turnline prefab created at {path}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }
}
#endif
