using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MiddleUpperButtonSetup
{
    [MenuItem("Tools/SRPG UI/Middle_Upper 버튼 구조 변환 (아이콘+텍스트 가로배치)")]
    public static void SetupButtons()
    {
        string prefabPath = "Assets/3.Prefabs/UI/Middle_Upper.prefab";
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("오류", "Middle_Upper.prefab을 찾을 수 없습니다.", "확인");
            return;
        }

        string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabAssetPath))
        {
            GameObject root = editScope.prefabContentsRoot;
            int converted = 0;

            foreach (Transform child in root.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn == null) continue;

                Image btnImage = child.GetComponent<Image>();
                TMP_Text existingText = child.GetComponentInChildren<TMP_Text>();
                if (existingText == null) continue;

                if (child.GetComponent<HorizontalLayoutGroup>() != null)
                    continue;

                HorizontalLayoutGroup hlg = child.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.padding = new RectOffset(8, 8, 4, 4);

                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObj.transform.SetParent(child, false);
                iconObj.transform.SetAsFirstSibling();

                Image iconImage = iconObj.GetComponent<Image>();
                iconImage.color = new Color(1f, 1f, 1f, 0.6f);
                iconImage.preserveAspect = true;

                LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 60;
                iconLayout.preferredHeight = 60;
                iconLayout.flexibleWidth = 0.3f;
                iconLayout.flexibleHeight = 0.3f;

                AspectRatioFitter arf = iconObj.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                arf.aspectRatio = 1f;

                LayoutElement textLayout = existingText.gameObject.GetComponent<LayoutElement>();
                if (textLayout == null)
                    textLayout = existingText.gameObject.AddComponent<LayoutElement>();
                textLayout.flexibleWidth = 1f;
                textLayout.flexibleHeight = 1f;
                textLayout.minWidth = 40;

                existingText.enableAutoSizing = true;
                existingText.fontSizeMin = 14;
                existingText.fontSizeMax = 60;

                RectTransform textRect = existingText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                converted++;
            }

            Debug.Log($"[MiddleUpperButtonSetup] {converted}개 버튼 변환 완료");
        }

        EditorUtility.DisplayDialog("완료",
            "Middle_Upper 버튼 구조 변환 완료!\n\n" +
            "각 버튼에 HorizontalLayoutGroup + Icon(Image) 추가됨.\n" +
            "Icon에 원하는 스프라이트를 드래그하세요.\n" +
            "버튼 크기를 조절하면 아이콘도 같이 줄고 커집니다.",
            "확인");
    }
}
