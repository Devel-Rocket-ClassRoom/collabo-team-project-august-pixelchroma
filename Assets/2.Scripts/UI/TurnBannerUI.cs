using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnBannerUI : MonoBehaviour
{
    public static TurnBannerUI Instance { get; private set; }

    [Header("Player Turn")]
    [SerializeField] private Color playerColor = new Color(0.78f, 0.16f, 0.16f, 1f);
    [Tooltip("비워두면 단색 바를 사용합니다")]
    [SerializeField] private Sprite playerBarSprite;

    [Header("Enemy Turn")]
    [SerializeField] private Color enemyColor = new Color(0.08f, 0.35f, 0.72f, 1f);
    [Tooltip("비워두면 단색 바를 사용합니다")]
    [SerializeField] private Sprite enemyBarSprite;

    [Header("Victory")]
    [SerializeField] private Color victoryColor = new Color(1f, 0.78f, 0.08f, 1f);
    [SerializeField] private Sprite victoryBarSprite;

    [Header("Defeat")]
    [SerializeField] private Color defeatColor = new Color(0.25f, 0.25f, 0.3f, 1f);
    [SerializeField] private Sprite defeatBarSprite;

    [Header("Timing")]
    [SerializeField] private float slideInDuration = 0.8f;
    [SerializeField] private float textFadeDuration = 0.3f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Layout")]
    [SerializeField] private float barHeight = 80f;
    [SerializeField] private float yOffset = 30f;
    [SerializeField] private float fontSize = 52f;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset bannerFont;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform leftBar;
    private RectTransform rightBar;
    private RectTransform mergedBar;
    private Image leftImage;
    private Image rightImage;
    private Image mergedImage;
    private TextMeshProUGUI bannerText;
    private Coroutine activeRoutine;

    public bool IsPlaying => activeRoutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUI();
    }

    public void SetFont(TMP_FontAsset font)
    {
        bannerFont = font;
        if (bannerText != null && font != null)
            bannerText.font = font;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("TurnBannerCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2220f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.GetComponent<GraphicRaycaster>().enabled = false;

        GameObject groupObj = new GameObject("BannerGroup",
            typeof(RectTransform), typeof(CanvasGroup));
        groupObj.transform.SetParent(canvasObj.transform, false);
        RectTransform groupRect = groupObj.GetComponent<RectTransform>();
        groupRect.anchorMin = Vector2.zero;
        groupRect.anchorMax = Vector2.one;
        groupRect.offsetMin = Vector2.zero;
        groupRect.offsetMax = Vector2.zero;

        canvasGroup = groupObj.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;

        leftBar = CreateBar(groupObj.transform, "LeftBar");
        leftImage = leftBar.GetComponent<Image>();

        rightBar = CreateBar(groupObj.transform, "RightBar");
        rightImage = rightBar.GetComponent<Image>();

        mergedBar = CreateBar(groupObj.transform, "MergedBar");
        mergedImage = mergedBar.GetComponent<Image>();

        float totalH = yOffset * 2f + barHeight;
        mergedBar.sizeDelta = new Vector2(0f, totalH);

        GameObject textObj = new GameObject("BannerText",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(mergedBar, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        bannerText = textObj.GetComponent<TextMeshProUGUI>();
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.fontSize = fontSize;
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = Color.white;
        bannerText.raycastTarget = false;

        if (bannerFont != null)
            bannerText.font = bannerFont;

        HideAll();
    }

    private RectTransform CreateBar(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, barHeight);

        Image img = obj.GetComponent<Image>();
        img.raycastTarget = false;

        obj.SetActive(false);
        return rect;
    }

    private void HideAll()
    {
        canvasGroup.alpha = 0f;
        leftBar.gameObject.SetActive(false);
        rightBar.gameObject.SetActive(false);
        mergedBar.gameObject.SetActive(false);
    }

    public void ShowPlayerTurn()
    {
        Show("플레이어 턴", playerColor, playerBarSprite);
    }

    public void ShowEnemyTurn()
    {
        Show("상대 턴", enemyColor, enemyBarSprite);
    }

    public void Show(string text, Color color, Sprite barSprite)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayBanner(text, color, barSprite));
    }

    public Coroutine ShowAndWait(string text, Color color, Sprite barSprite)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayBanner(text, color, barSprite));
        return activeRoutine;
    }

    public Coroutine ShowPlayerTurnAndWait()
    {
        return ShowAndWait("플레이어 턴", playerColor, playerBarSprite);
    }

    public Coroutine ShowEnemyTurnAndWait()
    {
        return ShowAndWait("상대 턴", enemyColor, enemyBarSprite);
    }

    public void ShowVictory(string text = null)
    {
        Show(text ?? "승리!", victoryColor, victoryBarSprite);
    }

    public Coroutine ShowVictoryAndWait(string text = null)
    {
        return ShowAndWait(text ?? "승리!", victoryColor, victoryBarSprite);
    }

    public void ShowDefeat(string text = null)
    {
        Show(text ?? "패배...", defeatColor, defeatBarSprite);
    }

    public Coroutine ShowDefeatAndWait(string text = null)
    {
        return ShowAndWait(text ?? "패배...", defeatColor, defeatBarSprite);
    }

    private void ApplyBarStyle(Image image, Color color, Sprite sprite)
    {
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
        }
    }

    private IEnumerator PlayBanner(string text, Color color, Sprite barSprite)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;
        float screenW = canvasRect != null ? canvasRect.rect.width : 1080f;

        ApplyBarStyle(leftImage, color, barSprite);
        ApplyBarStyle(rightImage, color, barSprite);
        ApplyBarStyle(mergedImage, color, barSprite);

        if (bannerFont != null)
        {
            bannerText.font = bannerFont;
            bannerFont.TryAddCharacters(text);
        }
        bannerText.text = text;
        bannerText.ForceMeshUpdate();

        mergedBar.gameObject.SetActive(false);
        leftBar.gameObject.SetActive(true);
        rightBar.gameObject.SetActive(true);

        leftBar.pivot = new Vector2(0f, 0.5f);
        rightBar.pivot = new Vector2(1f, 0.5f);

        float leftFixedY = yOffset;
        float rightFixedY = -yOffset;

        leftBar.anchoredPosition = new Vector2(-screenW * 0.5f, leftFixedY);
        rightBar.anchoredPosition = new Vector2(screenW * 0.5f, rightFixedY);
        leftBar.sizeDelta = new Vector2(0f, barHeight);
        rightBar.sizeDelta = new Vector2(0f, barHeight);

        canvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideInDuration);
            float e = EaseOutCubic(t);

            float w = screenW * e;
            leftBar.sizeDelta = new Vector2(w, barHeight);
            rightBar.sizeDelta = new Vector2(w, barHeight);

            yield return null;
        }

        leftBar.sizeDelta = new Vector2(screenW, barHeight);
        rightBar.sizeDelta = new Vector2(screenW, barHeight);

        leftBar.gameObject.SetActive(false);
        rightBar.gameObject.SetActive(false);

        float totalH = yOffset * 2f + barHeight;
        mergedBar.gameObject.SetActive(true);
        mergedBar.anchoredPosition = Vector2.zero;
        mergedBar.sizeDelta = new Vector2(screenW, totalH);
        mergedBar.localScale = Vector3.one;

        bannerText.alpha = 0f;
        elapsed = 0f;
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bannerText.alpha = Mathf.Clamp01(elapsed / textFadeDuration);
            yield return null;
        }
        bannerText.alpha = 1f;

        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            float scale = 1f + 0.1f * t;
            mergedBar.localScale = new Vector3(scale, scale, 1f);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        HideAll();
        activeRoutine = null;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
