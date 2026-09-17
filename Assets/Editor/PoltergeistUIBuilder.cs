using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Generates the POLTERGEIST UI redesign prefabs step by step (see Design/UI_Mockups).
public static class PoltergeistUIBuilder
{
    private const string CommonFolder = "Assets/3.Prefabs/Common";
    private const string TitleFolder = "Assets/3.Prefabs/0.Tilte";
    private const string IconFolder = "Assets/4.Image/UI/Common";
    private const string FontPath = "Assets/6.Font/경기천년제목_Medium SDF.asset";
    private const string CatalogPath = "Assets/Resources/UIScreenCatalog.asset";
    private const string LegacyTitlePrefabGuid = "dd75a538b18f79548971e107c25d56d6";

    private const string SafeAreaPath = CommonFolder + "/UI_SafeArea.prefab";
    private const string CurrencyItemPath = CommonFolder + "/UI_CurrencyItem.prefab";
    private const string CommonTopBarPath = CommonFolder + "/UI_CommonTopBar.prefab";
    private const string PrimaryButtonPath = CommonFolder + "/UI_PrimaryButton.prefab";
    private const string SecondaryButtonPath = CommonFolder + "/UI_SecondaryButton.prefab";
    private const string IconButtonPath = CommonFolder + "/UI_IconButton.prefab";
    private const string CharacterCardPath = CommonFolder + "/UI_CharacterCard.prefab";
    private const string StageNodePath = CommonFolder + "/UI_StageNode.prefab";
    private const string BottomNavigationPath = CommonFolder + "/UI_BottomNavigation.prefab";
    private const string PopupPanelPath = CommonFolder + "/UI_PopupPanel.prefab";
    private const string SettingPanelPath = CommonFolder + "/Setting_Panel.prefab";
    private const string TitleScreenPath = TitleFolder + "/UI_TitleScreen.prefab";

    private const string SettingIconSourcePath = "Assets/4.Image/UI/Tittle/option.png";
    private const string SettingIconPath = IconFolder + "/icon_setting_white.png";
    private const string CityBackgroundPath = "Assets/4.Image/BackGround/BG_BuildingRooftop.jpg";

    private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    private static TMP_FontAsset font;

    // ───────────────────────── Step 1 ─────────────────────────

    [MenuItem("Tools/POLTERGEIST UI/Step 1 - 공통 UI 프리팹 생성", priority = 1)]
    public static void BuildCommon()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        EnsureWhiteIcon(SettingIconSourcePath, SettingIconPath);

        BuildSafeArea();
        BuildCurrencyItem();
        BuildCommonTopBar();
        BuildPrimaryButton();
        BuildSecondaryButton();
        BuildIconButton();
        BuildCharacterCard();
        BuildStageNode();
        BuildBottomNavigation();
        BuildPopupPanel();

        AssetDatabase.SaveAssets();
        Debug.Log($"[POLTERGEIST UI] Step 1 완료 — 공통 프리팹 10종 생성: {CommonFolder}/UI_*.prefab");
    }

    private static void BuildSafeArea()
    {
        GameObject root = NewRoot("UI_SafeArea");
        Fill((RectTransform)root.transform);
        root.AddComponent<SafeAreaAdapter>();

        string[] layers = { "BackgroundLayer", "CharacterLayer", "ContentLayer", "OverlayLayer", "NavigationLayer" };
        for (int i = 0; i < layers.Length; i++)
        {
            float side = i >= 2 ? UITheme.SideMargin : 0f;
            Fill(Rect(layers[i], root.transform), side, 0f, side, 0f);
        }
        Save(root, SafeAreaPath);
    }

    private static void BuildCurrencyItem()
    {
        GameObject root = NewRoot("UI_CurrencyItem");
        ((RectTransform)root.transform).sizeDelta = new Vector2(190f, 56f);

        ChamferGraphic icon = Shape(root.transform, "Icon", UITheme.LightGray);
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
        icon.SetCuts(14f, 14f, 14f, 14f);

        TMP_Text value = Label(root.transform, "Value", "0", 30f, UITheme.White, TextAlignmentOptions.MidlineLeft);
        Fill(value.rectTransform, 40f, 0f, 0f, 0f);

        Bind(root.AddComponent<UICurrencyItemView>(), ("valueText", value));
        Save(root, CurrencyItemPath);
    }

    private static void BuildCommonTopBar()
    {
        GameObject root = NewRoot("UI_CommonTopBar");
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 104f);
        rect.anchoredPosition = Vector2.zero;

        ChamferGraphic background = Shape(root.transform, "Background", Color.white);
        Fill(background.rectTransform);
        Gradient(background, UITheme.WithAlpha(UITheme.Charcoal, 0.85f), UITheme.WithAlpha(UITheme.Charcoal, 0.35f));

        ChamferGraphic line = Shape(root.transform, "BottomLine", UITheme.WithAlpha(UITheme.LightGray, 0.18f));
        line.rectTransform.anchorMin = Vector2.zero;
        line.rectTransform.anchorMax = new Vector2(1f, 0f);
        line.rectTransform.pivot = new Vector2(0.5f, 0f);
        line.rectTransform.sizeDelta = new Vector2(-UITheme.SideMargin * 2f, 1f);
        line.rectTransform.anchoredPosition = Vector2.zero;

        RectTransform player = Rect("PlayerBlock", root.transform);
        Place(player, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(UITheme.SideMargin, 0f), new Vector2(440f, 80f));

        ChamferGraphic emblem = Shape(player, "Emblem", UITheme.WithAlpha(UITheme.LightGray, 0.6f), 2f);
        Place(emblem.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        emblem.SetCuts(14f, 14f, 14f, 14f);
        ChamferGraphic core = Shape(emblem.transform, "Core", UITheme.Accent);
        Place(core.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        core.SetCuts(10f, 10f, 10f, 10f);

        TMP_Text level = Label(player, "Level", "LV.1", 32f, UITheme.White, TextAlignmentOptions.MidlineLeft, 0f, true);
        Place(level.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 14f), new Vector2(120f, 40f));
        TMP_Text title = Label(player, "Title", "국장", 32f, UITheme.White, TextAlignmentOptions.MidlineLeft, 0f, true);
        Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(196f, 14f), new Vector2(220f, 40f));
        TMP_Text agency = Label(player, "Agency", "A.M.A  ANOMALY MANAGEMENT AGENCY", 15f,
            UITheme.WithAlpha(UITheme.LightGray, 0.5f), TextAlignmentOptions.MidlineLeft, 3f);
        Place(agency.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, -22f), new Vector2(350f, 24f));

        RectTransform currencyRow = Rect("Currencies", root.transform);
        Place(currencyRow, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-UITheme.SideMargin, 0f), new Vector2(640f, 60f));
        HorizontalLayoutGroup layout = currencyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = 20f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        string[] names = { "활동횟수", "활동자금", "기밀서류" };
        string[] values = { "99/99", "12,450", "1,200" };
        Vector4[] iconCuts = { new Vector4(0f, 12f, 0f, 12f), new Vector4(14f, 14f, 14f, 14f), Vector4.zero };
        UICurrencyItemView[] items = new UICurrencyItemView[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject item = Nest(CurrencyItemPath, currencyRow, names[i]);
            TMP_Text value = item.transform.Find("Value").GetComponent<TMP_Text>();
            value.text = values[i];
            ChamferGraphic icon = item.transform.Find("Icon").GetComponent<ChamferGraphic>();
            icon.SetCuts(iconCuts[i].x, iconCuts[i].y, iconCuts[i].z, iconCuts[i].w);
            icon.BorderWidth = i == 2 ? 3f : 0f;
            Record(value, icon);
            items[i] = item.GetComponent<UICurrencyItemView>();
        }

        CommonTopBarView view = root.AddComponent<CommonTopBarView>();
        Bind(view, ("levelText", level), ("titleText", title));
        SetArray(view, "currencies", items);
        Save(root, CommonTopBarPath);
    }

    private static void BuildPrimaryButton()
    {
        GameObject root = NewRoot("UI_PrimaryButton");
        ((RectTransform)root.transform).sizeDelta = new Vector2(440f, 124f);

        ChamferGraphic fill = Shape(root.transform, "Fill", UITheme.WithAlpha(UITheme.DarkPanel, 0.9f));
        Fill(fill.rectTransform);
        fill.SetCuts(28f, 0f, 28f, 0f);
        ChamferGraphic border = Shape(root.transform, "Border", UITheme.Accent, 3f);
        Fill(border.rectTransform);
        border.SetCuts(28f, 0f, 28f, 0f);

        ChamferGraphic icon = Shape(root.transform, "Icon", UITheme.Accent);
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(8f, 52f));
        icon.SetCuts(0f, 4f, 0f, 4f);

        TMP_Text caption = Label(root.transform, "Caption", "AMA OPERATION", 20f, UITheme.Accent, TextAlignmentOptions.TopLeft, 6f);
        Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -20f), new Vector2(320f, 28f));
        TMP_Text label = Label(root.transform, "Label", "작전 시작", 46f, UITheme.White, TextAlignmentOptions.BottomLeft, 2f, true);
        Place(label.rectTransform, Vector2.zero, Vector2.zero, new Vector2(56f, 16f), new Vector2(300f, 66f));
        TMP_Text arrow = Label(root.transform, "Arrow", ">", 46f, UITheme.Accent, TextAlignmentOptions.MidlineRight, 0f, true);
        Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, -6f), new Vector2(40f, 64f));

        AddButton(root, fill);
        Save(root, PrimaryButtonPath);
    }

    private static void BuildSecondaryButton()
    {
        GameObject root = NewRoot("UI_SecondaryButton");
        ((RectTransform)root.transform).sizeDelta = new Vector2(260f, 92f);

        ChamferGraphic fill = Shape(root.transform, "Fill", UITheme.WithAlpha(UITheme.Charcoal, 0.8f));
        Fill(fill.rectTransform);
        fill.SetCuts(18f, 0f, 18f, 0f);
        ChamferGraphic border = Shape(root.transform, "Border", UITheme.WithAlpha(UITheme.LightGray, 0.3f), 2f);
        Fill(border.rectTransform);
        border.SetCuts(18f, 0f, 18f, 0f);

        TMP_Text label = Label(root.transform, "Label", "BACK", 30f, UITheme.LightGray, TextAlignmentOptions.Center, 4f, true);
        Fill(label.rectTransform);

        AddButton(root, fill);
        Save(root, SecondaryButtonPath);
    }

    private static void BuildIconButton()
    {
        GameObject root = NewRoot("UI_IconButton");
        ((RectTransform)root.transform).sizeDelta = new Vector2(72f, 72f);

        ChamferGraphic fill = Shape(root.transform, "Fill", UITheme.WithAlpha(UITheme.Charcoal, 0.85f));
        Fill(fill.rectTransform);
        fill.SetCuts(14f, 0f, 14f, 0f);
        ChamferGraphic border = Shape(root.transform, "Border", UITheme.WithAlpha(UITheme.LightGray, 0.3f), 2f);
        Fill(border.rectTransform);
        border.SetCuts(14f, 0f, 14f, 0f);

        Image icon = Picture(root.transform, "Icon", AssetDatabase.LoadAssetAtPath<Sprite>(SettingIconPath), UITheme.LightGray);
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));

        ChamferGraphic badge = Shape(root.transform, "Badge", UITheme.Accent);
        Place(badge.rectTransform, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(14f, 14f));
        badge.SetCuts(7f, 7f, 7f, 7f);
        badge.gameObject.SetActive(false);

        AddButton(root, fill);
        Save(root, IconButtonPath);
    }

    private static void BuildCharacterCard()
    {
        GameObject root = NewRoot("UI_CharacterCard");
        ((RectTransform)root.transform).sizeDelta = new Vector2(280f, 350f);

        ChamferGraphic frame = Shape(root.transform, "Frame", UITheme.WithAlpha(UITheme.DarkPanel, 0.9f));
        Fill(frame.rectTransform);
        frame.SetCuts(0f, 26f, 0f, 26f);

        RectTransform filled = Rect("Filled", root.transform);
        Fill(filled);
        RectTransform mask = Rect("PortraitMask", filled);
        Fill(mask, 6f, 92f, 6f, 6f);
        mask.gameObject.AddComponent<RectMask2D>();
        Image portrait = Picture(mask, "Portrait", null, Color.white);
        Fill(portrait.rectTransform);
        portrait.enabled = false;
        ChamferGraphic shade = Shape(mask, "Shade", Color.white);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = new Vector2(1f, 0.45f);
        shade.rectTransform.offsetMin = Vector2.zero;
        shade.rectTransform.offsetMax = Vector2.zero;
        Gradient(shade, UITheme.WithAlpha(UITheme.Charcoal, 0f), UITheme.WithAlpha(UITheme.Charcoal, 0.85f));

        TMP_Text rarity = Label(filled, "Rarity", "SSR", 22f, UITheme.Accent, TextAlignmentOptions.MidlineLeft, 2f, true);
        Place(rarity.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(90f, 30f));
        TMP_Text displayName = Label(filled, "Name", "요원 이름", 30f, UITheme.White, TextAlignmentOptions.MidlineLeft, 0f, true);
        Place(displayName.rectTransform, Vector2.zero, Vector2.zero, new Vector2(16f, 48f), new Vector2(160f, 40f));
        displayName.enableAutoSizing = true;
        displayName.fontSizeMin = 20f;
        displayName.fontSizeMax = 30f;
        TMP_Text level = Label(filled, "Level", "Lv.1", 22f, UITheme.WithAlpha(UITheme.LightGray, 0.75f), TextAlignmentOptions.MidlineRight);
        Place(level.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 48f), new Vector2(80f, 40f));

        TMP_Text trait = Chip(filled, "TraitChip", "특성", UITheme.Accent, new Vector2(16f, 12f));
        TMP_Text role = Chip(filled, "RoleChip", "역할", UITheme.LightGray, new Vector2(144f, 12f));

        RectTransform empty = Rect("Empty", root.transform);
        Fill(empty);
        ChamferGraphic outline = Shape(empty, "Outline", UITheme.WithAlpha(UITheme.LightGray, 0.25f), 2f);
        Fill(outline.rectTransform);
        outline.SetCuts(0f, 26f, 0f, 26f);
        TMP_Text plus = Label(empty, "Plus", "+", 96f, UITheme.WithAlpha(UITheme.LightGray, 0.6f), TextAlignmentOptions.Center);
        Place(plus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(120f, 120f));
        TMP_Text addMember = Label(empty, "Caption", "ADD MEMBER", 22f, UITheme.WithAlpha(UITheme.LightGray, 0.6f), TextAlignmentOptions.Center, 6f);
        Place(addMember.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(260f, 32f));
        empty.gameObject.SetActive(false);

        ChamferGraphic selection = Shape(root.transform, "Selection", UITheme.Accent, 4f);
        Fill(selection.rectTransform);
        selection.SetCuts(0f, 26f, 0f, 26f);
        selection.gameObject.SetActive(false);

        Button button = AddButton(root, frame);
        Bind(root.AddComponent<CharacterCardView>(),
            ("button", button), ("filledGroup", filled.gameObject), ("emptyGroup", empty.gameObject),
            ("selectionFrame", selection.gameObject), ("portrait", portrait), ("nameText", displayName),
            ("levelText", level), ("traitText", trait), ("roleText", role), ("rarityText", rarity));
        Save(root, CharacterCardPath);
    }

    private static TMP_Text Chip(Transform parent, string name, string text, Color tint, Vector2 position)
    {
        ChamferGraphic chip = Shape(parent, name, UITheme.WithAlpha(tint, 0.16f));
        Place(chip.rectTransform, Vector2.zero, Vector2.zero, position, new Vector2(118f, 30f));
        chip.SetCuts(8f, 0f, 8f, 0f);
        TMP_Text label = Label(chip.transform, "Text", text, 20f, tint, TextAlignmentOptions.Center);
        Fill(label.rectTransform);
        return label;
    }

    private static void BuildStageNode()
    {
        GameObject root = NewRoot("UI_StageNode");
        ((RectTransform)root.transform).sizeDelta = new Vector2(200f, 176f);

        ChamferGraphic hit = Shape(root.transform, "Hit", Clear);
        Fill(hit.rectTransform);

        ChamferGraphic ring = Shape(root.transform, "Ring", UITheme.Accent, 3f);
        Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(120f, 120f));
        ring.SetCuts(18f, 18f, 18f, 18f);
        ChamferGraphic node = Shape(root.transform, "Node", UITheme.Accent);
        Place(node.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(92f, 92f));
        node.SetCuts(14f, 14f, 14f, 14f);
        TMP_Text state = Label(node.transform, "State", "OPEN", 20f, UITheme.Charcoal, TextAlignmentOptions.Center, 2f, true);
        Fill(state.rectTransform);

        TMP_Text caseLabel = Label(root.transform, "Case", "CASE 01", 26f, UITheme.Accent, TextAlignmentOptions.Center, 4f, true);
        Place(caseLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(200f, 34f));

        Button button = AddButton(root, hit);
        Bind(root.AddComponent<StageNodeView>(),
            ("button", button), ("node", node), ("ring", ring), ("caseLabel", caseLabel), ("stateLabel", state));
        Save(root, StageNodePath);
    }

    private static void BuildBottomNavigation()
    {
        GameObject root = NewRoot("UI_BottomNavigation");
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 150f);
        rect.anchoredPosition = Vector2.zero;

        ChamferGraphic background = Shape(root.transform, "Background", Color.white);
        Fill(background.rectTransform);
        Gradient(background, UITheme.WithAlpha(UITheme.Charcoal, 0.78f), UITheme.WithAlpha(UITheme.Charcoal, 0.94f));
        ChamferGraphic topLine = Shape(root.transform, "TopLine", UITheme.WithAlpha(UITheme.LightGray, 0.15f));
        topLine.rectTransform.anchorMin = new Vector2(0f, 1f);
        topLine.rectTransform.anchorMax = Vector2.one;
        topLine.rectTransform.pivot = new Vector2(0.5f, 1f);
        topLine.rectTransform.sizeDelta = new Vector2(0f, 1f);
        topLine.rectTransform.anchoredPosition = Vector2.zero;

        RectTransform tabRow = Rect("Tabs", root.transform);
        Fill(tabRow, UITheme.SideMargin, 0f, UITheme.SideMargin, 0f);
        HorizontalLayoutGroup layout = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        string[] labels = { "홈", "작전", "캐릭터", "채용", "상점" };
        string[] scenes = { "1.MainMenu", "2.Chapter Select", "5.CharList", "7.Gacha", "" };
        // Placeholder icon silhouettes until icon art is supplied.
        Vector4[] iconCuts =
        {
            new Vector4(22f, 22f, 0f, 0f), new Vector4(22f, 22f, 22f, 22f), new Vector4(22f, 22f, 8f, 8f),
            Vector4.zero, new Vector4(0f, 0f, 14f, 14f)
        };
        Color muted = UITheme.WithAlpha(UITheme.LightGray, 0.55f);

        MainNavigationBarView view = root.AddComponent<MainNavigationBarView>();
        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty tabs = serialized.FindProperty("tabs");
        tabs.arraySize = labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            bool selected = i == 0;
            RectTransform tab = Rect("Tab_" + labels[i], tabRow);
            ChamferGraphic hit = Shape(tab, "Hit", Clear);
            Fill(hit.rectTransform);
            ChamferGraphic icon = Shape(tab, "Icon", selected ? UITheme.Accent : muted, 3f);
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(44f, 44f));
            icon.SetCuts(iconCuts[i].x, iconCuts[i].y, iconCuts[i].z, iconCuts[i].w);
            TMP_Text label = Label(tab, "Label", labels[i], 26f, selected ? UITheme.Accent : muted, TextAlignmentOptions.Center, 0f, true);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(180f, 36f));
            ChamferGraphic indicator = Shape(tab, "Indicator", UITheme.Accent);
            Place(indicator.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(72f, 4f));
            indicator.gameObject.SetActive(selected);
            Button button = AddButton(tab.gameObject, hit);

            SerializedProperty element = tabs.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("button").objectReferenceValue = button;
            element.FindPropertyRelative("icon").objectReferenceValue = icon;
            element.FindPropertyRelative("label").objectReferenceValue = label;
            element.FindPropertyRelative("indicator").objectReferenceValue = indicator.gameObject;
            element.FindPropertyRelative("sceneName").stringValue = scenes[i];
        }
        serialized.FindProperty("selectedIndex").intValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Save(root, BottomNavigationPath);
    }

    private static void BuildPopupPanel()
    {
        GameObject root = NewRoot("UI_PopupPanel");
        Fill((RectTransform)root.transform);
        root.AddComponent<CanvasGroup>();
        UIPanelTransition transition = root.AddComponent<UIPanelTransition>();

        ChamferGraphic dim = Shape(root.transform, "Dim", new Color(0f, 0f, 0f, 0.65f));
        Fill(dim.rectTransform);
        dim.raycastTarget = true;

        ChamferGraphic window = Shape(root.transform, "Window", UITheme.WithAlpha(UITheme.DarkPanel, 0.94f));
        Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1000f));
        window.SetCuts(36f, 0f, 36f, 0f);
        window.raycastTarget = true;
        ChamferGraphic border = Shape(window.transform, "Border", UITheme.WithAlpha(UITheme.LightGray, 0.18f), 2f);
        Fill(border.rectTransform);
        border.SetCuts(36f, 0f, 36f, 0f);
        ChamferGraphic accentBar = Shape(window.transform, "AccentBar", UITheme.Accent);
        Place(accentBar.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -24f), new Vector2(120f, 4f));

        TMP_Text caption = Label(window.transform, "Caption", "AMA NOTICE", 20f, UITheme.Accent, TextAlignmentOptions.MidlineLeft, 8f);
        Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -52f), new Vector2(500f, 30f));
        TMP_Text title = Label(window.transform, "Title", "제목", 44f, UITheme.White, TextAlignmentOptions.MidlineLeft, 0f, true);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -100f), new Vector2(700f, 60f));

        RectTransform close = Rect("CloseButton", window.transform);
        Place(close, Vector2.one, Vector2.one, new Vector2(-28f, -28f), new Vector2(72f, 72f));
        ChamferGraphic closeFill = Shape(close, "Fill", UITheme.WithAlpha(UITheme.Charcoal, 0.85f));
        Fill(closeFill.rectTransform);
        closeFill.SetCuts(14f, 0f, 14f, 0f);
        ChamferGraphic closeBorder = Shape(close, "Border", UITheme.WithAlpha(UITheme.LightGray, 0.3f), 2f);
        Fill(closeBorder.rectTransform);
        closeBorder.SetCuts(14f, 0f, 14f, 0f);
        TMP_Text closeLabel = Label(close, "Label", "X", 34f, UITheme.LightGray, TextAlignmentOptions.Center, 0f, true);
        Fill(closeLabel.rectTransform);
        Button closeButton = AddButton(close.gameObject, closeFill);
        UnityEventTools.AddVoidPersistentListener(closeButton.onClick, transition.Close);

        Fill(Rect("Content", window.transform), 48f, 48f, 48f, 160f);

        Bind(transition, ("content", window.rectTransform));
        Save(root, PopupPanelPath);
    }

    // ───────────────────────── Step 2 ─────────────────────────

    [MenuItem("Tools/POLTERGEIST UI/Step 2 - 타이틀 화면 생성 및 적용", priority = 2)]
    public static void BuildTitle()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SafeAreaPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(IconButtonPath) == null)
            BuildCommon();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        BuildTitleScreen();
        AssignCatalogTitle(TitleScreenPath);
        Debug.Log($"[POLTERGEIST UI] Step 2 완료 — {TitleScreenPath} 생성, UIScreenCatalog 타이틀 교체 (기존 프리팹은 보존됨)");
    }

    [MenuItem("Tools/POLTERGEIST UI/Step 2 되돌리기 - 기존 타이틀 화면 사용", priority = 20)]
    public static void RevertTitle()
    {
        AssignCatalogTitle(AssetDatabase.GUIDToAssetPath(LegacyTitlePrefabGuid));
        Debug.Log("[POLTERGEIST UI] UIScreenCatalog 타이틀을 기존 프리팹으로 되돌렸습니다.");
    }

    private static void BuildTitleScreen()
    {
        GameObject root = NewCanvasRoot("UI_TitleScreen", 180);
        TitleScreenView view = root.AddComponent<TitleScreenView>();
        SerializedObject screenCanvas = new SerializedObject(root.GetComponent<UIScreenCanvas>());
        screenCanvas.FindProperty("hideLegacySceneCanvases").boolValue = true;
        screenCanvas.ApplyModifiedPropertiesWithoutUndo();

        // Full-bleed background sits outside the SafeArea so notched screens have no letterbox.
        RectTransform backdrop = Rect("FullBleedBackground", root.transform);
        Fill(backdrop);
        RectTransform city = BuildCoverImage(backdrop, "City", AssetDatabase.LoadAssetAtPath<Sprite>(CityBackgroundPath));
        city.GetComponent<Image>().color = new Color(0.78f, 0.84f, 0.92f, 1f);
        Fill(Shape(backdrop, "Tint", UITheme.WithAlpha(UITheme.Charcoal, 0.3f)).rectTransform);
        ChamferGraphic topShade = Shape(backdrop, "TopShade", Color.white);
        Band(topShade.rectTransform, 0.62f, 1f);
        Gradient(topShade, UITheme.WithAlpha(UITheme.Charcoal, 0.85f), UITheme.WithAlpha(UITheme.Charcoal, 0f));
        ChamferGraphic bottomShade = Shape(backdrop, "BottomShade", Color.white);
        Band(bottomShade.rectTransform, 0f, 0.45f);
        Gradient(bottomShade, UITheme.WithAlpha(UITheme.Charcoal, 0f), UITheme.WithAlpha(UITheme.Charcoal, 0.92f));

        ChamferGraphic touchArea = Shape(root.transform, "TouchArea", Clear);
        Fill(touchArea.rectTransform);
        Button startButton = AddButton(touchArea.gameObject, touchArea, false);

        GameObject safeArea = Nest(SafeAreaPath, root.transform, "SafeArea");
        Transform content = safeArea.transform.Find("ContentLayer");
        Transform navigation = safeArea.transform.Find("NavigationLayer");

        TMP_Text statusVersion = BuildServerStatus(content);
        BuildLogo(content);
        BuildTouchPrompt(content);

        TMP_Text buildInfo = Label(content, "BuildInfo", "UID: ----------\nVersion: 1.0.0", 20f,
            UITheme.WithAlpha(UITheme.LightGray, 0.6f), TextAlignmentOptions.BottomLeft);
        Place(buildInfo.rectTransform, Vector2.zero, Vector2.zero, new Vector2(12f, 40f), new Vector2(360f, 60f));
        TMP_Text copyright = Label(content, "Copyright", "© Studio Name", 20f,
            UITheme.WithAlpha(UITheme.LightGray, 0.5f), TextAlignmentOptions.Center, 2f);
        Place(copyright.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(400f, 30f));

        GameObject settingButton = Nest(IconButtonPath, navigation, "SettingButton");
        RectTransform settingRect = (RectTransform)settingButton.transform;
        Place(settingRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 44f), new Vector2(64f, 64f));
        Record(settingRect);
        settingButton.AddComponent<SettingPopup>();

        // SettingPopup looks up "Setting_Panel" as a direct child of the canvas root.
        GameObject settingPanel = Nest(SettingPanelPath, root.transform, "Setting_Panel");
        settingPanel.SetActive(false);
        Record(settingPanel);

        (CanvasGroup overlay, TMP_Text status, RectTransform progress) = BuildAccessOverlay(root.transform);

        Bind(view,
            ("startButton", startButton), ("accessOverlay", overlay), ("accessStatusText", status),
            ("accessProgress", progress), ("statusVersionText", statusVersion), ("footerVersionText", buildInfo));
        Save(root, TitleScreenPath);
    }

    private static TMP_Text BuildServerStatus(Transform parent)
    {
        RectTransform box = Rect("ServerStatus", parent);
        Place(box, Vector2.one, Vector2.one, new Vector2(0f, -28f), new Vector2(300f, 86f));

        ChamferGraphic fill = Shape(box, "Fill", UITheme.WithAlpha(UITheme.Charcoal, 0.45f));
        Fill(fill.rectTransform);
        fill.SetCuts(0f, 0f, 14f, 0f);
        ChamferGraphic border = Shape(box, "Border", UITheme.WithAlpha(UITheme.LightGray, 0.35f), 2f);
        Fill(border.rectTransform);
        border.SetCuts(0f, 0f, 14f, 0f);
        ChamferGraphic divider = Shape(box, "Divider", UITheme.WithAlpha(UITheme.LightGray, 0.25f));
        divider.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        divider.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        divider.rectTransform.sizeDelta = new Vector2(-24f, 1f);
        divider.rectTransform.anchoredPosition = Vector2.zero;

        ChamferGraphic dot = Shape(box, "OnlineDot", UITheme.Accent);
        Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 21f), new Vector2(10f, 10f));
        dot.SetCuts(3f, 3f, 3f, 3f);

        Color dim = UITheme.WithAlpha(UITheme.LightGray, 0.7f);
        StatusText(box, "Server", "KR-SEOUL", UITheme.WithAlpha(UITheme.LightGray, 0.85f), false, 38f, 21f);
        StatusText(box, "Online", "ONLINE", UITheme.Accent, true, -18f, 21f);
        TMP_Text version = StatusText(box, "Version", "v1.0.0", dim, false, 18f, -21f);
        StatusText(box, "Channel", "STABLE", dim, true, -18f, -21f);
        return version;
    }

    private static TMP_Text StatusText(Transform parent, string name, string text, Color color, bool rightAligned, float x, float y)
    {
        Vector2 anchor = new Vector2(rightAligned ? 1f : 0f, 0.5f);
        TMP_Text label = Label(parent, name, text, 20f, color,
            rightAligned ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft, 4f);
        Place(label.rectTransform, anchor, anchor, new Vector2(x, y), new Vector2(150f, 34f));
        return label;
    }

    private static void BuildLogo(Transform parent)
    {
        // The doc places the logo in the 30–55% band from the top; its center is 57.5% from the bottom.
        RectTransform group = Rect("LogoGroup", parent);
        Place(group, new Vector2(0.5f, 0.575f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 320f));

        TMP_Text logo = Label(group, "Logo", "POLTERGEIST", 112f, UITheme.White, TextAlignmentOptions.Center, 6f);
        Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700f, 150f));
        logo.enableAutoSizing = true;
        logo.fontSizeMin = 70f;
        logo.fontSizeMax = 112f;
        logo.enableVertexGradient = true;
        logo.colorGradient = new VertexGradient(UITheme.White, UITheme.White, UITheme.LightGray, UITheme.LightGray);

        TMP_Text tagline = Label(group, "Tagline", "UNSEEN THREATS.  A SAFER TOMORROW.", 22f,
            UITheme.WithAlpha(UITheme.LightGray, 0.7f), TextAlignmentOptions.Center, 10f);
        Place(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(700f, 34f));
        ChamferGraphic divider = Shape(group, "Divider", UITheme.WithAlpha(UITheme.LightGray, 0.5f));
        Place(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -72f), new Vector2(60f, 2f));
        TMP_Text taglineKo = Label(group, "TaglineKo", "보이지 않는 것을, 지키는 사람들.", 22f,
            UITheme.WithAlpha(UITheme.LightGray, 0.55f), TextAlignmentOptions.Center, 6f);
        Place(taglineKo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -104f), new Vector2(700f, 34f));
    }

    private static void BuildTouchPrompt(Transform parent)
    {
        // "TOUCH TO START" in the 75–85% band from the top.
        RectTransform prompt = Rect("TouchToStart", parent);
        Place(prompt, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 90f));
        prompt.gameObject.AddComponent<CanvasGroup>();
        prompt.gameObject.AddComponent<UIAlphaPulse>();

        TMP_Text label = Label(prompt, "Label", "TOUCH TO START", 38f, UITheme.White, TextAlignmentOptions.Center, 22f);
        Fill(label.rectTransform);

        Color lineColor = UITheme.WithAlpha(UITheme.LightGray, 0.6f);
        ChamferGraphic leftLine = Shape(prompt, "LineLeft", lineColor);
        Place(leftLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(130f, 2f));
        ChamferGraphic rightLine = Shape(prompt, "LineRight", lineColor);
        Place(rightLine.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(130f, 2f));
        ChamferGraphic glow = Shape(prompt, "Glow", UITheme.WithAlpha(UITheme.Accent, 0.8f));
        Place(glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(180f, 3f));
    }

    private static (CanvasGroup, TMP_Text, RectTransform) BuildAccessOverlay(Transform parent)
    {
        RectTransform layer = Rect("TransitionLayer", parent);
        Fill(layer);

        RectTransform overlay = Rect("AccessOverlay", layer);
        Fill(overlay);
        CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        ChamferGraphic dim = Shape(overlay, "Dim", UITheme.WithAlpha(UITheme.Charcoal, 0.94f));
        Fill(dim.rectTransform);
        dim.raycastTarget = true;

        RectTransform panel = Rect("Panel", overlay);
        Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 220f));
        TMP_Text header = Label(panel, "Header", "AMA SYSTEM", 24f, UITheme.Accent, TextAlignmentOptions.Center, 14f);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(620f, 34f));
        TMP_Text status = Label(panel, "Status", "AUTHENTICATING...", 44f, UITheme.White, TextAlignmentOptions.Center, 8f, true);
        Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(620f, 60f));

        ChamferGraphic track = Shape(panel, "ProgressTrack", UITheme.WithAlpha(UITheme.LightGray, 0.15f));
        Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 4f));
        ChamferGraphic bar = Shape(track.transform, "Progress", UITheme.Accent);
        Fill(bar.rectTransform);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.rectTransform.localScale = new Vector3(0f, 1f, 1f);

        return (group, status, bar.rectTransform);
    }

    private static RectTransform BuildCoverImage(Transform parent, string name, Sprite sprite)
    {
        Image image = Picture(parent, name, sprite, Color.white);
        image.preserveAspect = false;
        RectTransform rect = image.rectTransform;
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, UITheme.ReferenceResolution);
        if (sprite != null)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }
        return rect;
    }

    private static void AssignCatalogTitle(string prefabPath)
    {
        UIScreenCatalog catalog = AssetDatabase.LoadAssetAtPath<UIScreenCatalog>(CatalogPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        TitleScreenView view = prefab != null ? prefab.GetComponent<TitleScreenView>() : null;
        if (catalog == null || view == null)
        {
            Debug.LogError($"[POLTERGEIST UI] 카탈로그 또는 타이틀 프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        SerializedObject serialized = new SerializedObject(catalog);
        serialized.FindProperty("titleScreenPrefab").objectReferenceValue = view;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static void EnsureWhiteIcon(string sourcePath, string destinationPath)
    {
        if (!AssetDatabase.IsValidFolder(IconFolder))
            AssetDatabase.CreateFolder("Assets/4.Image/UI", "Common");
        if (File.Exists(destinationPath)) return;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(sourcePath));
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, pixels[i].a);
        texture.SetPixels32(pixels);
        File.WriteAllBytes(destinationPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(destinationPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(destinationPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static GameObject NewRoot(string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        return root;
    }

    private static GameObject NewCanvasRoot(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(UIScreenCanvas)) { layer = 5 };
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UITheme.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = UITheme.MatchWidthOrHeight;
        return root;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    private static ChamferGraphic Shape(Transform parent, string name, Color color, float border = 0f)
    {
        ChamferGraphic graphic = Rect(name, parent).gameObject.AddComponent<ChamferGraphic>();
        graphic.color = color;
        graphic.BorderWidth = border;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static Image Picture(Transform parent, string name, Sprite sprite, Color color)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions alignment, float spacing = 0f, bool bold = false)
    {
        TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.characterSpacing = spacing;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    private static void Gradient(Graphic graphic, Color top, Color bottom)
    {
        graphic.gameObject.AddComponent<UIVerticalGradient>().SetColors(top, bottom);
    }

    private static Button AddButton(GameObject target, Graphic hitGraphic, bool pressFeedback = true)
    {
        hitGraphic.raycastTarget = true;
        Button button = target.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitGraphic;
        if (pressFeedback) target.AddComponent<UIPressFeedback>();
        return button;
    }

    private static GameObject Nest(string prefabPath, Transform parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"[POLTERGEIST UI] 프리팹 없음: {prefabPath}");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        return instance;
    }

    // Edits made by script to nested prefab instances must be recorded or they are dropped on save.
    private static void Record(params Object[] targets)
    {
        foreach (Object target in targets)
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void Bind(Object target, params (string property, Object value)[] references)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach ((string property, Object value) in references)
        {
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                Debug.LogError($"[POLTERGEIST UI] {target.GetType().Name}.{property} 필드를 찾을 수 없습니다.");
                continue;
            }
            field.objectReferenceValue = value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(Object target, string property, T[] values) where T : Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void Fill(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Band(RectTransform rect, float minY, float maxY)
    {
        rect.anchorMin = new Vector2(0f, minY);
        rect.anchorMax = new Vector2(1f, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
