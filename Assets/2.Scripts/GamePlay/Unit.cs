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
    private int guardTurnsRemaining;
    private int skillCooldownRemaining;
    private bool sentryCounterAvailable = true;

    public bool IsDead => HP <= 0;
    public SkillData SkillData => CharacterData != null ? CharacterData.SkillData : null;
    public bool HasActiveGuard => guardTurnsRemaining > 0;
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
        unit.guardTurnsRemaining = 0;
        unit.skillCooldownRemaining = 0;
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

    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0)
        {
            Tile tile = GridManager.Instance.GetTile(GridPosition);
            if (tile != null) tile.RemoveUnit();
            Destroy(gameObject);
        }
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

    public void BeginPlayerTurn()
    {
        HasActed = false;
        sentryCounterAvailable = true;
        if (skillCooldownRemaining > 0)
            skillCooldownRemaining--;

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
               skillCooldownRemaining <= 0;
    }

    public void SpendSkill()
    {
        if (SkillData == null) return;
        skillCooldownRemaining = Mathf.Max(0, SkillData.CooldownTurns);
    }

    public void GrantSkillBuff(int attackPercent, int defensePercent, int turns)
    {
        skillAttackPercent = Mathf.Max(skillAttackPercent, attackPercent);
        skillDefensePercent = Mathf.Max(skillDefensePercent, defensePercent);
        skillBuffTurnsRemaining = Mathf.Max(skillBuffTurnsRemaining, turns);
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
            passiveAttackPercent + auraAttackPercent + skillAttackPercent);
        Defense = Mathf.Max(0,
            passiveDefensePercent + auraDefensePercent + skillDefensePercent);
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
