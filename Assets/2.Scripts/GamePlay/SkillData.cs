using System.Collections.Generic;
using UnityEngine;

public enum SkillActivationCondition
{
    Manual,
    TurnCooldown,
    EveryNTurn,
    OncePerBattleAfterTurn
}

public enum SkillTargetType
{
    Enemy,
    Ally,
    Self,
    AllAllies,
    Tile
}

public enum SkillStatType
{
    MaxHP,
    AttackPower,
    Defense,
    MoveRange,
    AttackRange
}

[System.Serializable]
public struct SkillStatModifier
{
    public SkillStatType StatType;
    public int FlatAmount;
    public float PercentAmount;
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField] private string skillId = "skill";
    [SerializeField] private string displayName = "Skill";
    [SerializeField, TextArea] private string description = "";

    [Header("Turn Activation")]
    [SerializeField] private SkillActivationCondition activationCondition = SkillActivationCondition.TurnCooldown;
    [SerializeField, Min(1)] private int availableFromTurn = 1;
    [SerializeField, Min(0)] private int cooldownTurns;
    [SerializeField, Min(0)] private int triggerIntervalTurns;
    [SerializeField, Min(0)] private int durationTurns;

    [Header("Targeting")]
    [SerializeField] private SkillTargetType targetType = SkillTargetType.Enemy;
    [SerializeField] private CharacterAttackPattern attackPattern = CharacterAttackPattern.SingleTarget;
    [SerializeField] private CharacterDamageType damageType = CharacterDamageType.Physical;
    [SerializeField, Min(0)] private int range;
    [SerializeField, Min(0)] private int areaRadius;
    [SerializeField, Min(1)] private int maxTargets = 1;

    [Header("Power")]
    [SerializeField, Min(0f)] private float powerPercent = 100f;
    [SerializeField, Min(0)] private int flatPower;
    [SerializeField] private bool ignoresCover;
    [SerializeField] private bool canFriendlyFire;
    [SerializeField] private List<SkillStatModifier> statModifiers = new List<SkillStatModifier>();

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;
    public SkillActivationCondition ActivationCondition => activationCondition;
    public int AvailableFromTurn => availableFromTurn;
    public int CooldownTurns => cooldownTurns;
    public int TriggerIntervalTurns => triggerIntervalTurns;
    public int DurationTurns => durationTurns;
    public SkillTargetType TargetType => targetType;
    public CharacterAttackPattern AttackPattern => attackPattern;
    public CharacterDamageType DamageType => damageType;
    public int Range => range;
    public int AreaRadius => areaRadius;
    public int MaxTargets => maxTargets;
    public float PowerPercent => powerPercent;
    public int FlatPower => flatPower;
    public bool IgnoresCover => ignoresCover;
    public bool CanFriendlyFire => canFriendlyFire;
    public IReadOnlyList<SkillStatModifier> StatModifiers => statModifiers;
}
