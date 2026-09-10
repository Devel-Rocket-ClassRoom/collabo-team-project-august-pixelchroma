using UnityEngine;

/// <summary>
/// 적 캐릭터 데이터입니다.
///
/// CharacterData를 상속하므로 이미지·프리팹·스탯 인스펙터 구성이
/// 플레이어 캐릭터와 완전히 동일하고, Unit.Create()에 그대로 넘길 수 있습니다.
/// 즉 2D 이미지를 지정하면 Unit이 CharacterVisual2D 자식을 만들고
/// SpriteBillboard를 붙이므로, 전장을 회전시켜도 이미지가 항상 MainCamera를 봅니다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyUnit", menuName = "Game/Enemy/Unit Data")]
public class EnemyUnitData : CharacterData
{
    [Header("── 적 분류 ──")]
    [Tooltip("AI 표적 우선순위를 결정합니다. 힐러가 최우선 표적입니다.")]
    [SerializeField] private EnemyRole role = EnemyRole.MeleeDealer;

    [Tooltip("이 유닛이 가하는 공격의 타입입니다. 상대 방어 타입과 상성을 이룹니다.")]
    [SerializeField] private AttackType attackType = AttackType.Pierce;

    [Tooltip("이 유닛이 받는 피해의 상성 기준입니다.")]
    [SerializeField] private ArmorType armorType = ArmorType.Light;

    [Header("── 특성 ──")]
    [Tooltip("보호·회복·강화 등 이 유닛만의 부가 능력입니다.")]
    [SerializeField] private EnemyTrait trait = EnemyTrait.None;

    [Tooltip("특성의 수치입니다. 회복량, 피해 감소량, 사거리 보정치 등으로 쓰입니다.")]
    [SerializeField, Min(0)] private int traitValue = 1;

    [Tooltip("특성이 영향을 미치는 범위(칸)입니다. 0이면 자기 자신에게만 적용합니다.")]
    [SerializeField, Min(0)] private int traitRadius = 1;

    [Tooltip("특성 재사용까지 필요한 턴 수입니다. 0이면 매 턴 사용 가능합니다.")]
    [SerializeField, Min(0)] private int traitCooldown;

    [Header("── AI 개별 보정 ──")]
    [Tooltip("체크하면 이 유닛만 단체 교리를 무시하고 아래 교리를 따릅니다.")]
    [SerializeField] private bool overrideDoctrine;
    [SerializeField] private SquadDoctrine doctrineOverride = SquadDoctrine.Assault;

    [Tooltip("이 유닛이 표적이 될 때 역할 가중치에 더해지는 값입니다. 보스 등에 사용합니다.")]
    [SerializeField] private int threatModifier;

    public EnemyRole Role => role;
    public AttackType EnemyAttackType => attackType;
    public ArmorType EnemyArmorType => armorType;

    public EnemyTrait Trait => trait;
    public int TraitValue => traitValue;
    public int TraitRadius => traitRadius;
    public int TraitCooldown => traitCooldown;

    public bool OverrideDoctrine => overrideDoctrine;
    public SquadDoctrine DoctrineOverride => doctrineOverride;
    public int ThreatModifier => threatModifier;

    /// <summary>붙어야 일하는 역할인지 여부입니다. AI의 접근 판단에 사용합니다.</summary>
    public bool IsFrontline => role == EnemyRole.Tank || role == EnemyRole.MeleeDealer;

    /// <summary>사거리를 유지해야 하는 역할인지 여부입니다.</summary>
    public bool IsBackline => role == EnemyRole.RangedDealer ||
                              role == EnemyRole.Healer ||
                              role == EnemyRole.Supporter;
}

/// <summary>적 유닛의 부가 능력입니다.</summary>
public enum EnemyTrait
{
    None,

    /// <summary>인접 아군이 받는 피해를 traitValue 만큼 줄입니다.</summary>
    Guard,

    /// <summary>아군 1명의 체력을 traitValue 만큼 회복합니다.</summary>
    Heal,

    /// <summary>범위 내 아군의 사거리를 traitValue 만큼 늘립니다.</summary>
    Spot,

    /// <summary>범위 내 적의 이동력을 traitValue 만큼 줄입니다.</summary>
    Slow,

    /// <summary>범위 내 아군의 공격력을 traitValue 만큼 늘립니다.</summary>
    Command,

    /// <summary>이동 거리가 2칸 이상이면 피해가 traitValue 만큼 늘어납니다.</summary>
    Momentum,

    /// <summary>고지대에 있을 때 피해가 traitValue 만큼 늘어납니다.</summary>
    HighGroundBonus,

    /// <summary>이동한 턴에는 공격할 수 없습니다.</summary>
    Emplaced
}
