using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 선택한 아군을 공격할 수 있는 적에서 그 아군까지 공중 곡선을 그립니다.
/// </summary>
public class ThreatArcRenderer : MonoBehaviour
{
    private const int Segments = 28;
    private const float StartHeight = 1.1f;
    private const float EndHeight = 0.6f;
    private const float ArcWidth = 0.2f;

    private static readonly Color ArcColor = new Color32(0x94, 0x1B, 0x34, 0xFF);

    private readonly List<LineRenderer> arcs = new List<LineRenderer>();
    private Material arcMaterial;
    private int activeCount;

    public void Show(List<Unit> attackers, Unit target)
    {
        Clear();
        if (target == null || attackers == null) return;

        foreach (Unit attacker in attackers)
        {
            if (attacker == null || attacker.IsDead) continue;
            DrawArc(attacker.transform.position, target.transform.position);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < activeCount && i < arcs.Count; i++)
            if (arcs[i] != null) arcs[i].enabled = false;
        activeCount = 0;
    }

    private void DrawArc(Vector3 from, Vector3 to)
    {
        LineRenderer line = GetArc(activeCount++);
        Vector3 start = from + Vector3.up * StartHeight;
        Vector3 end = to + Vector3.up * EndHeight;

        // 곡선이 지형과 유닛 위로 지나가도록 거리에 비례해 솟구치게 합니다.
        Vector3 peak = (start + end) * 0.5f +
                       Vector3.up * (1.2f + Vector3.Distance(start, end) * 0.35f);

        for (int i = 0; i < Segments; i++)
        {
            float t = i / (float)(Segments - 1);
            Vector3 a = Vector3.Lerp(start, peak, t);
            Vector3 b = Vector3.Lerp(peak, end, t);
            line.SetPosition(i, Vector3.Lerp(a, b, t));
        }
        line.enabled = true;
    }

    private LineRenderer GetArc(int index)
    {
        while (arcs.Count <= index)
            arcs.Add(CreateArc(arcs.Count));
        return arcs[index];
    }

    private LineRenderer CreateArc(int index)
    {
        if (arcMaterial == null) arcMaterial = CreateArcMaterial();

        GameObject obj = new GameObject($"ThreatArc_{index}");
        obj.transform.SetParent(transform, false);

        LineRenderer line = obj.AddComponent<LineRenderer>();

        // 선마다 펄스 시작 시점을 어긋나게 해서 한꺼번에 번쩍이지 않게 합니다.
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetFloat("_Phase", (index * 0.37f) % 1f);
        line.SetPropertyBlock(block);

        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.positionCount = Segments;
        line.widthMultiplier = ArcWidth;
        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.sharedMaterial = arcMaterial;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    // 안쪽은 비우고 테두리만 선명한 홀로그램 라인입니다.
    private static Material CreateArcMaterial()
    {
        Shader shader = Shader.Find("Custom/HologramArc");
        if (shader == null)
        {
            Debug.LogWarning("[ThreatArc] Custom/HologramArc 셰이더를 찾지 못해 기본 라인으로 표시합니다.");
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", ArcColor);
        if (material.HasProperty("_EdgeColor")) material.SetColor("_EdgeColor", ArcColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", ArcColor);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }
}
