// =============================================================================
// 8차시 (2) 다중 패스 + 임시 텍스처 관리
//
// 학습 목표
//  - 한 Renderer Feature 안에서 여러 블릿 패스를 연결하는 방법
//  - 임시 텍스처를 만들고 핑퐁(ping-pong)하는 패턴
//  - 다운샘플링으로 성능을 버는 방법
//  - Render Graph Viewer(Window > Analysis > Render Graph Viewer)로
//    패스 의존성과 리소스 수명 확인하기
//
// 사용법
//  1) URP Renderer 에셋 > Add Renderer Feature > Course Blur
//  2) Shader에 Course/08b_Blur 지정
// =============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    [SerializeField] Shader shader;
    [SerializeField, Range(0f, 8f)] float radius = 2f;
    [SerializeField, Range(1, 8)] int downsample = 2;
    [SerializeField, Range(1, 4)] int iterations = 1;
    [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    Material material;
    BlurPass pass;

    static readonly int IdRadius = Shader.PropertyToID("_BlurRadius");

    public override void Create()
    {
        if (shader == null) return;
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new BlurPass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null) return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) return;

        material.SetFloat(IdRadius, radius);
        pass.Setup(downsample, iterations);
        pass.renderPassEvent = injectionPoint;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
        pass = null;
    }

    // -------------------------------------------------------------------------
    class BlurPass : ScriptableRenderPass
    {
        readonly Material mat;
        int ds = 2;
        int iter = 1;

        public BlurPass(Material material)
        {
            mat = material;
            requiresIntermediateTexture = true;
        }

        public void Setup(int downsample, int iterations)
        {
            ds = Mathf.Max(1, downsample);
            iter = Mathf.Clamp(iterations, 1, 4);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle cameraColor = resourceData.activeColorTexture;

            // --- 축소된 임시 텍스처 두 장 (핑퐁용) ---
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.width  = Mathf.Max(1, desc.width  / ds);
            desc.height = Mathf.Max(1, desc.height / ds);

            TextureHandle bufferA = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_CourseBlurA", false, FilterMode.Bilinear);
            TextureHandle bufferB = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_CourseBlurB", false, FilterMode.Bilinear);

            // 1) 카메라 컬러 -> A (다운샘플 복사, Pass 2 = Copy)
            renderGraph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(cameraColor, bufferA, mat, 2),
                "Course Blur Downsample");

            // 2) 수평 -> 수직을 반복 (반복할수록 블러 반경이 커짐)
            for (int i = 0; i < iter; i++)
            {
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(bufferA, bufferB, mat, 0),
                    $"Course Blur H {i}");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(bufferB, bufferA, mat, 1),
                    $"Course Blur V {i}");
            }

            // 3) 결과를 풀 해상도 텍스처로 되돌림
            var fullDesc = cameraData.cameraTargetDescriptor;
            fullDesc.depthBufferBits = 0;
            fullDesc.msaaSamples = 1;
            TextureHandle result = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, fullDesc, "_CourseBlurResult", false, FilterMode.Bilinear);

            renderGraph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(bufferA, result, mat, 2),
                "Course Blur Upsample");

            resourceData.cameraColor = result;
        }
    }
}
