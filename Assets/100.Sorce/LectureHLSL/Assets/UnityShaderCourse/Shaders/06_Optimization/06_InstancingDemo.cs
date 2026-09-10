// =============================================================================
// 6차시 실습용: 배칭 / 인스턴싱 비교 스크립트
//
// 사용법
//  1) 빈 GameObject에 이 스크립트를 붙입니다
//  2) Material에 Course/06_VariantsAndInstancing 머티리얼을 지정
//  3) Play 후 Mode를 바꿔가며 Frame Debugger(Window > Analysis > Frame Debugger)
//     에서 드로우콜 개수와 배칭 이유를 확인합니다
//
// 관찰 포인트
//  - SameMaterial      : SRP Batcher가 하나의 큰 배치로 처리
//  - PerInstanceColor  : per-instance 프로퍼티 -> GPU Instancing 경로
//  - SeparateMaterials : 머티리얼이 모두 달라 배칭 불가 -> 드로우콜 폭증
//
// Frame Debugger에서 배치를 클릭하면 "Why this draw call can't be batched with
// the previous one" 설명이 나옵니다. 이 문장을 읽는 훈련이 핵심입니다.
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

public class InstancingDemo : MonoBehaviour
{
    public enum Mode
    {
        SameMaterial,       // 전부 동일 머티리얼
        PerInstanceColor,   // MaterialPropertyBlock으로 인스턴스별 색상
        SeparateMaterials   // 인스턴스마다 머티리얼 복제 (최악의 경우)
    }

    [SerializeField] Mode mode = Mode.SameMaterial;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
    [SerializeField, Range(1, 5000)] int count = 500;
    [SerializeField] float spread = 30f;
    [SerializeField] int randomSeed = 12345;

    static readonly int InstanceColorId = Shader.PropertyToID("_InstanceColor");

    readonly List<GameObject> spawned = new();
    readonly List<Material> createdMaterials = new();
    Mode builtMode;
    int builtCount;

    void OnEnable() => Rebuild();
    void OnDisable() => Clear();

    void Update()
    {
        // 인스펙터에서 값을 바꾸면 즉시 재생성
        if (mode != builtMode || count != builtCount)
            Rebuild();
    }

    void Rebuild()
    {
        Clear();

        if (material == null)
        {
            Debug.LogWarning("[InstancingDemo] Material을 지정하세요.");
            return;
        }
        if (mesh == null)
            mesh = DefaultCubeMesh();

        var rng = new System.Random(randomSeed);
        var mpb = new MaterialPropertyBlock();

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Instance_{i}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                (float)(rng.NextDouble() - 0.5) * spread,
                (float)(rng.NextDouble() - 0.5) * spread * 0.3f,
                (float)(rng.NextDouble() - 0.5) * spread);
            go.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();

            var color = Color.HSVToRGB((float)rng.NextDouble(), 0.7f, 1f);

            switch (mode)
            {
                case Mode.SameMaterial:
                    mr.sharedMaterial = material;
                    break;

                case Mode.PerInstanceColor:
                    mr.sharedMaterial = material;
                    mpb.Clear();
                    mpb.SetColor(InstanceColorId, color);
                    mr.SetPropertyBlock(mpb);
                    break;

                case Mode.SeparateMaterials:
                    var m = new Material(material);
                    m.SetColor(InstanceColorId, color);
                    createdMaterials.Add(m);
                    mr.sharedMaterial = m;
                    break;
            }

            spawned.Add(go);
        }

        builtMode = mode;
        builtCount = count;
        Debug.Log($"[InstancingDemo] mode={mode}, count={count}. " +
                  "Frame Debugger에서 드로우콜을 확인하세요.");
    }

    void Clear()
    {
        foreach (var go in spawned)
            if (go != null) DestroyImmediate(go);
        spawned.Clear();

        foreach (var m in createdMaterials)
            if (m != null) DestroyImmediate(m);
        createdMaterials.Clear();
    }

    static Mesh DefaultCubeMesh()
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(temp);
        return mesh;
    }
}
