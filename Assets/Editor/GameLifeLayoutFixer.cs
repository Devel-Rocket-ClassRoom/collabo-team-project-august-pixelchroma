using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameLifeLayoutFixer
{
    [MenuItem("Tools/SRPG UI/GameLife 레이아웃 정리")]
    public static void Fix()
    {
        var scene = EditorSceneManager.GetActiveScene();

        GameObject gameLifeObj = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindGameLife(root.transform);
            if (found != null)
            {
                gameLifeObj = found.gameObject;
                break;
            }
        }

        if (gameLifeObj == null)
        {
            EditorUtility.DisplayDialog("오류",
                "HorizontalLayoutGroup이 있는 GameLife 오브젝트를 찾을 수 없습니다.\n" +
                "1.MainMenu 씬을 열어주세요.", "확인");
            return;
        }

        // 잘못 생성된 Life_Icon 정리
        CleanupWrongLifeIcon(scene);

        Undo.RegisterFullObjectHierarchyUndo(gameLifeObj, "GameLife 레이아웃 정리");

        Transform gameLifeT = gameLifeObj.transform;

        // --- GameLife 컨테이너 설정 ---
        RectTransform gameLifeRect = gameLifeObj.GetComponent<RectTransform>();
        gameLifeRect.sizeDelta = new Vector2(340, 70);

        Image gameLifeBg = gameLifeObj.GetComponent<Image>();
        if (gameLifeBg != null)
        {
            gameLifeBg.color = new Color(1f, 1f, 1f, 0.85f);
            gameLifeBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            gameLifeBg.type = Image.Type.Sliced;
        }

        HorizontalLayoutGroup hlg = gameLifeObj.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 5;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.reverseArrangement = false;

        // --- 자식 찾기 ---
        Transform allGamelife = gameLifeT.Find("All_Gamelife");
        Transform remaining = gameLifeT.Find("remaining_gamelife");
        Transform plusBtn = gameLifeT.Find("Plus_GameLife");

        // --- 아이콘 추가 (없으면 생성) ---
        Transform icon = gameLifeT.Find("Life_Icon");
        if (icon == null)
        {
            GameObject iconObj = new GameObject("Life_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconObj, "Create Life_Icon");
            iconObj.transform.SetParent(gameLifeT, false);
            iconObj.layer = 5;
            icon = iconObj.transform;

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = new Color(0.3f, 0.85f, 0.4f, 1f);
            iconImg.preserveAspect = true;

            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 45;
            iconLE.preferredHeight = 45;
        }
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(45, 45);

        // --- 자식 순서: Icon → All_Gamelife → remaining_gamelife → Plus_GameLife ---
        icon.SetSiblingIndex(0);
        if (allGamelife != null) allGamelife.SetSiblingIndex(1);
        if (remaining != null) remaining.SetSiblingIndex(2);
        if (plusBtn != null) plusBtn.SetSiblingIndex(3);

        // --- All_Gamelife 설정 ---
        if (allGamelife != null)
        {
            RectTransform allRect = allGamelife.GetComponent<RectTransform>();
            allRect.sizeDelta = new Vector2(110, 50);

            LayoutElement allLE = allGamelife.GetComponent<LayoutElement>();
            if (allLE == null) allLE = allGamelife.gameObject.AddComponent<LayoutElement>();
            allLE.preferredWidth = 110;
            allLE.flexibleWidth = -1;

            TMP_Text allTmp = allGamelife.GetComponent<TMP_Text>();
            if (allTmp != null)
            {
                allTmp.fontSize = 36;
                allTmp.alignment = TextAlignmentOptions.Right;
                allTmp.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            }
        }

        // --- remaining_gamelife 설정 ---
        if (remaining != null)
        {
            RectTransform remRect = remaining.GetComponent<RectTransform>();
            remRect.sizeDelta = new Vector2(80, 50);

            LayoutElement remLE = remaining.GetComponent<LayoutElement>();
            if (remLE == null) remLE = remaining.gameObject.AddComponent<LayoutElement>();
            remLE.preferredWidth = 80;
            remLE.flexibleWidth = -1;

            TMP_Text remTmp = remaining.GetComponent<TMP_Text>();
            if (remTmp != null)
            {
                remTmp.fontSize = 36;
                remTmp.alignment = TextAlignmentOptions.Left;
                remTmp.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            }
        }

        // --- Plus_GameLife 설정 ---
        if (plusBtn != null)
        {
            RectTransform plusRect = plusBtn.GetComponent<RectTransform>();
            plusRect.sizeDelta = new Vector2(50, 50);

            LayoutElement plusLE = plusBtn.GetComponent<LayoutElement>();
            if (plusLE == null) plusLE = plusBtn.gameObject.AddComponent<LayoutElement>();
            plusLE.preferredWidth = 50;
            plusLE.preferredHeight = 50;

            Image plusImg = plusBtn.GetComponent<Image>();
            if (plusImg != null)
                plusImg.color = new Color(0.3f, 0.85f, 0.5f, 1f);

            foreach (Transform child in plusBtn)
            {
                TMP_Text t = child.GetComponent<TMP_Text>();
                if (t != null)
                {
                    t.fontSize = 36;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.3f, 0.85f, 0.5f, 1f);
                    break;
                }
            }
        }

        EditorUtility.SetDirty(gameLifeObj);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("완료",
            "GameLife 레이아웃 정리 완료!\n\n" +
            "배치: [⚡아이콘] [999/] [184] [+]\n\n" +
            "Life_Icon에 에너지 스프라이트를 할당하세요.\n" +
            "Ctrl+S로 씬을 저장하세요.",
            "확인");
    }

    static Transform FindGameLife(Transform parent)
    {
        if (parent.name == "GameLife" && parent.GetComponent<HorizontalLayoutGroup>() != null)
            return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindGameLife(child);
            if (found != null) return found;
        }
        return null;
    }

    static void CleanupWrongLifeIcon(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            CleanupLifeIconInChildren(root.transform);
        }
    }

    static void CleanupLifeIconInChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name == "Life_Icon")
            {
                Transform gameLifeParent = child.parent;
                if (gameLifeParent == null || gameLifeParent.GetComponent<HorizontalLayoutGroup>() == null)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    Debug.Log("[GameLifeLayoutFixer] 잘못 배치된 Life_Icon 삭제: " + gameLifeParent?.name);
                }
            }
            else
            {
                CleanupLifeIconInChildren(child);
            }
        }
    }
}
