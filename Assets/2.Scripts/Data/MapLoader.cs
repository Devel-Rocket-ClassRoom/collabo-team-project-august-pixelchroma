using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public static MapLoader Instance { get; private set; }

    [SerializeField] private MapData currentMap;

    private GameObject spawnedBackground;

    public MapData CurrentMap => currentMap;

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
        if (currentMap != null)
            LoadMap(currentMap);
    }

    public void LoadMap(MapData mapData)
    {
        ClearBackground();
        currentMap = mapData;

        if (mapData.backgroundPrefab != null)
        {
            spawnedBackground = Instantiate(mapData.backgroundPrefab);
            spawnedBackground.name = "Background_" + mapData.mapName;
            spawnedBackground.transform.position = mapData.backgroundPosition;
            spawnedBackground.transform.eulerAngles = mapData.backgroundRotation;
            spawnedBackground.transform.localScale = mapData.backgroundScale;
            spawnedBackground.isStatic = true;
        }
    }

    public void ClearBackground()
    {
        if (spawnedBackground != null)
        {
            Destroy(spawnedBackground);
            spawnedBackground = null;
        }
    }
}
