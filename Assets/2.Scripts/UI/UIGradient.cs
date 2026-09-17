using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color(0, 0, 0, 0);
    [SerializeField] private Color bottomColor = new Color(0, 0, 0, 1);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        UIVertex vertex = default;
        float yMin = float.MaxValue;
        float yMax = float.MinValue;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y < yMin) yMin = vertex.position.y;
            if (vertex.position.y > yMax) yMax = vertex.position.y;
        }

        float range = yMax - yMin;
        if (range <= 0f) return;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = (vertex.position.y - yMin) / range;
            vertex.color = Color32.Lerp(bottomColor, topColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
