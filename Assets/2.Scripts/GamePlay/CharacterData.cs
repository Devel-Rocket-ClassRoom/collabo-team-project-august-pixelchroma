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
    [SerializeField, Min(1)] private int maxHP = 3;
    [SerializeField, Min(0)] private int attackPower = 1;
    [SerializeField, Min(1)] private int moveRange = 3;
    [SerializeField, Min(1)] private int attackRange = 1;

    [Header("Special Attack")]
    [SerializeField] private CharacterAttackPattern attackPattern = CharacterAttackPattern.SingleTarget;
    [SerializeField] private CharacterDamageType damageType = CharacterDamageType.Physical;
    [SerializeField, Min(0)] private int areaRadius;
    [SerializeField, Min(1)] private int maxTargets = 1;
    [SerializeField, Min(0)] private int specialPower;
    [SerializeField, Min(0)] private int cooldownTurns;
    [SerializeField] private bool ignoresCover;
    [SerializeField] private bool canFriendlyFire;
    [SerializeField] private Color teamColor = new Color(0.15f, 0.4f, 1f, 1f);

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public string Description => description;
    public GameObject BattlePrefab => battlePrefab;
    public int MaxHP => maxHP;
    public int AttackPower => attackPower;
    public int MoveRange => moveRange;
    public int AttackRange => attackRange;
    public CharacterAttackPattern AttackPattern => attackPattern;
    public CharacterDamageType DamageType => damageType;
    public int AreaRadius => areaRadius;
    public int MaxTargets => maxTargets;
    public int SpecialPower => specialPower;
    public int CooldownTurns => cooldownTurns;
    public bool IgnoresCover => ignoresCover;
    public bool CanFriendlyFire => canFriendlyFire;
    public Color TeamColor => teamColor;

    public void ConfigureRuntime(
        string id,
        string name,
        GameObject prefab,
        int hp,
        int attack,
        int movement,
        int range,
        Color color)
    {
        characterId = id;
        displayName = name;
        battlePrefab = prefab;
        maxHP = hp;
        attackPower = attack;
        moveRange = movement;
        attackRange = range;
        teamColor = color;
    }
}
