using UnityEngine;

public enum CharacterAttackPattern
{
    SingleTarget,
    CrossArea,
    DiamondArea,
    PiercingLine,
    Cone,
    Chain
}

public enum CharacterDamageType
{
    Physical,
    Magical,
    Support
}

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField] private string characterId = "character";
    [SerializeField] private string displayName = "Character";
    [SerializeField, TextArea] private string description = "";
    [SerializeField] private GameObject battlePrefab;

    [Header("2D Battle Visual")]
    [Tooltip("전투 맵에서 캐릭터 대신 표시할 2D 이미지입니다. 비워 두면 프리팹 또는 GameManager의 기본 이미지를 사용합니다.")]
    [SerializeField] private Sprite battleSprite;
    [SerializeField, Min(1)] private int maxHP = 3;
    [SerializeField, Min(0)] private int attackPower = 1;
    [SerializeField, Min(1)] private int moveRange = 3;
    [SerializeField, Min(1)] private int attackRange = 1;

    [Header("Special Attack")]
    [SerializeField] private SkillData skillData;
    [SerializeField] private CharacterAttackPattern attackPattern = CharacterAttackPattern.SingleTarget;
    [SerializeField] private CharacterDamageType damageType = CharacterDamageType.Physical;
    [SerializeField, Min(0)] private int areaRadius;
    [SerializeField, Min(1)] private int maxTargets = 1;
    [SerializeField, Min(0)] private int specialPower;
    [SerializeField, Min(0)] private int cooldownTurns;
    [SerializeField] private bool ignoresCover;
    [SerializeField] private bool canFriendlyFire;
    [SerializeField] private Color teamColor = new Color(0.15f, 0.4f, 1f, 1f);

    [Header("Combat")]
    [Tooltip("기본 명중률(%)입니다. 대상의 회피를 빼서 최종 명중률을 구합니다.")]
    [SerializeField, Range(0, 100)] private int accuracy = 90;

    [Tooltip("회피율(%)입니다. 상대 명중률에서 이 값만큼 빠집니다.")]
    [SerializeField, Range(0, 100)] private int evasion = 5;

    [Tooltip("치명타 확률(%)입니다. 치명타는 피해가 1.5배가 됩니다.")]
    [SerializeField, Range(0, 100)] private int criticalRate = 10;

    [Tooltip("공격받았을 때 반격할 수 있는지 여부입니다. 사거리 안에 있어야 반격합니다.")]
    [SerializeField] private bool canCounter = true;

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public string Description => description;
    public GameObject BattlePrefab => battlePrefab;
    public Sprite BattleSprite => battleSprite;
    public int MaxHP => maxHP;
    public int AttackPower => attackPower;
    public int MoveRange => moveRange;
    public int AttackRange => attackRange;
    public SkillData SkillData => skillData;
    public CharacterAttackPattern AttackPattern => skillData != null ? skillData.AttackPattern : attackPattern;
    public CharacterDamageType DamageType => skillData != null ? skillData.DamageType : damageType;
    public int AreaRadius => skillData != null ? skillData.AreaRadius : areaRadius;
    public int MaxTargets => skillData != null ? skillData.MaxTargets : maxTargets;
    public int SpecialPower => skillData != null ? skillData.FlatPower : specialPower;
    public int CooldownTurns => skillData != null ? skillData.CooldownTurns : cooldownTurns;
    public bool IgnoresCover => skillData != null ? skillData.IgnoresCover : ignoresCover;
    public bool CanFriendlyFire => skillData != null ? skillData.CanFriendlyFire : canFriendlyFire;
    public Color TeamColor => teamColor;

    public int Accuracy => accuracy;
    public int Evasion => evasion;
    public int CriticalRate => criticalRate;
    public bool CanCounter => canCounter;

    public void ConfigureRuntime(
        string id,
        string name,
        GameObject prefab,
        int hp,
        int attack,
        int movement,
        int range,
        Color color,
        Sprite sprite = null)
    {
        characterId = id;
        displayName = name;
        battlePrefab = prefab;
        maxHP = hp;
        attackPower = attack;
        moveRange = movement;
        attackRange = range;
        teamColor = color;
        battleSprite = sprite;
    }
}
