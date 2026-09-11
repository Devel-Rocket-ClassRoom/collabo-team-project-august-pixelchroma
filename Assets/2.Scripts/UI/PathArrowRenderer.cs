using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PathArrowRenderer
{
    private static readonly List<GameObject> objects = new List<GameObject>();

    private const float PathWidth = 0.35f;
    private const float HeadWidth = 0.55f;
    private const float HeadLength = 0.45f;
    private const float PathY = 0.52f;
    private const int CurveSegments = 6;
    private const float CurveRadius = 0.35f;

    public static Coroutine ShowPathAnimated(
        MonoBehaviour runner, List<Vector2Int> path, Color color, float duration)
    {
        Clear();
        if (path == null || path.Count < 2) return null;

        var tilePositions = BuildTilePositions(path);
        var smoothPoints = BuildSmoothPath(tilePositions);
        if (smoothPoints.Count < 2) return null;

        Vector3 lastDir = (tilePositions[tilePositions.Count - 1]
            - tilePositions[tilePositions.Count - 2]);
        lastDir.y = 0f;
        lastDir = lastDir.normalized;

        Vector3 headBase = tilePositions[tilePositions.Count - 1]
            - lastDir * HeadLength * 0.6f;
        smoothPoints[smoothPoints.Count - 1] = headBase;

        return runner.StartCoroutine(
            AnimateDraw(smoothPoints, tilePositions[tilePositions.Count - 1],
                lastDir, color, duration));
    }

    public static void ShowPath(List<Vector2Int> path, Color color)
    {
        Clear();
        if (path == null || path.Count < 2) return;

        var tilePositions = BuildTilePositions(path);
        var smoothPoints = BuildSmoothPath(tilePositions);
        if (smoothPoints.Count < 2) return;

        Vector3 lastDir = (tilePositions[tilePositions.Count - 1]
            - tilePositions[tilePositions.Count - 2]);
        lastDir.y = 0f;
        lastDir = lastDir.normalized;

        Vector3 headBase = tilePositions[tilePositions.Count - 1]
            - lastDir * HeadLength * 0.6f;
        smoothPoints[smoothPoints.Count - 1] = headBase;

        CreateLine(smoothPoints, color);
        objects.Add(CreateArrowHead(
            tilePositions[tilePositions.Count - 1], lastDir, color));
    }

    private static IEnumerator AnimateDraw(
        List<Vector3> smoothPoints, Vector3 headPos,
        Vector3 headDir, Color color, float duration)
    {
        float[] distances = new float[smoothPoints.Count];
        distances[0] = 0f;
        for (int i = 1; i < smoothPoints.Count; i++)
        {
            distances[i] = distances[i - 1]
                + Vector3.Distance(smoothPoints[i - 1], smoothPoints[i]);
        }
        float totalLength = distances[smoothPoints.Count - 1];
        if (totalLength < 0.01f) yield break;

        GameObject lineObj = new GameObject("PathLine");
        objects.Add(lineObj);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = PathWidth;
        lr.endWidth = PathWidth;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 3;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.positionCount = 2;
        lr.SetPosition(0, smoothPoints[0]);
        lr.SetPosition(1, smoothPoints[0]);

        float elapsed = 0f;
        int lastPointIndex = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float targetDist = t * totalLength;

            int seg = lastPointIndex;
            while (seg < smoothPoints.Count - 1 && distances[seg + 1] < targetDist)
                seg++;
            lastPointIndex = Mathf.Max(lastPointIndex, seg > 0 ? seg - 1 : 0);

            float segStart = distances[seg];
            float segEnd = seg < smoothPoints.Count - 1
                ? distances[seg + 1] : distances[seg];
            float segLen = segEnd - segStart;
            float segT = segLen > 0.001f
                ? (targetDist - segStart) / segLen : 0f;

            Vector3 tip = seg < smoothPoints.Count - 1
                ? Vector3.Lerp(smoothPoints[seg], smoothPoints[seg + 1], segT)
                : smoothPoints[smoothPoints.Count - 1];

            int count = seg + 2;
            lr.positionCount = count;
            for (int i = 0; i <= seg; i++)
                lr.SetPosition(i, smoothPoints[i]);
            lr.SetPosition(count - 1, tip);

            yield return null;
        }

        lr.positionCount = smoothPoints.Count;
        for (int i = 0; i < smoothPoints.Count; i++)
            lr.SetPosition(i, smoothPoints[i]);

        objects.Add(CreateArrowHead(headPos, headDir, color));
    }

    private static List<Vector3> BuildTilePositions(List<Vector2Int> path)
    {
        GridManager grid = GridManager.Instance;
        var positions = new List<Vector3>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = grid.GridToWorldPosition(path[i].x, path[i].y);
            Tile tile = grid.GetTile(path[i]);
            p.y = (tile != null ? tile.HeightOffset : 0f) + PathY;
            positions.Add(p);
        }
        return positions;
    }

    private static GameObject CreateLine(List<Vector3> points, Color color)
    {
        GameObject lineObj = new GameObject("PathLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = PathWidth;
        lr.endWidth = PathWidth;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 3;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            lr.SetPosition(i, points[i]);

        objects.Add(lineObj);
        return lineObj;
    }

    private static List<Vector3> BuildSmoothPath(List<Vector3> points)
    {
        if (points.Count <= 2)
            return new List<Vector3>(points);

        var result = new List<Vector3>();
        result.Add(points[0]);

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 prev = points[i - 1];
            Vector3 curr = points[i];
            Vector3 next = points[i + 1];

            Vector3 dirIn = curr - prev;
            dirIn.y = 0f;
            dirIn.Normalize();
            Vector3 dirOut = next - curr;
            dirOut.y = 0f;
            dirOut.Normalize();

            float dot = Vector3.Dot(dirIn, dirOut);

            if (dot > 0.99f)
            {
                result.Add(curr);
                continue;
            }

            float halfDistPrev = Vector3.Distance(prev, curr) * 0.45f;
            float halfDistNext = Vector3.Distance(curr, next) * 0.45f;
            float radius = Mathf.Min(CurveRadius, Mathf.Min(halfDistPrev, halfDistNext));

            Vector3 curveStart = curr - dirIn * radius;
            Vector3 curveEnd = curr + dirOut * radius;
            curveStart.y = curr.y;
            curveEnd.y = curr.y;

            for (int s = 0; s <= CurveSegments; s++)
            {
                float t = (float)s / CurveSegments;
                Vector3 a = Vector3.Lerp(curveStart, curr, t);
                Vector3 b = Vector3.Lerp(curr, curveEnd, t);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = curr.y;
                result.Add(p);
            }
        }

        result.Add(points[points.Count - 1]);
        return result;
    }

    public static void Clear()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.Destroy(objects[i]);
        }
        objects.Clear();
    }

    private static GameObject CreateArrowHead(
        Vector3 position, Vector3 direction, Color color)
    {
        GameObject obj = new GameObject("PathArrowHead");
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        float hw = HeadWidth * 0.5f;
        float hl = HeadLength * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(0, 0, hl),
            new Vector3(-hw, 0, -hl),
            new Vector3(hw, 0, -hl)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return obj;
    }
}
