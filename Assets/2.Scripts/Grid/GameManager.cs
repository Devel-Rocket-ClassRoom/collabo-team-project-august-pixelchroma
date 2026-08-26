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

    [Header("Settings")]
    [SerializeField] private int maxPlayerUnits = 4;
    [SerializeField] private int maxEnemyUnits = 4;

    [Header("Deployment Roster")]
    [Tooltip("Characters that can be selected before battle. Empty uses a prototype roster.")]
    [SerializeField] private List<CharacterData> availableCharacters = new List<CharacterData>();

    [Header("Camera")]
    [SerializeField] private float cameraZoom = 0.7f;
    [SerializeField] private float cameraAngle = 60f;
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

    private static readonly Color DeployHighlight = new Color(0.2f, 0.85f, 0.3f, 1f);
    private static readonly Color MoveHighlight = new Color(0.3f, 0.75f, 1f, 1f);
    private static readonly Color AttackHighlight = new Color(1f, 0.25f, 0.25f, 1f);

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
        SetupCamera();
        SpawnEnemies();
        HideOriginalPrefabs();

        currentPhase = GamePhase.Deployment;
        deployedCount = 0;
        turnCount = 0;
        ShowDeployZone();
        SetupUI();
        SetupDeploymentRoster();
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
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Tile tile = hit.collider.GetComponent<Tile>();
        Unit clickedUnit = hit.collider.GetComponent<Unit>();

        if (clickedUnit != null)
            tile = GridManager.Instance.GetTile(clickedUnit.GridPosition);

        if (tile != null && clickedUnit == null && tile.OccupyingUnit != null)
            clickedUnit = tile.OccupyingUnit.GetComponent<Unit>();

        if (tile == null) return;

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
            selectedDeployCharacter);
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
        data.ConfigureRuntime(id, name, playerPrefab, hp, attack, movement, range, color);
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
            deploymentInfoText.color = selectedDeployCharacter == null
                ? Color.white
                : selectedDeployCharacter.TeamColor;
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
        var available = new List<Vector2Int>();

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 4; y <= 5; y++)
            {
                Tile tile = grid.GetTile(x, y);
                if (tile != null && tile.State == TileState.Empty)
                    available.Add(new Vector2Int(x, y));
            }
        }

        int count = Mathf.Min(maxEnemyUnits, available.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, available.Count);
            Vector2Int pos = available[idx];
            available.RemoveAt(idx);

            Unit enemy = Unit.Create(Team.Enemy, pos, enemyPrefab);
            enemyUnits.Add(enemy);
        }
    }

    // ─────────────────── Battle ───────────────────

    private void StartBattle()
    {
        currentPhase = GamePhase.PlayerTurn;
        battleState = BattleState.Idle;
        turnCount = 1;
        ResetPlayerActions();
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
        Tile blockingCover = FindBlockingCover(selectedUnit, target);
        ClearAllMarkers();

        if (blockingCover != null && blockingCover.AbsorbRangedAttack())
        {
            FinishUnitAction();
            return;
        }

        target.TakeDamage(selectedUnit.AttackPower);

        if (target.IsDead)
            enemyUnits.Remove(target);

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
        Tile blockingCover = FindBlockingCover(selectedUnit, target);
        int predictedDamage = blockingCover == null
            ? Mathf.Min(selectedUnit.AttackPower, target.HP)
            : 0;
        int remainingHP = Mathf.Max(0, target.HP - selectedUnit.AttackPower);
        if (blockingCover != null) remainingHP = target.HP;

        string terrainNotice = blockingCover != null
            ? "\n엄폐물이 원거리 공격을 1회 차단합니다"
            : IsOnHighGround(selectedUnit)
                ? "\n고지대 효과: 공격 사거리 +1"
                : "";
        attackPreviewText.text =
            $"예상 피해 {predictedDamage}    체력 {target.HP} > {remainingHP}\n" +
            $"이동 {selectedUnit.MoveRange}    사거리 {GetEffectiveAttackRange(selectedUnit)}" +
            terrainNotice;
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

    private int GetEffectiveAttackRange(Unit unit)
    {
        return unit.AttackRange + (IsOnHighGround(unit) ? 1 : 0);
    }

    private bool IsOnHighGround(Unit unit)
    {
        if (unit == null || GridManager.Instance == null) return false;
        Tile tile = GridManager.Instance.GetTile(unit.GridPosition);
        return tile != null && tile.Terrain == TileTerrain.HighGround;
    }

    private Tile FindBlockingCover(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return null;

        Vector2Int from = attacker.GridPosition;
        Vector2Int to = target.GridPosition;
        int distance = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        if (distance <= 1) return null;

        Vector2Int step;
        if (from.x == to.x)
            step = new Vector2Int(0, to.y > from.y ? 1 : -1);
        else if (from.y == to.y)
            step = new Vector2Int(to.x > from.x ? 1 : -1, 0);
        else
            return null;

        Vector2Int position = from + step;
        while (position != to)
        {
            Tile tile = GridManager.Instance.GetTile(position);
            if (tile != null && tile.Terrain == TileTerrain.Cover && tile.CoverDurability > 0)
                return tile;
            position += step;
        }

        return null;
    }

    // ─────────────────── Enemy AI ───────────────────

    private IEnumerator ProcessEnemyTurn()
    {
        currentPhase = GamePhase.EnemyTurn;
        yield return new WaitForSeconds(0.5f);

        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            Unit enemy = enemyUnits[i];
            if (enemy == null || enemy.IsDead) continue;

            Unit nearest = FindNearestAliveUnit(enemy.GridPosition, Team.Player);
            if (nearest == null) continue;

            List<Vector2Int> path = Pathfinding.FindPath(
                enemy.GridPosition, nearest.GridPosition);

            if (path != null && path.Count > 1)
            {
                int steps = Mathf.Min(path.Count - 1, enemy.MoveRange);
                for (int s = steps - 1; s >= 0; s--)
                {
                    Tile t = GridManager.Instance.GetTile(path[s]);
                    if (t != null && t.IsWalkable())
                    {
                        enemy.MoveTo(path[s]);
                        break;
                    }
                }
            }

            Unit attackTarget = FindUnitWithinRange(
                enemy.GridPosition,
                Team.Player,
                GetEffectiveAttackRange(enemy));
            if (attackTarget != null)
            {
                Tile blockingCover = FindBlockingCover(enemy, attackTarget);
                if (blockingCover != null)
                    blockingCover.AbsorbRangedAttack();
                else
                    attackTarget.TakeDamage(enemy.AttackPower);

                if (attackTarget.IsDead)
                    playerUnits.Remove(attackTarget);
            }

            if (CheckBattleEnd()) yield break;
            yield return new WaitForSeconds(0.4f);
        }

        if (!CheckBattleEnd())
        {
            turnCount++;
            currentPhase = GamePhase.PlayerTurn;
            battleState = BattleState.Idle;
            ResetPlayerActions();
        }
    }

    private Unit FindNearestAliveUnit(Vector2Int from, Team team)
    {
        List<Unit> targets = (team == Team.Player) ? playerUnits : enemyUnits;
        Unit nearest = null;
        int minDist = int.MaxValue;
        foreach (var u in targets)
        {
            if (u == null || u.IsDead) continue;
            int d = Mathf.Abs(from.x - u.GridPosition.x) + Mathf.Abs(from.y - u.GridPosition.y);
            if (d < minDist) { minDist = d; nearest = u; }
        }
        return nearest;
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

    private Unit FindUnitWithinRange(Vector2Int from, Team team, int range)
    {
        List<Unit> targets = team == Team.Player ? playerUnits : enemyUnits;
        Unit nearest = null;
        int nearestDistance = int.MaxValue;

        foreach (Unit unit in targets)
        {
            if (unit == null || unit.IsDead) continue;
            int distance = Mathf.Abs(from.x - unit.GridPosition.x) +
                           Mathf.Abs(from.y - unit.GridPosition.y);
            if (distance <= range && distance < nearestDistance)
            {
                nearest = unit;
                nearestDistance = distance;
            }
        }

        return nearest;
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
                return header;
            case GamePhase.EnemyTurn:
                return gameTextData.enemyTurn;
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
