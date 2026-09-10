using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 적이 어떤 후보를 검토했고 왜 그 행동을 골랐는지 남기는 기록입니다.
/// 미션 가이드의 "AI 판단 로그" 항목에 해당하며,
/// 개발 중 디버깅과 최종 발표 설명에 사용합니다.
/// </summary>
public static class EnemyAILog
{
    public struct Entry
    {
        public int Turn;
        public string UnitName;
        public string SquadName;
        public AIDifficulty Difficulty;
        public List<AICandidate> Candidates;
        public string Text;
    }

    private static readonly List<Entry> entries = new List<Entry>();

    /// <summary>기록을 유지할 최대 개수입니다. 오래된 것부터 버립니다.</summary>
    public const int MaxEntries = 200;

    /// <summary>true면 기록할 때마다 Console에도 출력합니다.</summary>
    public static bool EchoToConsole = true;

    public static IReadOnlyList<Entry> Entries => entries;

    public static void Record(
        int turn,
        Unit unit,
        EnemySquadData squad,
        List<AICandidate> candidates)
    {
        if (unit == null || candidates == null || candidates.Count == 0) return;

        string unitName = DescribeUnit(unit);
        string squadName = squad != null ? squad.SquadName : "(단체 없음)";
        AIDifficulty difficulty = squad != null ? squad.Difficulty : AIDifficulty.Easy;

        string text = Format(turn, unitName, squadName, difficulty, candidates);

        entries.Add(new Entry
        {
            Turn = turn,
            UnitName = unitName,
            SquadName = squadName,
            Difficulty = difficulty,
            Candidates = candidates,
            Text = text
        });

        while (entries.Count > MaxEntries)
            entries.RemoveAt(0);

        if (EchoToConsole)
            Debug.Log(text);
    }

    private static string Format(
        int turn,
        string unitName,
        string squadName,
        AIDifficulty difficulty,
        List<AICandidate> candidates)
    {
        var sb = new StringBuilder();
        sb.Append("[AI] ").Append(squadName).Append(" · ").Append(unitName)
          .Append(" — ").Append(turn).Append("턴 (")
          .Append(DifficultyLabel(difficulty)).AppendLine(")");

        for (int i = 0; i < candidates.Count; i++)
        {
            AICandidate c = candidates[i];
            string mark = i == 0 ? "✔ 선택" : "";
            sb.Append("  #").Append(i + 1).Append("  ")
              .Append(c.Describe().PadRight(30))
              .Append(" 점수 ").Append(c.Score.ToString().PadLeft(5))
              .Append("  ").AppendLine(mark);

            if (c.Breakdown == null) continue;
            foreach (AIScoreEntry entry in c.Breakdown)
            {
                string sign = entry.Value >= 0 ? "+" : "";
                sb.Append("        ")
                  .Append(MetricLabel(entry.Metric).PadRight(14))
                  .Append((sign + entry.Value).PadLeft(6))
                  .Append("   ").AppendLine(entry.Reason);
            }
        }

        return sb.ToString();
    }

    private static string DescribeUnit(Unit unit)
    {
        if (unit.CharacterData != null &&
            !string.IsNullOrEmpty(unit.CharacterData.DisplayName))
            return unit.CharacterData.DisplayName;
        return unit.name;
    }

    private static string DifficultyLabel(AIDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy: return "쉬움";
            case AIDifficulty.Normal: return "보통";
            case AIDifficulty.Hard: return "어려움";
            default: return difficulty.ToString();
        }
    }

    private static string MetricLabel(AIMetric metric)
    {
        switch (metric)
        {
            case AIMetric.Distance: return "distance";
            case AIMetric.RoleWeight: return "roleWeight";
            case AIMetric.KillBonus: return "killBonus";
            case AIMetric.HpRatio: return "hpRatio";
            case AIMetric.Affinity: return "affinity";
            case AIMetric.CounterRisk: return "counterRisk";
            case AIMetric.TerrainBonus: return "terrainBonus";
            case AIMetric.FocusPenalty: return "focusPenalty";
            case AIMetric.SelfPreserve: return "selfPreserve";
            default: return "-";
        }
    }

    public static void Clear() => entries.Clear();

    /// <summary>전체 기록을 텍스트로 반환합니다. 발표 자료용 덤프에 사용합니다.</summary>
    public static string DumpAll()
    {
        var sb = new StringBuilder();
        foreach (Entry entry in entries)
            sb.AppendLine(entry.Text);
        return sb.ToString();
    }
}
