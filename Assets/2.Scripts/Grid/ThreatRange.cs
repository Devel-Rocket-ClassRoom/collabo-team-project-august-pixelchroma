using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 공격 범위를 계산합니다. 이동은 포함하지 않고, 지금 서 있는 자리에서 닿는 칸만 봅니다.
/// 값은 그 칸을 노리는 적의 수이며, 표시 농도로 쓰입니다.
/// </summary>
public static class ThreatRange
{
    public static void Calculate(IReadOnlyList<Unit> enemies, Dictionary<Vector2Int, int> result)
    {
        result.Clear();

        GridManager grid = GridManager.Instance;
        if (grid == null || enemies == null) return;

        foreach (Unit enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            int range = CombatResolver.GetEffectiveRange(enemy);
            Vector2Int origin = enemy.GridPosition;

            for (int dx = -range; dx <= range; dx++)
            {
                int remain = range - Mathf.Abs(dx);
                for (int dy = -remain; dy <= remain; dy++)
                {
                    Vector2Int position = new Vector2Int(origin.x + dx, origin.y + dy);
                    if (position == origin || !grid.IsValidPosition(position)) continue;

                    result[position] = result.TryGetValue(position, out int count) ? count + 1 : 1;
                }
            }
        }
    }

    /// <summary>해당 칸을 지금 공격할 수 있는 적들을 모읍니다.</summary>
    public static void GetAttackersOf(
        IReadOnlyList<Unit> enemies, Vector2Int position, List<Unit> result)
    {
        result.Clear();
        if (enemies == null) return;

        foreach (Unit enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            int distance = Mathf.Abs(enemy.GridPosition.x - position.x) +
                           Mathf.Abs(enemy.GridPosition.y - position.y);
            if (distance > 0 && distance <= CombatResolver.GetEffectiveRange(enemy))
                result.Add(enemy);
        }
    }
}
