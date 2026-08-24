using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class Unit : MonoBehaviour
{
    public Team UnitTeam { get; private set; }
    public int HP { get; private set; }
    public int AttackPower { get; private set; }
    public int MoveRange { get; private set; }
    public int AttackRange { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public bool HasActed { get; set; }
    public CharacterData CharacterData { get; private set; }

    private Renderer unitRenderer;
    private Material unitMaterial;

    private static readonly Color PlayerColor = new Color(0.15f, 0.4f, 1f, 1f);
    private static readonly Color EnemyColor = new Color(1f, 0.2f, 0.15f, 1f);
    private static readonly Color SelectedColor = new Color(1f, 1f, 0.3f, 1f);
    private static readonly Color ActedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public bool IsDead => HP <= 0;

    public static Unit Create(Team team, Vector2Int gridPos, GameObject prefab)
    {
        return Create(team, gridPos, prefab, null);
    }

    public static Unit Create(
        Team team,
        Vector2Int gridPos,
        GameObject fallbackPrefab,
        CharacterData characterData)
    {
        GridManager grid = GridManager.Instance;
        GameObject prefab = characterData != null && characterData.BattlePrefab != null
            ? characterData.BattlePrefab
            : fallbackPrefab;

        GameObject obj;
        if (prefab != null)
        {
            obj = Object.Instantiate(prefab);
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        obj.name = $"{team}_{gridPos.x}_{gridPos.y}";

        float s = grid.CellSize * 0.45f;
        obj.transform.localScale = new Vector3(s, s, s);

        Vector3 worldPos = grid.GridToWorldPosition(gridPos.x, gridPos.y);
        obj.transform.position = worldPos + Vector3.up * 0.5f;

        Tile tile = grid.GetTile(gridPos);
        obj.transform.rotation = tile.transform.rotation;

        Unit unit = obj.GetComponent<Unit>();
        if (unit == null)
            unit = obj.AddComponent<Unit>();

        unit.UnitTeam = team;
        unit.CharacterData = characterData;
        unit.HP = characterData != null ? characterData.MaxHP : 3;
        unit.AttackPower = characterData != null ? characterData.AttackPower : 1;
        unit.MoveRange = characterData != null ? characterData.MoveRange : 3;
        unit.AttackRange = characterData != null ? characterData.AttackRange : 1;
        unit.GridPosition = gridPos;
        unit.HasActed = false;

        unit.unitRenderer = obj.GetComponent<Renderer>();
        if (unit.unitRenderer != null)
        {
            unit.unitMaterial = new Material(unit.unitRenderer.material);
            unit.unitRenderer.material = unit.unitMaterial;
            unit.SetTeamColor();
        }

        if (obj.GetComponent<Collider>() == null)
            obj.AddComponent<BoxCollider>();

        tile.PlaceUnit(obj);
        return unit;
    }

    public void MoveTo(Vector2Int newPos)
    {
        Tile oldTile = GridManager.Instance.GetTile(GridPosition);
        if (oldTile != null) oldTile.RemoveUnit();

        GridPosition = newPos;

        Tile newTile = GridManager.Instance.GetTile(newPos);
        if (newTile != null) newTile.PlaceUnit(gameObject);

        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(newPos.x, newPos.y);
        transform.position = worldPos + Vector3.up * 0.5f;
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0)
        {
            Tile tile = GridManager.Instance.GetTile(GridPosition);
            if (tile != null) tile.RemoveUnit();
            Destroy(gameObject);
        }
    }

    public void SetSelected(bool selected)
    {
        if (unitMaterial == null) return;
        Color color;
        if (selected)
            color = SelectedColor;
        else if (HasActed)
            color = ActedColor;
        else
            color = (UnitTeam == Team.Player) ? PlayerColor : EnemyColor;

        ApplyColor(color);
    }

    public void MarkActed()
    {
        HasActed = true;
        ApplyColor(ActedColor);
    }

    public void ResetTurn()
    {
        HasActed = false;
        SetTeamColor();
    }

    private void SetTeamColor()
    {
        Color color = UnitTeam == Team.Player && CharacterData != null
            ? CharacterData.TeamColor
            : (UnitTeam == Team.Player ? PlayerColor : EnemyColor);
        ApplyColor(color);
    }

    public void RemoveFromBoard()
    {
        Tile tile = GridManager.Instance != null
            ? GridManager.Instance.GetTile(GridPosition)
            : null;
        if (tile != null && tile.OccupyingUnit == gameObject)
            tile.RemoveUnit();

        Destroy(gameObject);
    }

    private void ApplyColor(Color color)
    {
        if (unitMaterial == null) return;
        unitMaterial.color = color;
        if (unitMaterial.HasProperty("_BaseColor"))
            unitMaterial.SetColor("_BaseColor", color);
    }
}
