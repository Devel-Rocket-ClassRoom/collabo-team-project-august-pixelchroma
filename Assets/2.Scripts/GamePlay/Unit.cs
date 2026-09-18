using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class Unit : MonoBehaviour
{
    [Header("2D Battle Visual")]
    [Tooltip("???좊떅 ?꾨━???꾩슜 2D ?대?吏?낅땲?? CharacterData ?대?吏媛 ?덉쑝硫?洹??대?吏媛 ?곗꽑?⑸땲??")]
    [SerializeField] private Sprite battleSprite;
    [Tooltip("?ㅽ봽?쇱씠???믪씠瑜?????ш린??留욎텣 ???곸슜??諛곗쑉?낅땲??")]
    [SerializeField, Min(0.1f)] private float spriteScale = 1.15f;
    [Tooltip("罹먮┃??諛??꾩튂瑜????諛붾떏?먯꽌 ?쇰쭏???щ┫吏 吏?뺥빀?덈떎.")]
    [SerializeField] private float spriteGroundOffset;

    public Team UnitTeam { get; private set; }
    public int HP { get; private set; }
    public int MaxHP { get; private set; }
    public int AttackPower { get; private set; }
    public int Defense { get; private set; }
    public int MoveRange { get; private set; }
    public int AttackRange { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public bool HasActed { get; set; }
    public CharacterData CharacterData { get; private set; }

    /// <summary>
    /// ???꾩슜 ?곗씠?곗엯?덈떎. EnemyUnitData??CharacterData瑜??곸냽?섎?濡?
    /// ?뚮젅?댁뼱 ?좊떅?먯꽌??null??諛섑솚?⑸땲??
    /// </summary>
    public EnemyUnitData EnemyData => CharacterData as EnemyUnitData;

    public EnemyRole Role => EnemyData != null ? EnemyData.Role : EnemyRole.MeleeDealer;
    public AttackType UnitAttackType =>
        EnemyData != null ? EnemyData.EnemyAttackType : AttackType.Pierce;
    public ArmorType UnitArmorType =>
        EnemyData != null ? EnemyData.EnemyArmorType : ArmorType.Light;

    /// <summary>?곸꽦??諛섏쁺???ㅼ젣 ?쇳빐?됱엯?덈떎.</summary>
    public int GetDamageAgainst(Unit target)
    {
        if (target == null) return 0;
        return TypeAffinity.ApplyToDamage(
            AttackPower, UnitAttackType, target.UnitArmorType);
    }

    private Renderer unitRenderer;
    private Material unitMaterial;
    private SpriteRenderer spriteRenderer;

    private static readonly Color PlayerColor = new Color(0.15f, 0.4f, 1f, 1f);
    private static readonly Color EnemyColor = new Color(1f, 0.2f, 0.15f, 1f);
    private static readonly Color SelectedColor = new Color(1f, 1f, 0.3f, 1f);
    private static readonly Color ActedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private int baseAttackPower;
    private int baseMoveRange;
    private int baseAttackRange;
    private int passiveAttackPercent;
    private int passiveDefensePercent;
    private int auraAttackPercent;
    private int auraDefensePercent;
    private int skillAttackPercent;
    private int skillDefensePercent;
    private int skillBuffTurnsRemaining;
    // 약화(적에게 거는 스킬): 음수 %로 공격력·방어력을 깎고, 걸린 쪽의 턴이 끝날 때마다 줄어듭니다.
    private int debuffAttackPercent;
    private int debuffDefensePercent;
    private int debuffTurnsRemaining;
    private int guardTurnsRemaining;
    private bool sentryCounterAvailable = true;

    public bool IsDead => HP <= 0;
    public SkillData SkillData => CharacterData != null ? CharacterData.SkillData : null;
    public bool HasActiveGuard => guardTurnsRemaining > 0;

    // SP: 캐릭터별 최대 5, 시작 0, 턴이 지날 때마다 +1. 전투가 끝나면 유닛과 함께 사라집니다.
    public const int MaxSP = 5;
    public int SP { get; private set; }

    // 유체화: 다음 자기 턴까지 적의 공격 대상에서 빠지고, 이후 정해진 턴 동안 공격력이 줄어듭니다.
    public bool IsPhased { get; private set; }
    private int phaseAttackPercent;
    private int phasePenaltyTurns;

    // 전투 중 저장/복구에 쓰는 상태값입니다.
    public int GuardTurnsRemaining => guardTurnsRemaining;
    public int SkillBuffTurnsRemaining => skillBuffTurnsRemaining;
    public bool SentryCounterAvailable => sentryCounterAvailable;
    public int PhaseAttackPercent => phaseAttackPercent;
    public int PhasePenaltyTurns => phasePenaltyTurns;
    public int SkillAttackPercent => skillAttackPercent;
    public int SkillDefensePercent => skillDefensePercent;
    public int DebuffAttackPercent => debuffAttackPercent;
    public int DebuffDefensePercent => debuffDefensePercent;
    public int DebuffTurnsRemaining => debuffTurnsRemaining;
    public bool HasDebuff => debuffTurnsRemaining > 0;

    /// <summary>저장해 둔 전투 상태를 그대로 되돌립니다.</summary>
    public void RestoreBattleState(
        int hp, bool hasActed, int sp, int guardTurns, int buffTurns, bool sentryAvailable,
        bool phased, int phaseAttack, int phaseTurns,
        int buffAttack, int buffDefense, int debuffAttack, int debuffDefense, int debuffTurns)
    {
        skillAttackPercent = buffAttack;
        skillDefensePercent = buffDefense;
        debuffAttackPercent = debuffAttack;
        debuffDefensePercent = debuffDefense;
        debuffTurnsRemaining = Mathf.Max(0, debuffTurns);
        HP = Mathf.Clamp(hp, 1, MaxHP);
        HasActed = hasActed;
        SP = Mathf.Clamp(sp, 0, MaxSP);
        guardTurnsRemaining = Mathf.Max(0, guardTurns);
        skillBuffTurnsRemaining = Mathf.Max(0, buffTurns);
        sentryCounterAvailable = sentryAvailable;
        IsPhased = phased;
        phaseAttackPercent = phaseAttack;
        phasePenaltyTurns = Mathf.Max(0, phaseTurns);

        RecalculateStats();
        if (hasActed) ApplyColor(ActedColor);
        else SetTeamColor();
    }

    /// <summary>유체화 발동. 적에게 공격받지 않게 되고, 이후 penaltyTurns 턴 동안 공격력이 줄어듭니다.</summary>
    public void ActivatePhase(int attackPercent, int penaltyTurns)
    {
        IsPhased = true;
        phaseAttackPercent = attackPercent;
        // 유체화가 풀리는 다음 턴부터 세기 위해 1을 더해 둡니다.
        phasePenaltyTurns = Mathf.Max(0, penaltyTurns) + 1;
        RecalculateStats();
    }
    public bool CanUseSentryCounter => IsCharacter("kamae_tomoka") && sentryCounterAvailable;

    public static Unit Create(Team team, Vector2Int gridPos, GameObject prefab)
    {
        return Create(team, gridPos, prefab, null);
    }

    public static Unit Create(
        Team team,
        Vector2Int gridPos,
        GameObject fallbackPrefab,
        CharacterData characterData,
        Sprite fallbackSprite = null)
    {
        GridManager grid = GridManager.Instance;
        GameObject prefab = characterData != null && characterData.BattlePrefab != null
            ? characterData.BattlePrefab
            : fallbackPrefab;

        GameObject obj;
        if (prefab != null)
        {
            obj = Object.Instantiate(prefab);
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        obj.name = $"{team}_{gridPos.x}_{gridPos.y}";

        float s = grid.CellSize * 0.45f;
        obj.transform.localScale = new Vector3(s, s, s);

        Tile tile = grid.GetTile(gridPos);
        Vector3 worldPos = grid.GridToWorldPosition(gridPos.x, gridPos.y);
        float tileHeight = tile != null ? tile.HeightOffset : 0f;
        obj.transform.position = worldPos + Vector3.up * (0.5f + tileHeight);

        obj.transform.rotation = tile.transform.rotation;

        Unit unit = obj.GetComponent<Unit>();
        if (unit == null)
            unit = obj.AddComponent<Unit>();

        unit.UnitTeam = team;
        unit.CharacterData = characterData;
        unit.HP = characterData != null ? characterData.MaxHP : 3;
        unit.MaxHP = unit.HP;
        unit.AttackPower = characterData != null ? characterData.AttackPower : 1;
        unit.MoveRange = characterData != null ? characterData.MoveRange : 3;
        unit.AttackRange = characterData != null ? characterData.AttackRange : 1;
        unit.GridPosition = gridPos;
        unit.HasActed = false;
        unit.baseAttackPower = unit.AttackPower;
        unit.baseMoveRange = unit.MoveRange;
        unit.baseAttackRange = unit.AttackRange;
        unit.passiveAttackPercent = 0;
        unit.passiveDefensePercent = 0;
        unit.auraAttackPercent = 0;
        unit.auraDefensePercent = 0;
        unit.skillAttackPercent = 0;
        unit.skillDefensePercent = 0;
        unit.skillBuffTurnsRemaining = 0;
        unit.debuffAttackPercent = 0;
        unit.debuffDefensePercent = 0;
        unit.debuffTurnsRemaining = 0;
        unit.guardTurnsRemaining = 0;
        unit.SP = 0;
        unit.IsPhased = false;
        unit.phaseAttackPercent = 0;
        unit.phasePenaltyTurns = 0;
        unit.sentryCounterAvailable = true;
        unit.ApplyIntrinsicPassives();
        unit.RecalculateStats();

        Sprite sprite = characterData != null && characterData.BattleSprite != null
            ? characterData.BattleSprite
            : (unit.battleSprite != null ? unit.battleSprite : fallbackSprite);

        unit.unitRenderer = obj.GetComponent<Renderer>();
        if (sprite != null)
        {
            unit.SetupSpriteVisual(sprite, grid.CellSize);
            if (unit.unitRenderer != null)
                unit.unitRenderer.enabled = false;
        }
        else
        {
            // Also support art prefabs that already contain their own
            // SpriteRenderer instead of using CharacterData.BattleSprite.
            unit.SetupExistingSpriteVisuals();
        }

        if (unit.spriteRenderer == null && unit.unitRenderer != null)
        {
            unit.unitMaterial = new Material(unit.unitRenderer.material);
            unit.unitRenderer.material = unit.unitMaterial;
            unit.SetTeamColor();
        }

        if (obj.GetComponent<Collider>() == null)
            obj.AddComponent<BoxCollider>();

        tile.PlaceUnit(obj);
        return unit;
    }

    public void MoveTo(Vector2Int newPos)
    {
        Tile oldTile = GridManager.Instance.GetTile(GridPosition);
        if (oldTile != null) oldTile.RemoveUnit();

        GridPosition = newPos;

        Tile newTile = GridManager.Instance.GetTile(newPos);
        if (newTile != null) newTile.PlaceUnit(gameObject);

        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(newPos.x, newPos.y);
        float tileHeight = newTile != null ? newTile.HeightOffset : 0f;
        transform.position = worldPos + Vector3.up * (0.5f + tileHeight);
    }

    public System.Collections.IEnumerator MoveAlongPath(
        System.Collections.Generic.List<Vector2Int> path, float totalDuration)
    {
        if (path == null || path.Count < 2) yield break;

        GridManager grid = GridManager.Instance;
        float stepDuration = totalDuration / (path.Count - 1);

        Tile oldTile = grid.GetTile(GridPosition);
        if (oldTile != null) oldTile.RemoveUnit();

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int from = path[i - 1];
            Vector2Int to = path[i];

            Vector3 fromWorld = grid.GridToWorldPosition(from.x, from.y);
            Tile fromTile = grid.GetTile(from);
            float fromH = fromTile != null ? fromTile.HeightOffset : 0f;
            fromWorld.y = 0.5f + fromH;

            Vector3 toWorld = grid.GridToWorldPosition(to.x, to.y);
            Tile toTile = grid.GetTile(to);
            float toH = toTile != null ? toTile.HeightOffset : 0f;
            toWorld.y = 0.5f + toH;

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                transform.position = Vector3.Lerp(fromWorld, toWorld, t);
                yield return null;
            }

            transform.position = toWorld;
            GridPosition = to;
        }

        Tile newTile = grid.GetTile(GridPosition);
        if (newTile != null) newTile.PlaceUnit(gameObject);
    }

    /// <summary>
    /// true면 체력이 0이 돼도 바로 사라지지 않습니다. 피격 연출을 끝까지 보여준 뒤 Die()를 부릅니다.
    /// </summary>
    public bool HoldDeath { get; set; }

    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0 && !HoldDeath)
            Die();
    }

    public void Die()
    {
        Tile tile = GridManager.Instance.GetTile(GridPosition);
        if (tile != null) tile.RemoveUnit();
        Destroy(gameObject);
    }

    private Transform VisualTransform => spriteRenderer != null ? spriteRenderer.transform : transform;

    /// <summary>공격하는 쪽이 대상 방향으로 짧게 튀어나갔다가 돌아옵니다.</summary>
    public System.Collections.IEnumerator PlayAttackLunge(Vector3 targetWorldPosition, float duration = 0.3f)
    {
        Transform visual = VisualTransform;
        Vector3 start = visual.position;
        Vector3 direction = targetWorldPosition - start;
        direction.y = 0f;
        float cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        Vector3 peak = start + direction.normalized * Mathf.Min(direction.magnitude * 0.4f, cell * 0.6f);

        float outTime = duration * 0.4f;
        for (float t = 0f; t < outTime; t += Time.deltaTime)
        {
            if (visual == null) yield break;
            float p = t / outTime;
            visual.position = Vector3.Lerp(start, peak, 1f - (1f - p) * (1f - p));
            yield return null;
        }
        float backTime = duration - outTime;
        for (float t = 0f; t < backTime; t += Time.deltaTime)
        {
            if (visual == null) yield break;
            visual.position = Vector3.Lerp(peak, start, t / backTime);
            yield return null;
        }
        if (visual != null) visual.position = start;
    }

    /// <summary>맞은 유닛이 흔들리며 빨간색으로 깜빡입니다.</summary>
    public System.Collections.IEnumerator PlayHitReaction(float duration, Color flashColor)
    {
        Transform visual = VisualTransform;
        Vector3 start = visual.position;
        Camera cam = Camera.main;
        Vector3 side = cam != null ? cam.transform.right : Vector3.right;
        float cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
        const float blinkInterval = 0.1f;
        const float shakeTime = 0.45f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (visual == null) yield break;

            bool red = Mathf.FloorToInt(t / blinkInterval) % 2 == 0;
            if (red) ApplyColor(flashColor);
            else RestoreStateColor();

            float shake = t < shakeTime
                ? Mathf.Sin(t * 70f) * cell * 0.12f * (1f - t / shakeTime)
                : 0f;
            visual.position = start + side * shake;
            yield return null;
        }

        if (visual == null) yield break;
        visual.position = start;
        RestoreStateColor();
    }

    private void RestoreStateColor()
    {
        if (HasActed) ApplyColor(ActedColor);
        else SetTeamColor();
    }

    public void SetSelected(bool selected)
    {
        if (unitMaterial == null && spriteRenderer == null) return;
        Color color;
        if (selected)
            color = SelectedColor;
        else if (HasActed)
            color = ActedColor;
        else
            color = (UnitTeam == Team.Player) ? PlayerColor : EnemyColor;

        ApplyColor(color);
    }

    public void MarkActed()
    {
        HasActed = true;
        ApplyColor(ActedColor);
    }

    public void ResetTurn()
    {
        HasActed = false;
        SetTeamColor();
    }

    private void SetTeamColor()
    {
        if (spriteRenderer != null)
        {
            ApplyColor(Color.white);
            return;
        }

        Color color = UnitTeam == Team.Player && CharacterData != null
            ? CharacterData.TeamColor
            : (UnitTeam == Team.Player ? PlayerColor : EnemyColor);
        ApplyColor(color);
    }

    public void RemoveFromBoard()
    {
        Tile tile = GridManager.Instance != null
            ? GridManager.Instance.GetTile(GridPosition)
            : null;
        if (tile != null && tile.OccupyingUnit == gameObject)
            tile.RemoveUnit();

        Destroy(gameObject);
    }

    private void ApplyColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;

        if (unitMaterial != null)
        {
            unitMaterial.color = color;
            if (unitMaterial.HasProperty("_BaseColor"))
                unitMaterial.SetColor("_BaseColor", color);
        }
    }


    /// <summary>최대 체력을 바꾸고 가득 채웁니다. (튜토리얼 등 특수 전투용)</summary>
    public void OverrideMaxHP(int maxHp)
    {
        MaxHP = Mathf.Max(1, maxHp);
        HP = MaxHP;
    }

    public int HealPercent(float percent)
    {
        if (IsDead || MaxHP <= 0 || percent <= 0f) return 0;
        int amount = Mathf.Max(1, Mathf.CeilToInt(MaxHP * percent / 100f));
        return Heal(amount);
    }

    public int Heal(int amount)
    {
        if (IsDead || amount <= 0) return 0;
        int before = HP;
        HP = Mathf.Min(MaxHP, HP + amount);
        return HP - before;
    }

    public void BeginPlayerTurn(bool gainSp)
    {
        HasActed = false;
        sentryCounterAvailable = true;
        if (gainSp) SP = Mathf.Min(MaxSP, SP + 1);

        IsPhased = false;
        if (phasePenaltyTurns > 0)
        {
            phasePenaltyTurns--;
            if (phasePenaltyTurns <= 0) phaseAttackPercent = 0;
        }

        if (skillBuffTurnsRemaining > 0)
        {
            skillBuffTurnsRemaining--;
            if (skillBuffTurnsRemaining <= 0)
            {
                skillAttackPercent = 0;
                skillDefensePercent = HasActiveGuard ? skillDefensePercent : 0;
            }
        }

        if (guardTurnsRemaining > 0)
        {
            guardTurnsRemaining--;
            if (guardTurnsRemaining <= 0 && skillBuffTurnsRemaining <= 0)
                skillDefensePercent = 0;
        }

        RecalculateStats();
        SetTeamColor();
    }

    public bool IsSkillReady(int currentTurn)
    {
        SkillData skill = SkillData;
        return skill != null &&
               currentTurn >= Mathf.Max(1, skill.AvailableFromTurn) &&
               SP >= skill.SpCost;
    }

    public void SpendSkill()
    {
        if (SkillData == null) return;
        SP = Mathf.Max(0, SP - SkillData.SpCost);
    }

    public void GrantSkillBuff(int attackPercent, int defensePercent, int turns)
    {
        skillAttackPercent = Mathf.Max(skillAttackPercent, attackPercent);
        skillDefensePercent = Mathf.Max(skillDefensePercent, defensePercent);
        skillBuffTurnsRemaining = Mathf.Max(skillBuffTurnsRemaining, turns);
        RecalculateStats();
    }

    /// <summary>약화를 겁니다. 음수 %를 받고, 여러 번 걸리면 더 강한 쪽과 더 긴 쪽을 남깁니다.</summary>
    public void ApplyDebuff(int attackPercent, int defensePercent, int turns)
    {
        debuffAttackPercent = Mathf.Min(debuffAttackPercent, attackPercent);
        debuffDefensePercent = Mathf.Min(debuffDefensePercent, defensePercent);
        debuffTurnsRemaining = Mathf.Max(debuffTurnsRemaining, turns);
        RecalculateStats();
    }

    /// <summary>약화 남은 턴을 1 줄입니다. 걸린 유닛의 진영 턴이 끝날 때 부릅니다.</summary>
    public void TickDebuff()
    {
        if (debuffTurnsRemaining <= 0) return;
        debuffTurnsRemaining--;
        if (debuffTurnsRemaining <= 0)
        {
            debuffAttackPercent = 0;
            debuffDefensePercent = 0;
        }
        RecalculateStats();
    }

    public void ActivateGuard(int turns, int defensePercent)
    {
        guardTurnsRemaining = Mathf.Max(guardTurnsRemaining, turns);
        skillDefensePercent = Mathf.Max(skillDefensePercent, defensePercent);
        RecalculateStats();
    }

    public void ClearAuraBonuses()
    {
        auraAttackPercent = 0;
        auraDefensePercent = 0;
        RecalculateStats();
    }

    public void AddAuraBonus(int attackPercent, int defensePercent)
    {
        auraAttackPercent += attackPercent;
        auraDefensePercent += defensePercent;
        RecalculateStats();
    }

    public bool IsCharacter(string characterId)
    {
        return CharacterData != null && CharacterData.CharacterId == characterId;
    }

    public bool TryConsumeSentryCounter()
    {
        if (!CanUseSentryCounter) return false;
        sentryCounterAvailable = false;
        return true;
    }

    private void ApplyIntrinsicPassives()
    {
        if (IsCharacter("tokikawa_hina"))
            passiveDefensePercent += 20;
    }

    private void RecalculateStats()
    {
        AttackPower = ApplyPercent(baseAttackPower,
            passiveAttackPercent + auraAttackPercent + skillAttackPercent + phaseAttackPercent +
            debuffAttackPercent);
        // 약화로 방어력이 음수가 되면 받는 피해가 늘어납니다. (-100%면 2배)
        Defense = Mathf.Max(-100,
            passiveDefensePercent + auraDefensePercent + skillDefensePercent + debuffDefensePercent);
        MoveRange = baseMoveRange;
        AttackRange = baseAttackRange;
    }

    private static int ApplyPercent(int value, int percent)
    {
        if (percent == 0) return value;
        return Mathf.Max(1, Mathf.CeilToInt(value * (100f + percent) / 100f));
    }

    private void SetupSpriteVisual(Sprite sprite, float cellSize)
    {
        Transform visualTransform = transform.Find("CharacterVisual2D");
        GameObject visualObject;
        if (visualTransform == null)
        {
            visualObject = new GameObject("CharacterVisual2D");
            visualTransform = visualObject.transform;
            visualTransform.SetParent(transform, false);
        }
        else
        {
            visualObject = visualTransform.gameObject;
        }

        spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = visualObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;

        SpriteBillboard billboard = visualObject.GetComponent<SpriteBillboard>();
        if (billboard == null)
            billboard = visualObject.AddComponent<SpriteBillboard>();

        float spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
        float targetHeight = cellSize * spriteScale;
        float parentScaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        float targetLocalHeight = targetHeight / parentScaleY;
        float scale = targetLocalHeight / spriteHeight;
        visualTransform.localScale = Vector3.one * scale;
        visualTransform.localPosition = Vector3.up *
            ((spriteGroundOffset + targetHeight * 0.5f) / parentScaleY);
    }

    private void SetupExistingSpriteVisuals()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        spriteRenderer = renderers[0];
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.GetComponent<SpriteBillboard>() == null)
                renderer.gameObject.AddComponent<SpriteBillboard>();
        }
    }
}
