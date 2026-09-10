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

public enum TileTerrain
{
    Normal,
    HighGround,
    Cover
}

public enum TileVisualType
{
    Default,
    Wireframe,
    Hologram
}

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public TileState State { get; set; }
    public TileZone Zone { get; private set; }
    public TileTerrain Terrain { get; private set; }
    public int CoverDurability { get; private set; }
    public GameObject OccupyingUnit { get; set; }

    private Renderer tileRenderer;
    private Material tileMaterial;
    private Color zoneColor;
    private Color highGroundColor = new Color(1f, 0.78f, 0.05f, 1f);
    private bool isHighlighted;
    private Color currentHighlightColor;
    private GameObject terrainVisual;
    private Renderer[] terrainRenderers;
    private Color[] terrainOriginalColors;

    private TileVisualType visualType = TileVisualType.Default;
    private Color baseBorderColor;
    private Color baseFillColor;
    private Color baseRimColor;

    private static readonly Color PlayerDeployColor = new Color(0.25f, 0.45f, 0.85f, 1f);
    private static readonly Color EnemyDeployColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    private static readonly Color NeutralColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    public void Init(int x, int y)
    {
        GridPosition = new Vector2Int(x, y);
        State = TileState.Empty;
        Zone = DetermineZone(y);
        Terrain = TileTerrain.Normal;

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

    public void SetVisualType(TileVisualType type, Material mat, Color primary, Color secondary)
    {
        visualType = type;
        tileMaterial = new Material(mat);
        tileRenderer.material = tileMaterial;

        switch (type)
        {
            case TileVisualType.Wireframe:
                baseBorderColor = primary;
                baseFillColor = secondary;
                break;
            case TileVisualType.Hologram:
                baseRimColor = primary;
                break;
        }

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
        return Terrain != TileTerrain.Cover &&
               State != TileState.Blocked &&
               State != TileState.Occupied;
    }

    public float HeightOffset { get; private set; }

    public void SetTerrain(TileTerrain terrain)
    {
        SetTerrain(terrain, 0.5f, new Color(1f, 0.78f, 0.05f, 1f), null);
    }

    public void SetTerrain(TileTerrain terrain, float height, Color color, Material overrideMaterial)
    {
        Terrain = terrain;
        CoverDurability = terrain == TileTerrain.Cover ? 1 : 0;
        State = terrain == TileTerrain.Cover ? TileState.Blocked : TileState.Empty;

        HeightOffset = terrain == TileTerrain.HighGround ? height : 0f;
        Vector3 pos = transform.localPosition;
        pos.y = HeightOffset;
        transform.localPosition = pos;

        if (terrain == TileTerrain.HighGround)
        {
            highGroundColor = color;
            if (overrideMaterial != null && tileRenderer != null)
            {
                tileMaterial = new Material(overrideMaterial);
                tileRenderer.material = tileMaterial;
                visualType = TileVisualType.Default;
            }
        }

        ApplyColor();
    }

    public void SetTerrainWithPrefab(TileTerrain terrain, float height, GameObject prefab)
    {
        Terrain = terrain;
        CoverDurability = terrain == TileTerrain.Cover ? 1 : 0;
        State = terrain == TileTerrain.Cover ? TileState.Blocked : TileState.Empty;
        HeightOffset = terrain == TileTerrain.HighGround ? height : 0f;

        Vector3 pos = transform.localPosition;
        pos.y = HeightOffset;
        transform.localPosition = pos;

        if (tileRenderer != null)
            tileRenderer.enabled = false;

        if (prefab != null)
        {
            terrainVisual = Instantiate(prefab, transform);
            terrainVisual.name = "TerrainVisual";
            terrainVisual.transform.localPosition = Vector3.zero;
            terrainVisual.transform.localRotation = Quaternion.identity;
            terrainVisual.SetActive(true);

            Vector3 ps = transform.localScale;
            terrainVisual.transform.localScale = new Vector3(
                1f / Mathf.Max(0.001f, ps.x),
                1f / Mathf.Max(0.001f, ps.y),
                1f / Mathf.Max(0.001f, ps.z));

            foreach (SpriteRenderer sprite in terrainVisual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sprite.GetComponent<SpriteBillboard>() == null)
                    sprite.gameObject.AddComponent<SpriteBillboard>();
            }

            terrainRenderers = terrainVisual.GetComponentsInChildren<Renderer>();
            terrainOriginalColors = new Color[terrainRenderers.Length];
            for (int i = 0; i < terrainRenderers.Length; i++)
            {
                terrainRenderers[i].material = new Material(terrainRenderers[i].material);
                terrainOriginalColors[i] = terrainRenderers[i].material.color;
            }
        }
    }

    public bool AbsorbRangedAttack()
    {
        if (Terrain != TileTerrain.Cover || CoverDurability <= 0)
            return false;

        CoverDurability--;
        if (CoverDurability <= 0)
        {
            Terrain = TileTerrain.Normal;
            State = TileState.Empty;
            ApplyColor();
        }
        return true;
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
        if (terrainRenderers != null && terrainRenderers.Length > 0)
        {
            for (int i = 0; i < terrainRenderers.Length; i++)
            {
                if (terrainRenderers[i] == null) continue;
                Color color = isHighlighted
                    ? Color.Lerp(terrainOriginalColors[i], currentHighlightColor, 0.55f)
                    : terrainOriginalColors[i];
                terrainRenderers[i].material.color = color;
                if (terrainRenderers[i].material.HasProperty("_BaseColor"))
                    terrainRenderers[i].material.SetColor("_BaseColor", color);
            }
            return;
        }

        if (tileMaterial == null) return;

        switch (visualType)
        {
            case TileVisualType.Wireframe:
                ApplyWireframeColor();
                break;
            case TileVisualType.Hologram:
                ApplyHologramColor();
                break;
            default:
                ApplyDefaultColor();
                break;
        }
    }

    private void ApplyWireframeColor()
    {
        Color fill = isHighlighted ? currentHighlightColor : baseFillColor;
        if (isHighlighted)
            fill.a *= 0.4f;

        if (tileMaterial.HasProperty("_BorderColor"))
            tileMaterial.SetColor("_BorderColor", isHighlighted ? currentHighlightColor : baseBorderColor);
        if (tileMaterial.HasProperty("_FillColor"))
            tileMaterial.SetColor("_FillColor", fill);

        ApplyMaterialColor(fill);
    }

    private void ApplyHologramColor()
    {
        Color color = isHighlighted ? currentHighlightColor : baseRimColor;
        if (!isHighlighted)
            color.a = Mathf.Min(color.a, 0.35f);

        if (tileMaterial.HasProperty("_RimColor"))
            tileMaterial.SetColor("_RimColor", color);

        ApplyMaterialColor(color);
    }

    private void ApplyDefaultColor()
    {
        Color baseColor = Terrain == TileTerrain.HighGround
            ? highGroundColor
            : Terrain == TileTerrain.Cover
                ? new Color(0.025f, 0.025f, 0.035f, 1f)
                : zoneColor;
        ApplyMaterialColor(isHighlighted ? currentHighlightColor : baseColor);
    }

    private void ApplyMaterialColor(Color color)
    {
        if (tileMaterial.HasProperty("_BaseColor"))
            tileMaterial.SetColor("_BaseColor", color);
        if (tileMaterial.HasProperty("_Color"))
            tileMaterial.SetColor("_Color", color);
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
