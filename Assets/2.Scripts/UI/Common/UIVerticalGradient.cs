using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIVerticalGradient : BaseMeshEffect
{
    [SerializeField] private Color top = Color.white;
    [SerializeField] private Color bottom = Color.white;

    public void SetColors(Color topColor, Color bottomColor)
    {
        top = topColor;
        bottom = bottomColor;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex vertex = default;
        float yMin = float.MaxValue, yMax = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            yMin = Mathf.Min(yMin, vertex.position.y);
            yMax = Mathf.Max(yMax, vertex.position.y);
        }

        float range = yMax - yMin;
        if (range <= 0f) return;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = (vertex.position.y - yMin) / range;
            vertex.color = (Color)vertex.color * Color.Lerp(bottom, top, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
