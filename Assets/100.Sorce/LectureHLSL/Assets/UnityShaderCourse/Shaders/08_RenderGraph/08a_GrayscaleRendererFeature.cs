// =============================================================================
// 8차시 (1) Render Graph 커스텀 렌더 패스 — 가장 단순한 형태
//
// 사용법
//  1) Project 창에서 URP Renderer 에셋 선택 (예: PC_Renderer)
//  2) Add Renderer Feature > Course Grayscale
//  3) Shader 필드에 Course/08a_Grayscale 셰이더 지정
//
// 학습 목표
//  - ScriptableRendererFeature / ScriptableRenderPass의 역할 분리
//  - RecordRenderGraph에서 리소스를 "선언"하고 실행은 나중에 일어나는 구조
//  - renderGraph.AddBlitPass 유틸리티
//  - resourceData.cameraColor를 새 텍스처로 바꿔치기하는 패턴
//
// [중요] Unity 6.1 이후 URP 커스텀 패스는 Render Graph만 지원합니다.
//        인터넷의 Execute(ScriptableRenderContext, ref RenderingData) 예제는
//        모두 구버전이므로 컴파일되지 않음.
//
// 참고: Package Manager > Universal RP > Samples 탭의
//       "URP RenderGraph Samples"를 임포트하면 공식 코드를 참조해 볼 수 있음.
// =============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class GrayscaleRendererFeature : ScriptableRendererFeature
{
    [SerializeField] Shader shader;
    [SerializeField, Range(0f, 1f)] float intensity = 1f;
    [SerializeField, Range(0f, 2f)] float vignette = 0f;
    [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    Material material;
    GrayscalePass pass;

    static readonly int IdIntensity = Shader.PropertyToID("_Intensity");
    static readonly int IdVignette  = Shader.PropertyToID("_Vignette");

    public override void Create()
    {
        if (shader == null)
            return;

        // CoreUtils.CreateEngineMaterial: hideFlags 처리까지 해 주는 유틸
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new GrayscalePass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null)
            return;

        // 프리뷰 카메라나 리플렉션 프로브에는 적용하지 않음
        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
            return;

        material.SetFloat(IdIntensity, intensity);
        material.SetFloat(IdVignette, vignette);

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
    class GrayscalePass : ScriptableRenderPass
    {
        readonly Material mat;

        public GrayscalePass(Material material)
        {
            mat = material;

            // 카메라 컬러를 읽으면서 동시에 쓰기 때문에 중간 텍스처가 필요.
            // 이 플래그가 없으면 백버퍼로 직접 렌더할 때 읽기/쓰기 충돌 발생.
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // frameData: 이 프레임의 리소스와 카메라 정보 컨테이너
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            // 백버퍼가 활성 타겟이면 스킵 (읽기 불가)
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;

            // 임시 텍스처 서술자. 깊이는 필요 없고 MSAA도 끌 것.
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_CourseGrayscaleTarget", false);

            // source -> destination 으로 머티리얼을 적용하며 복사
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                source, destination, mat, shaderPass: 0);
            renderGraph.AddBlitPass(blitParams, "Course Grayscale");

            // 이후 패스들이 우리 결과를 카메라 컬러로 인식하게 바꿔치기
            resourceData.cameraColor = destination;
        }
    }
}
