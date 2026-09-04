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

            ConvertToToonAndApplyEnvironment(spawnedBackground);
        }
    }

    void ConvertToToonAndApplyEnvironment(GameObject bg)
    {
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader == null) return;

        Renderer[] renderers = bg.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;

                Texture baseMap = null;
                if (mat.HasProperty("_BaseMap"))
                    baseMap = mat.GetTexture("_BaseMap");
                else if (mat.HasProperty("_MainTex"))
                    baseMap = mat.GetTexture("_MainTex");

                mat.shader = toonShader;
                if (baseMap != null) mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Flatten", 0.425f);
                mat.SetFloat("_ShadowThreshold", 0.68f);
                mat.SetFloat("_ShadowFeather", 0.10f);
                mat.SetColor("_ShadowTint", new Color(0.66f, 0.71f, 0.86f, 1f));
                mat.SetFloat("_ReceiveShadowStrength", 0f);
                mat.SetFloat("_AmbientStrength", 1.0f);
                mat.SetFloat("_AmbientFlatten", 0.6f);
                mat.SetFloat("_EnvironmentInfluence", 0.583f);
                mat.SetFloat("_Brightness", 1.0f);
                mat.SetFloat("_AdditionalLightIntensity", 0.5f);
            }
            rend.materials = mats;
        }

        var toonSettings = bg.GetComponent<EnvironmentToonSettings>();
        if (toonSettings == null)
            toonSettings = bg.AddComponent<EnvironmentToonSettings>();
        toonSettings.Apply();
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
