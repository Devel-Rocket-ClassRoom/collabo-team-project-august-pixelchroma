using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum GamePhase
{
    Deployment,
    ReadyToStart,
    PlayerTurn,
    EnemyTurn,
    BattleResult
}

public enum BattleState
{
    Idle,
    UnitSelected,
    UnitMoved
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("2D Character Images")]
    [Tooltip("CharacterData에 이미지가 없을 때 아군에게 사용할 기본 2D 이미지")]
    [SerializeField] private Sprite defaultPlayerSprite;
    [Tooltip("적 캐릭터에게 사용할 기본 2D 이미지. 적 프리팹의 Unit 이미지가 있으면 프리팹 이미지가 우선합니다.")]
    [SerializeField] private Sprite defaultEnemySprite;

    [Header("Settings")]
    [SerializeField] private int maxPlayerUnits = 4;
    [Tooltip("Enemy Squad가 비어 있을 때만 사용하는 적 수입니다. 단체를 지정하면 단체 편성이 우선합니다.")]
    [SerializeField] private int maxEnemyUnits = 4;

    [Header("Enemy Squad")]
    [Tooltip("이 전투에 배치할 적 단체입니다. 비워 두면 기존 방식(enemyPrefab 복제)으로 동작합니다.")]
    [SerializeField] private EnemySquadData enemySquad;

    [Header("Combat Random")]
    [Tooltip("전투 판정 난수의 시드입니다. 0이면 매 판 다른 시드를 쓰고 Console에 기록합니다.\n" +
             "버그를 재현하려면 로그에 찍힌 시드를 여기에 입력하세요.")]
    [SerializeField] private uint combatSeed;

    [Header("Deployment Roster")]
    [Tooltip("Characters that can be selected before battle. Empty uses a prototype roster.")]
    [SerializeField] private List<CharacterData> availableCharacters = new List<CharacterData>();

    [Header("Camera")]
    [SerializeField] private float cameraZoom = 0.9f;
    [SerializeField] private float cameraAngle = 20f;
    [SerializeField] private float cameraFOV = 50f;

    [Header("UI")]
    [SerializeField] private GameObject gameStartUI;
    [SerializeField] private GameObject gamePlayUI;
    [SerializeField] private TMP_Text gameInfoText;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_FontAsset uiFont;

    [Header("Runtime UI Prefabs")]
    [Tooltip("디자인팀이 직접 편집하는 캐릭터 배치 UI Prefab")]
    [SerializeField] private DeploymentUIView deploymentUIPrefab;
    [Tooltip("디자인팀이 직접 편집하는 공격 미리보기 UI Prefab")]
    [SerializeField] private AttackPreviewUIView attackPreviewUIPrefab;
    [Tooltip("턴 전환 배너 UI Prefab (Turnline)")]
    [SerializeField] private TurnBannerUI turnBannerPrefab;

    [Header("Runtime UI Images")]
    [Tooltip("공격 미리보기의 취소 버튼 이미지")]
    [SerializeField] private Sprite attackCancelButtonSprite;
    [Tooltip("공격 미리보기의 공격 확정 버튼 이미지")]
    [SerializeField] private Sprite attackConfirmButtonSprite;
    [Tooltip("배치 완료 후 표시되는 게임 시작 버튼 이미지")]
    [SerializeField] private Sprite deploymentStartButtonSprite;
    [Tooltip("게임 플레이의 되돌리기 버튼 이미지")]
    [SerializeField] private Sprite undoButtonSprite;
    [Tooltip("게임 플레이의 확정/대기 버튼 이미지")]
    [SerializeField] private Sprite confirmButtonSprite;

    [Header("Text Data")]
    [SerializeField] private GameTextData gameTextData;

    private GamePhase currentPhase;
    private BattleState battleState;

    private List<Unit> playerUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();

    private Unit selectedUnit;
    private List<Vector2Int> moveTiles = new List<Vector2Int>();
    private List<Vector2Int> attackTiles = new List<Vector2Int>();
    private int deployedCount;
    private string resultMessage = "";
    private int turnCount;
    private Vector2Int undoPosition;
    private CharacterData selectedDeployCharacter;
    private GameObject deploymentPanel;
    private readonly List<Button> characterButtons = new List<Button>();
    private TMP_Text deploymentInfoText;
    private Button deploymentStartButton;
    private GameObject deploymentCancelHint;
    private bool hasShownDeploymentCancelHint;
    private Unit attackPreviewTarget;
    private RectTransform attackPreviewPanel;
    private TextMeshProUGUI attackPreviewText;
    private Canvas attackPreviewCanvas;
    private Button attackConfirmButton;
    private Button attackCancelButton;

    /// <summary>가장 최근 공격의 결과 문구입니다. 명중/빗나감/치명타/반격을 알려줍니다.</summary>
    private string lastCombatMessage = "";

    /// <summary>이번 전투의 난수입니다. 시드를 다시 넣으면 같은 전투가 재현됩니다.</summary>
    private DeterministicRandom combatRandom;

    private Color DeployHighlight => GridManager.Instance != null
        ? GridManager.Instance.DeployHighlight
        : new Color(0.2f, 0.85f, 0.3f, 0.6f);
    private Color MoveHighlight => GridManager.Instance != null
        ? GridManager.Instance.MoveHighlight
        : new Color(0.3f, 0.75f, 1f, 0.6f);
    private Color AttackHighlight => GridManager.Instance != null
        ? GridManager.Instance.AttackHighlight
        : new Color(1f, 0.25f, 0.25f, 0.6f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (gameStartUI != null) gameStartUI.SetActive(false);
        if (gamePlayUI != null) gamePlayUI.SetActive(false);
        Invoke(nameof(InitGame), 0.1f);
    }

    private void OnDestroy()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.OnTap -= HandleTap;
    }

    private void InitGame()
    {
        InitCombatRandom();
        SetupCamera();
        SpawnEnemies();
        HideOriginalPrefabs();
        EnsureTurnBanner();

        currentPhase = GamePhase.Deployment;
        deployedCount = 0;
        turnCount = 0;
        ShowDeployZone();
        SetupUI();
        SetupDeploymentRoster();
    }

    /// <summary>
    /// 전투 난수를 초기화합니다. 시드가 0이면 매 판 다르게 뽑되 반드시 로그로 남깁니다.
    /// 버그를 재현할 때 그 시드를 Inspector에 넣으면 같은 전투가 그대로 재생됩니다.
    /// </summary>
    private void InitCombatRandom()
    {
        combatRandom = combatSeed == 0u
            ? DeterministicRandom.FromTime()
            : new DeterministicRandom(combatSeed);

        CombatResolver.InitRandom(combatRandom);
        Debug.Log($"[Combat] 전투 시드 = {combatRandom.Seed}  " +
                  "(재현하려면 GameManager의 Combat Seed에 이 값을 입력하세요)");
    }

    private void EnsureTurnBanner()
    {
        if (TurnBannerUI.Instance != null) return;
        if (turnBannerPrefab != null)
        {
            Instantiate(turnBannerPrefab);
        }
        else
        {
            new GameObject("TurnBannerUI", typeof(TurnBannerUI));
        }

        if (TurnBannerUI.Instance != null && uiFont != null)
            TurnBannerUI.Instance.SetFont(uiFont);
    }

    // ─────────────────── Camera ───────────────────

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector3 p = grid.GridToWorldPosition(x, y);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        Vector3 center = (min + max) * 0.5f;
        float spanX = (max.x - min.x) + grid.CellSize * 2f;
        float spanZ = (max.z - min.z) + grid.CellSize * 2f;
        float aspect = (float)Screen.width / Screen.height;

        cam.orthographic = false;
        cam.fieldOfView = cameraFOV;

        float sizeForWidth = spanX / (2f * aspect);
        float sizeForHeight = spanZ / 2f;
        float equivalentSize = Mathf.Max(sizeForWidth, sizeForHeight) * cameraZoom;
        float halfFov = cameraFOV * 0.5f * Mathf.Deg2Rad;
        float distance = equivalentSize / Mathf.Tan(halfFov);

        float angleRad = cameraAngle * Mathf.Deg2Rad;
        cam.transform.position = new Vector3(
            center.x,
            center.y + distance * Mathf.Sin(angleRad),
            center.z - distance * Mathf.Cos(angleRad));
        cam.transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

        CameraController ctrl = cam.GetComponent<CameraController>();
        if (ctrl == null)
            ctrl = cam.gameObject.AddComponent<CameraController>();
        ctrl.SetBounds(min, max, grid.CellSize, center.y);
        ctrl.OnTap += HandleTap;
    }

    // ─────────────────── Input ───────────────────

    private void HandleTap(Vector2 screenPosition)
    {
        if (currentPhase == GamePhase.BattleResult) return;
        if (currentPhase == GamePhase.EnemyTurn) return;
        if (currentPhase == GamePhase.ReadyToStart) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        if (hits.Length == 0) return;

        // A character's 3D collider can visually overlap a neighbouring tile
        // when a camera-facing 2D sprite is used. Resolve the actual board tile
        // first instead of allowing the closest unit collider to steal the tap.
        Tile tile = null;
        Unit directlyHitUnit = null;
        float closestTileDistance = float.MaxValue;
        float closestUnitDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            Tile hitTile = hit.collider.GetComponent<Tile>();
            if (hitTile != null && hit.distance < closestTileDistance)
            {
                tile = hitTile;
                closestTileDistance = hit.distance;
            }

            Unit hitUnit = hit.collider.GetComponent<Unit>();
            if (hitUnit != null && hit.distance < closestUnitDistance)
            {
                directlyHitUnit = hitUnit;
                closestUnitDistance = hit.distance;
            }
        }

        if (tile == null && directlyHitUnit != null)
            tile = GridManager.Instance.GetTile(directlyHitUnit.GridPosition);

        if (tile == null) return;

        Unit clickedUnit = tile.OccupyingUnit != null
            ? tile.OccupyingUnit.GetComponent<Unit>()
            : null;

        if (currentPhase == GamePhase.Deployment)
            HandleDeployClick(tile);
        else if (currentPhase == GamePhase.PlayerTurn)
            HandleBattleClick(tile, clickedUnit);
    }

    private void HideOriginalPrefabs()
    {
        if (enemyPrefab != null) enemyPrefab.SetActive(false);
        if (playerPrefab != null) playerPrefab.SetActive(false);
    }

    // ─────────────────── Deployment ───────────────────

    private void ShowDeployZone()
    {
        GridManager grid = GridManager.Instance;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                Tile t = grid.GetTile(x, y);
                if (t != null && t.State == TileState.Empty)
                    t.SetHighlight(DeployHighlight);
            }
        }
    }

    private void HandleDeployClick(Tile tile)
    {
        if (tile.Zone != TileZone.PlayerDeploy) return;

        if (tile.OccupyingUnit != null)
        {
            Unit placedUnit = tile.OccupyingUnit.GetComponent<Unit>();
            if (placedUnit != null && placedUnit.UnitTeam == Team.Player)
            {
                // While another character is armed for placement, an occupied
                // tile is not a cancel command. This also protects against a
                // sprite/collider overlapping the intended empty tile.
                if (selectedDeployCharacter != null)
                    return;

                playerUnits.Remove(placedUnit);
                placedUnit.RemoveFromBoard();
                deployedCount = Mathf.Max(0, deployedCount - 1);
                selectedDeployCharacter = placedUnit.CharacterData;
                RefreshDeploymentUI();
                tile.SetHighlight(DeployHighlight);
            }
            return;
        }

        if (deployedCount >= maxPlayerUnits) return;
        if (tile.State != TileState.Empty) return;
        if (selectedDeployCharacter == null) return;
        if (IsCharacterDeployed(selectedDeployCharacter)) return;

        Unit unit = Unit.Create(
            Team.Player,
            tile.GridPosition,
            playerPrefab,
            selectedDeployCharacter,
            defaultPlayerSprite);
        playerUnits.Add(unit);
        deployedCount++;
        tile.ClearHighlight();
        selectedDeployCharacter = null;
        RefreshDeploymentUI();
        ShowDeploymentCancelHintOnce();
    }

    private bool IsCharacterDeployed(CharacterData character)
    {
        return playerUnits.Exists(unit => unit != null && unit.CharacterData == character);
    }

    private CharacterData FindFirstUndeployedCharacter()
    {
        return availableCharacters.Find(character =>
            character != null && !IsCharacterDeployed(character));
    }

    private void SetupDeploymentRoster()
    {
        availableCharacters.RemoveAll(character => character == null);
        if (SquadSelectionState.SelectedCharacters.Count > 0)
        {
            availableCharacters.Clear();
            availableCharacters.AddRange(SquadSelectionState.SelectedCharacters);
        }
        if (availableCharacters.Count == 0)
            CreatePrototypeRoster();

        selectedDeployCharacter = null;
        CreateDeploymentPanel();
        RefreshDeploymentUI();
    }

    private void CreatePrototypeRoster()
    {
        availableCharacters.Add(CreateRuntimeCharacter(
            "swordsman", "검사", 5, 2, 3, 1, new Color(0.2f, 0.55f, 1f)));
        availableCharacters.Add(CreateRuntimeCharacter(
            "lancer", "창병", 4, 2, 3, 2, new Color(0.25f, 0.85f, 0.55f)));
        availableCharacters.Add(CreateRuntimeCharacter(
            "archer", "궁수", 3, 2, 2, 3, new Color(1f, 0.65f, 0.2f)));
        availableCharacters.Add(CreateRuntimeCharacter(
            "healer", "의무병", 3, 1, 3, 1, new Color(0.95f, 0.35f, 0.75f)));
    }

    private CharacterData CreateRuntimeCharacter(
        string id, string name, int hp, int attack, int movement, int range, Color color)
    {
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
        data.name = $"Runtime_{id}";
        data.ConfigureRuntime(id, name, playerPrefab, hp, attack, movement, range, color, defaultPlayerSprite);
        return data;
    }

    private void CreateDeploymentPanel()
    {
        if (deploymentUIPrefab != null)
        {
            CreateDeploymentPanelFromPrefab();
            return;
        }

        GameObject canvasObject = new GameObject(
            "DeploymentCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2220f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        deploymentPanel = new GameObject(
            "DeploymentRosterPanel",
            typeof(RectTransform),
            typeof(Image));
        deploymentPanel.transform.SetParent(canvas.transform, false);

        RectTransform rect = deploymentPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.025f, 0.08f);
        rect.anchorMax = new Vector2(0.975f, 0.28f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = deploymentPanel.GetComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.065f, 0.97f);

        GameObject startObject = new GameObject(
            "DeploymentStartButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        startObject.transform.SetParent(canvas.transform, false);
        RectTransform startRect = startObject.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.24f, 0.295f);
        startRect.anchorMax = new Vector2(0.76f, 0.36f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;

        Image startImage = startObject.GetComponent<Image>();
        startImage.color = new Color(1f, 0.72f, 0.08f, 1f);
        ApplyButtonSprite(startImage, deploymentStartButtonSprite);

        deploymentStartButton = startObject.GetComponent<Button>();
        deploymentStartButton.onClick.AddListener(OnGameStartClicked);

        GameObject startLabelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        startLabelObject.transform.SetParent(startObject.transform, false);
        RectTransform startLabelRect = startLabelObject.GetComponent<RectTransform>();
        startLabelRect.anchorMin = Vector2.zero;
        startLabelRect.anchorMax = Vector2.one;
        startLabelRect.offsetMin = Vector2.zero;
        startLabelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI startLabel = startLabelObject.GetComponent<TextMeshProUGUI>();
        startLabel.text = "게임 시작";
        startLabel.alignment = TextAlignmentOptions.Center;
        startLabel.fontSize = 34f;
        startLabel.fontStyle = FontStyles.Bold;
        startLabel.color = new Color(0.08f, 0.06f, 0.02f, 1f);
        startLabel.raycastTarget = false;
        if (uiFont != null) startLabel.font = uiFont;

        deploymentCancelHint = CreateDeploymentCancelHint(canvas.transform);

        GameObject infoObject = new GameObject(
            "SelectionInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObject.transform.SetParent(deploymentPanel.transform, false);
        RectTransform infoRect = infoObject.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.025f, 0.68f);
        infoRect.anchorMax = new Vector2(0.975f, 0.96f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        deploymentInfoText = infoObject.GetComponent<TextMeshProUGUI>();
        deploymentInfoText.alignment = TextAlignmentOptions.Center;
        deploymentInfoText.fontSize = 24f;
        deploymentInfoText.fontStyle = FontStyles.Bold;
        deploymentInfoText.color = Color.white;
        deploymentInfoText.raycastTarget = false;
        if (uiFont != null) deploymentInfoText.font = uiFont;

        GameObject rowObject = new GameObject(
            "CharacterRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(deploymentPanel.transform, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.025f, 0.06f);
        rowRect.anchorMax = new Vector2(0.975f, 0.66f);
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (CharacterData character in availableCharacters)
        {
            CharacterData capturedCharacter = character;
            Button button = CreateCharacterButton(rowObject.transform, character);
            button.onClick.AddListener(() => SelectDeployCharacter(capturedCharacter));
            characterButtons.Add(button);
        }
    }

    private void CreateDeploymentPanelFromPrefab()
    {
        DeploymentUIView view = Instantiate(deploymentUIPrefab);
        view.name = "DeploymentUI";
        deploymentPanel = view.Panel;
        deploymentInfoText = view.InfoText;
        deploymentStartButton = view.StartButton;
        deploymentCancelHint = view.CancelHint;

        if (deploymentStartButton != null)
        {
            ApplyButtonSprite(
                deploymentStartButton.GetComponent<Image>(),
                deploymentStartButtonSprite);
            deploymentStartButton.onClick.AddListener(OnGameStartClicked);
        }
        if (deploymentCancelHint != null)
            deploymentCancelHint.SetActive(false);

        RectTransform container = view.CharacterContainer;
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        foreach (CharacterData character in availableCharacters)
        {
            CharacterData capturedCharacter = character;
            Button button = CreateCharacterButton(container, character);
            button.onClick.AddListener(() => SelectDeployCharacter(capturedCharacter));
            characterButtons.Add(button);
        }
    }

    private Button CreateCharacterButton(Transform parent, CharacterData character)
    {
        GameObject buttonObject = new GameObject(
            $"Character_{character.CharacterId}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = character.TeamColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(character.TeamColor, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(character.TeamColor, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text =
            $"{character.DisplayName}\n" +
            $"체력 {character.MaxHP}  공격 {character.AttackPower}\n" +
            $"이동 {character.MoveRange}  사거리 {character.AttackRange}\n" +
            GetAttackPatternLabel(character.AttackPattern);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 21f;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 21f;
        label.color = Color.white;
        if (uiFont != null) label.font = uiFont;

        return button;
    }

    private GameObject CreateDeploymentCancelHint(Transform parent)
    {
        GameObject hintObject = new GameObject(
            "DeploymentCancelHint",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup));
        hintObject.transform.SetParent(parent, false);

        RectTransform rect = hintObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.292f);
        rect.anchorMax = new Vector2(0.92f, 0.347f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = hintObject.GetComponent<Image>();
        background.color = new Color(0.02f, 0.035f, 0.06f, 0.96f);
        background.raycastTarget = false;

        CanvasGroup group = hintObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject textObject = new GameObject(
            "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(hintObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 6f);
        textRect.offsetMax = new Vector2(-16f, -6f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "배치 취소하려면 배치된 캐릭터를 눌러주세요";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 27f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.82f, 0.18f, 1f);
        text.raycastTarget = false;
        if (uiFont != null) text.font = uiFont;

        hintObject.SetActive(false);
        return hintObject;
    }

    private void ShowDeploymentCancelHintOnce()
    {
        if (hasShownDeploymentCancelHint || deploymentCancelHint == null)
            return;

        hasShownDeploymentCancelHint = true;
        StartCoroutine(ShowDeploymentCancelHintRoutine());
    }

    private IEnumerator ShowDeploymentCancelHintRoutine()
    {
        deploymentCancelHint.SetActive(true);
        yield return new WaitForSecondsRealtime(4f);
        if (deploymentCancelHint != null)
            deploymentCancelHint.SetActive(false);
    }

    private string GetAttackPatternLabel(CharacterAttackPattern pattern)
    {
        switch (pattern)
        {
            case CharacterAttackPattern.CrossArea: return "십자 범위";
            case CharacterAttackPattern.DiamondArea: return "광역 폭발";
            case CharacterAttackPattern.PiercingLine: return "직선 관통";
            case CharacterAttackPattern.Cone: return "부채꼴 공격";
            case CharacterAttackPattern.Chain: return "연쇄 공격";
            default: return "단일 공격";
        }
    }

    private void SelectDeployCharacter(CharacterData character)
    {
        if (currentPhase != GamePhase.Deployment) return;
        if (character == null || IsCharacterDeployed(character)) return;

        selectedDeployCharacter = character;
        RefreshDeploymentUI();
    }

    private void RefreshDeploymentUI()
    {
        if (deploymentPanel != null)
            deploymentPanel.SetActive(currentPhase == GamePhase.Deployment);

        if (deploymentStartButton != null)
        {
            bool canStart = currentPhase == GamePhase.Deployment &&
                            deployedCount == maxPlayerUnits;
            deploymentStartButton.gameObject.SetActive(canStart);
            if (canStart && deploymentCancelHint != null)
                deploymentCancelHint.SetActive(false);
        }

        if (deploymentInfoText != null)
        {
            deploymentInfoText.text = selectedDeployCharacter == null
                ? $"배치할 유닛을 선택하세요  /  {deployedCount}명 배치 완료"
                : $"{selectedDeployCharacter.DisplayName} 선택  -  파란 칸을 누르세요";
            deploymentInfoText.color = Color.black;
        }

        for (int i = 0; i < characterButtons.Count && i < availableCharacters.Count; i++)
        {
            CharacterData character = availableCharacters[i];
            Button button = characterButtons[i];
            bool deployed = IsCharacterDeployed(character);
            button.interactable = !deployed;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = deployed
                    ? new Color(0.2f, 0.2f, 0.2f, 0.7f)
                    : character == selectedDeployCharacter
                        ? Color.Lerp(character.TeamColor, Color.white, 0.35f)
                        : character.TeamColor;
        }
    }

    // ─────────────────── Enemy Spawn ───────────────────

    private void SpawnEnemies()
    {
        GridManager grid = GridManager.Instance;

        // 단체가 지정되어 있으면 단체가 정한 배치 구역을 씁니다.
        Vector2Int rows = enemySquad != null
            ? enemySquad.SpawnRowRange
            : new Vector2Int(4, 5);

        var available = new List<Vector2Int>();
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = rows.x; y <= rows.y; y++)
            {
                Tile tile = grid.GetTile(x, y);
                if (tile != null && tile.State == TileState.Empty)
                    available.Add(new Vector2Int(x, y));
            }
        }

        if (enemySquad != null)
            SpawnSquadMembers(available);
        else
            SpawnGenericEnemies(available);
    }

    /// <summary>단체 편성표대로 적을 세웁니다. 스탯·이미지는 EnemyUnitData에서 옵니다.</summary>
    private void SpawnSquadMembers(List<Vector2Int> available)
    {
        foreach (EnemySquadData.SquadMember member in enemySquad.Members)
        {
            if (member.unit == null) continue;

            int count = Mathf.Max(1, member.count);
            for (int i = 0; i < count; i++)
            {
                Vector2Int pos = default;

                bool placed =
                    member.preferredCells != null &&
                    i < member.preferredCells.Length &&
                    TryTakeCell(available, member.preferredCells[i], out pos);

                if (!placed && !TryTakeRandomCell(available, out pos))
                    return;   // 빈 칸 소진

                Unit enemy = Unit.Create(
                    Team.Enemy, pos, enemyPrefab, member.unit, defaultEnemySprite);
                enemyUnits.Add(enemy);
            }
        }
    }

    /// <summary>단체가 없을 때의 기존 동작입니다.</summary>
    private void SpawnGenericEnemies(List<Vector2Int> available)
    {
        int count = Mathf.Min(maxEnemyUnits, available.Count);
        for (int i = 0; i < count; i++)
        {
            if (!TryTakeRandomCell(available, out Vector2Int pos)) break;

            Unit enemy = Unit.Create(
                Team.Enemy, pos, enemyPrefab, null, defaultEnemySprite);
            enemyUnits.Add(enemy);
        }
    }

    private static bool TryTakeCell(
        List<Vector2Int> available, Vector2Int wanted, out Vector2Int pos)
    {
        int idx = available.IndexOf(wanted);
        if (idx < 0)
        {
            pos = default;
            return false;
        }
        pos = available[idx];
        available.RemoveAt(idx);
        return true;
    }

    private static bool TryTakeRandomCell(
        List<Vector2Int> available, out Vector2Int pos)
    {
        if (available.Count == 0)
        {
            pos = default;
            return false;
        }
        int idx = Random.Range(0, available.Count);
        pos = available[idx];
        available.RemoveAt(idx);
        return true;
    }

    // ─────────────────── Battle ───────────────────

    private void StartBattle()
    {
        StartCoroutine(StartBattleRoutine());
    }

    private IEnumerator StartBattleRoutine()
    {
        currentPhase = GamePhase.PlayerTurn;
        battleState = BattleState.Idle;
        turnCount = 1;
        ResetPlayerActions();
        if (TurnBannerUI.Instance != null)
            yield return TurnBannerUI.Instance.ShowPlayerTurnAndWait();
    }

    private void ResetPlayerActions()
    {
        foreach (var unit in playerUnits)
        {
            if (unit != null && !unit.IsDead)
                unit.ResetTurn();
        }
    }

    // ─────────────────── Player Turn ───────────────────

    private void HandleBattleClick(Tile tile, Unit clickedUnit)
    {
        if (battleState == BattleState.Idle)
        {
            if (clickedUnit != null && clickedUnit.UnitTeam == Team.Player && !clickedUnit.HasActed)
                SelectUnit(clickedUnit);
        }
        else if (battleState == BattleState.UnitSelected)
        {
            if (clickedUnit != null && clickedUnit == selectedUnit)
            {
                DeselectUnit();
                return;
            }

            if (attackTiles.Contains(tile.GridPosition))
            {
                Unit target = GetUnitAt(tile.GridPosition, Team.Enemy);
                if (target != null)
                {
                    ConfirmOrPreviewAttack(target);
                    return;
                }
            }

            if (moveTiles.Contains(tile.GridPosition))
            {
                MoveSelectedUnit(tile.GridPosition);
                return;
            }

            DeselectUnit();
            if (clickedUnit != null && clickedUnit.UnitTeam == Team.Player && !clickedUnit.HasActed)
                SelectUnit(clickedUnit);
        }
        else if (battleState == BattleState.UnitMoved)
        {
            if (attackTiles.Contains(tile.GridPosition))
            {
                Unit target = GetUnitAt(tile.GridPosition, Team.Enemy);
                if (target != null)
                {
                    ConfirmOrPreviewAttack(target);
                    return;
                }
            }

            HideAttackPreview();
        }
    }

    private void SelectUnit(Unit unit)
    {
        DeselectUnit();
        selectedUnit = unit;
        selectedUnit.SetSelected(true);
        battleState = BattleState.UnitSelected;

        GridManager grid = GridManager.Instance;

        var reachable = Pathfinding.GetReachableTiles(unit.GridPosition, unit.MoveRange);
        moveTiles = reachable;
        foreach (var pos in moveTiles)
        {
            Tile t = grid.GetTile(pos);
            if (t != null) t.SetHighlight(MoveHighlight);
        }

        ShowAttackRange(unit.GridPosition, GetEffectiveAttackRange(unit));
    }

    private void DeselectUnit()
    {
        if (selectedUnit != null)
            selectedUnit.SetSelected(false);
        selectedUnit = null;
        battleState = BattleState.Idle;
        ClearAllMarkers();
    }

    private void MoveSelectedUnit(Vector2Int targetPos)
    {
        ClearAllMarkers();
        undoPosition = selectedUnit.GridPosition;
        selectedUnit.MoveTo(targetPos);
        battleState = BattleState.UnitMoved;
        ShowAttackRangeAfterMove();
    }

    private void UndoMove()
    {
        if (currentPhase != GamePhase.PlayerTurn ||
            battleState != BattleState.UnitMoved ||
            selectedUnit == null)
            return;

        ClearAllMarkers();
        selectedUnit.MoveTo(undoPosition);
        SelectUnit(selectedUnit);
    }

    private void ShowAttackRangeAfterMove()
    {
        ShowAttackRange(selectedUnit.GridPosition, GetEffectiveAttackRange(selectedUnit));
    }

    private void ShowAttackRange(Vector2Int origin, int range)
    {
        attackTiles.Clear();
        foreach (Vector2Int position in Pathfinding.GetTilesInRange(origin, range))
        {
            Tile tile = GridManager.Instance.GetTile(position);
            if (tile == null) continue;

            tile.SetHighlight(AttackHighlight);
            attackTiles.Add(position);
        }
    }

    private void AttackTarget(Unit target)
    {
        ClearAllMarkers();

        Unit attacker = selectedUnit;
        AttackOutcome outcome = CombatResolver.Resolve(attacker, target);
        lastCombatMessage = CombatResolver.DescribeOutcome(outcome, attacker, target);

        if (outcome.TargetDied)
            enemyUnits.Remove(target);

        // 반격으로 아군이 쓰러질 수 있습니다.
        if (outcome.AttackerDied)
        {
            playerUnits.Remove(attacker);
            if (selectedUnit == attacker) selectedUnit = null;
        }

        FinishUnitAction();
    }

    private void ConfirmOrPreviewAttack(Unit target)
    {
        ShowAttackPreview(target);
    }

    private void ShowAttackPreview(Unit target)
    {
        if (target == null || selectedUnit == null) return;

        EnsureAttackPreviewUI();
        if (attackPreviewPanel == null || attackPreviewText == null) return;

        attackPreviewTarget = target;

        // 실제 판정과 완전히 동일한 계산입니다. 예측과 결과가 갈라질 수 없습니다.
        AttackForecast forecast = CombatResolver.Forecast(selectedUnit, target);

        string terrainNotice = forecast.CoverBlocks
            ? "\n엄폐물이 원거리 공격을 1회 차단합니다"
            : IsOnHighGround(selectedUnit)
                ? $"\n고지대: 사거리 +1, 명중 +{CombatResolver.HighGroundAccuracyBonus}%"
                : "";

        attackPreviewText.text =
            CombatResolver.DescribeForecast(forecast, target) + terrainNotice;

        attackPreviewPanel.gameObject.SetActive(true);
        UpdateAttackPreviewPosition();
    }

    private void EnsureAttackPreviewUI()
    {
        if (attackPreviewPanel != null) return;

        if (attackPreviewUIPrefab != null)
        {
            CreateAttackPreviewFromPrefab();
            return;
        }

        GameObject canvasObject = new GameObject(
            "AttackPreviewCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        attackPreviewCanvas = canvasObject.GetComponent<Canvas>();
        attackPreviewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        attackPreviewCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2220f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        GameObject panelObject = new GameObject(
            "AttackPreview",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup));
        panelObject.transform.SetParent(attackPreviewCanvas.transform, false);
        attackPreviewPanel = panelObject.GetComponent<RectTransform>();
        attackPreviewPanel.anchorMin = new Vector2(0.5f, 0.5f);
        attackPreviewPanel.anchorMax = new Vector2(0.5f, 0.5f);
        attackPreviewPanel.pivot = new Vector2(0.5f, 0f);
        attackPreviewPanel.sizeDelta = new Vector2(640f, 390f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.035f, 0.025f, 0.025f, 0.98f);
        background.raycastTarget = false;

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.12f, 1f);
        outline.effectDistance = new Vector2(7f, -7f);

        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        GameObject textObject = new GameObject(
            "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.43f);
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        attackPreviewText = textObject.GetComponent<TextMeshProUGUI>();
        attackPreviewText.alignment = TextAlignmentOptions.Center;
        attackPreviewText.fontSize = 36f;
        attackPreviewText.fontStyle = FontStyles.Bold;
        attackPreviewText.color = Color.white;
        attackPreviewText.raycastTarget = false;
        if (uiFont != null) attackPreviewText.font = uiFont;

        attackCancelButton = CreateAttackPreviewButton(
            panelObject.transform,
            "CancelButton",
            "취소",
            new Vector2(0.035f, 0.045f),
            new Vector2(0.485f, 0.39f),
            new Color(0.24f, 0.27f, 0.32f, 1f),
            attackCancelButtonSprite);
        attackCancelButton.onClick.AddListener(HideAttackPreview);

        attackConfirmButton = CreateAttackPreviewButton(
            panelObject.transform,
            "AttackButton",
            "공격",
            new Vector2(0.515f, 0.045f),
            new Vector2(0.965f, 0.39f),
            new Color(0.85f, 0.16f, 0.08f, 1f),
            attackConfirmButtonSprite);
        attackConfirmButton.onClick.AddListener(ConfirmPreviewedAttack);

        panelObject.SetActive(false);
    }

    private void CreateAttackPreviewFromPrefab()
    {
        AttackPreviewUIView view = Instantiate(attackPreviewUIPrefab);
        view.name = "AttackPreviewUI";
        attackPreviewPanel = view.Panel;
        attackPreviewText = view.PreviewText as TextMeshProUGUI;
        attackCancelButton = view.CancelButton;
        attackConfirmButton = view.ConfirmButton;
        attackPreviewCanvas = view.GetComponent<Canvas>();

        if (attackCancelButton != null)
        {
            ApplyButtonSprite(
                attackCancelButton.GetComponent<Image>(),
                attackCancelButtonSprite);
            attackCancelButton.onClick.AddListener(HideAttackPreview);
        }
        if (attackConfirmButton != null)
        {
            ApplyButtonSprite(
                attackConfirmButton.GetComponent<Image>(),
                attackConfirmButtonSprite);
            attackConfirmButton.onClick.AddListener(ConfirmPreviewedAttack);
        }
        if (attackPreviewPanel != null)
            attackPreviewPanel.gameObject.SetActive(false);
    }

    private Button CreateAttackPreviewButton(
        Transform parent,
        string objectName,
        string labelText,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        Sprite sprite)
    {
        GameObject buttonObject = new GameObject(
            objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        ApplyButtonSprite(image, sprite);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        button.colors = colors;

        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        if (uiFont != null) label.font = uiFont;

        return button;
    }

    private void ConfirmPreviewedAttack()
    {
        Unit target = attackPreviewTarget;
        if (target == null || target.IsDead || selectedUnit == null)
        {
            HideAttackPreview();
            return;
        }

        AttackTarget(target);
    }

    private void UpdateAttackPreviewPosition()
    {
        if (attackPreviewTarget == null ||
            attackPreviewPanel == null ||
            !attackPreviewPanel.gameObject.activeSelf)
            return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 worldPosition = attackPreviewTarget.transform.position + Vector3.up * 1.1f;
        Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.y < 0f) return;

        RectTransform canvasRect = attackPreviewCanvas.transform as RectTransform;

        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, null, out Vector2 localPoint))
        {
            localPoint.y += 55f;

            Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
            float halfPanelWidth = attackPreviewPanel.rect.width * 0.5f;
            float panelHeight = attackPreviewPanel.rect.height;
            const float margin = 24f;

            localPoint.x = Mathf.Clamp(
                localPoint.x,
                -halfCanvas.x + halfPanelWidth + margin,
                halfCanvas.x - halfPanelWidth - margin);
            localPoint.y = Mathf.Clamp(
                localPoint.y,
                -halfCanvas.y + margin,
                halfCanvas.y - panelHeight - margin);

            attackPreviewPanel.anchoredPosition = localPoint;
            attackPreviewPanel.SetAsLastSibling();
        }
    }

    private void HideAttackPreview()
    {
        attackPreviewTarget = null;
        if (attackPreviewPanel != null)
            attackPreviewPanel.gameObject.SetActive(false);
    }

    private void SkipAttack()
    {
        FinishUnitAction();
    }

    private void FinishUnitAction()
    {
        ClearAllMarkers();

        if (selectedUnit != null)
        {
            selectedUnit.MarkActed();
            selectedUnit = null;
        }

        battleState = BattleState.Idle;

        if (CheckBattleEnd()) return;

        if (AllPlayersDone())
            StartCoroutine(ProcessEnemyTurn());
    }

    private void EndPlayerTurn()
    {
        if (selectedUnit != null)
        {
            selectedUnit.MarkActed();
            selectedUnit.SetSelected(false);
            selectedUnit = null;
        }
        ClearAllMarkers();
        battleState = BattleState.Idle;

        foreach (var unit in playerUnits)
        {
            if (unit != null && !unit.IsDead && !unit.HasActed)
                unit.MarkActed();
        }

        if (!CheckBattleEnd())
            StartCoroutine(ProcessEnemyTurn());
    }

    private bool AllPlayersDone()
    {
        foreach (var unit in playerUnits)
        {
            if (unit != null && !unit.IsDead && !unit.HasActed)
                return false;
        }
        return true;
    }

    private Unit GetUnitAt(Vector2Int pos, Team team)
    {
        Tile tile = GridManager.Instance.GetTile(pos);
        if (tile == null || tile.OccupyingUnit == null) return null;
        Unit u = tile.OccupyingUnit.GetComponent<Unit>();
        if (u != null && u.UnitTeam == team && !u.IsDead) return u;
        return null;
    }

    // 전투 규칙은 CombatResolver 한 곳에만 둡니다. 여기서는 위임만 합니다.

    private int GetEffectiveAttackRange(Unit unit)
        => CombatResolver.GetEffectiveRange(unit);

    private bool IsOnHighGround(Unit unit)
    {
        if (unit == null || GridManager.Instance == null) return false;
        Tile tile = GridManager.Instance.GetTile(unit.GridPosition);
        return tile != null && tile.Terrain == TileTerrain.HighGround;
    }

    // ─────────────────── Enemy AI ───────────────────

    private IEnumerator ProcessEnemyTurn()
    {
        currentPhase = GamePhase.EnemyTurn;
        if (TurnBannerUI.Instance != null)
            yield return TurnBannerUI.Instance.ShowEnemyTurnAndWait();
        else
            yield return new WaitForSeconds(0.5f);

        lastCombatMessage = "";

        // 이번 턴에 이미 처치가 확정된 표적입니다. 어려움 난이도에서 표적 분산에 씁니다.
        var doomed = new HashSet<Unit>();

        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            Unit enemy = enemyUnits[i];
            if (enemy == null || enemy.IsDead) continue;
            if (playerUnits.Count == 0) break;

            var topCandidates = new List<AICandidate>();
            AICandidate decision = EnemyAI.Decide(
                enemy, playerUnits, enemyUnits, enemySquad, doomed, topCandidates);

            EnemyAILog.Record(turnCount, enemy, enemySquad, topCandidates);

            yield return ExecuteEnemyAction(enemy, decision, doomed);

            if (CheckBattleEnd()) yield break;
            yield return new WaitForSeconds(0.4f);
        }

        if (!CheckBattleEnd())
        {
            turnCount++;
            currentPhase = GamePhase.PlayerTurn;
            battleState = BattleState.Idle;
            lastCombatMessage = "";
            ResetPlayerActions();
            if (TurnBannerUI.Instance != null)
                yield return TurnBannerUI.Instance.ShowPlayerTurnAndWait();
        }
    }

    /// <summary>AI가 고른 행동을 실제로 수행합니다.</summary>
    private IEnumerator ExecuteEnemyAction(
        Unit enemy, AICandidate decision, HashSet<Unit> doomed)
    {
        // ── 이동 ────────────────────────────────────
        if (decision.Destination != enemy.GridPosition)
        {
            Tile destinationTile = GridManager.Instance.GetTile(decision.Destination);
            if (destinationTile != null && destinationTile.IsWalkable())
            {
                enemy.MoveTo(decision.Destination);
                yield return new WaitForSeconds(0.2f);
            }
        }

        // ── 공격 ────────────────────────────────────
        if (decision.Kind != AIActionKind.Attack) yield break;

        Unit target = decision.Target;
        if (target == null || target.IsDead) yield break;

        int distance = Mathf.Abs(enemy.GridPosition.x - target.GridPosition.x) +
                       Mathf.Abs(enemy.GridPosition.y - target.GridPosition.y);
        if (distance > GetEffectiveAttackRange(enemy)) yield break;

        // 플레이어 공격과 완전히 같은 규칙을 씁니다.
        AttackOutcome outcome = CombatResolver.Resolve(enemy, target);
        lastCombatMessage = CombatResolver.DescribeOutcome(outcome, enemy, target);

        if (outcome.TargetDied)
        {
            playerUnits.Remove(target);
            doomed.Remove(target);
        }
        else if (outcome.Hit && outcome.Damage >= target.HP)
        {
            // 다음 아군이 같은 표적에 낭비하지 않도록 표시합니다.
            doomed.Add(target);
        }

        // 플레이어의 반격으로 적이 쓰러질 수 있습니다.
        if (outcome.AttackerDied)
            enemyUnits.Remove(enemy);
    }

    private Unit FindAdjacentUnit(Vector2Int pos, Team team)
    {
        Tile[] neighbors = GridManager.Instance.GetNeighbors(pos);
        foreach (var t in neighbors)
        {
            if (t.State != TileState.Occupied || t.OccupyingUnit == null) continue;
            Unit u = t.OccupyingUnit.GetComponent<Unit>();
            if (u != null && u.UnitTeam == team && !u.IsDead) return u;
        }
        return null;
    }

    // ─────────────────── Result ───────────────────

    private bool CheckBattleEnd()
    {
        playerUnits.RemoveAll(u => u == null || u.IsDead);
        enemyUnits.RemoveAll(u => u == null || u.IsDead);

        if (enemyUnits.Count == 0)
        {
            currentPhase = GamePhase.BattleResult;
            resultMessage = gameTextData != null ? gameTextData.victory : "VICTORY!";
            StageProgressManager.UnlockNextStage();
            return true;
        }
        if (playerUnits.Count == 0)
        {
            currentPhase = GamePhase.BattleResult;
            resultMessage = gameTextData != null ? gameTextData.defeat : "DEFEAT...";
            return true;
        }
        return false;
    }

    // ─────────────────── Helpers ───────────────────

    private void ClearAllMarkers()
    {
        HideAttackPreview();
        GridManager grid = GridManager.Instance;
        foreach (var pos in moveTiles)
        { Tile t = grid.GetTile(pos); if (t != null) t.ClearHighlight(); }
        foreach (var pos in attackTiles)
        { Tile t = grid.GetTile(pos); if (t != null) t.ClearHighlight(); }
        moveTiles.Clear();
        attackTiles.Clear();
    }

    // ─────────────────── UI ───────────────────

    private void SetupUI()
    {
        if (undoButton != null)
            ApplyButtonSprite(undoButton.GetComponent<Image>(), undoButtonSprite);
        if (playButton != null)
            ApplyButtonSprite(playButton.GetComponent<Image>(), confirmButtonSprite);

        if (gameStartButton != null)
            gameStartButton.onClick.AddListener(OnGameStartClicked);
        if (undoButton != null)
            undoButton.onClick.AddListener(UndoMove);
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        RefreshUI();
    }

    private void LateUpdate()
    {
        UpdateAttackPreviewPosition();
        RefreshUI();
    }

    private static void ApplyButtonSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
    }

    private void RefreshUI()
    {
        bool showStart = currentPhase == GamePhase.ReadyToStart ||
                         currentPhase == GamePhase.BattleResult;
        bool showPlay = currentPhase == GamePhase.PlayerTurn;

        if (gameStartUI != null)
            gameStartUI.SetActive(showStart);

        if (gamePlayUI != null)
            gamePlayUI.SetActive(showPlay);

        bool moved = showPlay && battleState == BattleState.UnitMoved;
        bool idle = showPlay && battleState == BattleState.Idle;

        if (undoButton != null)
            undoButton.gameObject.SetActive(moved);

        if (playButton != null)
            playButton.gameObject.SetActive(moved || idle);

        if (gameInfoText != null)
            gameInfoText.text = GetInfoText();
    }

    private string GetInfoText()
    {
        if (gameTextData == null) return "";

        switch (currentPhase)
        {
            case GamePhase.Deployment:
                return string.Format(gameTextData.deployFormat, deployedCount, maxPlayerUnits);
            case GamePhase.ReadyToStart:
                return gameTextData.readyToStart;
            case GamePhase.PlayerTurn:
                string header = string.Format(gameTextData.playerTurnHeader, turnCount) + "\n";
                if (battleState == BattleState.Idle)
                    header += gameTextData.idle;
                else if (battleState == BattleState.UnitSelected)
                    header += gameTextData.unitSelected;
                else if (battleState == BattleState.UnitMoved)
                    header += gameTextData.unitMoved;
                if (selectedUnit != null)
                    header += string.Format(gameTextData.unitStatsFormat,
                        selectedUnit.HP, selectedUnit.AttackPower, selectedUnit.MoveRange);
                if (!string.IsNullOrEmpty(lastCombatMessage))
                    header += "\n" + lastCombatMessage;
                return header;
            case GamePhase.EnemyTurn:
                return string.IsNullOrEmpty(lastCombatMessage)
                    ? gameTextData.enemyTurn
                    : gameTextData.enemyTurn + "\n" + lastCombatMessage;
            case GamePhase.BattleResult:
                return resultMessage;
            default:
                return "";
        }
    }

    private void OnGameStartClicked()
    {
        if (currentPhase == GamePhase.Deployment && deployedCount == maxPlayerUnits)
        {
            GridManager.Instance.ClearAllHighlights();
            if (deploymentPanel != null) deploymentPanel.SetActive(false);
            if (deploymentStartButton != null)
                deploymentStartButton.gameObject.SetActive(false);
            StartBattle();
        }
        else if (currentPhase == GamePhase.ReadyToStart)
            StartBattle();
        else if (currentPhase == GamePhase.BattleResult)
            SceneManager.LoadScene("3.Stage List");
    }

    private void OnPlayClicked()
    {
        if (currentPhase != GamePhase.PlayerTurn) return;

        if (battleState == BattleState.UnitMoved)
            SkipAttack();
        else if (battleState == BattleState.Idle)
            EndPlayerTurn();
    }
}
