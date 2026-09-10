// =============================================================================
// 8차시 (3) 아웃라인 Renderer Feature
//
// ConfigureInput으로 깊이/노멀 버퍼를 "요청"하는 방법을 배웁니다.
// 이 호출이 없으면 URP가 DepthNormals 프리패스를 실행하지 않아
// _CameraNormalsTexture가 비어 있습니다.
//
// 사용법
//  1) URP Renderer 에셋 > Add Renderer Feature > Course Outline
//  2) Shader에 Course/08c_Outline 지정
//
// ─────────────────────────────────────────────────────────────────────────────
// ★ Plan B (권장 대안)
//   만약 Render Graph의 전역 텍스처 검증 오류
//   ("... global texture is not declared ...")가 발생하거나 버전 차이로
//   이 스크립트가 동작하지 않으면, 이 스크립트를 지우고 URP 내장 기능을 쓰세요.
//
//     URP Renderer 에셋 > Add Renderer Feature > Full Screen Pass Renderer Feature
//       - Pass Material   : Course/08c_Outline 머티리얼
//       - Injection Point : Before Rendering Post Processing
//       - Requirements    : Depth, Normal 체크
//
//   셰이더는 수정 없이 그대로 동작합니다. 수업 목표(셰이더 작성)에는 지장이 없고,
//   오히려 "엔진이 제공하는 기능을 먼저 확인한다"는 실무 습관을 가르칠 수 있습니다.
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField] Shader shader;
    [SerializeField] Color outlineColor = Color.black;
    [SerializeField, Range(1f, 6f)] float thickness = 1f;
    [SerializeField, Range(0.0001f, 0.5f)] float depthThreshold = 0.02f;
    [SerializeField, Range(0f, 5f)] float depthWeight = 1f;
    [SerializeField, Range(0.01f, 2f)] float normalThreshold = 0.4f;
    [SerializeField, Range(0f, 5f)] float normalWeight = 1f;
    [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    Material material;
    OutlinePass pass;

    static readonly int IdColor           = Shader.PropertyToID("_OutlineColor");
    static readonly int IdThickness       = Shader.PropertyToID("_Thickness");
    static readonly int IdDepthThreshold  = Shader.PropertyToID("_DepthThreshold");
    static readonly int IdDepthWeight     = Shader.PropertyToID("_DepthWeight");
    static readonly int IdNormalThreshold = Shader.PropertyToID("_NormalThreshold");
    static readonly int IdNormalWeight    = Shader.PropertyToID("_NormalWeight");

    public override void Create()
    {
        if (shader == null) return;
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new OutlinePass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null) return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) return;

        material.SetColor(IdColor, outlineColor);
        material.SetFloat(IdThickness, thickness);
        material.SetFloat(IdDepthThreshold, depthThreshold);
        material.SetFloat(IdDepthWeight, depthWeight);
        material.SetFloat(IdNormalThreshold, normalThreshold);
        material.SetFloat(IdNormalWeight, normalWeight);

        pass.renderPassEvent = injectionPoint;

        // ★ 핵심: 깊이와 노멀 버퍼를 요청.
        //   이 호출이 URP에게 DepthNormals 프리패스를 실행하라고 알립니다.
        pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
        pass = null;
    }

    // -------------------------------------------------------------------------
    class OutlinePass : ScriptableRenderPass
    {
        readonly Material material;

        public OutlinePass(Material mat)
        {
            material = mat;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_CourseOutlineTarget", false);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                source, destination, material, shaderPass: 0);
            renderGraph.AddBlitPass(blitParams, "Course Outline");

            resourceData.cameraColor = destination;
        }
    }
}
