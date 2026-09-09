// =============================================================================
// 7차시 GPU 파티클 시스템 (C# 측)
//
// 사용법
//  1) 빈 GameObject에 이 스크립트를 붙일 것
//  2) Compute Shader  : 07_GpuParticles.compute
//  3) Material        : 07_ParticleRender 셰이더로 만든 머티리얼
//  4) Play. 마우스를 움직이면 인력 중심이 따라옴
//
// 학습 목표
//  - GraphicsBuffer 생성/해제와 stride 계산
//  - 스레드 그룹 수 계산: ceil(count / 64)
//  - Graphics.RenderPrimitives로 메시 없이 그리기
//  - CPU-GPU 왕복(GetData)을 왜 피해야 하는가
//
// 실습
//  - particleCount를 100,000 -> 1,000,000으로 올려 GPU ms 측정
//  - CPU 파티클(Update에서 Vector3 배열 순회 + 개별 Transform)과 비교
// =============================================================================
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

public class GpuParticleSystem : MonoBehaviour
{
    // GPU 측 struct와 바이트 레이아웃이 정확히 같아야 함.
    // float3(12) + float(4) + float3(12) + float(4) = 32 bytes
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector3 position;
        public float   life;
        public Vector3 velocity;
        public float   seed;
    }

    const int ThreadGroupSize = 64;   // .compute의 numthreads(64,1,1)과 일치

    [Header("Resources")]
    [SerializeField] ComputeShader compute;
    [SerializeField] Material material;

    [Header("Count")]
    [SerializeField, Range(1024, 1000000)] int particleCount = 100000;

    [Header("Spawn")]
    [SerializeField] float spawnRadius = 3f;
    [SerializeField] float initialSpeed = 2f;
    [SerializeField] float lifeSpan = 4f;

    [Header("Forces")]
    [SerializeField] float attractorStrength = 12f;
    [SerializeField] Vector3 gravity = new Vector3(0f, -1.5f, 0f);
    [SerializeField, Range(0f, 5f)] float damping = 0.4f;
    [SerializeField, Range(0f, 5f)] float noiseStrength = 1.2f;
    [SerializeField] float attractorDistanceFromCamera = 12f;

    [Header("Bounds (컬링용)")]
    [SerializeField] float boundsSize = 200f;

    GraphicsBuffer buffer;
    int kernelInit, kernelSimulate;
    int builtCount;

    // Shader property ID는 미리 캐싱 (문자열 해싱 비용 제거)
    static readonly int IdParticles          = Shader.PropertyToID("_Particles");
    static readonly int IdParticleCount      = Shader.PropertyToID("_ParticleCount");
    static readonly int IdDeltaTime          = Shader.PropertyToID("_DeltaTime");
    static readonly int IdElapsedTime        = Shader.PropertyToID("_ElapsedTime");
    static readonly int IdSpawnCenter        = Shader.PropertyToID("_SpawnCenter");
    static readonly int IdSpawnRadius        = Shader.PropertyToID("_SpawnRadius");
    static readonly int IdInitialSpeed       = Shader.PropertyToID("_InitialSpeed");
    static readonly int IdLifeSpan           = Shader.PropertyToID("_LifeSpan");
    static readonly int IdAttractorPosition  = Shader.PropertyToID("_AttractorPosition");
    static readonly int IdAttractorStrength  = Shader.PropertyToID("_AttractorStrength");
    static readonly int IdGravity            = Shader.PropertyToID("_Gravity");
    static readonly int IdDamping            = Shader.PropertyToID("_Damping");
    static readonly int IdNoiseStrength      = Shader.PropertyToID("_NoiseStrength");

    void OnEnable()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("[GpuParticleSystem] 이 플랫폼은 Compute Shader를 지원하지 않습니다.");
            enabled = false;
            return;
        }
        Allocate();
    }

    void OnDisable() => Release();

    void Allocate()
    {
        Release();

        if (compute == null || material == null)
        {
            Debug.LogWarning("[GpuParticleSystem] Compute Shader와 Material을 지정하세요.");
            return;
        }

        kernelInit     = compute.FindKernel("InitParticles");
        kernelSimulate = compute.FindKernel("SimulateParticles");

        int stride = Marshal.SizeOf<Particle>();   // 32
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, stride);
        builtCount = particleCount;

        PushCommonParams();

        compute.SetBuffer(kernelInit, IdParticles, buffer);
        compute.Dispatch(kernelInit, GroupCount(particleCount), 1, 1);

        material.SetBuffer(IdParticles, buffer);
    }

    void Release()
    {
        buffer?.Release();
        buffer = null;
    }

    static int GroupCount(int count) =>
        Mathf.Max(1, (count + ThreadGroupSize - 1) / ThreadGroupSize);

    void PushCommonParams()
    {
        compute.SetInt(IdParticleCount, particleCount);
        compute.SetVector(IdSpawnCenter, transform.position);
        compute.SetFloat(IdSpawnRadius, spawnRadius);
        compute.SetFloat(IdInitialSpeed, initialSpeed);
        compute.SetFloat(IdLifeSpan, lifeSpan);
        compute.SetVector(IdGravity, gravity);
        compute.SetFloat(IdDamping, damping);
        compute.SetFloat(IdNoiseStrength, noiseStrength);
    }

    void Update()
    {
        // 인스펙터에서 개수를 바꿨으면 재할당
        if (buffer == null || builtCount != particleCount)
        {
            Allocate();
            if (buffer == null) return;
        }

        Simulate();
        Render();
    }

    void Simulate()
    {
        PushCommonParams();
        compute.SetFloat(IdDeltaTime, Time.deltaTime);
        compute.SetFloat(IdElapsedTime, Time.time);
        compute.SetVector(IdAttractorPosition, GetAttractorPosition());
        compute.SetFloat(IdAttractorStrength, attractorStrength);

        compute.SetBuffer(kernelSimulate, IdParticles, buffer);
        compute.Dispatch(kernelSimulate, GroupCount(particleCount), 1, 1);
    }

    Vector3 GetAttractorPosition()
    {
        var cam = Camera.main;
        if (cam == null) return transform.position;

        // 마우스 위치를 카메라 앞 일정 거리의 월드 좌표로 변환
        Vector3 mouse = Mouse.current.position.ReadValue();
        mouse.z = attractorDistanceFromCamera;
        return cam.ScreenToWorldPoint(mouse);
    }

    void Render()
    {
        var rp = new RenderParams(material)
        {
            worldBounds = new Bounds(transform.position, Vector3.one * boundsSize),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            layer = gameObject.layer
        };

        // 파티클 1개 = 정점 6개 (삼각형 2개)
        // 메시도, GameObject도 없이 그립니다.
        Graphics.RenderPrimitives(rp, MeshTopology.Triangles, particleCount * 6);
    }
}
