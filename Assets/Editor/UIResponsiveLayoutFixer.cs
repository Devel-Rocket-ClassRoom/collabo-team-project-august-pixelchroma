using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIResponsiveLayoutFixer
{
    private static readonly string[] OverlayPrefabPaths =
    {
        "Assets/3.Prefabs/4.MainGame/DeploymentUI.prefab",
        "Assets/3.Prefabs/4.MainGame/AttackPreviewUI.prefab"
    };

    [MenuItem("Tools/SRPG UI/5차 전투 UI 안전영역 적용")]
    public static void Apply()
    {
        foreach (string path in OverlayPrefabPaths)
            ApplyToPrefab(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SRPG UI] 5차 전투 UI 안전영역 적용 완료");
    }

    private static void ApplyToPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            throw new InvalidOperationException($"전투 UI 프리팹을 찾을 수 없습니다: {path}");

        try
        {
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = root.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2220f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            Transform existing = root.transform.Find("SafeArea");
            RectTransform safeArea;
            if (existing == null)
            {
                GameObject safeObject = new GameObject(
                    "SafeArea", typeof(RectTransform), typeof(SafeAreaAdapter));
                safeArea = safeObject.GetComponent<RectTransform>();
                safeArea.SetParent(root.transform, false);
                safeArea.anchorMin = Vector2.zero;
                safeArea.anchorMax = Vector2.one;
                safeArea.offsetMin = Vector2.zero;
                safeArea.offsetMax = Vector2.zero;

                for (int index = root.transform.childCount - 2; index >= 0; index--)
                    root.transform.GetChild(index).SetParent(safeArea, false);
            }
            else
            {
                safeArea = existing as RectTransform;
                if (safeArea == null)
                    throw new InvalidOperationException($"SafeArea에 RectTransform이 없습니다: {path}");
                if (safeArea.GetComponent<SafeAreaAdapter>() == null)
                    safeArea.gameObject.AddComponent<SafeAreaAdapter>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
