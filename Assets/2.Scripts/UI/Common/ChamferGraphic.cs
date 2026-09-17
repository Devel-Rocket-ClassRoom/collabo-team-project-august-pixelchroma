using UnityEngine;
using UnityEngine.UI;

// Panel/button shape with diagonally cut corners. borderWidth 0 draws a filled shape, otherwise an outline.
[RequireComponent(typeof(CanvasRenderer))]
public class ChamferGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float topLeft;
    [SerializeField, Min(0f)] private float topRight;
    [SerializeField, Min(0f)] private float bottomRight;
    [SerializeField, Min(0f)] private float bottomLeft;
    [SerializeField, Min(0f)] private float borderWidth;

    private static readonly Vector2[] Outer = new Vector2[8];
    private static readonly Vector2[] Inner = new Vector2[8];

    public float BorderWidth
    {
        get => borderWidth;
        set { borderWidth = Mathf.Max(0f, value); SetVerticesDirty(); }
    }

    public void SetCuts(float tl, float tr, float br, float bl)
    {
        topLeft = tl;
        topRight = tr;
        bottomRight = br;
        bottomLeft = bl;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f) return;

        BuildOutline(rect, 0f, Outer);
        if (borderWidth <= 0f)
        {
            vh.AddVert(rect.center, color, Vector2.zero);
            for (int i = 0; i < 8; i++)
                vh.AddVert(Outer[i], color, Vector2.zero);
            for (int i = 0; i < 8; i++)
                vh.AddTriangle(0, 1 + i, 1 + (i + 1) % 8);
            return;
        }

        float inset = Mathf.Min(borderWidth, Mathf.Min(rect.width, rect.height) * 0.5f);
        BuildOutline(rect, inset, Inner);
        for (int i = 0; i < 8; i++)
        {
            vh.AddVert(Outer[i], color, Vector2.zero);
            vh.AddVert(Inner[i], color, Vector2.zero);
        }
        for (int i = 0; i < 8; i++)
        {
            int o0 = i * 2, i0 = o0 + 1;
            int o1 = (i + 1) % 8 * 2, i1 = o1 + 1;
            vh.AddTriangle(o0, o1, i1);
            vh.AddTriangle(o0, i1, i0);
        }
    }

    private void BuildOutline(Rect rect, float inset, Vector2[] points)
    {
        float xMin = rect.xMin + inset, xMax = rect.xMax - inset;
        float yMin = rect.yMin + inset, yMax = rect.yMax - inset;
        float limit = Mathf.Max(0f, Mathf.Min(xMax - xMin, yMax - yMin) * 0.5f);
        // Insetting a 45° cut moves its endpoints in by inset * tan(22.5°).
        float shrink = inset * 0.4142f;
        float tl = Mathf.Clamp(topLeft - shrink, 0f, limit);
        float tr = Mathf.Clamp(topRight - shrink, 0f, limit);
        float br = Mathf.Clamp(bottomRight - shrink, 0f, limit);
        float bl = Mathf.Clamp(bottomLeft - shrink, 0f, limit);

        points[0] = new Vector2(xMin, yMax - tl);
        points[1] = new Vector2(xMin + tl, yMax);
        points[2] = new Vector2(xMax - tr, yMax);
        points[3] = new Vector2(xMax, yMax - tr);
        points[4] = new Vector2(xMax, yMin + br);
        points[5] = new Vector2(xMax - br, yMin);
        points[6] = new Vector2(xMin + bl, yMin);
        points[7] = new Vector2(xMin, yMin + bl);
    }
}
