using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Camera")]
    [SerializeField] private float cameraZoom = 0.7f;
    [SerializeField] private float cameraAngle = 60f;
    [SerializeField] private float cameraFOV = 50f;

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

    private Texture2D bgTex;

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

        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
        bgTex.Apply();
    }

    private void Start()
    {
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
        if (deployedCount >= maxPlayerUnits) return;
        if (tile.Zone != TileZone.PlayerDeploy) return;
        if (tile.State != TileState.Empty) return;

        Unit unit = Unit.Create(Team.Player, tile.GridPosition, playerPrefab);
        playerUnits.Add(unit);
        deployedCount++;
        tile.ClearHighlight();

        if (deployedCount >= maxPlayerUnits)
        {
            GridManager.Instance.ClearAllHighlights();
            currentPhase = GamePhase.ReadyToStart;
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
                    AttackTarget(target);
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
                    AttackTarget(target);
                    return;
                }
            }
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

        attackTiles.Clear();
        Tile[] neighbors = grid.GetNeighbors(unit.GridPosition);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.State == TileState.Occupied && neighbor.OccupyingUnit != null)
            {
                Unit target = neighbor.OccupyingUnit.GetComponent<Unit>();
                if (target != null && target.UnitTeam == Team.Enemy && !target.IsDead)
                {
                    neighbor.SetHighlight(AttackHighlight);
                    attackTiles.Add(neighbor.GridPosition);
                }
            }
        }
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
        ClearAllMarkers();
        selectedUnit.MoveTo(undoPosition);
        SelectUnit(selectedUnit);
    }

    private void ShowAttackRangeAfterMove()
    {
        attackTiles.Clear();
        Tile[] neighbors = GridManager.Instance.GetNeighbors(selectedUnit.GridPosition);

        foreach (var neighbor in neighbors)
        {
            if (neighbor.State == TileState.Occupied && neighbor.OccupyingUnit != null)
            {
                Unit target = neighbor.OccupyingUnit.GetComponent<Unit>();
                if (target != null && target.UnitTeam == Team.Enemy && !target.IsDead)
                {
                    neighbor.SetHighlight(AttackHighlight);
                    attackTiles.Add(neighbor.GridPosition);
                }
            }
        }
    }

    private void AttackTarget(Unit target)
    {
        ClearAllMarkers();
        target.TakeDamage(selectedUnit.AttackPower);

        if (target.IsDead)
            enemyUnits.Remove(target);

        FinishUnitAction();
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

            Unit adj = FindAdjacentUnit(enemy.GridPosition, Team.Player);
            if (adj != null)
            {
                adj.TakeDamage(enemy.AttackPower);
                if (adj.IsDead)
                    playerUnits.Remove(adj);
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

    // ─────────────────── Result ───────────────────

    private bool CheckBattleEnd()
    {
        playerUnits.RemoveAll(u => u == null || u.IsDead);
        enemyUnits.RemoveAll(u => u == null || u.IsDead);

        if (enemyUnits.Count == 0)
        { currentPhase = GamePhase.BattleResult; resultMessage = "VICTORY!"; return true; }
        if (playerUnits.Count == 0)
        { currentPhase = GamePhase.BattleResult; resultMessage = "DEFEAT..."; return true; }
        return false;
    }

    // ─────────────────── Helpers ───────────────────

    private void ClearAllMarkers()
    {
        GridManager grid = GridManager.Instance;
        foreach (var pos in moveTiles)
        { Tile t = grid.GetTile(pos); if (t != null) t.ClearHighlight(); }
        foreach (var pos in attackTiles)
        { Tile t = grid.GetTile(pos); if (t != null) t.ClearHighlight(); }
        moveTiles.Clear();
        attackTiles.Clear();
    }

    // ─────────────────── UI ───────────────────

    private void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        int fontSize = Mathf.Max(20, Mathf.RoundToInt(sh * 0.03f));
        int btnFontSize = Mathf.Max(24, Mathf.RoundToInt(sh * 0.035f));
        float btnW = sw * 0.6f;
        float btnH = Mathf.Max(50, sh * 0.08f);
        float btnX = (sw - btnW) * 0.5f;
        float pad = 12f;

        GUIStyle bg = new GUIStyle();
        bg.normal.background = bgTex;

        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.normal.textColor = Color.white;
        label.padding = new RectOffset(8, 8, 4, 4);

        GUIStyle bigLabel = new GUIStyle(label);
        bigLabel.fontSize = Mathf.Max(36, Mathf.RoundToInt(sh * 0.06f));
        bigLabel.alignment = TextAnchor.MiddleCenter;

        GUIStyle btn = new GUIStyle(GUI.skin.button);
        btn.fontSize = btnFontSize;
        btn.fontStyle = FontStyle.Bold;

        // ── Top Panel ──
        float topH = fontSize * 3 + pad * 2;
        GUI.Box(new Rect(0, 0, sw, topH), "", bg);

        float ty = pad;

        switch (currentPhase)
        {
            case GamePhase.Deployment:
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    $"DEPLOY: {deployedCount} / {maxPlayerUnits}", label);
                ty += fontSize + 4;
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    "Tap green tiles to place units", label);
                break;

            case GamePhase.ReadyToStart:
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    "All units ready!", label);
                ty += fontSize + 4;
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    "Press GAME START below", label);
                break;

            case GamePhase.PlayerTurn:
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    $"YOUR TURN  -  Turn {turnCount}", label);
                ty += fontSize + 4;

                string info = "";
                if (battleState == BattleState.Idle)
                    info = "Tap your unit to select";
                else if (battleState == BattleState.UnitSelected)
                    info = "Blue=Move  Red=Attack  Other=Cancel";
                else if (battleState == BattleState.UnitMoved)
                    info = "Red=Attack  UNDO/SKIP below";
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8), info, label);

                if (selectedUnit != null)
                {
                    ty += fontSize + 4;
                    GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                        $"HP:{selectedUnit.HP}  ATK:{selectedUnit.AttackPower}  MOV:{selectedUnit.MoveRange}",
                        label);
                }
                break;

            case GamePhase.EnemyTurn:
                GUI.Label(new Rect(pad, ty, sw, fontSize + 8),
                    "ENEMY TURN...", label);
                break;
        }

        // ── Bottom Buttons ──
        float bottomY = sh - btnH - pad * 2;

        if (currentPhase == GamePhase.ReadyToStart)
        {
            if (GUI.Button(new Rect(btnX, bottomY, btnW, btnH), "GAME START", btn))
                StartBattle();
        }

        if (currentPhase == GamePhase.PlayerTurn)
        {
            if (battleState == BattleState.UnitMoved)
            {
                float halfW = btnW * 0.48f;
                float gap = btnW * 0.04f;

                if (GUI.Button(new Rect(btnX, bottomY, halfW, btnH), "UNDO", btn))
                    UndoMove();
                if (GUI.Button(new Rect(btnX + halfW + gap, bottomY, halfW, btnH), "SKIP", btn))
                    SkipAttack();
            }
            else if (battleState == BattleState.Idle)
            {
                if (GUI.Button(new Rect(btnX, bottomY, btnW, btnH), "END TURN", btn))
                    EndPlayerTurn();
            }
        }

        if (currentPhase == GamePhase.BattleResult)
        {
            GUI.Box(new Rect(0, sh * 0.3f, sw, sh * 0.15f), "", bg);
            GUI.Label(new Rect(0, sh * 0.32f, sw, sh * 0.1f), resultMessage, bigLabel);

            if (GUI.Button(new Rect(btnX, bottomY, btnW, btnH), "RESTART", btn))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
