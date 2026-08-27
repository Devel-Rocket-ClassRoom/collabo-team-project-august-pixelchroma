using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainGameOptionSetup
{
    [MenuItem("Tools/SRPG UI/MainGame - InGameoption에 SettingPopup 연결")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();

        Transform inGameOption = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            inGameOption = FindInChildren(root.transform, "InGameoption");
            if (inGameOption != null) break;
        }

        if (inGameOption == null)
        {
            EditorUtility.DisplayDialog("오류", "InGameoption을 찾을 수 없습니다.\n4.MainGame 씬을 열어주세요.", "확인");
            return;
        }

        Button btn = inGameOption.GetComponent<Button>();
        if (btn == null)
        {
            EditorUtility.DisplayDialog("오류", "InGameoption에 Button 컴포넌트가 없습니다.", "확인");
            return;
        }

        SettingPopup existing = inGameOption.GetComponent<SettingPopup>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("알림", "이미 SettingPopup이 연결되어 있습니다.", "확인");
            return;
        }

        Undo.AddComponent<SettingPopup>(inGameOption.gameObject);
        EditorUtility.SetDirty(inGameOption.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("완료",
            "InGameoption에 SettingPopup 컴포넌트 추가 완료!\n\n" +
            "Ctrl+S로 씬을 저장하세요.\n" +
            "Play하면 InGameoption 클릭 시 Setting_Panel이 슬라이드 팝업됩니다.",
            "확인");
    }

    static Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
