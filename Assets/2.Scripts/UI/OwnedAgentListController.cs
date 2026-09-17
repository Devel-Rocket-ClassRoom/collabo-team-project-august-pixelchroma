using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 이전 씬과의 직렬화 호환성을 위해 남겨 둔 컴포넌트입니다.
// 실제 화면은 ScreenPrefabBootstrap이 AgentListScreen 프리팹을 불러옵니다.
public class OwnedAgentListController : MonoBehaviour
{
    private const string ListSceneName = "5.CharList";
    private const string MainSceneName = "1.MainMenu";

    private OwnedAgentCatalog catalog;
    private TMP_FontAsset uiFont;
    private RectTransform safeAreaRoot;
    private RectTransform detailPanel;
    private TMP_Text detailTitle;
    private TMP_Text detailBody;
    private Rect lastSafeArea;

    private static readonly Color Background = new Color(0.025f, 0.035f, 0.055f, 1f);
    private static readonly Color Panel = new Color(0.065f, 0.085f, 0.12f, 1f);
    private static readonly Color Cyan = new Color(0.08f, 0.78f, 1f, 1f);
    private static readonly Color Yellow = new Color(1f, 0.78f, 0.08f, 1f);
    private static readonly Color Muted = new Color(0.62f, 0.68f, 0.77f, 1f);

    private static void InitializeScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == MainSceneName)
            BindMainMenuButton();
        else if (sceneName == ListSceneName && FindFirstObjectByType<OwnedAgentListController>() == null)
            new GameObject("OwnedAgentListController").AddComponent<OwnedAgentListController>();
    }

    private static void BindMainMenuButton()
    {
        GameObject listGroup = GameObject.Find("Char_List");
        if (listGroup == null) return;

        Button button = listGroup.GetComponentInChildren<Button>(true);
        if (button == null) return;

        button.onClick.RemoveListener(OpenAgentList);
        button.onClick.AddListener(OpenAgentList);
    }

    private static void OpenAgentList()
    {
        SceneManager.LoadScene(ListSceneName);
    }

    private void Awake()
    {
        enabled = false;
        return;
#pragma warning disable CS0162
        catalog = Resources.Load<OwnedAgentCatalog>("OwnedAgentCatalog");
        uiFont = catalog != null ? catalog.UIFont : null;
        EnsureEventSystem();
        BuildScreen();
#pragma warning restore CS0162
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildScreen()
    {
        GameObject canvasObject = new GameObject(
            "AgentListCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2220f);
        scaler.matchWidthOrHeight = 0f;

        Image background = CreateImage(canvasObject.transform, "Background", Background);
        Stretch(background.rectTransform);

        safeAreaRoot = CreateRect(canvasObject.transform, "SafeArea");
        ApplySafeArea();
        BuildHeader();
        BuildRoster();
        BuildDetailPanel(canvasObject.transform);
    }

    private void BuildHeader()
    {
        Image header = CreateImage(safeAreaRoot, "Header", new Color(0.04f, 0.055f, 0.08f, 1f));
        SetAnchors(header.rectTransform, 0f, 0.88f, 1f, 1f);

        Button back = CreateButton(header.transform, "BackButton", "<", new Color(0.11f, 0.14f, 0.2f, 1f));
        SetAnchors((RectTransform)back.transform, 0.035f, 0.18f, 0.16f, 0.82f);
        back.onClick.AddListener(() => SceneManager.LoadScene(MainSceneName));

        TMP_Text title = CreateText(header.transform, "Title", "요원 리스트", 48f, TextAlignmentOptions.Left);
        SetAnchors(title.rectTransform, 0.19f, 0.42f, 0.95f, 0.86f);
        title.fontStyle = FontStyles.Bold;

        int count = catalog != null ? catalog.OwnedAgents.Count : 0;
        TMP_Text countText = CreateText(header.transform, "OwnedCount", $"보유 요원  {count}명", 25f, TextAlignmentOptions.Left);
        SetAnchors(countText.rectTransform, 0.195f, 0.12f, 0.85f, 0.45f);
        countText.color = Cyan;
    }

    private void BuildRoster()
    {
        GameObject scrollObject = new GameObject(
            "AgentScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(safeAreaRoot, false);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        SetAnchors(scrollRectTransform, 0.025f, 0.025f, 0.975f, 0.865f);
        scrollObject.GetComponent<Image>().color = new Color(0.035f, 0.048f, 0.072f, 1f);

        GameObject viewportObject = new GameObject(
            "Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        GameObject contentObject = new GameObject(
            "Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(26, 26, 34, 34);
        grid.cellSize = new Vector2(460f, 610f);
        grid.spacing = new Vector2(34f, 34f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 55f;

        if (catalog == null) return;
        foreach (CharacterData agent in catalog.OwnedAgents)
        {
            if (agent != null) CreateAgentCard(content, agent);
        }
    }

    private void CreateAgentCard(Transform parent, CharacterData agent)
    {
        Button card = CreateButton(parent, $"Agent_{agent.CharacterId}", "", Panel);
        card.onClick.AddListener(() => ShowDetails(agent));

        Image accent = CreateImage(card.transform, "TypeAccent", agent.TeamColor);
        SetAnchors(accent.rectTransform, 0f, 0.965f, 1f, 1f);

        Image portrait = CreateImage(card.transform, "Portrait", Color.Lerp(agent.TeamColor, Background, 0.58f));
        SetAnchors(portrait.rectTransform, 0.035f, 0.37f, 0.965f, 0.95f);
        if (agent.IllustrationSprite != null)
        {
            portrait.sprite = agent.IllustrationSprite;
            portrait.preserveAspect = true;
            portrait.color = Color.white;
        }
        else
        {
            TMP_Text monogram = CreateText(portrait.transform, "Monogram", GetInitials(agent.DisplayName), 92f, TextAlignmentOptions.Center);
            Stretch(monogram.rectTransform);
            monogram.fontStyle = FontStyles.Bold;
            monogram.color = Color.Lerp(agent.TeamColor, Color.white, 0.4f);
        }

        Image ownedBadge = CreateImage(portrait.transform, "OwnedBadge", new Color(0.02f, 0.08f, 0.12f, 0.9f));
        SetAnchors(ownedBadge.rectTransform, 0.04f, 0.82f, 0.35f, 0.96f);
        TMP_Text owned = CreateText(ownedBadge.transform, "Text", "보유 중", 20f, TextAlignmentOptions.Center);
        Stretch(owned.rectTransform);
        owned.color = Cyan;

        TMP_Text name = CreateText(card.transform, "Name", agent.DisplayName, 35f, TextAlignmentOptions.Left);
        SetAnchors(name.rectTransform, 0.055f, 0.245f, 0.945f, 0.36f);
        name.fontStyle = FontStyles.Bold;

        TMP_Text role = CreateText(card.transform, "Role", GetPatternName(agent.AttackPattern), 21f, TextAlignmentOptions.Left);
        SetAnchors(role.rectTransform, 0.055f, 0.17f, 0.945f, 0.255f);
        role.color = Yellow;

        TMP_Text stats = CreateText(card.transform, "Stats",
            $"체력 {agent.MaxHP}   공격 {agent.AttackPower}\n이동 {agent.MoveRange}   사거리 {agent.AttackRange}",
            24f, TextAlignmentOptions.Left);
        SetAnchors(stats.rectTransform, 0.055f, 0.025f, 0.945f, 0.17f);
        stats.color = Muted;
    }

    private void BuildDetailPanel(Transform parent)
    {
        Image dim = CreateImage(parent, "AgentDetail", new Color(0f, 0f, 0f, 0.72f));
        Stretch(dim.rectTransform);
        detailPanel = dim.rectTransform;

        Image box = CreateImage(dim.transform, "Panel", new Color(0.045f, 0.06f, 0.09f, 1f));
        SetAnchors(box.rectTransform, 0.07f, 0.22f, 0.93f, 0.78f);

        detailTitle = CreateText(box.transform, "Title", "", 46f, TextAlignmentOptions.Left);
        SetAnchors(detailTitle.rectTransform, 0.07f, 0.78f, 0.93f, 0.94f);
        detailTitle.fontStyle = FontStyles.Bold;

        detailBody = CreateText(box.transform, "Body", "", 28f, TextAlignmentOptions.TopLeft);
        SetAnchors(detailBody.rectTransform, 0.07f, 0.22f, 0.93f, 0.76f);

        Button close = CreateButton(box.transform, "CloseButton", "확인", Cyan);
        SetAnchors((RectTransform)close.transform, 0.18f, 0.055f, 0.82f, 0.18f);
        close.onClick.AddListener(() => detailPanel.gameObject.SetActive(false));
        detailPanel.gameObject.SetActive(false);
    }

    private void ShowDetails(CharacterData agent)
    {
        detailTitle.text = agent.DisplayName;
        string description = string.IsNullOrWhiteSpace(agent.Description)
            ? "등록된 요원 설명이 없습니다."
            : agent.Description;
        detailBody.text =
            $"{GetPatternName(agent.AttackPattern)}  /  {GetDamageTypeName(agent.DamageType)}\n\n" +
            $"체력 {agent.MaxHP}    공격력 {agent.AttackPower}\n" +
            $"이동 거리 {agent.MoveRange}    공격 사거리 {agent.AttackRange}\n" +
            $"특수 위력 {agent.SpecialPower}    재사용 {agent.CooldownTurns}턴\n\n" +
            description;
        detailPanel.gameObject.SetActive(true);
        detailPanel.SetAsLastSibling();
    }

    private void ApplySafeArea()
    {
        if (safeAreaRoot == null) return;
        lastSafeArea = Screen.safeArea;
        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;
        safeAreaRoot.anchorMin = min;
        safeAreaRoot.anchorMax = max;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    private TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        if (uiFont != null) text.font = uiFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(15f, size * 0.65f);
        text.fontSizeMax = size;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        Button button = obj.GetComponent<Button>();
        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText(obj.transform, "Label", label, 30f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
        }
        return button;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string GetInitials(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "?" : value.Substring(0, Mathf.Min(2, value.Length));
    }

    private static string GetPatternName(CharacterAttackPattern pattern)
    {
        switch (pattern)
        {
            case CharacterAttackPattern.CrossArea: return "십자 범위 공격";
            case CharacterAttackPattern.DiamondArea: return "다이아 범위 공격";
            case CharacterAttackPattern.PiercingLine: return "직선 관통 공격";
            case CharacterAttackPattern.Cone: return "부채꼴 공격";
            case CharacterAttackPattern.Chain: return "연쇄 공격";
            default: return "단일 대상 공격";
        }
    }

    private static string GetDamageTypeName(CharacterDamageType type)
    {
        switch (type)
        {
            case CharacterDamageType.Magical: return "마법 피해";
            case CharacterDamageType.Support: return "지원형";
            default: return "물리 피해";
        }
    }
}
