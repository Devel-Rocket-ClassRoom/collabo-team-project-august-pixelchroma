using UnityEngine;

/// <summary>공격 한 번의 예측값입니다. 난수를 소비하지 않으므로 몇 번이든 호출해도 안전합니다.</summary>
public struct AttackForecast
{
    public bool Valid;

    /// <summary>사선이 엄폐물에 막혀 있습니다. 이 경우 피해 대신 엄폐물이 1회 소모됩니다.</summary>
    public bool CoverBlocks;

    public int HitChance;
    public int CriticalChance;

    /// <summary>명중 시 피해입니다.</summary>
    public int Damage;

    /// <summary>치명타 시 피해입니다.</summary>
    public int CriticalDamage;

    public bool IsEffective;
    public bool IsResisted;

    /// <summary>명중 시 대상이 죽는지 여부입니다.</summary>
    public bool IsLethal;

    public bool CounterPossible;
    public int CounterHitChance;
    public int CounterDamage;

    /// <summary>반격으로 공격자가 죽을 수 있는지 여부입니다.</summary>
    public bool CounterIsLethal;
}

/// <summary>실제 판정 결과입니다. 난수를 소비합니다.</summary>
public struct AttackOutcome
{
    public bool CoverAbsorbed;
    public bool Hit;
    public bool Critical;
    public int Damage;
    public bool TargetDied;

    public bool CounterHappened;
    public bool CounterHit;
    public bool CounterCritical;
    public int CounterDamage;
    public bool AttackerDied;

    public int HitRoll;
    public int CounterRoll;
}

/// <summary>
/// 명중·피해·반격·치명타를 하나의 규칙으로 처리합니다.
///
/// 미션 가이드 요구: "명중·피해·반격·치명타 계산이 하나의 규칙으로 정리되어야 하며,
/// 전투 전 예측 값과 실제 결과가 일치해야 함."
///
/// 이를 보장하기 위해 Resolve()는 내부에서 Forecast()를 호출해 같은 수치를 쓰고,
/// 그 위에 난수 판정만 얹습니다. 두 경로가 갈라질 수 없는 구조입니다.
/// </summary>
public static class CombatResolver
{
    // ── 규칙 상수 ────────────────────────────────
    public const int MinHitChance = 15;
    public const int MaxHitChance = 100;

    /// <summary>고지대에서 공격할 때의 명중 보정입니다. (미션 가이드: 고지 — 명중과 사거리 상승)</summary>
    public const int HighGroundAccuracyBonus = 15;

    /// <summary>치명타 피해 배율입니다.</summary>
    public const float CriticalMultiplier = 1.5f;

    /// <summary>전투 판정에 쓰는 난수입니다. GameManager가 전투 시작 시 초기화합니다.</summary>
    public static DeterministicRandom Random { get; private set; }
        = new DeterministicRandom(1u);

    public static void InitRandom(DeterministicRandom random)
    {
        Random = random ?? new DeterministicRandom(1u);
    }

    // ─────────────────── 예측 ───────────────────

    /// <summary>
    /// 공격자가 attackerPosition 에 서서 대상을 공격할 때의 예측입니다.
    /// attackerPosition 을 따로 받는 이유는, AI가 "이 칸으로 이동한 뒤 공격"을
    /// 평가할 때 아직 이동하지 않은 상태로 계산해야 하기 때문입니다.
    /// </summary>
    public static AttackForecast Forecast(
        Unit attacker, Unit target, Vector2Int attackerPosition)
    {
        var forecast = new AttackForecast();
        if (attacker == null || target == null || target.IsDead) return forecast;

        forecast.Valid = true;

        // ── 엄폐물 차단 ──────────────────────────
        if (IsBlockedByCover(attackerPosition, target.GridPosition))
        {
            forecast.CoverBlocks = true;
            return forecast;
        }

        // ── 명중률 ───────────────────────────────
        int accuracy = GetAccuracy(attacker);
        if (IsHighGround(attackerPosition)) accuracy += HighGroundAccuracyBonus;

        forecast.HitChance = Mathf.Clamp(
            accuracy - GetEvasion(target), MinHitChance, MaxHitChance);

        // ── 피해 ─────────────────────────────────
        int raw = GetRawPower(attacker, attackerPosition);
        AttackType attackType = attacker.UnitAttackType;
        ArmorType armorType = target.UnitArmorType;

        forecast.Damage = TypeAffinity.ApplyToDamage(raw, attackType, armorType);
        forecast.CriticalDamage = ApplyCritical(forecast.Damage);
        forecast.CriticalChance = GetCriticalRate(attacker);

        forecast.IsEffective = TypeAffinity.IsEffective(attackType, armorType);
        forecast.IsResisted = TypeAffinity.IsResisted(attackType, armorType);
        forecast.IsLethal = forecast.Damage >= target.HP;

        // ── 반격 ─────────────────────────────────
        // 이 공격으로 대상이 죽으면 반격은 없습니다.
        if (!forecast.IsLethal && CanCounter(target, attackerPosition))
        {
            forecast.CounterPossible = true;

            int counterAccuracy = GetAccuracy(target);
            if (IsHighGround(target.GridPosition))
                counterAccuracy += HighGroundAccuracyBonus;

            forecast.CounterHitChance = Mathf.Clamp(
                counterAccuracy - GetEvasion(attacker), MinHitChance, MaxHitChance);

            int counterRaw = GetRawPower(target, target.GridPosition);
            forecast.CounterDamage = TypeAffinity.ApplyToDamage(
                counterRaw, target.UnitAttackType, attacker.UnitArmorType);

            forecast.CounterIsLethal = forecast.CounterDamage >= attacker.HP;
        }

        return forecast;
    }

    public static AttackForecast Forecast(Unit attacker, Unit target)
        => Forecast(attacker, target, attacker != null ? attacker.GridPosition : default);

    // ─────────────────── 실행 ───────────────────

    /// <summary>
    /// 실제로 공격을 판정하고 피해를 적용합니다. 난수를 소비합니다.
    /// 반격까지 한 번에 처리하며, 반격이 또 다른 반격을 부르지는 않습니다.
    /// </summary>
    public static AttackOutcome Resolve(Unit attacker, Unit target)
    {
        var outcome = new AttackOutcome();
        if (attacker == null || target == null || target.IsDead) return outcome;

        // 예측과 같은 계산을 그대로 씁니다. 두 경로가 갈라질 수 없습니다.
        AttackForecast forecast = Forecast(attacker, target, attacker.GridPosition);
        if (!forecast.Valid) return outcome;

        // ── 엄폐물이 대신 맞음 ───────────────────
        if (forecast.CoverBlocks)
        {
            Tile cover = FindBlockingCover(attacker.GridPosition, target.GridPosition);
            if (cover != null)
            {
                cover.AbsorbRangedAttack();
                outcome.CoverAbsorbed = true;
            }
            return outcome;
        }

        // ── 명중 판정 ────────────────────────────
        outcome.HitRoll = Random.Roll100();
        outcome.Hit = outcome.HitRoll <= forecast.HitChance;

        if (!outcome.Hit) return outcome;

        outcome.Critical = Random.Chance(forecast.CriticalChance);
        outcome.Damage = outcome.Critical
            ? forecast.CriticalDamage
            : forecast.Damage;

        target.TakeDamage(outcome.Damage);
        outcome.TargetDied = target.IsDead;

        if (outcome.TargetDied) return outcome;

        // ── 반격 ─────────────────────────────────
        if (!CanCounter(target, attacker.GridPosition)) return outcome;

        outcome.CounterHappened = true;
        outcome.CounterRoll = Random.Roll100();

        // 반격 명중률도 예측과 동일한 값을 씁니다.
        int counterHitChance = forecast.CounterPossible
            ? forecast.CounterHitChance
            : ComputeCounterHitChance(target, attacker);

        outcome.CounterHit = outcome.CounterRoll <= counterHitChance;
        if (!outcome.CounterHit) return outcome;

        outcome.CounterCritical = Random.Chance(GetCriticalRate(target));

        int counterRaw = GetRawPower(target, target.GridPosition);
        int counterDamage = TypeAffinity.ApplyToDamage(
            counterRaw, target.UnitAttackType, attacker.UnitArmorType);
        if (outcome.CounterCritical) counterDamage = ApplyCritical(counterDamage);

        outcome.CounterDamage = counterDamage;
        attacker.TakeDamage(counterDamage);
        outcome.AttackerDied = attacker.IsDead;

        return outcome;
    }

    // ─────────────────── 규칙 조각 ───────────────────

    /// <summary>대상이 공격자 위치를 반격할 수 있는지 판정합니다.</summary>
    public static bool CanCounter(Unit defender, Vector2Int attackerPosition)
    {
        if (defender == null || defender.IsDead) return false;

        // 데이터가 없는 유닛은 기본적으로 반격합니다.
        if (defender.CharacterData != null && !defender.CharacterData.CanCounter)
            return false;

        // 고정포처럼 자리를 지키는 유닛은 반격하지 않습니다.
        if (defender.EnemyData != null &&
            defender.EnemyData.Trait == EnemyTrait.Emplaced)
            return false;

        // 반격도 사거리 안이어야 하고, 사선이 막히면 안 됩니다.
        int distance = Manhattan(defender.GridPosition, attackerPosition);
        if (distance > GetEffectiveRange(defender)) return false;
        if (IsBlockedByCover(defender.GridPosition, attackerPosition)) return false;

        return true;
    }

    private static int ComputeCounterHitChance(Unit defender, Unit attacker)
    {
        int accuracy = GetAccuracy(defender);
        if (IsHighGround(defender.GridPosition))
            accuracy += HighGroundAccuracyBonus;
        return Mathf.Clamp(
            accuracy - GetEvasion(attacker), MinHitChance, MaxHitChance);
    }

    /// <summary>특성 보정까지 반영한 상성 적용 전 공격력입니다.</summary>
    private static int GetRawPower(Unit unit, Vector2Int position)
    {
        int raw = unit.AttackPower;

        EnemyUnitData data = unit.EnemyData;
        if (data == null) return raw;

        if (data.Trait == EnemyTrait.HighGroundBonus && IsHighGround(position))
            raw += data.TraitValue;

        return raw;
    }

    private static int ApplyCritical(int damage)
    {
        if (damage <= 0) return damage;
        int crit = Mathf.FloorToInt(damage * CriticalMultiplier);
        // 치명타가 일반 피해와 같아지지 않도록 최소 +1을 보장합니다.
        return Mathf.Max(damage + 1, crit);
    }

    private static int GetAccuracy(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.Accuracy : 90;

    private static int GetEvasion(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.Evasion : 5;

    private static int GetCriticalRate(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.CriticalRate : 10;

    /// <summary>고지대 보정을 반영한 실효 사거리입니다.</summary>
    public static int GetEffectiveRange(Unit unit)
    {
        if (unit == null) return 0;
        return unit.AttackRange + (IsHighGround(unit.GridPosition) ? 1 : 0);
    }

    private static bool IsHighGround(Vector2Int position)
    {
        if (GridManager.Instance == null) return false;
        Tile tile = GridManager.Instance.GetTile(position);
        return tile != null && tile.Terrain == TileTerrain.HighGround;
    }

    private static bool IsBlockedByCover(Vector2Int from, Vector2Int to)
        => FindBlockingCover(from, to) != null;

    /// <summary>직선 사이를 막고 있는 엄폐 타일을 반환합니다.</summary>
    public static Tile FindBlockingCover(Vector2Int from, Vector2Int to)
    {
        if (GridManager.Instance == null) return null;

        int distance = Manhattan(from, to);
        if (distance <= 1) return null;

        Vector2Int step;
        if (from.x == to.x)
            step = new Vector2Int(0, to.y > from.y ? 1 : -1);
        else if (from.y == to.y)
            step = new Vector2Int(to.x > from.x ? 1 : -1, 0);
        else
            return null;

        Vector2Int position = from + step;
        while (position != to)
        {
            Tile tile = GridManager.Instance.GetTile(position);
            if (tile != null &&
                tile.Terrain == TileTerrain.Cover &&
                tile.CoverDurability > 0)
                return tile;
            position += step;
        }
        return null;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    /// <summary>
    /// 판정 결과를 플레이어에게 보여줄 한 줄로 만듭니다.
    /// 공격이 빗나갈 수 있게 된 이상, 무슨 일이 일어났는지 알려주지 않으면
    /// 플레이어는 버그로 읽습니다.
    /// </summary>
    public static string DescribeOutcome(
        AttackOutcome outcome, Unit attacker, Unit target)
    {
        string attackerName = DescribeUnit(attacker);
        string targetName = DescribeUnit(target);

        if (outcome.CoverAbsorbed)
            return $"{targetName} — 엄폐물이 막아냄";

        if (!outcome.Hit)
            return $"{attackerName} → {targetName}  빗나감";

        string line = outcome.Critical
            ? $"{attackerName} → {targetName}  치명타! {outcome.Damage}피해"
            : $"{attackerName} → {targetName}  {outcome.Damage}피해";

        if (outcome.TargetDied)
            return line + " — 격파";

        if (!outcome.CounterHappened) return line;

        line += outcome.CounterHit
            ? (outcome.CounterCritical
                ? $"\n반격 치명타! {outcome.CounterDamage}피해"
                : $"\n반격 {outcome.CounterDamage}피해")
            : "\n반격 빗나감";

        if (outcome.AttackerDied)
            line += " — 격파당함";

        return line;
    }

    private static string DescribeUnit(Unit unit)
    {
        if (unit == null) return "?";
        if (unit.CharacterData != null &&
            !string.IsNullOrEmpty(unit.CharacterData.DisplayName))
            return unit.CharacterData.DisplayName;
        return unit.name;
    }

    /// <summary>예측을 플레이어에게 보여줄 한 줄 요약으로 만듭니다.</summary>
    public static string DescribeForecast(AttackForecast f, Unit target)
    {
        if (!f.Valid) return "";
        if (f.CoverBlocks) return "엄폐물에 막힘 — 피해 없음";

        string affinity =
            f.IsEffective ? "  <color=#7FE0BC>유효</color>" :
            f.IsResisted ? "  <color=#F09090>저항</color>" : "";

        string line =
            $"명중 {f.HitChance}%   피해 {f.Damage}{affinity}\n" +
            $"치명 {f.CriticalChance}% ({f.CriticalDamage})";

        if (target != null)
            line += $"   체력 {target.HP} → {Mathf.Max(0, target.HP - f.Damage)}";

        line += f.CounterPossible
            ? $"\n반격 있음 — {f.CounterHitChance}% / {f.CounterDamage}피해"
            : "\n반격 없음";

        return line;
    }
}
