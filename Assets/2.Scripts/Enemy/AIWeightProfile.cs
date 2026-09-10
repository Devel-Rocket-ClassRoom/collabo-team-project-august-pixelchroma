using UnityEngine;

/// <summary>
/// 행동 평가에 쓰이는 모든 가중치입니다.
///
/// 미션 요구사항의 "평가 기준은 외부 데이터로 정의되어 유형·난이도별로
/// 다르게 조정할 수 있어야 함"에 대응합니다. 여기 있는 값만 바꾸면
/// 코드 수정 없이 적의 성향이 바뀝니다.
/// </summary>
[CreateAssetMenu(fileName = "AIWeights", menuName = "Game/Enemy/AI Weight Profile")]
public class AIWeightProfile : ScriptableObject
{
    [Header("── 역할별 표적 가중치 ──")]
    [Tooltip("때려도 이득이 적으므로 의도적으로 최하위입니다.")]
    [SerializeField] private int tankWeight = 15;
    [SerializeField] private int meleeDealerWeight = 40;
    [SerializeField] private int rangedDealerWeight = 55;
    [SerializeField] private int supporterWeight = 70;
    [Tooltip("미션 요구사항이 명시한 최우선 표적입니다.")]
    [SerializeField] private int healerWeight = 100;

    [Tooltip("쉬움 난이도에서 역할 구분 없이 사용할 고정 점수입니다.")]
    [SerializeField] private int flatWeightOnEasy = 50;

    [Header("── 상황 가중치 ──")]
    [Tooltip("이번 공격으로 대상을 처치할 수 있을 때 가산됩니다.")]
    [SerializeField] private int killBonus = 120;

    [Tooltip("대상의 잃은 체력 비율에 곱해집니다.")]
    [SerializeField] private int hpRatioScale = 40;

    [SerializeField] private int affinityBonus = 35;
    [SerializeField] private int affinityPenalty = 25;

    [Tooltip("받을 것으로 예상되는 반격 피해에 곱해 감점합니다.")]
    [SerializeField] private int counterRiskScale = 30;

    [Tooltip("이번 턴에 도달하지 못하는 칸 수에 곱해 감점합니다.")]
    [SerializeField] private int distancePenalty = 20;

    [SerializeField] private int highGroundBonus = 20;
    [SerializeField] private int coverBonus = 12;

    [Tooltip("이미 처치가 확정된 대상을 또 노릴 때 감점합니다. 표적 분산용입니다.")]
    [SerializeField] private int focusPenalty = 45;

    [Tooltip("후퇴 임계 이하일 때 적에게 접근하는 행동을 감점합니다.")]
    [SerializeField] private int selfPreserveScale = 60;

    [Header("── 난이도별 활성 지표 ──")]
    [SerializeField] private AIMetric easyMetrics = AIMetricPresets.Easy;
    [SerializeField] private AIMetric normalMetrics = AIMetricPresets.Normal;
    [SerializeField] private AIMetric hardMetrics = AIMetricPresets.Hard;

    public int KillBonus => killBonus;
    public int HpRatioScale => hpRatioScale;
    public int AffinityBonus => affinityBonus;
    public int AffinityPenalty => affinityPenalty;
    public int CounterRiskScale => counterRiskScale;
    public int DistancePenalty => distancePenalty;
    public int HighGroundBonus => highGroundBonus;
    public int CoverBonus => coverBonus;
    public int FocusPenalty => focusPenalty;
    public int SelfPreserveScale => selfPreserveScale;
    public int FlatWeightOnEasy => flatWeightOnEasy;

    public int GetRoleWeight(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Tank: return tankWeight;
            case EnemyRole.MeleeDealer: return meleeDealerWeight;
            case EnemyRole.RangedDealer: return rangedDealerWeight;
            case EnemyRole.Supporter: return supporterWeight;
            case EnemyRole.Healer: return healerWeight;
            default: return 0;
        }
    }

    public AIMetric GetMetrics(AIDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy: return easyMetrics;
            case AIDifficulty.Normal: return normalMetrics;
            case AIDifficulty.Hard: return hardMetrics;
            default: return AIMetric.None;
        }
    }

    public bool IsEnabled(AIDifficulty difficulty, AIMetric metric)
        => (GetMetrics(difficulty) & metric) != 0;
}
