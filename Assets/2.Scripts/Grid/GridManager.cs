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

    private Tile[,] grid;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObj = Instantiate(tilePrefab, transform);
                tileObj.name = $"Tile_({x},{y})";
                tileObj.transform.localPosition = GridToLocalPosition(x, y);
                tileObj.transform.localRotation = Quaternion.Euler(tileRotationX, 0f, 0f);

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile == null)
                    tile = tileObj.AddComponent<Tile>();

                tile.Init(x, y);
                grid[x, y] = tile;
            }
        }

        ApplyTerrainLayout();
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
            if (tile != null)
                tile.SetTerrain(TileTerrain.HighGround, highGroundHeight, highGroundColor, highGroundMaterial);
        }

        foreach (Vector2Int position in coverPositions)
        {
            Tile tile = GetTile(position);
            if (tile != null) tile.SetTerrain(TileTerrain.Cover);
        }
    }

    private void ClearGrid()
    {
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
