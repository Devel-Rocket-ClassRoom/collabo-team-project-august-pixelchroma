using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SettingPanelRebuilder
{
    [MenuItem("Tools/SRPG UI/Setting_Panel 배치도 재구성")]
    public static void Rebuild()
    {
        string path = "Assets/3.Prefabs/UI/Setting_Panel.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("오류", "Setting_Panel.prefab을 찾을 수 없습니다.", "확인");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;
            Transform panel = root.transform.childCount > 0 ? root.transform.GetChild(0) : null;
            if (panel == null)
            {
                Debug.LogError("Panel 자식을 찾을 수 없습니다.");
                return;
            }

            // 기존 자식 모두 제거
            while (panel.childCount > 0)
                Object.DestroyImmediate(panel.GetChild(0).gameObject);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900, 600);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0.93f, 0.95f, 0.97f, 1f);
            panelBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panelBg.type = Image.Type.Sliced;

            // VerticalLayoutGroup on Panel
            VerticalLayoutGroup panelVlg = panel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (panelVlg == null) panelVlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(0, 0, 0, 0);
            panelVlg.spacing = 0;
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = false;
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;

            Font defaultFont = null;
            TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/4.Image/Font/Galmuri11 SDF.asset");
            if (tmpFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
                if (guids.Length > 0)
                    tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // === 1. TITLE BAR ===
            GameObject titleBar = CreateChild(panel, "TitleBar", 0, 50);
            AddLayoutElement(titleBar, -1, 50);
            HorizontalLayoutGroup titleHlg = titleBar.AddComponent<HorizontalLayoutGroup>();
            titleHlg.padding = new RectOffset(20, 10, 5, 5);
            titleHlg.childAlignment = TextAnchor.MiddleCenter;
            titleHlg.childControlWidth = true;
            titleHlg.childControlHeight = true;
            titleHlg.childForceExpandWidth = true;
            titleHlg.childForceExpandHeight = true;

            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.85f, 0.91f, 0.96f, 1f);

            // Title spacer (left)
            GameObject titleSpacerL = CreateChild(titleBar.transform, "Spacer_L", 40, 40);
            AddLayoutElement(titleSpacerL, 40, 40);

            // Title text
            GameObject titleText = CreateTMPText(titleBar.transform, "Title_Text", "옵션", 36, tmpFont);
            LayoutElement titleTextLE = titleText.GetComponent<LayoutElement>();
            if (titleTextLE == null) titleTextLE = titleText.AddComponent<LayoutElement>();
            titleTextLE.flexibleWidth = 1;
            TMP_Text titleTmp = titleText.GetComponent<TMP_Text>();
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.2f, 0.3f, 0.4f, 1f);

            // Close button
            GameObject closeBtn = CreateChild(titleBar.transform, "Close_Button", 40, 40);
            AddLayoutElement(closeBtn, 40, 40);
            closeBtn.AddComponent<CanvasRenderer>();
            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(1f, 1f, 1f, 0f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.targetGraphic = closeBtnImg;

            GameObject closeX = CreateTMPText(closeBtn.transform, "X_Text", "✕", 28, tmpFont);
            RectTransform closeXRect = closeX.GetComponent<RectTransform>();
            closeXRect.anchorMin = Vector2.zero;
            closeXRect.anchorMax = Vector2.one;
            closeXRect.sizeDelta = Vector2.zero;
            TMP_Text closeXTmp = closeX.GetComponent<TMP_Text>();
            closeXTmp.alignment = TextAlignmentOptions.Center;
            closeXTmp.color = new Color(0.4f, 0.4f, 0.5f, 1f);

            // === 2. BODY (Sidebar + Content) ===
            GameObject body = CreateChild(panel, "Body", 0, 500);
            AddLayoutElement(body, -1, 500);
            HorizontalLayoutGroup bodyHlg = body.AddComponent<HorizontalLayoutGroup>();
            bodyHlg.padding = new RectOffset(0, 0, 0, 0);
            bodyHlg.spacing = 0;
            bodyHlg.childControlWidth = false;
            bodyHlg.childControlHeight = true;
            bodyHlg.childForceExpandWidth = false;
            bodyHlg.childForceExpandHeight = true;

            // === 2a. SIDEBAR ===
            GameObject sidebar = CreateChild(body.transform, "Sidebar", 160, 500);
            AddLayoutElement(sidebar, 160, -1);
            VerticalLayoutGroup sideVlg = sidebar.AddComponent<VerticalLayoutGroup>();
            sideVlg.padding = new RectOffset(5, 5, 10, 10);
            sideVlg.spacing = 5;
            sideVlg.childAlignment = TextAnchor.UpperCenter;
            sideVlg.childControlWidth = true;
            sideVlg.childControlHeight = false;
            sideVlg.childForceExpandWidth = true;
            sideVlg.childForceExpandHeight = false;

            Image sidebarBg = sidebar.AddComponent<Image>();
            sidebarBg.color = new Color(0.88f, 0.92f, 0.96f, 1f);

            string[] tabNames = { "게임", "그래픽", "음량", "알림", "언어" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                GameObject tab = CreateChild(sidebar.transform, "Tab_" + tabNames[i], 150, 70);
                AddLayoutElement(tab, -1, 70);
                tab.AddComponent<CanvasRenderer>();
                Image tabImg = tab.AddComponent<Image>();
                tabImg.color = (i == 2)
                    ? new Color(0.95f, 0.97f, 1f, 1f)
                    : new Color(0.88f, 0.92f, 0.96f, 0f);
                tabImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                tabImg.type = Image.Type.Sliced;
                Button tabBtn = tab.AddComponent<Button>();
                tabBtn.targetGraphic = tabImg;

                var colors = tabBtn.colors;
                colors.normalColor = (i == 2) ? new Color(0.95f, 0.97f, 1f, 1f) : new Color(0.88f, 0.92f, 0.96f, 0f);
                colors.highlightedColor = new Color(0.92f, 0.95f, 0.98f, 1f);
                colors.pressedColor = new Color(0.85f, 0.9f, 0.95f, 1f);
                tabBtn.colors = colors;

                GameObject tabText = CreateTMPText(tab.transform, "Text", tabNames[i], 24, tmpFont);
                RectTransform tabTextRect = tabText.GetComponent<RectTransform>();
                tabTextRect.anchorMin = Vector2.zero;
                tabTextRect.anchorMax = Vector2.one;
                tabTextRect.sizeDelta = Vector2.zero;
                TMP_Text tabTmp = tabText.GetComponent<TMP_Text>();
                tabTmp.alignment = TextAlignmentOptions.Center;
                tabTmp.color = new Color(0.25f, 0.35f, 0.5f, 1f);
            }

            // === 2b. CONTENT AREA ===
            GameObject content = CreateChild(body.transform, "Content", 740, 500);
            AddLayoutElement(content, 740, -1);
            content.AddComponent<CanvasRenderer>();
            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.96f, 0.97f, 0.98f, 1f);

            VerticalLayoutGroup contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(30, 30, 30, 30);
            contentVlg.spacing = 20;
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            string[] sliderNames = { "BGM", "효과음", "보이스" };
            string[] sliderObjNames = { "BGM_Setting_Slider", "SFX_Setting_Slider", "Voice_Setting_Slider" };

            for (int i = 0; i < sliderNames.Length; i++)
            {
                GameObject row = CreateChild(content.transform, sliderObjNames[i], 680, 60);
                AddLayoutElement(row, -1, 60);
                HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
                rowHlg.padding = new RectOffset(10, 10, 5, 5);
                rowHlg.spacing = 10;
                rowHlg.childAlignment = TextAnchor.MiddleCenter;
                rowHlg.childControlWidth = false;
                rowHlg.childControlHeight = true;
                rowHlg.childForceExpandWidth = false;
                rowHlg.childForceExpandHeight = true;

                // Speaker icon placeholder
                GameObject icon = CreateChild(row.transform, "Icon", 40, 40);
                AddLayoutElement(icon, 40, 40);
                icon.AddComponent<CanvasRenderer>();
                Image iconImg = icon.AddComponent<Image>();
                iconImg.color = new Color(0.5f, 0.7f, 0.9f, 1f);
                iconImg.preserveAspect = true;

                // Label
                GameObject label = CreateTMPText(row.transform, "Label", sliderNames[i], 22, tmpFont);
                AddLayoutElement(label, 70, -1);
                TMP_Text labelTmp = label.GetComponent<TMP_Text>();
                labelTmp.alignment = TextAlignmentOptions.Left;
                labelTmp.color = new Color(0.3f, 0.4f, 0.5f, 1f);

                // Slider
                CreateSlider(row.transform, "Slider", 340, 30);

                // Mute text
                GameObject muteLabel = CreateTMPText(row.transform, "MuteLabel", "음소거", 20, tmpFont);
                AddLayoutElement(muteLabel, 60, -1);
                TMP_Text muteTmp = muteLabel.GetComponent<TMP_Text>();
                muteTmp.alignment = TextAlignmentOptions.Center;
                muteTmp.color = new Color(0.4f, 0.5f, 0.6f, 1f);

                // Mute toggle (checkbox)
                GameObject muteToggle = CreateChild(row.transform, "MuteToggle", 30, 30);
                AddLayoutElement(muteToggle, 30, 30);
                muteToggle.AddComponent<CanvasRenderer>();
                Image toggleBg = muteToggle.AddComponent<Image>();
                toggleBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                toggleBg.type = Image.Type.Sliced;
                toggleBg.color = Color.white;

                Toggle toggle = muteToggle.AddComponent<Toggle>();
                toggle.targetGraphic = toggleBg;

                GameObject checkmark = CreateChild(muteToggle.transform, "Checkmark", 20, 20);
                RectTransform checkRect = checkmark.GetComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(0.1f, 0.1f);
                checkRect.anchorMax = new Vector2(0.9f, 0.9f);
                checkRect.sizeDelta = Vector2.zero;
                checkRect.anchoredPosition = Vector2.zero;
                checkmark.AddComponent<CanvasRenderer>();
                Image checkImg = checkmark.AddComponent<Image>();
                checkImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
                checkImg.color = new Color(0.3f, 0.6f, 0.9f, 1f);
                toggle.graphic = checkImg;
            }

            // Spacer
            GameObject spacer = CreateChild(content.transform, "Spacer", 10, 10);
            AddLayoutElement(spacer, -1, -1, 1);

            // Default button
            GameObject defaultBtnRow = CreateChild(content.transform, "DefaultButtonRow", 680, 50);
            AddLayoutElement(defaultBtnRow, -1, 50);
            HorizontalLayoutGroup defRowHlg = defaultBtnRow.AddComponent<HorizontalLayoutGroup>();
            defRowHlg.childAlignment = TextAnchor.MiddleRight;
            defRowHlg.childControlWidth = false;
            defRowHlg.childControlHeight = false;
            defRowHlg.childForceExpandWidth = true;
            defRowHlg.childForceExpandHeight = false;

            GameObject defSpacer = CreateChild(defaultBtnRow.transform, "Spacer", 10, 10);
            AddLayoutElement(defSpacer, -1, -1, 1);

            GameObject defaultBtn = CreateChild(defaultBtnRow.transform, "Default_Button", 120, 40);
            AddLayoutElement(defaultBtn, 120, 40);
            defaultBtn.AddComponent<CanvasRenderer>();
            Image defBtnImg = defaultBtn.AddComponent<Image>();
            defBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            defBtnImg.type = Image.Type.Sliced;
            defBtnImg.color = new Color(0.7f, 0.85f, 0.95f, 1f);
            Button defBtnComp = defaultBtn.AddComponent<Button>();
            defBtnComp.targetGraphic = defBtnImg;

            GameObject defBtnText = CreateTMPText(defaultBtn.transform, "Text", "기본값", 22, tmpFont);
            RectTransform defBtnTextRect = defBtnText.GetComponent<RectTransform>();
            defBtnTextRect.anchorMin = Vector2.zero;
            defBtnTextRect.anchorMax = Vector2.one;
            defBtnTextRect.sizeDelta = Vector2.zero;
            TMP_Text defTmp = defBtnText.GetComponent<TMP_Text>();
            defTmp.alignment = TextAlignmentOptions.Center;
            defTmp.color = new Color(0.2f, 0.35f, 0.5f, 1f);

            // === 3. FOOTER ===
            GameObject footer = CreateChild(panel, "Footer", 0, 30);
            AddLayoutElement(footer, -1, 30);
            Image footerBg = footer.AddComponent<Image>();
            footerBg.color = new Color(0.9f, 0.93f, 0.96f, 0.5f);

            Debug.Log("[SettingPanelRebuilder] Setting_Panel 재구성 완료");
        }

        EditorUtility.DisplayDialog("완료",
            "Setting_Panel이 스크린샷 배치도대로 재구성되었습니다.\n\n" +
            "구조:\n" +
            "- TitleBar: 옵션 + X 닫기\n" +
            "- Body: 사이드바(5탭) + 콘텐츠(슬라이더3개)\n" +
            "- 기본값 버튼\n\n" +
            "각 Icon에 스피커 스프라이트를 할당하세요.",
            "확인");
    }

    static GameObject CreateChild(Transform parent, string name, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        return go;
    }

    static void AddLayoutElement(GameObject go, float prefW, float prefH, float flexW = -1)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexW >= 0) le.flexibleWidth = flexW;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text, float fontSize, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = false;
        if (font != null) tmp.font = font;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return go;
    }

    static void CreateSlider(Transform parent, string name, float width, float height)
    {
        GameObject sliderObj = CreateChild(parent, name, width, height);
        AddLayoutElement(sliderObj, width, height);
        sliderObj.AddComponent<CanvasRenderer>();

        // Background
        GameObject bg = CreateChild(sliderObj.transform, "Background", 0, 0);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.82f, 0.87f, 0.92f, 1f);

        // Fill Area
        GameObject fillArea = CreateChild(sliderObj.transform, "Fill Area", 0, 0);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = CreateChild(fillArea.transform, "Fill", 0, 0);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = new Vector2(10, 0);
        fill.AddComponent<CanvasRenderer>();
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.45f, 0.75f, 0.95f, 1f);

        // Handle Area
        GameObject handleArea = CreateChild(sliderObj.transform, "Handle Slide Area", 0, 0);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = CreateChild(handleArea.transform, "Handle", 20, 0);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(0, 1);
        handleRect.sizeDelta = new Vector2(20, 0);
        handle.AddComponent<CanvasRenderer>();
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImg.color = new Color(0.55f, 0.8f, 0.95f, 1f);

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.5f;
    }
}
