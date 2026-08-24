using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SquadSelectionState
{
    public static readonly List<CharacterData> SelectedCharacters = new List<CharacterData>();
}

public class SquadSelectionController : MonoBehaviour
{
    [SerializeField] private List<CharacterData> roster = new List<CharacterData>();
    [SerializeField] private int squadSize = 4;
    [SerializeField] private string battleSceneName = "4.MainGame";
    [SerializeField] private string previousSceneName = "3.Stage List";
    [SerializeField] private TMP_FontAsset uiFont;

    private readonly List<CharacterData> selected = new List<CharacterData>();
    private readonly List<Button> rosterButtons = new List<Button>();
    private readonly List<TMP_Text> rosterStatusTexts = new List<TMP_Text>();
    private readonly List<Button> squadButtons = new List<Button>();
    private readonly List<TMP_Text> squadNameTexts = new List<TMP_Text>();
    private readonly List<TMP_Text> squadDetailTexts = new List<TMP_Text>();

    private RectTransform safeAreaRoot;
    private TMP_Text countText;
    private TMP_Text powerText;
    private Button startButton;
    private Rect lastSafeArea;

    private static readonly Color Background = new Color(0.035f, 0.047f, 0.07f, 1f);
    private static readonly Color Panel = new Color(0.075f, 0.094f, 0.13f, 0.98f);
    private static readonly Color Yellow = new Color(1f, 0.78f, 0.08f, 1f);
    private static readonly Color Cyan = new Color(0.05f, 0.78f, 1f, 1f);
    private static readonly Color Muted = new Color(0.48f, 0.53f, 0.62f, 1f);

    private void Awake()
    {
        roster.RemoveAll(character => character == null);
        EnsureEventSystem();
        BuildScreen();
        RefreshScreen();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject(
            "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildScreen()
    {
        GameObject canvasObject = new GameObject(
            "SquadCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage(canvasObject.transform, "Background", Background);
        Stretch(background.rectTransform);

        safeAreaRoot = CreateRect(canvasObject.transform, "SafeArea");
        ApplySafeArea();

        BuildHeader();
        BuildSquadSlots();
        BuildRoster();
        BuildFooter();
    }

    private void BuildHeader()
    {
        Image header = CreateImage(safeAreaRoot, "Header", new Color(0.045f, 0.06f, 0.09f, 1f));
        SetAnchors(header.rectTransform, 0f, 0.86f, 1f, 1f);

        Button back = CreateButton(header.transform, "Back", "<", new Color(0.12f, 0.15f, 0.2f, 1f));
        SetAnchors((RectTransform)back.transform, 0.035f, 0.2f, 0.15f, 0.78f);
        back.onClick.AddListener(() => SceneManager.LoadScene(previousSceneName));

        TMP_Text title = CreateText(header.transform, "Title", "스쿼드 편성", 46f, TextAlignmentOptions.Left);
        SetAnchors(title.rectTransform, 0.18f, 0.48f, 0.96f, 0.9f);
        title.fontStyle = FontStyles.Bold;

        TMP_Text stage = CreateText(header.transform, "Stage", "작전 01  /  도심 구역", 24f, TextAlignmentOptions.Left);
        SetAnchors(stage.rectTransform, 0.185f, 0.15f, 0.78f, 0.48f);
        stage.color = Cyan;

        powerText = CreateText(header.transform, "Power", "전투력 0000", 25f, TextAlignmentOptions.Right);
        SetAnchors(powerText.rectTransform, 0.68f, 0.12f, 0.96f, 0.46f);
        powerText.color = Yellow;
    }

    private void BuildSquadSlots()
    {
        Image section = CreateImage(safeAreaRoot, "SquadSection", Panel);
        SetAnchors(section.rectTransform, 0.035f, 0.56f, 0.965f, 0.845f);

        TMP_Text label = CreateText(section.transform, "Label", "출전 슬롯", 25f, TextAlignmentOptions.Left);
        SetAnchors(label.rectTransform, 0.035f, 0.84f, 0.7f, 0.98f);
        label.color = Muted;

        countText = CreateText(section.transform, "Count", "0 / 4", 25f, TextAlignmentOptions.Right);
        SetAnchors(countText.rectTransform, 0.72f, 0.84f, 0.965f, 0.98f);
        countText.color = Yellow;

        for (int i = 0; i < squadSize; i++)
        {
            int capturedIndex = i;
            float left = 0.035f + i * 0.238f;
            Button slot = CreateButton(section.transform, $"SquadSlot_{i}", "+", new Color(0.11f, 0.14f, 0.19f, 1f));
            SetAnchors((RectTransform)slot.transform, left, 0.08f, left + 0.21f, 0.79f);
            slot.onClick.AddListener(() => RemoveFromSquad(capturedIndex));

            TMP_Text name = CreateText(slot.transform, "Name", "빈 슬롯", 25f, TextAlignmentOptions.Center);
            SetAnchors(name.rectTransform, 0.04f, 0.5f, 0.96f, 0.92f);
            name.fontStyle = FontStyles.Bold;

            TMP_Text detail = CreateText(slot.transform, "Detail", "+", 38f, TextAlignmentOptions.Center);
            SetAnchors(detail.rectTransform, 0.04f, 0.08f, 0.96f, 0.52f);
            detail.color = Muted;

            squadButtons.Add(slot);
            squadNameTexts.Add(name);
            squadDetailTexts.Add(detail);
        }
    }

    private void BuildRoster()
    {
        Image section = CreateImage(safeAreaRoot, "RosterSection", new Color(0.055f, 0.07f, 0.1f, 1f));
        SetAnchors(section.rectTransform, 0.035f, 0.17f, 0.965f, 0.54f);

        TMP_Text label = CreateText(section.transform, "Label", "출전 가능 유닛", 27f, TextAlignmentOptions.Left);
        SetAnchors(label.rectTransform, 0.035f, 0.86f, 0.7f, 0.98f);
        label.fontStyle = FontStyles.Bold;

        for (int i = 0; i < roster.Count; i++)
        {
            CharacterData captured = roster[i];
            int column = i % 3;
            int row = i / 3;
            float left = 0.035f + column * 0.322f;
            float top = 0.82f - row * 0.39f;

            Button card = CreateButton(section.transform, $"Roster_{captured.CharacterId}", "", captured.TeamColor);
            SetAnchors((RectTransform)card.transform, left, top - 0.34f, left + 0.285f, top);
            card.onClick.AddListener(() => ToggleCharacter(captured));

            Image portrait = CreateImage(card.transform, "Portrait", Color.Lerp(captured.TeamColor, Color.black, 0.35f));
            SetAnchors(portrait.rectTransform, 0.04f, 0.34f, 0.96f, 0.96f);

            string initials = GetInitials(captured.DisplayName);
            TMP_Text monogram = CreateText(portrait.transform, "Monogram", initials, 50f, TextAlignmentOptions.Center);
            Stretch(monogram.rectTransform);
            monogram.fontStyle = FontStyles.Bold;

            TMP_Text name = CreateText(card.transform, "Name", captured.DisplayName, 22f, TextAlignmentOptions.Center);
            SetAnchors(name.rectTransform, 0.03f, 0.17f, 0.97f, 0.36f);
            name.fontStyle = FontStyles.Bold;

            TMP_Text stats = CreateText(
                card.transform,
                "Stats",
                $"체력 {captured.MaxHP}  공격 {captured.AttackPower}  이동 {captured.MoveRange}  사거리 {captured.AttackRange}",
                16f,
                TextAlignmentOptions.Center);
            SetAnchors(stats.rectTransform, 0.02f, 0.01f, 0.98f, 0.18f);

            TMP_Text status = CreateText(card.transform, "Status", "", 20f, TextAlignmentOptions.Center);
            SetAnchors(status.rectTransform, 0f, 0f, 1f, 1f);
            status.fontStyle = FontStyles.Bold;
            status.color = Yellow;

            rosterButtons.Add(card);
            rosterStatusTexts.Add(status);
        }
    }

    private void BuildFooter()
    {
        Image footer = CreateImage(safeAreaRoot, "Footer", new Color(0.045f, 0.06f, 0.09f, 1f));
        SetAnchors(footer.rectTransform, 0f, 0f, 1f, 0.15f);

        TMP_Text hint = CreateText(footer.transform, "Hint", "출전할 유닛 4명을 선택하세요", 22f, TextAlignmentOptions.Left);
        SetAnchors(hint.rectTransform, 0.05f, 0.58f, 0.72f, 0.9f);
        hint.color = Muted;

        startButton = CreateButton(footer.transform, "StartButton", "전투 시작  >", Yellow);
        SetAnchors((RectTransform)startButton.transform, 0.47f, 0.12f, 0.95f, 0.58f);
        startButton.onClick.AddListener(StartBattle);
    }

    private void ToggleCharacter(CharacterData character)
    {
        if (selected.Contains(character))
            selected.Remove(character);
        else if (selected.Count < squadSize)
            selected.Add(character);

        RefreshScreen();
    }

    private void RemoveFromSquad(int index)
    {
        if (index < selected.Count)
        {
            selected.RemoveAt(index);
            RefreshScreen();
        }
    }

    private void StartBattle()
    {
        if (selected.Count != squadSize) return;

        SquadSelectionState.SelectedCharacters.Clear();
        SquadSelectionState.SelectedCharacters.AddRange(selected);
        SceneManager.LoadScene(battleSceneName);
    }

    private void RefreshScreen()
    {
        int power = 0;
        for (int i = 0; i < selected.Count; i++)
            power += selected[i].MaxHP * 100 + selected[i].AttackPower * 180 + selected[i].MoveRange * 40;

        countText.text = $"{selected.Count} / {squadSize}";
        powerText.text = $"전투력 {power:0000}";

        for (int i = 0; i < squadSize; i++)
        {
            bool filled = i < selected.Count;
            CharacterData character = filled ? selected[i] : null;
            squadNameTexts[i].text = filled ? character.DisplayName : "빈 슬롯";
            squadDetailTexts[i].text = filled
                ? $"{GetInitials(character.DisplayName)}\n이동 {character.MoveRange} / 사거리 {character.AttackRange}"
                : "+";
            squadButtons[i].GetComponent<Image>().color = filled
                ? character.TeamColor
                : new Color(0.11f, 0.14f, 0.19f, 1f);
        }

        for (int i = 0; i < roster.Count; i++)
        {
            bool deployed = selected.Contains(roster[i]);
            rosterStatusTexts[i].text = deployed ? "편성 완료" : "";
            rosterButtons[i].GetComponent<Image>().color = deployed
                ? Color.Lerp(roster[i].TeamColor, Color.black, 0.58f)
                : roster[i].TeamColor;
        }

        bool ready = selected.Count == squadSize;
        startButton.interactable = ready;
        startButton.GetComponent<Image>().color = ready ? Yellow : new Color(0.22f, 0.24f, 0.28f, 1f);
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

    private static string GetInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "?";
        string[] words = value.Split(' ');
        return words.Length > 1
            ? $"{words[0][0]}{words[1][0]}"
            : value.Substring(0, Mathf.Min(2, value.Length));
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
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
        text.fontSizeMin = Mathf.Max(12f, size * 0.62f);
        text.fontSizeMax = size;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;

        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText(obj.transform, "Label", label, 28f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
        }

        return button;
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
}
