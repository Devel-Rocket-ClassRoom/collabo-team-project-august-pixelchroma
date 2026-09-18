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


/// <summary>
/// 스킬이 실제로 하는 일입니다. 새 스킬은 효과와 아래 수치만 채우면 코드 수정 없이 동작합니다.
/// </summary>
public enum SkillEffectType
{
    [InspectorName("피해 (적)")] Damage,
    [InspectorName("회복 (아군)")] Heal,
    [InspectorName("강화 (아군)")] Buff,
    [InspectorName("약화 (적)")] Debuff,
    [InspectorName("수호 (주변 아군 대신 맞기)")] Guard,
    [InspectorName("유체화 (칸 이동 + 공격 대상 제외)")] Phase
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

    [Header("SP")]
    [Tooltip("스킬에 드는 SP입니다. 캐릭터 SP는 0에서 시작해 턴마다 1씩 차고 최대 5입니다.")]
    [SerializeField, Range(1, 5)] private int spCost = 3;

    [Header("Effect")]
    [Tooltip("스킬이 하는 일. 피해/약화는 적을 눌러 쓰고, 회복/강화/수호는 누르면 바로 발동, 유체화는 칸을 골라 씁니다.")]
    [SerializeField] private SkillEffectType effectType = SkillEffectType.Damage;
    [Tooltip("범위 스킬에서 처음 고른 적 이외의 적에게 들어가는 효과 비율(%)")]
    [SerializeField, Range(0, 100)] private int splashPercent = 70;
    [Tooltip("피해 스킬을 맞은 적이 반격할 수 있는지 여부")]
    [SerializeField] private bool allowCounter = true;

    [Header("Cut-in")]
    [Tooltip("스킬 컷인에 쓸 이미지. 비우면 캐릭터 일러스트를 씁니다.")]
    [SerializeField] private Sprite cutInImage;
    [Tooltip("이 스킬만 다른 컷인 연출을 쓰고 싶을 때 넣습니다. 비우면 기본 컷인 프리팹을 씁니다.")]
    [SerializeField] private SkillCutInView cutInPrefab;

    [Header("Turn Activation")]
    [SerializeField] private SkillActivationCondition activationCondition = SkillActivationCondition.TurnCooldown;
    [SerializeField, Min(1)] private int availableFromTurn = 1;
    [Tooltip("SP 시스템 도입으로 쓰지 않습니다. (기획: 별도 쿨타임 없음)")]
    [SerializeField, Min(0)] private int cooldownTurns;
    [SerializeField, Min(0)] private int triggerIntervalTurns;
    [Tooltip("강화/약화/수호/유체화 효과가 유지되는 턴 수")]
    [SerializeField, Min(0)] private int durationTurns;

    [Header("Targeting")]
    [Tooltip("아군 스킬의 대상: Self=자신, Ally=사거리 안 아군(MaxTargets명), AllAllies=사거리 안 아군 전원")]
    [SerializeField] private SkillTargetType targetType = SkillTargetType.Enemy;
    [SerializeField] private CharacterAttackPattern attackPattern = CharacterAttackPattern.SingleTarget;
    [SerializeField] private CharacterDamageType damageType = CharacterDamageType.Physical;
    [Tooltip("적 스킬: 공격 사거리 / 아군 스킬: 시전자 주변 칸 수(0이면 맵 전체) / 유체화: 이동 거리")]
    [SerializeField, Min(0)] private int range;
    [Tooltip("적 스킬: 처음 고른 적 주변 몇 칸까지 함께 맞는지 (0이면 단일)")]
    [SerializeField, Min(0)] private int areaRadius;
    [SerializeField, Min(1)] private int maxTargets = 1;

    [Header("Power")]
    [Tooltip("피해: 공격력 대비 % / 회복: 최대 체력 대비 %")]
    [SerializeField, Min(0f)] private float powerPercent = 100f;
    [Tooltip("피해·회복에 더해지는 고정 수치")]
    [SerializeField, Min(0)] private int flatPower;
    [SerializeField] private bool ignoresCover;
    [SerializeField] private bool canFriendlyFire;
    [Tooltip("강화/약화/수호/유체화가 바꾸는 능력치(%). 약화는 음수로 적습니다.")]
    [SerializeField] private List<SkillStatModifier> statModifiers = new List<SkillStatModifier>();

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;
    public int SpCost => spCost;
    public SkillEffectType EffectType => effectType;
    public int SplashPercent => splashPercent;
    public bool AllowCounter => allowCounter;
    public Sprite CutInImage => cutInImage;
    public SkillCutInView CutInPrefab => cutInPrefab;
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

    /// <summary>적을 눌러 쓰는 스킬인지 (피해 · 약화)</summary>
    public bool TargetsEnemy =>
        effectType == SkillEffectType.Damage || effectType == SkillEffectType.Debuff;

    /// <summary>능력치 보정 중 해당 항목의 % 합계</summary>
    public int GetStatPercent(SkillStatType statType)
    {
        int total = 0;
        foreach (SkillStatModifier modifier in statModifiers)
        {
            if (modifier.StatType == statType)
                total += Mathf.RoundToInt(modifier.PercentAmount);
        }
        return total;
    }
}
