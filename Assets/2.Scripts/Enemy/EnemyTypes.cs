using System;

/// <summary>
/// 적 부대 설계의 분류 축입니다.
/// 역할(Role)이 표적 우선순위를, 타입(Attack/Armor)이 상성을,
/// 교리(Doctrine)가 단체의 행동 방식을 결정합니다.
/// </summary>
public enum EnemyRole
{
    Tank,
    MeleeDealer,
    RangedDealer,
    Healer,
    Supporter
}

public enum AttackType
{
    Pierce,
    Mystic,
    Explosive
}

public enum ArmorType
{
    Light,
    Heavy,
    Special
}

/// <summary>단체가 따르는 교전 교리입니다. 미션 요구사항의 적 AI 4종에 대응합니다.</summary>
public enum SquadDoctrine
{
    Assault,
    Hold,
    Snipe,
    Command
}

public enum AIDifficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// 행동 평가에 사용할 지표입니다. 난이도는 코드 분기가 아니라
/// 이 플래그를 몇 개 켜는가로만 구분합니다.
/// </summary>
[Flags]
public enum AIMetric
{
    None = 0,
    Distance = 1 << 0,
    RoleWeight = 1 << 1,
    KillBonus = 1 << 2,
    HpRatio = 1 << 3,
    Affinity = 1 << 4,
    CounterRisk = 1 << 5,
    TerrainBonus = 1 << 6,
    FocusPenalty = 1 << 7,
    SelfPreserve = 1 << 8
}

/// <summary>
/// 난이도별 기본 지표 조합입니다.
/// AIMetric 안에 두면 인스펙터 마스크 드롭다운에 조합 항목이 함께 나와
/// 선택이 혼란스러워지므로 밖으로 분리했습니다.
/// </summary>
public static class AIMetricPresets
{
    /// <summary>눈앞의 한 수만 봅니다. 역할 구분 없이 거리만 고려합니다.</summary>
    public const AIMetric Easy = AIMetric.Distance;

    /// <summary>힐러를 우선하고, 킬을 놓치지 않으며, 받을 반격을 계산합니다.</summary>
    public const AIMetric Normal =
        AIMetric.Distance | AIMetric.RoleWeight | AIMetric.KillBonus |
        AIMetric.HpRatio | AIMetric.Affinity | AIMetric.CounterRisk;

    /// <summary>고지를 선점하고, 표적을 나누며, 위험하면 물러납니다.</summary>
    public const AIMetric Hard =
        Normal | AIMetric.TerrainBonus |
        AIMetric.FocusPenalty | AIMetric.SelfPreserve;
}

/// <summary>공격 타입 × 방어 타입 상성 계산입니다.</summary>
public static class TypeAffinity
{
    public const float Effective = 1.5f;
    public const float Neutral = 1.0f;
    public const float Resisted = 0.6f;

    public static float GetMultiplier(AttackType attack, ArmorType armor)
    {
        switch (attack)
        {
            case AttackType.Pierce:
                if (armor == ArmorType.Heavy) return Effective;
                if (armor == ArmorType.Light) return Resisted;
                return Neutral;

            case AttackType.Explosive:
                if (armor == ArmorType.Light) return Effective;
                if (armor == ArmorType.Special) return Resisted;
                return Neutral;

            case AttackType.Mystic:
                if (armor == ArmorType.Special) return Effective;
                if (armor == ArmorType.Heavy) return Resisted;
                return Neutral;
        }
        return Neutral;
    }

    /// <summary>내림 처리하되 최소 1의 피해는 보장합니다.</summary>
    public static int ApplyToDamage(int rawDamage, AttackType attack, ArmorType armor)
    {
        if (rawDamage <= 0) return 0;
        float scaled = rawDamage * GetMultiplier(attack, armor);
        return UnityEngine.Mathf.Max(1, UnityEngine.Mathf.FloorToInt(scaled));
    }

    public static bool IsEffective(AttackType attack, ArmorType armor)
        => GetMultiplier(attack, armor) > Neutral;

    public static bool IsResisted(AttackType attack, ArmorType armor)
        => GetMultiplier(attack, armor) < Neutral;
}
