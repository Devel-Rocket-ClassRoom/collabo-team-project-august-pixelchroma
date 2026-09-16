using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class UIOverhaulPrefabBuilder
{
    private const string RootFolder = "Assets/3.Prefabs/UI";
    private const string CommonFolder = RootFolder + "/Common";
    private const string CardsFolder = RootFolder + "/Cards";
    private const string ScreensFolder = RootFolder + "/Screens";
    private const string TopBarPath = CommonFolder + "/상단_재화바.prefab";
    private const string BottomNavPath = CommonFolder + "/하단_내비게이션.prefab";
    private const string AgentCardPath = CardsFolder + "/요원_카드.prefab";
    private const string HomeScreenPath = ScreensFolder + "/메인_로비_화면.prefab";
    private const string AgentListPath = ScreensFolder + "/요원_리스트_화면.prefab";
    private const string ChapterPath = ScreensFolder + "/챕터_선택_화면.prefab";
    private const string StagePath = ScreensFolder + "/스테이지_선택_화면.prefab";
    private const string SquadPath = RootFolder + "/SquadFormationUI.prefab";
    private const string CatalogPath = "Assets/Resources/UIScreenCatalog.asset";

    private static readonly Color Ink = new Color(0.035f, 0.045f, 0.06f, 1f);
    private static readonly Color InkSoft = new Color(0.08f, 0.095f, 0.12f, 0.96f);
    private static readonly Color Cyan = new Color(0.02f, 0.68f, 0.95f, 1f);
    private static readonly Color Yellow = new Color(1f, 0.72f, 0.08f, 1f);
    private static readonly Color Paper = new Color(0.94f, 0.95f, 0.96f, 1f);

    static UIOverhaulPrefabBuilder()
    {
        EditorApplication.delayCall += BuildMissing;
    }

    [MenuItem("Tools/SRPG UI/니케형 UI 1차 프리팹 생성")]
    public static void RebuildAll()
    {
        EnsureFolders();
        BuildTopBar();
        BuildBottomNavigation();
        BuildAgentCard();
        BuildHomeScreen();
        BuildAgentListScreen();
        BuildChapterScreen();
        BuildStageScreen();
        BuildSquadFormation();
        BuildCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UIOverhaul] 공통 UI, 메인 로비, 요원 리스트 프리팹 생성 완료");
    }

    private static void BuildMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (AssetDatabase.LoadAssetAtPath<UIScreenCatalog>(CatalogPath) == null)
            RebuildAll();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/3.Prefabs", "UI");
        EnsureFolder(RootFolder, "Common");
        EnsureFolder(RootFolder, "Cards");
        EnsureFolder(RootFolder, "Screens");
        EnsureFolder("Assets", "Resources");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static TMP_FontAsset Font()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/6.Font/경기천년제목_Medium SDF.asset");
    }

    private static void BuildTopBar()
    {
        GameObject root = CreateImageObject("상단 재화바", InkSoft);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image profile = CreateImage(root.transform, "지휘관 프로필", new Color(0.12f, 0.16f, 0.21f, 1f));
        SetAnchors(profile.rectTransform, 0.025f, 0.14f, 0.16f, 0.86f);
        CreateLabel(profile.transform, "등급", "RANK 01", 22f, Color.white);

        Image credit = CreateImage(root.transform, "작전 자원", new Color(0f, 0f, 0f, 0.38f));
        SetAnchors(credit.rectTransform, 0.18f, 0.16f, 0.47f, 0.84f);
        CreateLabel(credit.transform, "문구", "작전  1,400  +", 28f, Color.white);

        Image supply = CreateImage(root.transform, "보급 자원", new Color(0f, 0f, 0f, 0.38f));
        SetAnchors(supply.rectTransform, 0.485f, 0.16f, 0.75f, 0.84f);
        CreateLabel(supply.transform, "문구", "보급  55,685", 28f, Color.white);

        Button mail = CreateButton(root.transform, "우편 버튼", "우편", new Color(0.12f, 0.14f, 0.17f, 0.95f), 22f);
        SetAnchors((RectTransform)mail.transform, 0.79f, 0.16f, 0.875f, 0.84f);
        Button menu = CreateButton(root.transform, "메뉴 버튼", "메뉴", new Color(0.12f, 0.14f, 0.17f, 0.95f), 22f);
        SetAnchors((RectTransform)menu.transform, 0.885f, 0.16f, 0.97f, 0.84f);

        SavePrefab(root, TopBarPath);
    }

    private static void BuildBottomNavigation()
    {
        GameObject root = CreateImageObject("하단 내비게이션", new Color(0.025f, 0.03f, 0.04f, 0.98f));
        BottomNavigationView view = root.AddComponent<BottomNavigationView>();
        string[] labels = { "로비\n홈", "요원\n목록", "스쿼드\n편성", "작전\n출격" };
        string[] names = { "로비 버튼", "요원 버튼", "스쿼드 버튼", "작전 버튼" };
        Button[] buttons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float left = i * 0.25f;
            buttons[i] = CreateButton(root.transform, names[i], labels[i], new Color(1f, 1f, 1f, 0.015f), 25f);
            SetAnchors((RectTransform)buttons[i].transform, left, 0f, left + 0.25f, 1f);
        }

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("lobbyButton").objectReferenceValue = buttons[0];
        serialized.FindProperty("agentsButton").objectReferenceValue = buttons[1];
        serialized.FindProperty("squadButton").objectReferenceValue = buttons[2];
        serialized.FindProperty("operationButton").objectReferenceValue = buttons[3];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, BottomNavPath);
    }

    private static void BuildAgentCard()
    {
        GameObject root = CreateImageObject("요원 카드", Color.white);
        AgentCardView view = root.AddComponent<AgentCardView>();
        Button button = root.AddComponent<Button>();

        Image accent = CreateImage(root.transform, "병과 색상", Cyan);
        SetAnchors(accent.rectTransform, 0f, 0.965f, 1f, 1f);
        Image portrait = CreateImage(root.transform, "요원 이미지", new Color(0.18f, 0.21f, 0.26f, 1f));
        SetAnchors(portrait.rectTransform, 0.035f, 0.28f, 0.965f, 0.95f);
        TMP_Text power = CreateText(root.transform, "전투력", "전투력 0000", 20f, TextAlignmentOptions.Left, Ink);
        SetAnchors(power.rectTransform, 0.055f, 0.19f, 0.95f, 0.28f);
        TMP_Text name = CreateText(root.transform, "요원 이름", "요원 이름", 27f, TextAlignmentOptions.Left, Ink);
        name.fontStyle = FontStyles.Bold;
        SetAnchors(name.rectTransform, 0.055f, 0.09f, 0.95f, 0.2f);
        TMP_Text role = CreateText(root.transform, "공격 유형", "공격 유형", 18f, TextAlignmentOptions.Left, new Color(0.32f, 0.36f, 0.42f));
        SetAnchors(role.rectTransform, 0.055f, 0.015f, 0.95f, 0.1f);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.FindProperty("portrait").objectReferenceValue = portrait;
        serialized.FindProperty("roleAccent").objectReferenceValue = accent;
        serialized.FindProperty("nameText").objectReferenceValue = name;
        serialized.FindProperty("roleText").objectReferenceValue = role;
        serialized.FindProperty("levelText").objectReferenceValue = power;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, AgentCardPath);
    }

    private static void BuildHomeScreen()
    {
        GameObject root = CreateCanvas("메인 로비 화면", 180);
        HomeScreenView view = root.AddComponent<HomeScreenView>();

        Image city = CreateImage(root.transform, "도시 배경", Color.white);
        Stretch(city.rectTransform);
        city.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.Image/BackGround/BG_BuildingRooftop.jpg");
        city.preserveAspect = false;
        Image shade = CreateImage(root.transform, "가독성 그라데이션", new Color(0.015f, 0.025f, 0.04f, 0.32f));
        Stretch(shade.rectTransform);

        Image character = CreateImage(root.transform, "대표 요원", Color.white);
        SetAnchors(character.rectTransform, 0.01f, 0.12f, 0.82f, 0.9f);
        character.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.Image/BackGround/aru_default_04.png");
        character.preserveAspect = true;

        RectTransform safe = CreateSafeArea(root.transform);
        InstantiateNested(TopBarPath, safe, "공통 상단바", 0f, 0.91f, 1f, 1f);

        Image commander = CreateImage(safe, "지휘 안내", new Color(0.02f, 0.03f, 0.05f, 0.76f));
        SetAnchors(commander.rectTransform, 0.37f, 0.73f, 0.96f, 0.82f);
        TMP_Text guide = CreateText(commander.transform, "문구", "지휘관님, 출격 준비가 완료됐습니다.", 25f, TextAlignmentOptions.Center, Color.white);
        Stretch(guide.rectTransform);

        Button agents = CreateButton(safe, "요원 리스트 바로가기", "요원 리스트  >", new Color(0.035f, 0.055f, 0.08f, 0.9f), 26f);
        SetAnchors((RectTransform)agents.transform, 0.70f, 0.60f, 0.97f, 0.665f);
        Button squad = CreateButton(safe, "스쿼드 편성 바로가기", "스쿼드 편성  >", new Color(0.035f, 0.055f, 0.08f, 0.9f), 26f);
        SetAnchors((RectTransform)squad.transform, 0.70f, 0.525f, 0.97f, 0.59f);

        Button campaign = CreateButton(safe, "메인 작전 버튼", "작전 출격\n현재 진행  01-01", new Color(0.01f, 0.44f, 0.76f, 0.95f), 34f);
        SetAnchors((RectTransform)campaign.transform, 0.49f, 0.22f, 0.97f, 0.37f);
        Button mission = CreateButton(safe, "임무 선택 버튼", "임무 선택\n도심 외곽", new Color(0.04f, 0.08f, 0.13f, 0.93f), 29f);
        SetAnchors((RectTransform)mission.transform, 0.05f, 0.22f, 0.46f, 0.34f);

        InstantiateNested(BottomNavPath, safe, "공통 하단 내비게이션", 0f, 0f, 1f, 0.105f);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("campaignButton").objectReferenceValue = campaign;
        serialized.FindProperty("squadButton").objectReferenceValue = squad;
        serialized.FindProperty("agentsButton").objectReferenceValue = agents;
        serialized.FindProperty("missionButton").objectReferenceValue = mission;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, HomeScreenPath);
    }

    private static void BuildAgentListScreen()
    {
        GameObject root = CreateCanvas("요원 리스트 화면", 180);
        AgentListScreenView view = root.AddComponent<AgentListScreenView>();
        Image background = CreateImage(root.transform, "배경", Paper);
        Stretch(background.rectTransform);
        RectTransform safe = CreateSafeArea(root.transform);
        InstantiateNested(TopBarPath, safe, "공통 상단바", 0f, 0.91f, 1f, 1f);

        Image header = CreateImage(safe, "요원 헤더", Ink);
        SetAnchors(header.rectTransform, 0f, 0.785f, 1f, 0.91f);
        Button back = CreateButton(header.transform, "뒤로가기", "<", new Color(1f, 1f, 1f, 0.05f), 42f);
        SetAnchors((RectTransform)back.transform, 0.025f, 0.18f, 0.13f, 0.83f);
        TMP_Text title = CreateText(header.transform, "제목", "요원 리스트", 42f, TextAlignmentOptions.Left, Color.white);
        SetAnchors(title.rectTransform, 0.15f, 0.48f, 0.62f, 0.88f);
        TMP_Text count = CreateText(header.transform, "보유 인원", "보유 요원  0명", 23f, TextAlignmentOptions.Left, Cyan);
        SetAnchors(count.rectTransform, 0.155f, 0.13f, 0.55f, 0.48f);
        TMP_Text power = CreateText(header.transform, "총 전투력", "총 전투력  0", 23f, TextAlignmentOptions.Right, Yellow);
        SetAnchors(power.rectTransform, 0.54f, 0.15f, 0.95f, 0.5f);

        Image filter = CreateImage(safe, "검색 및 필터", Color.white);
        SetAnchors(filter.rectTransform, 0f, 0.72f, 1f, 0.785f);
        string[] filters = { "검색", "전체", "I", "II", "III", "전투력" };
        float[] widths = { 0.12f, 0.17f, 0.12f, 0.12f, 0.12f, 0.26f };
        float x = 0.025f;
        for (int i = 0; i < filters.Length; i++)
        {
            Button f = CreateButton(filter.transform, "필터_" + filters[i], filters[i], new Color(0.16f, 0.17f, 0.19f, 1f), 23f);
            SetAnchors((RectTransform)f.transform, x, 0.16f, x + widths[i] - 0.01f, 0.84f);
            x += widths[i];
        }

        GameObject scrollObject = new GameObject("요원 카드 스크롤", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(safe, false);
        SetAnchors(scrollObject.GetComponent<RectTransform>(), 0.02f, 0.115f, 0.98f, 0.715f);
        scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        Stretch(viewportObject.GetComponent<RectTransform>());
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(18, 18, 24, 24);
        grid.cellSize = new Vector2(232f, 430f);
        grid.spacing = new Vector2(22f, 24f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportObject.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 55f;

        InstantiateNested(BottomNavPath, safe, "공통 하단 내비게이션", 0f, 0f, 1f, 0.105f);

        Image dim = CreateImage(root.transform, "요원 상세 팝업", new Color(0f, 0f, 0f, 0.72f));
        Stretch(dim.rectTransform);
        Image box = CreateImage(dim.transform, "상세 카드", new Color(0.055f, 0.065f, 0.085f, 1f));
        SetAnchors(box.rectTransform, 0.07f, 0.2f, 0.93f, 0.8f);
        Image detailPortrait = CreateImage(box.transform, "상세 이미지", new Color(0.14f, 0.16f, 0.2f, 1f));
        SetAnchors(detailPortrait.rectTransform, 0.06f, 0.39f, 0.47f, 0.94f);
        TMP_Text detailName = CreateText(box.transform, "상세 이름", "요원 이름", 42f, TextAlignmentOptions.Left, Color.white);
        SetAnchors(detailName.rectTransform, 0.52f, 0.79f, 0.94f, 0.94f);
        TMP_Text detailRole = CreateText(box.transform, "상세 유형", "공격 유형", 24f, TextAlignmentOptions.Left, Cyan);
        SetAnchors(detailRole.rectTransform, 0.52f, 0.7f, 0.94f, 0.81f);
        TMP_Text detailStats = CreateText(box.transform, "상세 능력치", "체력 0     공격력 0", 25f, TextAlignmentOptions.TopLeft, Color.white);
        SetAnchors(detailStats.rectTransform, 0.52f, 0.42f, 0.94f, 0.68f);
        TMP_Text detailDescription = CreateText(box.transform, "상세 설명", "요원 설명", 24f, TextAlignmentOptions.TopLeft, new Color(0.78f, 0.82f, 0.88f));
        SetAnchors(detailDescription.rectTransform, 0.07f, 0.14f, 0.93f, 0.36f);
        Button close = CreateButton(box.transform, "닫기", "확인", Cyan, 30f);
        SetAnchors((RectTransform)close.transform, 0.28f, 0.035f, 0.72f, 0.125f);

        AgentCardView cardPrefab = LoadPrefabComponent<AgentCardView>(AgentCardPath);
        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
        serialized.FindProperty("cardContainer").objectReferenceValue = content;
        serialized.FindProperty("ownedCountText").objectReferenceValue = count;
        serialized.FindProperty("totalPowerText").objectReferenceValue = power;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("detailCloseButton").objectReferenceValue = close;
        serialized.FindProperty("detailPanel").objectReferenceValue = dim.gameObject;
        serialized.FindProperty("detailPortrait").objectReferenceValue = detailPortrait;
        serialized.FindProperty("detailNameText").objectReferenceValue = detailName;
        serialized.FindProperty("detailRoleText").objectReferenceValue = detailRole;
        serialized.FindProperty("detailStatsText").objectReferenceValue = detailStats;
        serialized.FindProperty("detailDescriptionText").objectReferenceValue = detailDescription;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, AgentListPath);
    }

    private static void BuildChapterScreen()
    {
        GameObject root = CreateCanvas("챕터 선택 화면", 180);
        OperationSelectionScreenView view = root.AddComponent<OperationSelectionScreenView>();
        Image background = CreateImage(root.transform, "도시 배경", new Color(0.09f, 0.12f, 0.16f, 1f));
        Stretch(background.rectTransform);
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.Image/BackGround/BG_BuildingRooftop.jpg");
        background.preserveAspect = false;
        Image dim = CreateImage(root.transform, "배경 음영", new Color(0.015f, 0.025f, 0.04f, 0.56f));
        Stretch(dim.rectTransform);

        RectTransform safe = CreateSafeArea(root.transform);
        InstantiateNested(TopBarPath, safe, "공통 상단바", 0f, 0.91f, 1f, 1f);
        Image header = CreateImage(safe, "챕터 헤더", Ink);
        SetAnchors(header.rectTransform, 0f, 0.79f, 1f, 0.91f);
        Button back = CreateButton(header.transform, "뒤로가기", "<", new Color(1f, 1f, 1f, 0.05f), 42f);
        SetAnchors((RectTransform)back.transform, 0.025f, 0.2f, 0.13f, 0.82f);
        TMP_Text title = CreateText(header.transform, "제목", "챕터 선택", 43f, TextAlignmentOptions.Left, Color.white);
        SetAnchors(title.rectTransform, 0.16f, 0.42f, 0.7f, 0.88f);
        TMP_Text sub = CreateText(header.transform, "설명", "작전 구역을 선택하세요", 22f, TextAlignmentOptions.Left, Cyan);
        SetAnchors(sub.rectTransform, 0.165f, 0.12f, 0.75f, 0.45f);

        GameObject scrollObject = new GameObject("챕터 스크롤", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(safe, false);
        SetAnchors(scrollObject.GetComponent<RectTransform>(), 0.045f, 0.12f, 0.955f, 0.77f);
        scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 20, 20);
        layout.spacing = 28f;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;

        Button[] chapters = new Button[3];
        string[] chapterNames = { "CHAPTER 01\n도심 외곽 봉쇄선", "CHAPTER 02\n붕괴된 상업 지구", "CHAPTER 03\n중앙 통제 구역" };
        for (int i = 0; i < chapters.Length; i++)
        {
            chapters[i] = CreateButton(content, "챕터 " + (i + 1), chapterNames[i], i == 0 ? new Color(0.02f, 0.5f, 0.78f, 0.96f) : InkSoft, 34f);
            RectTransform rect = (RectTransform)chapters[i].transform;
            rect.sizeDelta = new Vector2(0f, 360f);
            LayoutElement element = chapters[i].gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 360f;
        }
        InstantiateNested(BottomNavPath, safe, "공통 하단 내비게이션", 0f, 0f, 1f, 0.105f);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("screenKind").enumValueIndex = 0;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        SetObjectArray(serialized.FindProperty("selectionButtons"), chapters);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, ChapterPath);
    }

    private static void BuildStageScreen()
    {
        GameObject root = CreateCanvas("스테이지 선택 화면", 180);
        OperationSelectionScreenView view = root.AddComponent<OperationSelectionScreenView>();
        Image background = CreateImage(root.transform, "배경", new Color(0.89f, 0.91f, 0.93f, 1f));
        Stretch(background.rectTransform);
        RectTransform safe = CreateSafeArea(root.transform);
        InstantiateNested(TopBarPath, safe, "공통 상단바", 0f, 0.91f, 1f, 1f);

        Image header = CreateImage(safe, "스테이지 헤더", Ink);
        SetAnchors(header.rectTransform, 0f, 0.78f, 1f, 0.91f);
        Button back = CreateButton(header.transform, "뒤로가기", "<", new Color(1f, 1f, 1f, 0.05f), 42f);
        SetAnchors((RectTransform)back.transform, 0.025f, 0.2f, 0.13f, 0.82f);
        TMP_Text title = CreateText(header.transform, "제목", "도심 외곽 봉쇄선", 41f, TextAlignmentOptions.Left, Color.white);
        SetAnchors(title.rectTransform, 0.16f, 0.44f, 0.76f, 0.9f);
        TMP_Text selected = CreateText(header.transform, "선택 작전", "선택 작전  01-01", 23f, TextAlignmentOptions.Left, Cyan);
        SetAnchors(selected.rectTransform, 0.165f, 0.12f, 0.72f, 0.45f);
        TMP_Text progress = CreateText(header.transform, "진행도", "진행도  0 / 5", 23f, TextAlignmentOptions.Right, Yellow);
        SetAnchors(progress.rectTransform, 0.68f, 0.15f, 0.95f, 0.5f);

        Image map = CreateImage(safe, "작전 지도", new Color(0.82f, 0.85f, 0.88f, 1f));
        SetAnchors(map.rectTransform, 0.035f, 0.13f, 0.965f, 0.755f);
        Image route = CreateImage(map.transform, "작전 경로", new Color(0.12f, 0.18f, 0.24f, 0.22f));
        SetAnchors(route.rectTransform, 0.47f, 0.07f, 0.53f, 0.93f);

        Button[] stages = new Button[5];
        float[] x = { 0.19f, 0.68f, 0.28f, 0.72f, 0.36f };
        float[] y = { 0.12f, 0.29f, 0.47f, 0.64f, 0.81f };
        for (int i = 0; i < stages.Length; i++)
        {
            string label = i == 0 ? $"01-{i + 1:00}\n출격 가능" : $"01-{i + 1:00}\n미개방";
            stages[i] = CreateButton(map.transform, "스테이지 " + (i + 1), label,
                i == 0 ? new Color(0.02f, 0.58f, 0.85f, 1f) : new Color(0.16f, 0.19f, 0.23f, 0.94f), 27f);
            SetAnchors((RectTransform)stages[i].transform, x[i] - 0.14f, y[i] - 0.07f, x[i] + 0.14f, y[i] + 0.07f);
            if (i > 0) stages[i].interactable = false;
        }
        InstantiateNested(BottomNavPath, safe, "공통 하단 내비게이션", 0f, 0f, 1f, 0.105f);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("screenKind").enumValueIndex = 1;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("selectionTitle").objectReferenceValue = selected;
        SetObjectArray(serialized.FindProperty("selectionButtons"), stages);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, StagePath);
    }

    private static void BuildSquadFormation()
    {
        GameObject root = CreateCanvas("스쿼드 편성 UI", 180);
        SquadFormationUIView view = root.AddComponent<SquadFormationUIView>();
        Image background = CreateImage(root.transform, "배경", Paper);
        Stretch(background.rectTransform);
        RectTransform safe = CreateSafeArea(root.transform);
        InstantiateNested(TopBarPath, safe, "공통 상단바", 0f, 0.91f, 1f, 1f);

        Image header = CreateImage(safe, "편성 헤더", Ink);
        SetAnchors(header.rectTransform, 0f, 0.78f, 1f, 0.91f);
        Button back = CreateButton(header.transform, "뒤로가기", "<", new Color(1f, 1f, 1f, 0.05f), 42f);
        SetAnchors((RectTransform)back.transform, 0.025f, 0.2f, 0.13f, 0.82f);
        TMP_Text title = CreateText(header.transform, "제목", "스쿼드 편성", 42f, TextAlignmentOptions.Left, Color.white);
        SetAnchors(title.rectTransform, 0.16f, 0.46f, 0.62f, 0.9f);
        TMP_Text count = CreateText(header.transform, "편성 인원", "0 / 4", 24f, TextAlignmentOptions.Left, Cyan);
        SetAnchors(count.rectTransform, 0.165f, 0.12f, 0.45f, 0.46f);
        TMP_Text power = CreateText(header.transform, "전투력", "전투력 0000", 24f, TextAlignmentOptions.Right, Yellow);
        SetAnchors(power.rectTransform, 0.55f, 0.14f, 0.95f, 0.48f);

        Image squadPanel = CreateImage(safe, "출전 스쿼드", new Color(0.08f, 0.095f, 0.12f, 1f));
        SetAnchors(squadPanel.rectTransform, 0.025f, 0.53f, 0.975f, 0.765f);
        CreateLabel(squadPanel.transform, "영역 제목", "출전 스쿼드", 24f, Color.white);
        squadPanel.transform.Find("영역 제목").GetComponent<RectTransform>().anchorMin = new Vector2(0.03f, 0.84f);
        squadPanel.transform.Find("영역 제목").GetComponent<RectTransform>().anchorMax = new Vector2(0.4f, 0.98f);

        Button[] slots = new Button[4];
        TMP_Text[] slotNames = new TMP_Text[4];
        TMP_Text[] slotDetails = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            float left = 0.025f + i * 0.245f;
            slots[i] = CreateButton(squadPanel.transform, "출전 슬롯 " + (i + 1), "", new Color(0.14f, 0.16f, 0.2f, 1f), 25f);
            SetAnchors((RectTransform)slots[i].transform, left, 0.07f, left + 0.215f, 0.8f);
            slotNames[i] = CreateText(slots[i].transform, "이름", "빈 슬롯", 25f, TextAlignmentOptions.Center, Color.white);
            SetAnchors(slotNames[i].rectTransform, 0.05f, 0.53f, 0.95f, 0.88f);
            slotDetails[i] = CreateText(slots[i].transform, "정보", "+", 34f, TextAlignmentOptions.Center, Color.white);
            SetAnchors(slotDetails[i].rectTransform, 0.05f, 0.1f, 0.95f, 0.55f);
        }

        Image rosterPanel = CreateImage(safe, "보유 요원", Color.white);
        SetAnchors(rosterPanel.rectTransform, 0.025f, 0.145f, 0.975f, 0.515f);
        TMP_Text rosterTitle = CreateText(rosterPanel.transform, "영역 제목", "보유 요원", 27f, TextAlignmentOptions.Left, Ink);
        SetAnchors(rosterTitle.rectTransform, 0.025f, 0.86f, 0.45f, 0.98f);
        Button[] cards = new Button[6];
        TMP_Text[] cardNames = new TMP_Text[6];
        TMP_Text[] cardStats = new TMP_Text[6];
        TMP_Text[] cardStatuses = new TMP_Text[6];
        TMP_Text[] cardMonograms = new TMP_Text[6];
        for (int i = 0; i < 6; i++)
        {
            int column = i % 3;
            int row = i / 3;
            float left = 0.025f + column * 0.325f;
            float top = 0.83f - row * 0.4f;
            cards[i] = CreateButton(rosterPanel.transform, "요원 카드 " + (i + 1), "", InkSoft, 22f);
            SetAnchors((RectTransform)cards[i].transform, left, top - 0.35f, left + 0.295f, top);
            cardMonograms[i] = CreateText(cards[i].transform, "초상", "요원", 32f, TextAlignmentOptions.Center, Color.white);
            SetAnchors(cardMonograms[i].rectTransform, 0.04f, 0.38f, 0.96f, 0.95f);
            cardNames[i] = CreateText(cards[i].transform, "이름", "요원 이름", 22f, TextAlignmentOptions.Center, Color.white);
            SetAnchors(cardNames[i].rectTransform, 0.04f, 0.2f, 0.96f, 0.4f);
            cardStats[i] = CreateText(cards[i].transform, "능력치", "체력 / 공격 / 이동", 15f, TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.86f));
            SetAnchors(cardStats[i].rectTransform, 0.03f, 0.03f, 0.97f, 0.21f);
            cardStatuses[i] = CreateText(cards[i].transform, "편성 상태", "", 20f, TextAlignmentOptions.Center, Yellow);
            Stretch(cardStatuses[i].rectTransform);
        }

        Button start = CreateButton(safe, "전투 시작", "전투 시작  >", Yellow, 33f);
        SetAnchors((RectTransform)start.transform, 0.5f, 0.055f, 0.96f, 0.13f);
        TMP_Text hint = CreateText(safe, "편성 안내", "출전할 요원 4명을 선택하세요", 21f, TextAlignmentOptions.Left, new Color(0.28f, 0.32f, 0.38f));
        SetAnchors(hint.rectTransform, 0.04f, 0.065f, 0.48f, 0.12f);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("safeAreaRoot").objectReferenceValue = safe;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.FindProperty("powerText").objectReferenceValue = power;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("startButton").objectReferenceValue = start;
        SetObjectArray(serialized.FindProperty("squadSlots"), slots);
        SetObjectArray(serialized.FindProperty("squadNames"), slotNames);
        SetObjectArray(serialized.FindProperty("squadDetails"), slotDetails);
        SetObjectArray(serialized.FindProperty("rosterCards"), cards);
        SetObjectArray(serialized.FindProperty("rosterNames"), cardNames);
        SetObjectArray(serialized.FindProperty("rosterStats"), cardStats);
        SetObjectArray(serialized.FindProperty("rosterStatuses"), cardStatuses);
        SetObjectArray(serialized.FindProperty("rosterMonograms"), cardMonograms);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, SquadPath);
    }

    private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void BuildCatalog()
    {
        UIScreenCatalog catalog = AssetDatabase.LoadAssetAtPath<UIScreenCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UIScreenCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        SerializedObject serialized = new SerializedObject(catalog);
        serialized.FindProperty("homeScreenPrefab").objectReferenceValue = LoadPrefabComponent<HomeScreenView>(HomeScreenPath);
        serialized.FindProperty("agentListScreenPrefab").objectReferenceValue = LoadPrefabComponent<AgentListScreenView>(AgentListPath);
        serialized.FindProperty("chapterScreenPrefab").objectReferenceValue = LoadPrefabComponent<OperationSelectionScreenView>(ChapterPath);
        serialized.FindProperty("stageScreenPrefab").objectReferenceValue = LoadPrefabComponent<OperationSelectionScreenView>(StagePath);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasAutoScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2220f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        return root;
    }

    private static RectTransform CreateSafeArea(Transform parent)
    {
        GameObject obj = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaAdapter));
        obj.transform.SetParent(parent, false);
        Stretch(obj.GetComponent<RectTransform>());
        return obj.GetComponent<RectTransform>();
    }

    private static GameObject CreateImageObject(string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = CreateImageObject(name, color);
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<Image>();
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.font = Font();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(15f, size * 0.7f);
        text.fontSizeMax = size;
        return text;
    }

    private static void CreateLabel(Transform parent, string name, string value, float size, Color color)
    {
        TMP_Text text = CreateText(parent, name, value, size, TextAlignmentOptions.Center, color);
        Stretch(text.rectTransform);
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, float fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.93f, 1f, 1f);
        colors.pressedColor = new Color(0.68f, 0.78f, 0.88f, 1f);
        button.colors = colors;
        CreateLabel(obj.transform, "문구", label, fontSize, Color.white);
        return button;
    }

    private static GameObject InstantiateNested(string prefabPath, Transform parent, string name, float minX, float minY, float maxX, float maxY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        SetAnchors((RectTransform)instance.transform, minX, minY, maxX, maxY);
        return instance;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static T LoadPrefabComponent<T>(string path) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, 0f, 0f, 1f, 1f);
    }

    private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
