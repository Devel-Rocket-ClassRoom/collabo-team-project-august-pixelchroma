using UnityEngine;

public enum TileState
{
    Empty,
    Occupied,
    Blocked
}

public enum TileZone
{
    PlayerDeploy,
    Neutral,
    EnemyDeploy
}

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public TileState State { get; set; }
    public TileZone Zone { get; private set; }
    public GameObject OccupyingUnit { get; set; }

    private Renderer tileRenderer;
    private Material tileMaterial;
    private Color zoneColor;
    private bool isHighlighted;
    private Color currentHighlightColor;

    private static readonly Color PlayerDeployColor = new Color(0.25f, 0.45f, 0.85f, 1f);
    private static readonly Color EnemyDeployColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    private static readonly Color NeutralColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    public void Init(int x, int y)
    {
        GridPosition = new Vector2Int(x, y);
        State = TileState.Empty;
        Zone = DetermineZone(y);

        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            tileMaterial = new Material(tileRenderer.material);
            tileRenderer.material = tileMaterial;
        }

        if (Zone == TileZone.PlayerDeploy)
            zoneColor = PlayerDeployColor;
        else if (Zone == TileZone.EnemyDeploy)
            zoneColor = EnemyDeployColor;
        else
            zoneColor = NeutralColor;

        ApplyColor();
    }

    private TileZone DetermineZone(int y)
    {
        if (y <= 1) return TileZone.PlayerDeploy;
        if (y >= 4) return TileZone.EnemyDeploy;
        return TileZone.Neutral;
    }

    public bool IsWalkable()
    {
        return State != TileState.Blocked && State != TileState.Occupied;
    }

    public void SetHighlight(Color color)
    {
        isHighlighted = true;
        currentHighlightColor = color;
        ApplyColor();
    }

    public void ClearHighlight()
    {
        isHighlighted = false;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (tileMaterial == null) return;
        tileMaterial.color = isHighlighted ? currentHighlightColor : zoneColor;
        if (tileMaterial.HasProperty("_BaseColor"))
            tileMaterial.SetColor("_BaseColor", tileMaterial.color);
    }

    public void PlaceUnit(GameObject unit)
    {
        OccupyingUnit = unit;
        State = TileState.Occupied;
    }

    public void RemoveUnit()
    {
        OccupyingUnit = null;
        State = TileState.Empty;
    }
}
