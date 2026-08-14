using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static List<Vector2Int> GetReachableTiles(Vector2Int start, int range)
    {
        var reachable = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        visited.Add(start);
        queue.Enqueue((start, 0));

        GridManager grid = GridManager.Instance;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Vector2Int pos = current.pos;
            int cost = current.cost;

            if (cost > 0)
                reachable.Add(pos);

            if (cost >= range)
                continue;

            foreach (var dir in Directions)
            {
                Vector2Int next = pos + dir;
                if (visited.Contains(next)) continue;
                if (!grid.IsValidPosition(next)) continue;

                Tile tile = grid.GetTile(next);
                if (tile == null || !tile.IsWalkable()) continue;

                visited.Add(next);
                queue.Enqueue((next, cost + 1));
            }
        }

        return reachable;
    }

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        GridManager grid = GridManager.Instance;
        if (!grid.IsValidPosition(start) || !grid.IsValidPosition(goal))
            return null;

        var openSet = new List<Node>();
        var closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start, null, 0, ManhattanDistance(start, goal));
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            int lowestIndex = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].F < openSet[lowestIndex].F)
                    lowestIndex = i;
            }

            Node current = openSet[lowestIndex];
            openSet.RemoveAt(lowestIndex);

            if (current.Position == goal)
                return ReconstructPath(current);

            closedSet.Add(current.Position);

            foreach (var dir in Directions)
            {
                Vector2Int neighborPos = current.Position + dir;

                if (closedSet.Contains(neighborPos)) continue;
                if (!grid.IsValidPosition(neighborPos)) continue;

                Tile tile = grid.GetTile(neighborPos);
                if (tile == null) continue;
                if (neighborPos != goal && !tile.IsWalkable()) continue;

                int newG = current.G + 1;

                Node existing = openSet.Find(n => n.Position == neighborPos);
                if (existing != null)
                {
                    if (newG < existing.G)
                    {
                        existing.G = newG;
                        existing.Parent = current;
                    }
                }
                else
                {
                    int h = ManhattanDistance(neighborPos, goal);
                    openSet.Add(new Node(neighborPos, current, newG, h));
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructPath(Node endNode)
    {
        var path = new List<Vector2Int>();
        Node current = endNode;

        while (current.Parent != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int G;
        public int H;
        public int F => G + H;

        public Node(Vector2Int position, Node parent, int g, int h)
        {
            Position = position;
            Parent = parent;
            G = g;
            H = h;
        }
    }
}
