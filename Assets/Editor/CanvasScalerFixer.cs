using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CanvasScalerFixer
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 2220f);

    [MenuItem("Tools/SRPG UI/모든 씬 Canvas 반응형 설정")]
    public static void FixAllScenes()
    {
        if (!EditorUtility.DisplayDialog(
                "Canvas 반응형 설정",
                "모든 씬의 CanvasScaler를 ScaleWithScreenSize (1080x1920, Match 0.5)로 설정하고,\n" +
                "SafeAreaAdapter / CanvasAutoScaler 컴포넌트를 추가합니다.\n\n계속하시겠습니까?",
                "실행", "취소"))
            return;

        string currentScene = SceneManager.GetActiveScene().path;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/1.Sence" });
        int fixedCount = 0;

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int count = FixCanvasesInScene(scene);
            if (count > 0)
            {
                EditorSceneManager.SaveScene(scene);
                fixedCount += count;
                Debug.Log($"[CanvasScalerFixer] {path} — Canvas {count}개 수정됨");
            }
        }

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene);

        EditorUtility.DisplayDialog("완료", $"Canvas {fixedCount}개 수정 완료", "확인");
    }

    [MenuItem("Tools/SRPG UI/현재 씬 Canvas 반응형 설정")]
    public static void FixCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        int count = FixCanvasesInScene(scene);
        if (count > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("완료", $"현재 씬 Canvas {count}개 수정됨", "확인");
    }

    private static int FixCanvasesInScene(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                    canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    continue;

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                    scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

                Undo.RecordObject(scaler, "Fix CanvasScaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
                EditorUtility.SetDirty(scaler);

                if (canvas.GetComponent<CanvasAutoScaler>() == null)
                    Undo.AddComponent<CanvasAutoScaler>(canvas.gameObject);

                AddSafeAreaToCanvas(canvas);
                count++;
            }
        }
        return count;
    }

    private static void AddSafeAreaToCanvas(Canvas canvas)
    {
        Transform existing = canvas.transform.Find("SafeArea");
        if (existing != null && existing.GetComponent<SafeAreaAdapter>() != null)
            return;

        GameObject safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaAdapter));
        safeArea.transform.SetParent(canvas.transform, false);
        Undo.RegisterCreatedObjectUndo(safeArea, "Add SafeArea");

        RectTransform rect = safeArea.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
