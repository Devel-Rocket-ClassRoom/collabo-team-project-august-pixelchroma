using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class EnvironmentToonSettings : MonoBehaviour
{
    [Header("키 라이트")]
    public Color lightColor = new Color(1f, 0.957f, 0.902f);
    [Range(0f, 3f)]
    public float lightIntensity = 1.425f;
    [Range(0f, 1f)]
    public float shadowStrength = 0.36f;
    public bool disableExtraDirectionalLights = true;
    [Range(0f, 89f)]
    public float lightAngleVertical = 20.3f;
    [Range(-180f, 180f)]
    public float lightAngleHorizontal = -1.4f;

    [Header("환경광 (Gradient)")]
    public Color ambientSky = new Color(0.60f, 0.68f, 0.82f);
    public Color ambientEquator = new Color(0.52f, 0.56f, 0.65f);
    public Color ambientGround = new Color(0.42f, 0.43f, 0.47f);
    [Range(0f, 2f)]
    public float ambientIntensity = 0.866f;
    [Range(0f, 1f)]
    public float reflectionIntensity = 0.257f;

    [Header("안개 (공기원근)")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.72f, 0.82f, 0.94f);
    public float fogStart = 8.7f;
    public float fogEnd = 263.3f;

    [Header("머티리얼 셰이딩")]
    [Range(0f, 1f)] public float flatten = 0.425f;
    [Range(0f, 1f)] public float shadowThreshold = 0.68f;
    [Range(0.001f, 0.4f)] public float shadowFeather = 0.10f;
    public Color shadowTint = new Color(0.66f, 0.71f, 0.86f, 1f);
    [Range(0f, 1f)] public float receiveShadowStrength = 0f;
    [Range(0f, 1f)] public float environmentInfluence = 0.583f;
    [Range(0f, 1f)] public float ambientFlatten = 0.6f;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        ApplyLighting();
        ApplyAmbient();
        ApplyFog();
        ApplyMaterials();
    }

    void ApplyLighting()
    {
        Light keyLight = FindBrightestDirectional();
        if (keyLight == null) return;

        if (disableExtraDirectionalLights)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == keyLight) continue;
                if (l.type != LightType.Directional) continue;
                if (l.enabled) l.enabled = false;
            }
        }

        keyLight.enabled = true;
        keyLight.color = lightColor;
        keyLight.intensity = lightIntensity;
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = shadowStrength;
        keyLight.shadowBias = 0.08f;
        keyLight.shadowNormalBias = 0.45f;
        keyLight.transform.rotation = Quaternion.Euler(lightAngleVertical, lightAngleHorizontal, 0f);
    }

    Light FindBrightestDirectional()
    {
        Light best = null;
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            if (best == null || l.intensity > best.intensity) best = l;
        }
        return best;
    }

    void ApplyAmbient()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = reflectionIntensity;
    }

    void ApplyFog()
    {
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(fogEnd, fogStart + 1f);
        }
    }

    void ApplyMaterials()
    {
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            Material[] mats = Application.isPlaying ? rend.materials : rend.sharedMaterials;
            foreach (Material mat in mats)
            {
                if (mat == null) continue;
                if (mat.shader != toonShader) continue;

                mat.SetFloat("_Flatten", flatten);
                mat.SetFloat("_ShadowThreshold", shadowThreshold);
                mat.SetFloat("_ShadowFeather", shadowFeather);
                mat.SetColor("_ShadowTint", shadowTint);
                mat.SetFloat("_ReceiveShadowStrength", receiveShadowStrength);
                mat.SetFloat("_EnvironmentInfluence", environmentInfluence);
                mat.SetFloat("_AmbientFlatten", ambientFlatten);
            }

            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }
}
