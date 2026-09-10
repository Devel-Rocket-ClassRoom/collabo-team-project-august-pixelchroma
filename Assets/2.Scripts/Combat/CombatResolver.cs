using UnityEngine;

/// <summary>怨듦꺽 ??踰덉쓽 ?덉륫媛믪엯?덈떎. ?쒖닔瑜??뚮퉬?섏? ?딆쑝誘濡?紐?踰덉씠???몄텧?대룄 ?덉쟾?⑸땲??</summary>
public struct AttackForecast
{
    public bool Valid;

    /// <summary>?ъ꽑???꾪룓臾쇱뿉 留됲? ?덉뒿?덈떎. ??寃쎌슦 ?쇳빐 ????꾪룓臾쇱씠 1???뚮え?⑸땲??</summary>
    public bool CoverBlocks;

    public int HitChance;
    public int CriticalChance;

    /// <summary>紐낆쨷 ???쇳빐?낅땲??</summary>
    public int Damage;

    /// <summary>移섎챸? ???쇳빐?낅땲??</summary>
    public int CriticalDamage;

    public bool IsEffective;
    public bool IsResisted;

    /// <summary>紐낆쨷 ????곸씠 二쎈뒗吏 ?щ??낅땲??</summary>
    public bool IsLethal;

    public bool CounterPossible;
    public int CounterHitChance;
    public int CounterDamage;

    /// <summary>諛섍꺽?쇰줈 怨듦꺽?먭? 二쎌쓣 ???덈뒗吏 ?щ??낅땲??</summary>
    public bool CounterIsLethal;
}

/// <summary>?ㅼ젣 ?먯젙 寃곌낵?낅땲?? ?쒖닔瑜??뚮퉬?⑸땲??</summary>
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
/// 紐낆쨷쨌?쇳빐쨌諛섍꺽쨌移섎챸?瑜??섎굹??洹쒖튃?쇰줈 泥섎━?⑸땲??
///
/// 誘몄뀡 媛?대뱶 ?붽뎄: "紐낆쨷쨌?쇳빐쨌諛섍꺽쨌移섎챸? 怨꾩궛???섎굹??洹쒖튃?쇰줈 ?뺣━?섏뼱???섎ŉ,
/// ?꾪닾 ???덉륫 媛믨낵 ?ㅼ젣 寃곌낵媛 ?쇱튂?댁빞 ??"
///
/// ?대? 蹂댁옣?섍린 ?꾪빐 Resolve()???대??먯꽌 Forecast()瑜??몄텧??媛숈? ?섏튂瑜??곌퀬,
/// 洹??꾩뿉 ?쒖닔 ?먯젙留??뱀뒿?덈떎. ??寃쎈줈媛 媛덈씪吏????녿뒗 援ъ“?낅땲??
/// </summary>
public static class CombatResolver
{
    // ?? 洹쒖튃 ?곸닔 ????????????????????????????????
    public const int MinHitChance = 15;
    public const int MaxHitChance = 100;

    /// <summary>怨좎???먯꽌 怨듦꺽???뚯쓽 紐낆쨷 蹂댁젙?낅땲?? (誘몄뀡 媛?대뱶: 怨좎? ??紐낆쨷怨??ш굅由??곸듅)</summary>
    public const int HighGroundAccuracyBonus = 15;

    /// <summary>移섎챸? ?쇳빐 諛곗쑉?낅땲??</summary>
    public const float CriticalMultiplier = 1.5f;

    /// <summary>?꾪닾 ?먯젙???곕뒗 ?쒖닔?낅땲?? GameManager媛 ?꾪닾 ?쒖옉 ??珥덇린?뷀빀?덈떎.</summary>
    public static DeterministicRandom Random { get; private set; }
        = new DeterministicRandom(1u);

    public static void InitRandom(DeterministicRandom random)
    {
        Random = random ?? new DeterministicRandom(1u);
    }

    // ??????????????????? ?덉륫 ???????????????????

    /// <summary>
    /// 怨듦꺽?먭? attackerPosition ???쒖꽌 ??곸쓣 怨듦꺽???뚯쓽 ?덉륫?낅땲??
    /// attackerPosition ???곕줈 諛쏅뒗 ?댁쑀?? AI媛 "??移몄쑝濡??대룞????怨듦꺽"??    /// ?됯??????꾩쭅 ?대룞?섏? ?딆? ?곹깭濡?怨꾩궛?댁빞 ?섍린 ?뚮Ц?낅땲??
    /// </summary>
    public static AttackForecast Forecast(
        Unit attacker, Unit target, Vector2Int attackerPosition)
    {
        var forecast = new AttackForecast();
        if (attacker == null || target == null || target.IsDead) return forecast;

        forecast.Valid = true;

        // ?? ?꾪룓臾?李⑤떒 ??????????????????????????
        if (IsBlockedByCover(attackerPosition, target.GridPosition) &&
            !IgnoresCover(attacker, null))
        {
            forecast.CoverBlocks = true;
            return forecast;
        }

        // ?? 紐낆쨷瑜????????????????????????????????
        int accuracy = GetAccuracy(attacker);
        if (IsHighGround(attackerPosition)) accuracy += HighGroundAccuracyBonus;

        forecast.HitChance = Mathf.Clamp(
            accuracy - GetEvasion(target), MinHitChance, MaxHitChance);

        // ?? ?쇳빐 ?????????????????????????????????
        int raw = GetRawPower(attacker, attackerPosition);
        AttackType attackType = attacker.UnitAttackType;
        ArmorType armorType = target.UnitArmorType;

        forecast.Damage = ComputeDamage(raw, attackType, target);
        forecast.CriticalDamage = ApplyCritical(forecast.Damage);
        forecast.CriticalChance = GetCriticalRate(attacker);

        forecast.IsEffective = TypeAffinity.IsEffective(attackType, armorType);
        forecast.IsResisted = TypeAffinity.IsResisted(attackType, armorType);
        forecast.IsLethal = forecast.Damage >= target.HP;

        // ?? 諛섍꺽 ?????????????????????????????????
        // ??怨듦꺽?쇰줈 ??곸씠 二쎌쑝硫?諛섍꺽? ?놁뒿?덈떎.
        bool sentryCounter = CanSentryCounter(target, attacker);
        if (!forecast.IsLethal && (sentryCounter || CanCounter(target, attackerPosition)))
        {
            forecast.CounterPossible = true;

            int counterAccuracy = GetAccuracy(target);
            if (IsHighGround(target.GridPosition))
                counterAccuracy += HighGroundAccuracyBonus;

            forecast.CounterHitChance = Mathf.Clamp(
                counterAccuracy - GetEvasion(attacker), MinHitChance, MaxHitChance);

            forecast.CounterDamage = sentryCounter
                ? GetSentryCounterDamage(target, attacker)
                : ComputeDamage(GetRawPower(target, target.GridPosition),
                    target.UnitAttackType, attacker);

            forecast.CounterIsLethal = forecast.CounterDamage >= attacker.HP;
        }

        return forecast;
    }

    public static AttackForecast Forecast(Unit attacker, Unit target)
        => Forecast(attacker, target, attacker != null ? attacker.GridPosition : default);

    public static AttackForecast ForecastSkill(
        Unit attacker, Unit target, SkillData skill, float powerScale = 1f)
    {
        var forecast = new AttackForecast();
        if (attacker == null || target == null || target.IsDead || skill == null)
            return forecast;

        forecast.Valid = true;

        if (IsBlockedByCover(attacker.GridPosition, target.GridPosition) &&
            !IgnoresCover(attacker, skill))
        {
            forecast.CoverBlocks = true;
            return forecast;
        }

        int accuracy = GetAccuracy(attacker);
        if (IsHighGround(attacker.GridPosition))
            accuracy += HighGroundAccuracyBonus;

        forecast.HitChance = Mathf.Clamp(
            accuracy - GetEvasion(target), MinHitChance, MaxHitChance);

        AttackType attackType = GetSkillAttackType(attacker, skill);
        ArmorType armorType = target.UnitArmorType;
        int raw = GetSkillRawPower(attacker, skill, powerScale);

        forecast.Damage = ComputeDamage(raw, attackType, target);
        forecast.CriticalDamage = ApplyCritical(forecast.Damage);
        forecast.CriticalChance = GetCriticalRate(attacker);
        forecast.IsEffective = TypeAffinity.IsEffective(attackType, armorType);
        forecast.IsResisted = TypeAffinity.IsResisted(attackType, armorType);
        forecast.IsLethal = forecast.Damage >= target.HP;

        if (!forecast.IsLethal && CanCounter(target, attacker.GridPosition))
        {
            forecast.CounterPossible = true;
            forecast.CounterHitChance = ComputeCounterHitChance(target, attacker);
            forecast.CounterDamage = ComputeDamage(
                GetRawPower(target, target.GridPosition),
                target.UnitAttackType,
                attacker);
            forecast.CounterIsLethal = forecast.CounterDamage >= attacker.HP;
        }

        return forecast;
    }

    public static AttackOutcome ResolveSkill(
        Unit attacker,
        Unit target,
        SkillData skill,
        float powerScale = 1f,
        bool allowCounter = true)
    {
        var outcome = new AttackOutcome();
        if (attacker == null || target == null || target.IsDead || skill == null)
            return outcome;

        AttackForecast forecast = ForecastSkill(attacker, target, skill, powerScale);
        if (!forecast.Valid) return outcome;

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

        outcome.HitRoll = Random.Roll100();
        outcome.Hit = outcome.HitRoll <= forecast.HitChance;
        if (!outcome.Hit) return outcome;

        outcome.Critical = Random.Chance(forecast.CriticalChance);
        outcome.Damage = outcome.Critical
            ? forecast.CriticalDamage
            : forecast.Damage;

        target.TakeDamage(outcome.Damage);
        outcome.TargetDied = target.IsDead;

        if (outcome.TargetDied || !allowCounter || !CanCounter(target, attacker.GridPosition))
            return outcome;

        outcome.CounterHappened = true;
        outcome.CounterRoll = Random.Roll100();
        outcome.CounterHit = outcome.CounterRoll <= forecast.CounterHitChance;
        if (!outcome.CounterHit) return outcome;

        outcome.CounterCritical = Random.Chance(GetCriticalRate(target));
        int counterDamage = ComputeDamage(
            GetRawPower(target, target.GridPosition),
            target.UnitAttackType,
            attacker);
        if (outcome.CounterCritical)
            counterDamage = ApplyCritical(counterDamage);

        outcome.CounterDamage = counterDamage;
        attacker.TakeDamage(counterDamage);
        outcome.AttackerDied = attacker.IsDead;

        return outcome;
    }

    // ??????????????????? ?ㅽ뻾 ???????????????????

    /// <summary>
    /// ?ㅼ젣濡?怨듦꺽???먯젙?섍퀬 ?쇳빐瑜??곸슜?⑸땲?? ?쒖닔瑜??뚮퉬?⑸땲??
    /// 諛섍꺽源뚯? ??踰덉뿉 泥섎━?섎ŉ, 諛섍꺽?????ㅻⅨ 諛섍꺽??遺瑜댁????딆뒿?덈떎.
    /// </summary>
    public static AttackOutcome Resolve(Unit attacker, Unit target)
    {
        var outcome = new AttackOutcome();
        if (attacker == null || target == null || target.IsDead) return outcome;

        // ?덉륫怨?媛숈? 怨꾩궛??洹몃?濡??곷땲?? ??寃쎈줈媛 媛덈씪吏????놁뒿?덈떎.
        AttackForecast forecast = Forecast(attacker, target, attacker.GridPosition);
        if (!forecast.Valid) return outcome;

        // ?? ?꾪룓臾쇱씠 ???留욎쓬 ???????????????????
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

        // ?? 紐낆쨷 ?먯젙 ????????????????????????????
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

        // ?? 諛섍꺽 ?????????????????????????????????
        bool sentryCounter = CanSentryCounter(target, attacker);
        if (!sentryCounter && !CanCounter(target, attacker.GridPosition)) return outcome;

        outcome.CounterHappened = true;
        outcome.CounterRoll = Random.Roll100();

        // 諛섍꺽 紐낆쨷瑜좊룄 ?덉륫怨??숈씪??媛믪쓣 ?곷땲??
        int counterHitChance = forecast.CounterPossible
            ? forecast.CounterHitChance
            : ComputeCounterHitChance(target, attacker);

        if (sentryCounter)
            target.TryConsumeSentryCounter();

        outcome.CounterHit = outcome.CounterRoll <= counterHitChance;
        if (!outcome.CounterHit) return outcome;

        outcome.CounterCritical = Random.Chance(GetCriticalRate(target));

        int counterDamage = sentryCounter
            ? GetSentryCounterDamage(target, attacker)
            : ComputeDamage(GetRawPower(target, target.GridPosition),
                target.UnitAttackType, attacker);
        if (!sentryCounter && outcome.CounterCritical)
            counterDamage = ApplyCritical(counterDamage);

        outcome.CounterDamage = counterDamage;
        attacker.TakeDamage(counterDamage);
        outcome.AttackerDied = attacker.IsDead;

        return outcome;
    }

    // ??????????????????? 洹쒖튃 議곌컖 ???????????????????

    /// <summary>??곸씠 怨듦꺽???꾩튂瑜?諛섍꺽?????덈뒗吏 ?먯젙?⑸땲??</summary>
    public static bool CanCounter(Unit defender, Vector2Int attackerPosition)
    {
        if (defender == null || defender.IsDead) return false;

        // ?곗씠?곌? ?녿뒗 ?좊떅? 湲곕낯?곸쑝濡?諛섍꺽?⑸땲??
        if (defender.CharacterData != null && !defender.CharacterData.CanCounter)
            return false;

        // 怨좎젙?ъ쿂???먮━瑜?吏?ㅻ뒗 ?좊떅? 諛섍꺽?섏? ?딆뒿?덈떎.
        if (defender.EnemyData != null &&
            defender.EnemyData.Trait == EnemyTrait.Emplaced)
            return false;

        // 諛섍꺽???ш굅由??덉씠?댁빞 ?섍퀬, ?ъ꽑??留됲엳硫????⑸땲??
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

    /// <summary>?뱀꽦 蹂댁젙源뚯? 諛섏쁺???곸꽦 ?곸슜 ??怨듦꺽?μ엯?덈떎.</summary>
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
        // 移섎챸?媛 ?쇰컲 ?쇳빐? 媛숈븘吏吏 ?딅룄濡?理쒖냼 +1??蹂댁옣?⑸땲??
        return Mathf.Max(damage + 1, crit);
    }

    private static int ComputeDamage(int rawDamage, AttackType attackType, Unit defender)
    {
        int damage = TypeAffinity.ApplyToDamage(rawDamage, attackType, defender.UnitArmorType);
        return ApplyDefenseReduction(damage, defender);
    }

    private static int ApplyDefenseReduction(int damage, Unit defender)
    {
        if (damage <= 0 || defender == null || defender.Defense <= 0)
            return damage;

        float multiplier = Mathf.Clamp01((100f - defender.Defense) / 100f);
        return Mathf.Max(1, Mathf.CeilToInt(damage * multiplier));
    }

    private static bool IgnoresCover(Unit attacker, SkillData skill)
    {
        if (skill != null)
            return skill.IgnoresCover;
        return attacker != null &&
               attacker.CharacterData != null &&
               attacker.CharacterData.IgnoresCover;
    }

    private static bool CanSentryCounter(Unit defender, Unit attacker)
        => defender != null &&
           attacker != null &&
           attacker.UnitTeam == Team.Enemy &&
           defender.CanUseSentryCounter;

    private static int GetSentryCounterDamage(Unit defender, Unit attacker)
    {
        int raw = Mathf.Max(1, Mathf.CeilToInt(defender.AttackPower * 0.3f));
        return ComputeDamage(raw, defender.UnitAttackType, attacker);
    }

    private static int GetSkillRawPower(Unit attacker, SkillData skill, float powerScale)
    {
        float scaledPower = attacker.AttackPower * skill.PowerPercent / 100f;
        int percentDamage = Mathf.CeilToInt(scaledPower * powerScale);
        int flatDamage = Mathf.CeilToInt(skill.FlatPower * powerScale);
        return Mathf.Max(1, Mathf.Max(percentDamage, flatDamage));
    }

    private static AttackType GetSkillAttackType(Unit attacker, SkillData skill)
    {
        switch (skill.DamageType)
        {
            case CharacterDamageType.Magical:
                return AttackType.Mystic;
            case CharacterDamageType.Support:
                return attacker.UnitAttackType;
            default:
                return AttackType.Pierce;
        }
    }

    private static int GetAccuracy(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.Accuracy : 90;

    private static int GetEvasion(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.Evasion : 5;

    private static int GetCriticalRate(Unit unit)
        => unit.CharacterData != null ? unit.CharacterData.CriticalRate : 10;

    /// <summary>怨좎?? 蹂댁젙??諛섏쁺???ㅽ슚 ?ш굅由ъ엯?덈떎.</summary>
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

    /// <summary>吏곸꽑 ?ъ씠瑜?留됯퀬 ?덈뒗 ?꾪룓 ??쇱쓣 諛섑솚?⑸땲??</summary>
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
    /// ?먯젙 寃곌낵瑜??뚮젅?댁뼱?먭쾶 蹂댁뿬以???以꾨줈 留뚮벊?덈떎.
    /// 怨듦꺽??鍮쀫굹媛????덇쾶 ???댁긽, 臾댁뒯 ?쇱씠 ?쇱뼱?щ뒗吏 ?뚮젮二쇱? ?딆쑝硫?    /// ?뚮젅?댁뼱??踰꾧렇濡??쎌뒿?덈떎.
    /// </summary>
    public static string DescribeOutcome(
        AttackOutcome outcome, Unit attacker, Unit target)
    {
        string attackerName = DescribeUnit(attacker);
        string targetName = DescribeUnit(target);

        if (outcome.CoverAbsorbed)
            return $"{targetName}의 엄폐물이 공격을 막았습니다.";

        if (!outcome.Hit)
            return $"{attackerName}의 공격이 {targetName}에게 빗나갔습니다.";

        string line = outcome.Critical
            ? $"{attackerName}의 치명타! {targetName}에게 {outcome.Damage} 피해"
            : $"{attackerName}이 {targetName}에게 {outcome.Damage} 피해";

        if (outcome.TargetDied)
            return line + " / 대상 격파";

        if (!outcome.CounterHappened) return line;

        line += outcome.CounterHit
            ? (outcome.CounterCritical
                ? $"\n반격 치명타! {outcome.CounterDamage} 피해"
                : $"\n반격 {outcome.CounterDamage} 피해")
            : "\n반격 빗나감";

        if (outcome.AttackerDied)
            line += " / 공격자 격파";

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

    /// <summary>?덉륫???뚮젅?댁뼱?먭쾶 蹂댁뿬以???以??붿빟?쇰줈 留뚮벊?덈떎.</summary>
    public static string DescribeForecast(AttackForecast f, Unit target)
    {
        if (!f.Valid) return "";
        if (f.CoverBlocks) return "엄폐물이 이 공격을 막습니다.";

        string affinity =
            f.IsEffective ? "  <color=#7FE0BC>효과적</color>" :
            f.IsResisted ? "  <color=#F09090>저항</color>" : "";

        string line =
            $"명중 {f.HitChance}%   피해 {f.Damage}{affinity}\n" +
            $"치명타 {f.CriticalChance}% ({f.CriticalDamage})";

        if (target != null)
            line += $"   체력 {target.HP} -> {Mathf.Max(0, target.HP - f.Damage)}";

        line += f.CounterPossible
            ? $"\n반격 {f.CounterHitChance}% / {f.CounterDamage} 피해"
            : "\n반격 없음";

        return line;
    }
}
