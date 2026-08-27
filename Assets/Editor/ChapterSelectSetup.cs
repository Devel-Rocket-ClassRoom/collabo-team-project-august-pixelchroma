using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ChapterSelectSetup
{
    [MenuItem("Tools/SRPG UI/Chapter Select - Option_Button에 SettingPopup 연결")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();

        Transform optionButton = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            optionButton = FindInChildren(root.transform, "Option_Button");
            if (optionButton != null) break;
        }

        if (optionButton == null)
        {
            EditorUtility.DisplayDialog("오류", "Option_Button을 찾을 수 없습니다.\n2.Chapter Select 씬을 열어주세요.", "확인");
            return;
        }

        Transform buttonChild = optionButton.Find("Button");
        if (buttonChild == null)
        {
            EditorUtility.DisplayDialog("오류", "Option_Button 안에 Button 자식이 없습니다.", "확인");
            return;
        }

        Button btn = buttonChild.GetComponent<Button>();
        if (btn == null)
        {
            EditorUtility.DisplayDialog("오류", "Button 자식에 Button 컴포넌트가 없습니다.", "확인");
            return;
        }

        SettingPopup existing = buttonChild.GetComponent<SettingPopup>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("알림", "이미 SettingPopup이 연결되어 있습니다.", "확인");
            return;
        }

        Undo.AddComponent<SettingPopup>(buttonChild.gameObject);
        EditorUtility.SetDirty(buttonChild.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("완료",
            "Option_Button > Button에 SettingPopup 컴포넌트 추가 완료!\n\n" +
            "Ctrl+S로 씬을 저장하세요.\n" +
            "Play하면 Button 클릭 시 Setting_Panel이 슬라이드 팝업됩니다.",
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
