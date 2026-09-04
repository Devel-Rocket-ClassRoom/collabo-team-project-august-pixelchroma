using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPAndToonSetup
{
    [MenuItem("Tools/SRPG UI/1. URP 에셋 생성 및 할당")]
    public static void SetupURP()
    {
        string folder = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        string rendererPath = folder + "/URP_Renderer.asset";
        string pipelinePath = folder + "/URP_PipelineAsset.asset";

        var existingRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (existingRenderer == null)
        {
            existingRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(existingRenderer, rendererPath);
        }

        var existingPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (existingPipeline == null)
        {
            existingPipeline = UniversalRenderPipelineAsset.Create(existingRenderer);
            AssetDatabase.CreateAsset(existingPipeline, pipelinePath);
        }

        existingPipeline.shadowDistance = 50f;
        EditorUtility.SetDirty(existingPipeline);

        GraphicsSettings.defaultRenderPipeline = existingPipeline;
        QualitySettings.renderPipeline = existingPipeline;

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료",
            "URP 파이프라인 에셋 생성 및 할당 완료!\n\n" +
            "경로: Assets/Settings/\n" +
            "Graphics Settings와 Quality Settings에 자동 할당됨.",
            "확인");
    }

    [MenuItem("Tools/SRPG UI/2. 배경 머터리얼 → 툰 셰이더 변환")]
    public static void ConvertToToon()
    {
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader == null)
        {
            EditorUtility.DisplayDialog("오류",
                "Custom/ToonLit 셰이더를 찾을 수 없습니다.\n" +
                "Assets/Shaders/ToonShader.shader 파일이 있는지 확인하세요.",
                "확인");
            return;
        }

        string matFolder = "Assets/7.Background/Anime Tokyo/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            EditorUtility.DisplayDialog("오류",
                "머터리얼 폴더를 찾을 수 없습니다:\n" + matFolder,
                "확인");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { matFolder });
        int converted = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Texture baseMap = null;
            Color baseColor = Color.white;

            if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                baseMap = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                baseMap = mat.GetTexture("_MainTex");

            if (mat.HasProperty("_BaseColor"))
                baseColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                baseColor = mat.GetColor("_Color");

            mat.shader = toonShader;

            if (baseMap != null)
                mat.SetTexture("_BaseMap", baseMap);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Flatten", 0.425f);
            mat.SetColor("_ShadowTint", new Color(0.66f, 0.71f, 0.86f, 1f));
            mat.SetFloat("_ShadowThreshold", 0.68f);
            mat.SetFloat("_ShadowFeather", 0.10f);
            mat.SetFloat("_ReceiveShadowStrength", 0f);
            mat.SetFloat("_AmbientStrength", 1.0f);
            mat.SetFloat("_AmbientFlatten", 0.6f);
            mat.SetFloat("_EnvironmentInfluence", 0.583f);
            mat.SetFloat("_AdditionalLightIntensity", 0.5f);

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료",
            $"총 {converted}개 머터리얼을 툰 셰이더로 변환 완료!\n\n" +
            "VRProject ToonLit 기반 배경 셰이더:\n" +
            "- SH 앰비언트 (방향성 + 평탄화)\n" +
            "- 채도 유지 색 그림자 (albedo × ShadowTint)\n" +
            "- 추가 광원 지원\n" +
            "- Forward+ 호환\n\n" +
            "Directional Light Mode를 Realtime으로 설정하세요.",
            "확인");
    }
}
