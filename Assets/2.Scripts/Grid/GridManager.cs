using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 6;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileRotationX = 60f;

    [Header("High Ground")]
    [SerializeField] private Vector2Int[] highGroundPositions =
    {
        new Vector2Int(1, 2),
        new Vector2Int(3, 3)
    };
    [SerializeField, Range(0.1f, 2f)] private float highGroundHeight = 0.5f;
    [SerializeField] private Material highGroundMaterial;
    [SerializeField] private Color highGroundColor = new Color(1f, 0.78f, 0.05f, 1f);

    [Header("Cover")]
    [SerializeField] private Vector2Int[] coverPositions =
    {
        new Vector2Int(2, 2),
        new Vector2Int(2, 3)
    };

    [Header("Tile Appearance")]
    [SerializeField, Range(0.7f, 0.98f)] private float tileScale = 0.88f;
    [SerializeField, Range(0.02f, 0.15f)] private float tileThickness = 0.06f;
    [SerializeField] private Color gridBaseColor = new Color(0.1f, 0.1f, 0.13f, 0.1f);

    [Header("Wireframe (Neutral Zone)")]
    [SerializeField] private Color wireframeBorderColor = new Color(0.4f, 0.6f, 0.8f, 0.6f);
    [SerializeField, Range(0.01f, 0.2f)] private float wireframeBorderWidth = 0.05f;
    [SerializeField] private Color wireframeFillColor = new Color(0.1f, 0.15f, 0.2f, 0.03f);

    [Header("Hologram (Player Zone)")]
    [SerializeField] private Color hologramColor = new Color(0.25f, 0.45f, 0.85f, 1f);

    [Header("Enemy Zone")]
    [SerializeField] private Color enemyBorderColor = new Color(0.85f, 0.25f, 0.25f, 0.5f);
    [SerializeField] private Color enemyFillColor = new Color(0.85f, 0.15f, 0.15f, 0.03f);

    [Header("Highlight Colors")]
    [SerializeField] private Color deployHighlightColor = new Color(0.2f, 0.85f, 0.3f, 0.6f);
    [SerializeField] private Color moveHighlightColor = new Color(0.3f, 0.75f, 1f, 0.6f);
    [SerializeField] private Color attackHighlightColor = new Color(1f, 0.25f, 0.25f, 0.6f);

    [Header("Terrain Prefabs")]
    [Tooltip("고지대 전용 프리팹 (비워 두면 기본 타일을 높이만 올림)")]
    [SerializeField] private GameObject highGroundPrefab;
    [Tooltip("엄폐물 전용 프리팹 (비워 두면 기본 블록 타일을 사용)")]
    [SerializeField] private GameObject coverPrefab;

    private Tile[,] grid;
    private GameObject gridBase;

    private Material wireframeMat;
    private Material hologramMat;
    private Material enemyWireframeMat;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public Color DeployHighlight => deployHighlightColor;
    public Color MoveHighlight => moveHighlightColor;
    public Color AttackHighlight => attackHighlightColor;

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
        GenerateGrid();

        if (tilePrefab != null)
            tilePrefab.SetActive(false);
    }

    public void GenerateGrid()
    {
        ClearGrid();
        grid = new Tile[width, height];

        CreateZoneMaterials();
        CreateGridBase();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObj = CreateArknightsTile();
                tileObj.transform.SetParent(transform, false);
                tileObj.name = $"Tile_({x},{y})";
                tileObj.transform.localPosition = GridToLocalPosition(x, y);

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile == null)
                    tile = tileObj.AddComponent<Tile>();

                tile.Init(x, y);
                ApplyZoneMaterial(tile);
                grid[x, y] = tile;
            }
        }

        ApplyTerrainLayout();
    }

    private void CreateZoneMaterials()
    {
        Shader wireShader = Shader.Find("Custom/GridWireframe");
        Shader holoShader = Shader.Find("Custom/Hologram");

        if (wireShader != null)
        {
            wireframeMat = new Material(wireShader);
            wireframeMat.SetColor("_BorderColor", wireframeBorderColor);
            wireframeMat.SetFloat("_BorderWidth", wireframeBorderWidth);
            wireframeMat.SetColor("_FillColor", wireframeFillColor);

            enemyWireframeMat = new Material(wireShader);
            enemyWireframeMat.SetColor("_BorderColor", enemyBorderColor);
            enemyWireframeMat.SetFloat("_BorderWidth", wireframeBorderWidth);
            enemyWireframeMat.SetColor("_FillColor", enemyFillColor);
        }

        if (holoShader != null)
        {
            hologramMat = new Material(holoShader);
            hologramMat.SetColor("_RimColor", hologramColor);
        }
    }

    private void ApplyZoneMaterial(Tile tile)
    {
        switch (tile.Zone)
        {
            case TileZone.Neutral:
                if (wireframeMat != null)
                    tile.SetVisualType(TileVisualType.Wireframe, wireframeMat,
                        wireframeBorderColor, wireframeFillColor);
                break;

            case TileZone.PlayerDeploy:
                if (hologramMat != null)
                    tile.SetVisualType(TileVisualType.Hologram, hologramMat,
                        hologramColor, Color.clear);
                break;

            case TileZone.EnemyDeploy:
                if (enemyWireframeMat != null)
                    tile.SetVisualType(TileVisualType.Wireframe, enemyWireframeMat,
                        enemyBorderColor, enemyFillColor);
                break;
        }
    }

    private static Material CreateURPMaterial(Color color, float smoothness = 0.35f)
    {
        Renderer probe = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
        Material mat = new Material(probe.sharedMaterial);
        DestroyImmediate(probe.gameObject);

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        Renderer probe = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
        Material mat = new Material(probe.sharedMaterial);
        DestroyImmediate(probe.gameObject);

        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_SrcBlend", 5f);
            mat.SetFloat("_DstBlend", 10f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        return mat;
    }

    private GameObject CreateArknightsTile()
    {
        GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        float size = cellSize * tileScale;
        tileObj.transform.localScale = new Vector3(size, tileThickness, size);

        BoxCollider col = tileObj.GetComponent<BoxCollider>();
        float expand = 1f / tileScale;
        col.size = new Vector3(expand, col.size.y, expand);

        Renderer rend = tileObj.GetComponent<Renderer>();
        rend.material = CreateURPMaterial(Color.gray);

        return tileObj;
    }

    private void CreateGridBase()
    {
        gridBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gridBase.name = "GridBase";
        gridBase.transform.SetParent(transform, false);

        float baseThickness = 0.02f;
        float padding = cellSize * 0.3f;
        float totalWidth = width * cellSize + padding;
        float totalDepth = height * cellSize + padding;
        gridBase.transform.localScale = new Vector3(totalWidth, baseThickness, totalDepth);
        gridBase.transform.localPosition = new Vector3(0f, -(tileThickness + baseThickness) * 0.5f, 0f);

        Renderer rend = gridBase.GetComponent<Renderer>();
        Shader wireShader = Shader.Find("Custom/GridWireframe");
        if (wireShader != null)
        {
            Material mat = new Material(wireShader);
            mat.SetColor("_BorderColor", gridBaseColor);
            mat.SetFloat("_BorderWidth", 0f);
            mat.SetColor("_FillColor", gridBaseColor);
            rend.material = mat;
        }
        else
        {
            rend.material = CreateTransparentMaterial(gridBaseColor);
        }

        Destroy(gridBase.GetComponent<Collider>());
    }

    private void ApplyTerrainLayout()
    {
        if (highGroundPositions == null || highGroundPositions.Length == 0)
        {
            highGroundPositions = new[] { new Vector2Int(1, 2), new Vector2Int(3, 3) };
        }
        if (coverPositions == null || coverPositions.Length == 0)
        {
            coverPositions = new[] { new Vector2Int(2, 2), new Vector2Int(2, 3) };
        }

        foreach (Vector2Int position in highGroundPositions)
        {
            Tile tile = GetTile(position);
            if (tile == null) continue;
            if (highGroundPrefab != null)
                tile.SetTerrainWithPrefab(TileTerrain.HighGround, highGroundHeight, highGroundPrefab);
            else
                tile.SetTerrain(TileTerrain.HighGround, highGroundHeight, highGroundColor, highGroundMaterial);
        }

        foreach (Vector2Int position in coverPositions)
        {
            Tile tile = GetTile(position);
            if (tile == null) continue;
            if (coverPrefab != null)
                tile.SetTerrainWithPrefab(TileTerrain.Cover, 0f, coverPrefab);
            else
                tile.SetTerrain(TileTerrain.Cover);
        }
    }

    private void ClearGrid()
    {
        if (gridBase != null)
        {
            Destroy(gridBase);
            gridBase = null;
        }

        if (grid == null) return;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != null)
                    Destroy(grid[x, y].gameObject);
            }
        }
        grid = null;
    }

    public Vector3 GridToLocalPosition(int x, int y)
    {
        float localX = (x - (width - 1) * 0.5f) * cellSize;
        float localZ = (y - (height - 1) * 0.5f) * cellSize;
        return new Vector3(localX, 0f, localZ);
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        return transform.TransformPoint(GridToLocalPosition(x, y));
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        int x = Mathf.RoundToInt(localPos.x / cellSize + (width - 1) * 0.5f);
        int y = Mathf.RoundToInt(localPos.z / cellSize + (height - 1) * 0.5f);
        return new Vector2Int(x, y);
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsValidPosition(Vector2Int pos)
    {
        return IsValidPosition(pos.x, pos.y);
    }

    public Tile GetTile(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return grid[x, y];
    }

    public Tile GetTile(Vector2Int pos)
    {
        return GetTile(pos.x, pos.y);
    }

    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return GetTile(gridPos);
    }

    public bool IsPlayerDeployZone(int x, int y)
    {
        return IsValidPosition(x, y) && y <= 1;
    }

    public bool IsEnemyDeployZone(int x, int y)
    {
        return IsValidPosition(x, y) && y >= 4;
    }

    public Tile[] GetNeighbors(Vector2Int pos)
    {
        return GetNeighbors(pos.x, pos.y);
    }

    public Tile[] GetNeighbors(int x, int y)
    {
        var list = new System.Collections.Generic.List<Tile>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsValidPosition(nx, ny))
                list.Add(grid[nx, ny]);
        }
        return list.ToArray();
    }

    public void ClearAllHighlights()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y].ClearHighlight();
            }
        }
    }
}
