using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public class EnvironmentToonSettings : MonoBehaviour
{
    // ── 비교용 ──────────────────────────────────────────────────
    [Header("비교용")]
    [Tooltip("켜면 배경 머티리얼을 URP Lit으로 되돌리고 툰 설정을 적용하지 않는다.")]
    public bool disableToon = false;

    [Tooltip("disableToon과 함께 조명/환경/포스트도 원래대로 되돌린다.")]
    public bool revertLighting = true;

    [Serializable]
    private class LightState
    {
        public Light light;
        public bool enabled;
        public Color color;
        public float intensity;
        public Vector3 euler;
        public float shadowStrength;
        public LightShadows shadows;
    }

    [SerializeField, HideInInspector] private bool originalCaptured;
    [SerializeField, HideInInspector] private List<LightState> originalLights = new List<LightState>();
    [SerializeField, HideInInspector] private AmbientMode originalAmbientMode;
    [SerializeField, HideInInspector] private Color originalSkyColor, originalEquatorColor, originalGroundColor;
    [SerializeField, HideInInspector] private float originalAmbientIntensity, originalReflectionIntensity;
    [SerializeField, HideInInspector] private DefaultReflectionMode originalReflectionMode;
    [SerializeField, HideInInspector] private bool originalFog;
    [SerializeField, HideInInspector] private Color originalFogColor;
    [SerializeField, HideInInspector] private FogMode originalFogMode;
    [SerializeField, HideInInspector] private float originalFogStart, originalFogEnd;

    // ── 배경 셰이딩 ─────────────────────────────────────────────
    [Header("배경 셰이딩")]
    [Tooltip("배경 머티리얼에 값을 적용할지.")]
    public bool applyShading = true;

    [Tooltip("URP Lit 머티리얼을 만나면 자동으로 Custom/ToonLit으로 교체한다.")]
    public bool autoReplaceShader = true;

    [Range(0f, 1f)] public float flatten = 0.45f;
    [Range(0f, 1f)] public float shadowThreshold = 0.46f;
    [Range(0.001f, 0.4f)] public float shadowFeather = 0.14f;
    public Color shadowTint = new Color(0.66f, 0.71f, 0.86f, 1f);
    [Range(0f, 1f)] public float receiveShadowStrength = 0.45f;
    [Range(0f, 1f)] public float environmentInfluence = 0.6f;
    [Range(0f, 1f)] public float ambientFlatten = 0.6f;
    [Range(0f, 2f)] public float ambientStrength = 1.0f;
    [Range(0.5f, 3f)] public float brightness = 1.0f;

    [Tooltip("배경 스펙큘러. 강하면 즉시 리얼해진다. 낮게 권장.")]
    [Range(0f, 1f)] public float specularIntensity = 0.03f;

    [Tooltip("배경 아웃라인. 드로우콜이 2배 되므로 0 권장.")]
    [Range(0f, 8f)] public float outlineWidth = 0f;

    // ── 키 라이트 ───────────────────────────────────────────────
    [Header("키 라이트")]
    [Tooltip("비워두면 가장 밝은 디렉셔널 라이트를 자동으로 쓴다.")]
    public Light keyLight;
    public bool disableExtraDirectionalLights = true;
    public bool applyLight = true;

    public Color lightColor = new Color(1f, 0.957f, 0.902f);
    [Range(0f, 3f)] public float lightIntensity = 1.0f;
    [Range(0f, 1f)] public float shadowStrength = 0.35f;
    [Range(0f, 89f)] public float lightAngleVertical = 50f;
    [Range(-180f, 180f)] public float lightAngleHorizontal = -35f;

    // ── 환경광 ──────────────────────────────────────────────────
    [Header("환경광")]
    public bool applyAmbient = true;
    public Color ambientSky = new Color(0.60f, 0.68f, 0.82f);
    public Color ambientEquator = new Color(0.52f, 0.56f, 0.65f);
    public Color ambientGround = new Color(0.42f, 0.43f, 0.47f);
    [Range(0f, 3f)] public float ambientIntensity = 1.0f;
    [Range(0f, 1f)] public float reflectionIntensity = 0f;

    // ── 안개 ────────────────────────────────────────────────────
    [Header("안개 (공기원근)")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.72f, 0.82f, 0.94f);
    public float fogStart = 25f;
    public float fogEnd = 260f;

    // ── 포스트 프로세싱 ─────────────────────────────────────────
    [Header("포스트 프로세싱")]
    public VolumeProfile volumeProfile;
    public bool applyPost = true;

    [Range(-3f, 3f)] public float postExposure = -0.15f;
    [Range(-100f, 100f)] public float postContrast = -14f;
    [Range(-100f, 100f)] public float postSaturation = 10f;
    public Color shadowColorGrade = new Color(0.94f, 0.98f, 1.12f);
    [Range(0f, 0.3f)] public float shadowLift = 0.06f;
    [Range(0f, 3f)] public float bloomThreshold = 1.15f;
    [Range(0f, 2f)] public float bloomIntensity = 0.35f;

    // ────────────────────────────────────────────────────────────
    private Renderer[] cachedRenderers;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    public void Apply() => ApplyAll();

    [ContextMenu("전체 다시 적용")]
    public void ApplyAll()
    {
        if (disableToon)
        {
            RevertShading();
            if (revertLighting) RevertLighting();
            return;
        }

        CaptureOriginal();
        ApplyShading();
        ApplyLighting();
        ApplyAmbientSettings();
        ApplyFog();
        ApplyPostProcessing();
    }

    private void CaptureOriginal()
    {
        if (originalCaptured) return;

        originalLights = new List<LightState>();
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            originalLights.Add(new LightState
            {
                light = l,
                enabled = l.enabled,
                color = l.color,
                intensity = l.intensity,
                euler = l.transform.rotation.eulerAngles,
                shadowStrength = l.shadowStrength,
                shadows = l.shadows,
            });
        }

        originalAmbientMode = RenderSettings.ambientMode;
        originalSkyColor = RenderSettings.ambientSkyColor;
        originalEquatorColor = RenderSettings.ambientEquatorColor;
        originalGroundColor = RenderSettings.ambientGroundColor;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionMode = RenderSettings.defaultReflectionMode;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;

        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;

        originalCaptured = true;
    }

    private void RevertShading()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.shader.name == "Custom/ToonLit")
                    ToonMaterialUtil.ToUrpLit(m);
            }
            r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        }
    }

    private void RevertLighting()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.enabled = true;
            l.shadowStrength = 1f;
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.fog = false;

        Volume v = FindFirstObjectByType<Volume>();
        if (v != null) v.enabled = false;
    }

    [ContextMenu("원본 캡처 초기화")]
    public void ClearCapture()
    {
        originalCaptured = false;
        originalLights = new List<LightState>();
    }

    // ── 셰이딩 ──────────────────────────────────────────────────
    private void ApplyShading()
    {
        if (!applyShading) return;

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

        Shader toon = Shader.Find("Custom/ToonLit");
        if (toon == null) return;

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;

            Material[] mats = Application.isPlaying ? r.materials : r.sharedMaterials;
            foreach (Material m in mats)
            {
                if (m == null) continue;

                if (autoReplaceShader && m.shader != toon)
                {
                    ToonMaterialUtil.ToToon(m);
                }

                if (m.shader != toon) continue;

                m.SetFloat("_Flatten", flatten);
                m.SetFloat("_ShadowThreshold", shadowThreshold);
                m.SetFloat("_ShadowFeather", shadowFeather);
                m.SetColor("_ShadowTint", shadowTint);
                m.SetFloat("_ReceiveShadowStrength", receiveShadowStrength);
                m.SetFloat("_EnvironmentInfluence", environmentInfluence);
                m.SetFloat("_AmbientFlatten", ambientFlatten);
                m.SetFloat("_AmbientStrength", ambientStrength);
                m.SetFloat("_Brightness", brightness);
                m.SetFloat("_OutlineWidth", outlineWidth);
                m.SetFloat("_OutlineEnabled", outlineWidth > 0f ? 1f : 0f);

                m.SetColor("_SpecularColor", Color.white * specularIntensity);
                m.SetFloat("_RimIntensity", 0f);

                ToonMaterialUtil.SyncKeywords(m);
            }

            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    // ── 라이트 ──────────────────────────────────────────────────
    private void ApplyLighting()
    {
        if (!applyLight) return;

        Light key = keyLight != null ? keyLight : FindBrightestDirectional();
        if (key == null) return;

        if (disableExtraDirectionalLights)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == key) continue;
                if (l.type != LightType.Directional) continue;
                if (l.enabled) l.enabled = false;
            }
        }

        key.enabled = true;
        key.color = lightColor;
        key.intensity = lightIntensity;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = shadowStrength;
        key.shadowBias = 0.08f;
        key.shadowNormalBias = 0.45f;
        key.transform.rotation = Quaternion.Euler(lightAngleVertical, lightAngleHorizontal, 0f);
    }

    private Light FindBrightestDirectional()
    {
        Light best = null;
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            if (best == null || l.intensity > best.intensity) best = l;
        }
        return best;
    }

    // ── 환경광 ──────────────────────────────────────────────────
    private void ApplyAmbientSettings()
    {
        if (!applyAmbient) return;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = reflectionIntensity;
    }

    // ── 안개 ────────────────────────────────────────────────────
    private void ApplyFog()
    {
        RenderSettings.fog = enableFog;
        if (!enableFog) return;

        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = Mathf.Max(fogEnd, fogStart + 1f);
    }

    // ── 포스트 ──────────────────────────────────────────────────
    private void ApplyPostProcessing()
    {
        if (!applyPost) return;

        VolumeProfile profile = volumeProfile;
        Volume vol = FindFirstObjectByType<Volume>();

        if (vol != null && !vol.enabled) vol.enabled = true;
        if (profile == null && vol != null) profile = vol.sharedProfile;
        if (profile == null) return;

        if (profile.TryGet(out Tonemapping tm))
        {
            tm.active = true;
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.Neutral;
        }

        if (profile.TryGet(out ColorAdjustments ca))
        {
            ca.active = true;
            ca.postExposure.overrideState = true; ca.postExposure.value = postExposure;
            ca.contrast.overrideState = true;     ca.contrast.value = postContrast;
            ca.saturation.overrideState = true;   ca.saturation.value = postSaturation;
        }

        if (profile.TryGet(out ShadowsMidtonesHighlights smh))
        {
            smh.active = true;
            smh.shadows.overrideState = true;
            smh.shadows.value = new Vector4(shadowColorGrade.r, shadowColorGrade.g,
                                            shadowColorGrade.b, shadowLift);
        }

        if (profile.TryGet(out Bloom bloom))
        {
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = bloomThreshold;
            bloom.intensity.overrideState = true; bloom.intensity.value = bloomIntensity;
            bloom.scatter.overrideState = true;   bloom.scatter.value = 0.65f;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Global Volume + 프로파일 만들기")]
    private void CreateVolume()
    {
        Volume v = FindFirstObjectByType<Volume>();
        if (v == null)
        {
            var go = new GameObject("Global Volume (Toon)");
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Toon Volume");
            v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 0f;
        }

        if (v.sharedProfile == null)
        {
            const string dir = "Assets/Settings";
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                $"{dir}/ToonVolumeProfile.asset");
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            UnityEditor.AssetDatabase.CreateAsset(p, path);

            p.Add<Tonemapping>(true);
            p.Add<ColorAdjustments>(true);
            p.Add<ShadowsMidtonesHighlights>(true);
            p.Add<Bloom>(true);

            UnityEditor.EditorUtility.SetDirty(p);
            UnityEditor.AssetDatabase.SaveAssets();
            v.sharedProfile = p;
        }

        volumeProfile = v.sharedProfile;
        ApplyPostProcessing();
        Debug.Log($"[EnvironmentToonSettings] Volume 준비 완료: {volumeProfile.name}", this);
    }

    [ContextMenu("현재 렌더링 상태 출력")]
    public void LogRenderState()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[EnvironmentToonSettings] 현재 상태 (disableToon={disableToon})");

        sb.AppendLine("== 디렉셔널 라이트 ==");
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            sb.AppendLine($"  {l.name,-26} enabled={l.enabled,-5} intensity={l.intensity:F2} " +
                          $"shadow={l.shadows}({l.shadowStrength:F2})");
        }

        sb.AppendLine("== 환경광 ==");
        sb.AppendLine($"  Ambient mode={RenderSettings.ambientMode} intensity={RenderSettings.ambientIntensity:F2}");
        sb.AppendLine($"  Reflection mode={RenderSettings.defaultReflectionMode} " +
                      $"intensity={RenderSettings.reflectionIntensity:F2}");
        sb.AppendLine($"  Fog {RenderSettings.fog} {RenderSettings.fogMode} " +
                      $"{RenderSettings.fogStartDistance:F0}~{RenderSettings.fogEndDistance:F0}");

        Debug.Log(sb.ToString(), this);
    }
#endif
}
