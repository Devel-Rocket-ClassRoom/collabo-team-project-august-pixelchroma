using System.Collections.Generic;
using UnityEngine;

/// <summary>적이 한 턴에 취할 수 있는 행동의 종류입니다.</summary>
public enum AIActionKind
{
    Wait,
    Move,
    Attack
}

/// <summary>
/// 평가된 행동 후보 하나입니다.
/// destination 으로 이동한 뒤 kind 를 target 에게 수행하는 것을 의미합니다.
/// </summary>
public struct AICandidate
{
    public Vector2Int Destination;
    public AIActionKind Kind;
    public Unit Target;
    public int Score;

    /// <summary>지표별 점수 내역입니다. AI 판단 로그에 사용합니다.</summary>
    public List<AIScoreEntry> Breakdown;

    public bool IsMove => Destination != Origin;
    public Vector2Int Origin;

    public string Describe()
    {
        string place = $"({Destination.x},{Destination.y})";
        switch (Kind)
        {
            case AIActionKind.Attack:
                return $"{place} → 공격 → {DescribeTarget()}";
            case AIActionKind.Move:
                return $"{place} → 이동";
            default:
                return $"{place} → 대기";
        }
    }

    private string DescribeTarget()
    {
        if (Target == null) return "없음";
        if (Target.CharacterData != null &&
            !string.IsNullOrEmpty(Target.CharacterData.DisplayName))
            return Target.CharacterData.DisplayName;
        return Target.name;
    }
}

public struct AIScoreEntry
{
    public AIMetric Metric;
    public int Value;
    public string Reason;

    public AIScoreEntry(AIMetric metric, int value, string reason)
    {
        Metric = metric;
        Value = value;
        Reason = reason;
    }
}

/// <summary>
/// 적 AI의 의사결정 엔진입니다.
///
/// 행동 후보를 전부 생성하고, 외부 데이터(AIWeightProfile)로 정의된 가중치로
/// 점수를 매겨 최고점을 고릅니다. 난이도는 코드 분기가 아니라
/// "활성화된 지표가 몇 개인가"로만 구분됩니다.
/// </summary>
public static class EnemyAI
{
    /// <summary>
    /// 한 유닛의 최선 행동을 결정합니다.
    /// </summary>
    /// <param name="self">행동할 적 유닛</param>
    /// <param name="targets">표적 후보 (플레이어 유닛)</param>
    /// <param name="allies">같은 편 유닛. 대기형 판정과 자기 보존에 사용합니다.</param>
    /// <param name="squad">소속 단체. null이면 돌격형·쉬움으로 동작합니다.</param>
    /// <param name="doomed">이번 턴에 이미 처치가 확정된 표적. 표적 분산에 사용합니다.</param>
    /// <param name="topCandidates">로그용으로 상위 후보를 돌려받을 리스트. null 가능.</param>
    public static AICandidate Decide(
        Unit self,
        IReadOnlyList<Unit> targets,
        IReadOnlyList<Unit> allies,
        EnemySquadData squad,
        HashSet<Unit> doomed,
        List<AICandidate> topCandidates = null)
    {
        AICandidate fallback = new AICandidate
        {
            Origin = self.GridPosition,
            Destination = self.GridPosition,
            Kind = AIActionKind.Wait,
            Score = int.MinValue,
            Breakdown = new List<AIScoreEntry>()
        };

        if (self == null || self.IsDead) return fallback;

        AIWeightProfile weights = squad != null ? squad.WeightProfile : null;
        if (weights == null) return DecideWithoutProfile(self, targets, fallback);

        AIDifficulty difficulty = squad.Difficulty;
        AIMetric metrics = weights.GetMetrics(difficulty);
        SquadDoctrine doctrine = squad.GetDoctrineFor(self.EnemyData);

        // 대기형: 발동 반경 밖이면 움직이지 않습니다.
        if (doctrine == SquadDoctrine.Hold &&
            !IsAnyTargetWithin(self, targets, squad.ActivationRange))
        {
            fallback.Score = 0;
            fallback.Breakdown.Add(new AIScoreEntry(
                AIMetric.None, 0,
                $"대기형 — 반경 {squad.ActivationRange} 밖이라 유지"));
            topCandidates?.Add(fallback);
            return fallback;
        }

        List<AICandidate> candidates = GenerateCandidates(self, targets);
        if (candidates.Count == 0) return fallback;

        for (int i = 0; i < candidates.Count; i++)
        {
            AICandidate c = candidates[i];
            c.Score = Evaluate(
                self, c, weights, metrics, doctrine, squad, targets, allies, doomed);
            candidates[i] = c;
        }

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

        if (topCandidates != null)
        {
            int take = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < take; i++)
                topCandidates.Add(candidates[i]);
        }

        return candidates[0];
    }

    // ─────────────────── 후보 생성 ───────────────────

    /// <summary>
    /// (목적지, 행동, 대상) 조합을 전부 만듭니다.
    /// 5×6 그리드에서 유닛당 수십 개 규모라 전수 평가가 가능합니다.
    /// </summary>
    public static List<AICandidate> GenerateCandidates(
        Unit self, IReadOnlyList<Unit> targets)
    {
        var candidates = new List<AICandidate>();
        Vector2Int origin = self.GridPosition;

        // 이동 목적지 = 제자리 + 도달 가능한 칸
        var destinations = new List<Vector2Int> { origin };
        bool emplaced = self.EnemyData != null &&
                        self.EnemyData.Trait == EnemyTrait.Emplaced;

        if (!emplaced)
        {
            List<Vector2Int> reachable =
                Pathfinding.GetReachableTiles(origin, self.MoveRange);
            foreach (Vector2Int cell in reachable)
            {
                Tile tile = GridManager.Instance.GetTile(cell);
                if (tile != null && tile.IsWalkable())
                    destinations.Add(cell);
            }
        }

        int attackRange = GetAttackRange(self);

        foreach (Vector2Int destination in destinations)
        {
            bool foundAttack = false;

            foreach (Unit target in targets)
            {
                if (target == null || target.IsDead) continue;

                int distance = Manhattan(destination, target.GridPosition);
                if (distance > attackRange) continue;

                candidates.Add(new AICandidate
                {
                    Origin = origin,
                    Destination = destination,
                    Kind = AIActionKind.Attack,
                    Target = target,
                    Breakdown = new List<AIScoreEntry>()
                });
                foundAttack = true;
            }

            // 공격할 수 없는 칸은 순수 이동(또는 대기) 후보로 남깁니다.
            if (!foundAttack)
            {
                candidates.Add(new AICandidate
                {
                    Origin = origin,
                    Destination = destination,
                    Kind = destination == origin ? AIActionKind.Wait : AIActionKind.Move,
                    Target = null,
                    Breakdown = new List<AIScoreEntry>()
                });
            }
        }

        return candidates;
    }

    // ─────────────────── 평가 ───────────────────

    private static int Evaluate(
        Unit self,
        AICandidate candidate,
        AIWeightProfile w,
        AIMetric metrics,
        SquadDoctrine doctrine,
        EnemySquadData squad,
        IReadOnlyList<Unit> targets,
        IReadOnlyList<Unit> allies,
        HashSet<Unit> doomed)
    {
        int score = 0;
        List<AIScoreEntry> log = candidate.Breakdown;

        Unit target = candidate.Target;

        // ── 표적 가치 ────────────────────────────────
        if (target != null)
        {
            int roleScore;
            if (Has(metrics, AIMetric.RoleWeight))
            {
                roleScore = w.GetRoleWeight(target.Role);
                if (target.EnemyData != null)
                    roleScore += target.EnemyData.ThreatModifier;
                log.Add(new AIScoreEntry(AIMetric.RoleWeight, roleScore,
                    RoleLabel(target.Role)));
            }
            else
            {
                roleScore = w.FlatWeightOnEasy;
                log.Add(new AIScoreEntry(AIMetric.RoleWeight, roleScore,
                    "역할 무시 (쉬움)"));
            }
            score += roleScore;

            AttackForecast forecast =
                CombatResolver.Forecast(self, target, candidate.Destination);

            int damage = forecast.CoverBlocks ? 0 : forecast.Damage;

            // 명중률이 낮은 공격은 그만큼 가치가 떨어집니다.
            if (!forecast.CoverBlocks && forecast.HitChance < 100)
            {
                int miss = -(100 - forecast.HitChance) / 2;
                score += miss;
                log.Add(new AIScoreEntry(AIMetric.None, miss,
                    $"명중 {forecast.HitChance}%"));
            }

            if (Has(metrics, AIMetric.KillBonus) && damage >= target.HP && damage > 0)
            {
                score += w.KillBonus;
                log.Add(new AIScoreEntry(AIMetric.KillBonus, w.KillBonus,
                    $"{damage}피해 ≥ 잔여 HP {target.HP}"));
            }

            if (Has(metrics, AIMetric.HpRatio) && target.MaxHP > 0)
            {
                float lost = 1f - (float)target.HP / target.MaxHP;
                int value = Mathf.RoundToInt(lost * w.HpRatioScale);
                if (value != 0)
                {
                    score += value;
                    log.Add(new AIScoreEntry(AIMetric.HpRatio, value,
                        $"체력 {target.HP}/{target.MaxHP}"));
                }
            }

            if (Has(metrics, AIMetric.Affinity))
            {
                AttackType atk = self.UnitAttackType;
                ArmorType arm = target.UnitArmorType;
                if (TypeAffinity.IsEffective(atk, arm))
                {
                    score += w.AffinityBonus;
                    log.Add(new AIScoreEntry(AIMetric.Affinity, w.AffinityBonus,
                        $"{atk} → {arm} 유효"));
                }
                else if (TypeAffinity.IsResisted(atk, arm))
                {
                    score -= w.AffinityPenalty;
                    log.Add(new AIScoreEntry(AIMetric.Affinity, -w.AffinityPenalty,
                        $"{atk} → {arm} 저항"));
                }
            }

            if (Has(metrics, AIMetric.FocusPenalty) &&
                doomed != null && doomed.Contains(target))
            {
                score -= w.FocusPenalty;
                log.Add(new AIScoreEntry(AIMetric.FocusPenalty, -w.FocusPenalty,
                    "이미 처치 확정된 표적"));
            }

            // 엄폐물에 막히면 표적을 때릴 수 없습니다.
            if (forecast.CoverBlocks)
            {
                score -= w.KillBonus;
                log.Add(new AIScoreEntry(AIMetric.None, -w.KillBonus,
                    "엄폐물에 사선 차단됨"));
            }
        }

        // ── 도달 가능성 ──────────────────────────────
        if (Has(metrics, AIMetric.Distance))
        {
            int shortfall = GetApproachShortfall(self, candidate, targets);
            if (shortfall > 0)
            {
                int penalty = shortfall * w.DistancePenalty;
                score -= penalty;
                log.Add(new AIScoreEntry(AIMetric.Distance, -penalty,
                    $"표적까지 {shortfall}칸 부족"));
            }
        }

        // ── 반격 위험 ────────────────────────────────
        if (Has(metrics, AIMetric.CounterRisk))
        {
            int risk = PredictIncomingDamage(self, candidate, targets);
            if (risk > 0)
            {
                int penalty = risk * w.CounterRiskScale;
                score -= penalty;
                log.Add(new AIScoreEntry(AIMetric.CounterRisk, -penalty,
                    $"반격·피격 {risk}피해 예상"));
            }
        }

        // ── 지형 이점 ────────────────────────────────
        if (Has(metrics, AIMetric.TerrainBonus))
        {
            Tile tile = GridManager.Instance.GetTile(candidate.Destination);
            if (tile != null && tile.Terrain == TileTerrain.HighGround)
            {
                score += w.HighGroundBonus;
                log.Add(new AIScoreEntry(AIMetric.TerrainBonus, w.HighGroundBonus,
                    "목적지가 고지대"));
            }
            else if (IsBehindCover(candidate.Destination, targets))
            {
                score += w.CoverBonus;
                log.Add(new AIScoreEntry(AIMetric.TerrainBonus, w.CoverBonus,
                    "엄폐물 뒤"));
            }
        }

        // ── 자기 보존 (지휘형 후퇴) ──────────────────
        if (Has(metrics, AIMetric.SelfPreserve) &&
            doctrine == SquadDoctrine.Command &&
            self.MaxHP > 0 &&
            (float)self.HP / self.MaxHP <= squad.RetreatHpRatio)
        {
            int before = MinDistanceToTargets(self.GridPosition, targets);
            int after = MinDistanceToTargets(candidate.Destination, targets);
            int approach = before - after;   // 양수면 접근한 것
            if (approach > 0)
            {
                int penalty = approach * w.SelfPreserveScale;
                score -= penalty;
                log.Add(new AIScoreEntry(AIMetric.SelfPreserve, -penalty,
                    $"후퇴 임계 이하인데 {approach}칸 접근"));
            }
        }

        // ── 교리 보정 ────────────────────────────────
        int doctrineBonus = ApplyDoctrine(self, candidate, doctrine, targets, w);
        if (doctrineBonus != 0)
        {
            score += doctrineBonus;
            log.Add(new AIScoreEntry(AIMetric.None, doctrineBonus,
                $"{DoctrineLabel(doctrine)} 교리"));
        }

        return score;
    }

    /// <summary>교리별 성향 보정입니다. 같은 상황에서도 단체마다 다르게 움직이게 합니다.</summary>
    private static int ApplyDoctrine(
        Unit self,
        AICandidate candidate,
        SquadDoctrine doctrine,
        IReadOnlyList<Unit> targets,
        AIWeightProfile w)
    {
        switch (doctrine)
        {
            case SquadDoctrine.Assault:
                // 접근을 선호합니다.
                {
                    int before = MinDistanceToTargets(self.GridPosition, targets);
                    int after = MinDistanceToTargets(candidate.Destination, targets);
                    return (before - after) * 8;
                }

            case SquadDoctrine.Snipe:
                // 사거리를 유지하며 붙는 것을 꺼립니다.
                {
                    int after = MinDistanceToTargets(candidate.Destination, targets);
                    int ideal = GetAttackRange(self);
                    return -Mathf.Abs(ideal - after) * 10;
                }

            case SquadDoctrine.Hold:
                // 제자리를 선호합니다.
                return candidate.Destination == self.GridPosition ? 15 : 0;

            case SquadDoctrine.Command:
                // 후열 유지를 선호합니다.
                {
                    int after = MinDistanceToTargets(candidate.Destination, targets);
                    return Mathf.Min(after, 5) * 6;
                }
        }
        return 0;
    }

    // ─────────────────── 보조 계산 ───────────────────

    /// <summary>가중치 프로필이 없을 때의 최소 동작입니다. 기존 최근접 추적과 동일합니다.</summary>
    private static AICandidate DecideWithoutProfile(
        Unit self, IReadOnlyList<Unit> targets, AICandidate fallback)
    {
        List<AICandidate> candidates = GenerateCandidates(self, targets);
        if (candidates.Count == 0) return fallback;

        AICandidate best = fallback;
        int bestScore = int.MinValue;

        foreach (AICandidate c in candidates)
        {
            int score = c.Kind == AIActionKind.Attack ? 100 : 0;
            score -= MinDistanceToTargets(c.Destination, targets) * 10;
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        best.Score = bestScore;
        return best;
    }

    /// <summary>
    /// 이 공격의 기대 피해입니다. 명중률을 반영한 기대값이라
    /// 명중률이 낮은 공격은 자연히 낮게 평가됩니다.
    /// </summary>
    public static int PredictDamage(Unit attacker, Unit target)
        => PredictDamage(attacker, target,
            attacker != null ? attacker.GridPosition : default);

    public static int PredictDamage(
        Unit attacker, Unit target, Vector2Int fromPosition)
    {
        if (attacker == null || target == null) return 0;

        AttackForecast f = CombatResolver.Forecast(attacker, target, fromPosition);
        if (!f.Valid || f.CoverBlocks) return 0;
        return f.Damage;
    }

    /// <summary>
    /// 이 칸에서 공격했을 때 받게 될 위험입니다.
    /// 즉시 돌아오는 반격 + 다음 턴에 사거리 안에 들어가 맞을 피해를 함께 봅니다.
    /// </summary>
    private static int PredictIncomingDamage(
        Unit self, AICandidate candidate, IReadOnlyList<Unit> targets)
    {
        int total = 0;
        Vector2Int destination = candidate.Destination;

        // ── 즉시 반격 ────────────────────────────
        if (candidate.Kind == AIActionKind.Attack && candidate.Target != null)
        {
            AttackForecast f = CombatResolver.Forecast(
                self, candidate.Target, destination);
            if (f.CounterPossible)
                total += Mathf.RoundToInt(
                    f.CounterDamage * (f.CounterHitChance / 100f));
        }

        // ── 다음 턴 피격 위험 ────────────────────
        foreach (Unit target in targets)
        {
            if (target == null || target.IsDead) continue;
            int distance = Manhattan(destination, target.GridPosition);
            if (distance > GetAttackRange(target)) continue;
            total += TypeAffinity.ApplyToDamage(
                target.AttackPower, target.UnitAttackType, self.UnitArmorType);
        }

        return total;
    }

    /// <summary>표적에 닿기까지 몇 칸이 모자란지 반환합니다. 0이면 이번 턴에 공격 가능합니다.</summary>
    private static int GetApproachShortfall(
        Unit self, AICandidate candidate, IReadOnlyList<Unit> targets)
    {
        if (candidate.Kind == AIActionKind.Attack) return 0;

        int nearest = MinDistanceToTargets(candidate.Destination, targets);
        if (nearest >= NoTargetDistance) return 0;

        int reach = GetAttackRange(self);
        return Mathf.Max(0, nearest - reach);
    }

    private static bool IsAnyTargetWithin(
        Unit self, IReadOnlyList<Unit> targets, int range)
    {
        return MinDistanceToTargets(self.GridPosition, targets) <= range;
    }

    /// <summary>
    /// 표적이 하나도 없을 때 int.MaxValue를 돌려주면 교리 보정의 뺄셈에서
    /// 오버플로가 나므로, 그리드 크기를 넘는 유한한 값을 씁니다.
    /// </summary>
    public const int NoTargetDistance = 99;

    private static int MinDistanceToTargets(
        Vector2Int from, IReadOnlyList<Unit> targets)
    {
        int min = NoTargetDistance;
        foreach (Unit target in targets)
        {
            if (target == null || target.IsDead) continue;
            int distance = Manhattan(from, target.GridPosition);
            if (distance < min) min = distance;
        }
        return min;
    }

    /// <summary>
    /// 고지대 보정을 반영한 실효 사거리입니다.
    /// 전투 규칙은 CombatResolver 한 곳에만 두고 여기서는 위임만 합니다.
    /// </summary>
    public static int GetAttackRange(Unit unit)
        => CombatResolver.GetEffectiveRange(unit);

    /// <summary>표적 방향에 엄폐물을 끼고 있는 자리인지 확인합니다.</summary>
    private static bool IsBehindCover(
        Vector2Int destination, IReadOnlyList<Unit> targets)
    {
        foreach (Unit target in targets)
        {
            if (target == null || target.IsDead) continue;
            if (CombatResolver.FindBlockingCover(
                    destination, target.GridPosition) != null)
                return true;
        }
        return false;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static bool Has(AIMetric mask, AIMetric metric)
        => (mask & metric) != 0;

    private static string RoleLabel(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Tank: return "탱커";
            case EnemyRole.MeleeDealer: return "근접딜러";
            case EnemyRole.RangedDealer: return "원거리딜러";
            case EnemyRole.Healer: return "힐러";
            case EnemyRole.Supporter: return "서포터";
            default: return role.ToString();
        }
    }

    private static string DoctrineLabel(SquadDoctrine doctrine)
    {
        switch (doctrine)
        {
            case SquadDoctrine.Assault: return "돌격";
            case SquadDoctrine.Hold: return "대기";
            case SquadDoctrine.Snipe: return "저격";
            case SquadDoctrine.Command: return "지휘";
            default: return doctrine.ToString();
        }
    }
}
