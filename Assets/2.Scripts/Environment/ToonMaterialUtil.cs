using UnityEngine;
using UnityEngine.Rendering;

public static class ToonMaterialUtil
{
    public const string ToonShaderName = "Custom/ToonLit";
    public const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    public static Shader ToonShader => Shader.Find(ToonShaderName);
    public static Shader UrpLitShader => Shader.Find(UrpLitShaderName);

    public static bool IsToon(Material m) =>
        m != null && m.shader != null && m.shader.name == ToonShaderName;

    public static void ToToon(Material m, bool isEnvironment = false)
    {
        Shader toon = ToonShader;
        if (m == null || toon == null || m.shader == toon) return;

        Texture baseMap = GetTex(m, "_BaseMap", "_MainTex");
        Color baseColor = GetCol(m, "_BaseColor", "_Color");

        bool wasAlphaClip = m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f;
        if (!wasAlphaClip)
            wasAlphaClip = m.IsKeywordEnabled("_ALPHATEST_ON");
        float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

        bool wasTransparent = m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;
        if (!wasTransparent)
            wasTransparent = m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");

        float srcBlend = m.HasProperty("_SrcBlend") ? m.GetFloat("_SrcBlend") : 1f;
        float dstBlend = m.HasProperty("_DstBlend") ? m.GetFloat("_DstBlend") : 0f;
        float zWrite = m.HasProperty("_ZWrite") ? m.GetFloat("_ZWrite") : 1f;

        Texture emissionMap = GetTex(m, "_EmissionMap");
        Color emissionColor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;

        Vector2 tiling = m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap") :
                         m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 offset = m.HasProperty("_BaseMap") ? m.GetTextureOffset("_BaseMap") :
                         m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;

        int renderQueue = m.renderQueue;

        m.shader = toon;

        if (baseMap != null)
            m.SetTexture("_BaseMap", baseMap);
        m.SetColor("_BaseColor", baseColor);
        m.SetTextureScale("_BaseMap", tiling);
        m.SetTextureOffset("_BaseMap", offset);

        if (wasAlphaClip)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
        }
        else
        {
            m.SetFloat("_AlphaClip", 0f);
            m.DisableKeyword("_ALPHATEST_ON");
        }

        if (wasTransparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", srcBlend);
            m.SetFloat("_DstBlend", dstBlend);
            m.SetFloat("_ZWrite", 0f);
            m.renderQueue = renderQueue > 0 ? renderQueue : (int)RenderQueue.Transparent;
        }
        else
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_SrcBlend", 1f);
            m.SetFloat("_DstBlend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.renderQueue = -1;
        }

        if (emissionMap != null)
            m.SetTexture("_EmissionMap", emissionMap);
        m.SetColor("_EmissionColor", emissionColor);

        if (isEnvironment)
            ApplyEnvironmentPreset(m);

        SyncKeywords(m);
    }

    public static void ToUrpLit(Material m)
    {
        Shader lit = UrpLitShader;
        if (m == null || lit == null || m.shader == lit) return;

        Texture baseMap = GetTex(m, "_BaseMap", "_MainTex");
        Color baseColor = GetCol(m, "_BaseColor", "_Color");
        bool transparent = m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;
        bool clip = m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f;
        float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

        m.shader = lit;

        if (baseMap != null) m.SetTexture("_BaseMap", baseMap);
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Cutoff", cutoff);

        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = -1;
        }

        m.SetFloat("_AlphaClip", clip ? 1f : 0f);
        if (clip) m.EnableKeyword("_ALPHATEST_ON");
        else m.DisableKeyword("_ALPHATEST_ON");

        m.SetShaderPassEnabled("ShadowCaster", true);
    }

    public static void ApplyEnvironmentPreset(Material m)
    {
        m.SetFloat("_Flatten", 0.45f);
        m.SetFloat("_ShadowThreshold", 0.46f);
        m.SetFloat("_ShadowFeather", 0.14f);
        m.SetColor("_ShadowTint", new Color(0.66f, 0.71f, 0.86f, 1f));
        m.SetFloat("_ReceiveShadowStrength", 0.45f);
        m.SetFloat("_AmbientStrength", 1.0f);
        m.SetFloat("_AmbientFlatten", 0.6f);
        m.SetFloat("_EnvironmentInfluence", 0.6f);
        m.SetFloat("_Brightness", 1.0f);
        m.SetFloat("_AdditionalLightIntensity", 0.5f);
        m.SetFloat("_OutlineEnabled", 0f);
        m.SetFloat("_OutlineWidth", 0f);
        m.SetFloat("_RimIntensity", 0f);
        m.SetColor("_SpecularColor", new Color(0.03f, 0.03f, 0.03f, 1f));
        m.DisableKeyword("_OUTLINE_ON");
    }

    public static void SyncKeywords(Material m)
    {
        if (m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f)
            m.EnableKeyword("_ALPHATEST_ON");
        else
            m.DisableKeyword("_ALPHATEST_ON");

        if (m.HasProperty("_OutlineEnabled") && m.GetFloat("_OutlineEnabled") > 0.5f)
            m.EnableKeyword("_OUTLINE_ON");
        else
            m.DisableKeyword("_OUTLINE_ON");
    }

    private static Texture GetTex(Material m, params string[] names)
    {
        foreach (string n in names)
            if (m.HasProperty(n) && m.GetTexture(n) != null) return m.GetTexture(n);
        return null;
    }

    private static Color GetCol(Material m, params string[] names)
    {
        foreach (string n in names)
            if (m.HasProperty(n)) return m.GetColor(n);
        return Color.white;
    }
}
