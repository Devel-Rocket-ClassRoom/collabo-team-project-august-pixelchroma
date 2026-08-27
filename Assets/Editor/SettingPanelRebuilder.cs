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
            Transform panel = root.transform.Find("Panel");
            if (panel == null)
            {
                panel = root.transform.childCount > 0 ? root.transform.GetChild(0) : null;
                if (panel == null)
                {
                    Debug.LogError("Panel을 찾을 수 없습니다.");
                    return;
                }
            }

            while (panel.childCount > 0)
                Object.DestroyImmediate(panel.GetChild(0).gameObject);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(920, 1200);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0.93f, 0.95f, 0.97f, 0.97f);
            panelBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panelBg.type = Image.Type.Sliced;

            VerticalLayoutGroup panelVlg = panel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (panelVlg == null) panelVlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(20, 20, 10, 20);
            panelVlg.spacing = 0;
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = false;
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;

            TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/4.Image/Font/Galmuri11 SDF.asset");
            if (tmpFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
                if (guids.Length > 0)
                    tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // === 1. TITLE BAR ===
            GameObject titleBar = CreateChild(panel, "TitleBar", 0, 100);
            AddLayoutElement(titleBar, -1, 100);

            GameObject titleText = CreateTMPText(titleBar.transform, "Title_Text", "옵션", 48, tmpFont);
            RectTransform titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.sizeDelta = Vector2.zero;
            TMP_Text titleTmp = titleText.GetComponent<TMP_Text>();
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.15f, 0.2f, 0.3f, 1f);
            titleTmp.fontStyle = FontStyles.Bold;

            // Title divider line
            GameObject titleDivider = CreateChild(titleBar.transform, "Divider", 0, 2);
            RectTransform divRect = titleDivider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.05f, 0f);
            divRect.anchorMax = new Vector2(0.95f, 0f);
            divRect.sizeDelta = new Vector2(0, 2);
            divRect.anchoredPosition = new Vector2(0, 5);
            titleDivider.AddComponent<CanvasRenderer>();
            Image divImg = titleDivider.AddComponent<Image>();
            divImg.color = new Color(0.7f, 0.75f, 0.82f, 0.6f);

            // === 2. BODY (Sidebar + Content) ===
            GameObject body = CreateChild(panel, "Body", 0, 900);
            AddLayoutElement(body, -1, 900);
            HorizontalLayoutGroup bodyHlg = body.AddComponent<HorizontalLayoutGroup>();
            bodyHlg.padding = new RectOffset(0, 0, 10, 10);
            bodyHlg.spacing = 0;
            bodyHlg.childControlWidth = false;
            bodyHlg.childControlHeight = true;
            bodyHlg.childForceExpandWidth = false;
            bodyHlg.childForceExpandHeight = true;

            // === 2a. SIDEBAR ===
            GameObject sidebar = CreateChild(body.transform, "Sidebar", 170, 0);
            AddLayoutElement(sidebar, 170, -1);
            VerticalLayoutGroup sideVlg = sidebar.AddComponent<VerticalLayoutGroup>();
            sideVlg.padding = new RectOffset(5, 5, 20, 20);
            sideVlg.spacing = 8;
            sideVlg.childAlignment = TextAnchor.UpperCenter;
            sideVlg.childControlWidth = true;
            sideVlg.childControlHeight = false;
            sideVlg.childForceExpandWidth = true;
            sideVlg.childForceExpandHeight = false;

            string[] tabNames = { "게임", "그래픽", "음량", "알림", "언어" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                GameObject tab = CreateChild(sidebar.transform, "Tab_" + tabNames[i], 0, 80);
                AddLayoutElement(tab, -1, 80);
                tab.AddComponent<CanvasRenderer>();
                Image tabImg = tab.AddComponent<Image>();
                tabImg.color = (i == 2)
                    ? new Color(0.96f, 0.97f, 1f, 1f)
                    : new Color(1f, 1f, 1f, 0f);
                tabImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                tabImg.type = Image.Type.Sliced;
                Button tabBtn = tab.AddComponent<Button>();
                tabBtn.targetGraphic = tabImg;

                var colors = tabBtn.colors;
                colors.normalColor = (i == 2) ? new Color(0.96f, 0.97f, 1f, 1f) : new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = new Color(0.92f, 0.95f, 0.98f, 1f);
                colors.pressedColor = new Color(0.85f, 0.9f, 0.95f, 1f);
                tabBtn.colors = colors;

                GameObject tabText = CreateTMPText(tab.transform, "Text", tabNames[i], 30, tmpFont);
                RectTransform tabTextRect = tabText.GetComponent<RectTransform>();
                tabTextRect.anchorMin = Vector2.zero;
                tabTextRect.anchorMax = Vector2.one;
                tabTextRect.sizeDelta = Vector2.zero;
                TMP_Text tabTmp = tabText.GetComponent<TMP_Text>();
                tabTmp.alignment = TextAlignmentOptions.Center;
                tabTmp.color = new Color(0.2f, 0.3f, 0.45f, 1f);
                tabTmp.fontStyle = (i == 2) ? FontStyles.Bold : FontStyles.Normal;
            }

            // Sidebar vertical divider
            GameObject sideDiv = CreateChild(body.transform, "SidebarDivider", 2, 0);
            AddLayoutElement(sideDiv, 2, -1);
            sideDiv.AddComponent<CanvasRenderer>();
            Image sideDivImg = sideDiv.AddComponent<Image>();
            sideDivImg.color = new Color(0.75f, 0.8f, 0.85f, 0.5f);

            // === 2b. CONTENT AREA ===
            GameObject content = CreateChild(body.transform, "Content", 706, 0);
            AddLayoutElement(content, 706, -1);
            content.AddComponent<CanvasRenderer>();
            Image contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.97f, 0.98f, 0.99f, 1f);

            VerticalLayoutGroup contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(30, 30, 40, 30);
            contentVlg.spacing = 30;
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            string[] sliderNames = { "BGM", "효과음", "보이스" };
            string[] sliderObjNames = { "BGM_Setting_Slider", "SFX_Setting_Slider", "Voice_Setting_Slider" };

            for (int i = 0; i < sliderNames.Length; i++)
            {
                GameObject row = CreateChild(content.transform, sliderObjNames[i], 0, 70);
                AddLayoutElement(row, -1, 70);
                HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
                rowHlg.padding = new RectOffset(10, 10, 5, 5);
                rowHlg.spacing = 15;
                rowHlg.childAlignment = TextAnchor.MiddleCenter;
                rowHlg.childControlWidth = false;
                rowHlg.childControlHeight = true;
                rowHlg.childForceExpandWidth = false;
                rowHlg.childForceExpandHeight = true;

                // Speaker icon
                GameObject icon = CreateChild(row.transform, "Icon", 40, 40);
                AddLayoutElement(icon, 40, 40);
                icon.AddComponent<CanvasRenderer>();
                Image iconImg = icon.AddComponent<Image>();
                iconImg.color = new Color(0.5f, 0.65f, 0.85f, 1f);
                iconImg.preserveAspect = true;

                // Label
                GameObject label = CreateTMPText(row.transform, "Label", sliderNames[i], 28, tmpFont);
                AddLayoutElement(label, 90, -1);
                TMP_Text labelTmp = label.GetComponent<TMP_Text>();
                labelTmp.alignment = TextAlignmentOptions.Left;
                labelTmp.color = new Color(0.25f, 0.3f, 0.4f, 1f);

                // Slider
                CreateSlider(row.transform, "Slider", 300, 35);

                // Mute label
                GameObject muteLabel = CreateTMPText(row.transform, "MuteLabel", "음소거", 24, tmpFont);
                AddLayoutElement(muteLabel, 80, -1);
                TMP_Text muteTmp = muteLabel.GetComponent<TMP_Text>();
                muteTmp.alignment = TextAlignmentOptions.Center;
                muteTmp.color = new Color(0.35f, 0.4f, 0.5f, 1f);

                // Mute checkbox
                GameObject muteToggle = CreateChild(row.transform, "MuteToggle", 36, 36);
                AddLayoutElement(muteToggle, 36, 36);
                muteToggle.AddComponent<CanvasRenderer>();
                Image toggleBg = muteToggle.AddComponent<Image>();
                toggleBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                toggleBg.type = Image.Type.Sliced;
                toggleBg.color = Color.white;

                Toggle toggle = muteToggle.AddComponent<Toggle>();
                toggle.targetGraphic = toggleBg;

                GameObject checkmark = CreateChild(muteToggle.transform, "Checkmark", 0, 0);
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

            // Spacer to push 기본값 to bottom-right
            GameObject spacer = CreateChild(content.transform, "Spacer", 0, 10);
            AddLayoutElement(spacer, -1, -1, 1);
            LayoutElement spacerLE = spacer.GetComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1;

            // 기본값 button row (right-aligned)
            GameObject defaultBtnRow = CreateChild(content.transform, "DefaultButtonRow", 0, 55);
            AddLayoutElement(defaultBtnRow, -1, 55);
            HorizontalLayoutGroup defRowHlg = defaultBtnRow.AddComponent<HorizontalLayoutGroup>();
            defRowHlg.childAlignment = TextAnchor.MiddleRight;
            defRowHlg.childControlWidth = false;
            defRowHlg.childControlHeight = false;
            defRowHlg.childForceExpandWidth = false;
            defRowHlg.childForceExpandHeight = false;

            GameObject defSpacer = CreateChild(defaultBtnRow.transform, "Spacer", 0, 0);
            AddLayoutElement(defSpacer, -1, -1, 1);

            GameObject defaultBtn = CreateChild(defaultBtnRow.transform, "Default_Button", 130, 45);
            AddLayoutElement(defaultBtn, 130, 45);
            defaultBtn.AddComponent<CanvasRenderer>();
            Image defBtnImg = defaultBtn.AddComponent<Image>();
            defBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            defBtnImg.type = Image.Type.Sliced;
            defBtnImg.color = new Color(0.7f, 0.82f, 0.92f, 1f);
            Button defBtnComp = defaultBtn.AddComponent<Button>();
            defBtnComp.targetGraphic = defBtnImg;

            GameObject defBtnText = CreateTMPText(defaultBtn.transform, "Text", "기본값", 24, tmpFont);
            RectTransform defBtnTextRect = defBtnText.GetComponent<RectTransform>();
            defBtnTextRect.anchorMin = Vector2.zero;
            defBtnTextRect.anchorMax = Vector2.one;
            defBtnTextRect.sizeDelta = Vector2.zero;
            TMP_Text defTmp = defBtnText.GetComponent<TMP_Text>();
            defTmp.alignment = TextAlignmentOptions.Center;
            defTmp.color = new Color(0.2f, 0.3f, 0.45f, 1f);

            // === 3. BOTTOM DIVIDER ===
            GameObject bottomDivider = CreateChild(panel, "BottomDivider", 0, 2);
            AddLayoutElement(bottomDivider, -1, 2);
            bottomDivider.AddComponent<CanvasRenderer>();
            Image btmDivImg = bottomDivider.AddComponent<Image>();
            btmDivImg.color = new Color(0.7f, 0.75f, 0.82f, 0.4f);

            // === 4. FOOTER with 닫기 button ===
            GameObject footer = CreateChild(panel, "Footer", 0, 160);
            AddLayoutElement(footer, -1, 160);
            VerticalLayoutGroup footerVlg = footer.AddComponent<VerticalLayoutGroup>();
            footerVlg.padding = new RectOffset(0, 0, 20, 20);
            footerVlg.childAlignment = TextAnchor.MiddleCenter;
            footerVlg.childControlWidth = false;
            footerVlg.childControlHeight = false;
            footerVlg.childForceExpandWidth = false;
            footerVlg.childForceExpandHeight = false;

            GameObject closeBtn = CreateChild(footer.transform, "Game_Exit_Button", 260, 90);
            AddLayoutElement(closeBtn, 260, 90);
            closeBtn.AddComponent<CanvasRenderer>();
            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.95f, 0.96f, 0.97f, 0.9f);

            Outline closeBtnOutline = closeBtn.AddComponent<Outline>();
            closeBtnOutline.effectColor = new Color(0.6f, 0.65f, 0.72f, 0.7f);
            closeBtnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.targetGraphic = closeBtnImg;
            var closeBtnColors = closeBtnComp.colors;
            closeBtnColors.normalColor = new Color(0.95f, 0.96f, 0.97f, 0.9f);
            closeBtnColors.highlightedColor = new Color(0.9f, 0.92f, 0.95f, 1f);
            closeBtnColors.pressedColor = new Color(0.85f, 0.88f, 0.92f, 1f);
            closeBtnComp.colors = closeBtnColors;

            GameObject closeBtnText = CreateTMPText(closeBtn.transform, "Text", "닫기", 40, tmpFont);
            RectTransform closeBtnTextRect = closeBtnText.GetComponent<RectTransform>();
            closeBtnTextRect.anchorMin = Vector2.zero;
            closeBtnTextRect.anchorMax = Vector2.one;
            closeBtnTextRect.sizeDelta = Vector2.zero;
            TMP_Text closeTmp = closeBtnText.GetComponent<TMP_Text>();
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            closeTmp.fontStyle = FontStyles.Bold;

            Debug.Log("[SettingPanelRebuilder] Setting_Panel 재구성 완료 (레퍼런스 기준)");
        }

        EditorUtility.DisplayDialog("완료",
            "Setting_Panel이 레퍼런스대로 재구성되었습니다.\n\n" +
            "구조:\n" +
            "- TitleBar: 옵션 제목 + 구분선\n" +
            "- Body: 사이드바(5탭) + 콘텐츠(슬라이더3개 + 기본값)\n" +
            "- Footer: 닫기 버튼\n\n" +
            "Unity에서 확인하세요.",
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
        fillImg.color = new Color(0.45f, 0.7f, 0.9f, 1f);

        GameObject handleArea = CreateChild(sliderObj.transform, "Handle Slide Area", 0, 0);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = CreateChild(handleArea.transform, "Handle", 24, 0);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(0, 1);
        handleRect.sizeDelta = new Vector2(24, 0);
        handle.AddComponent<CanvasRenderer>();
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImg.color = new Color(0.5f, 0.75f, 0.92f, 1f);

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
