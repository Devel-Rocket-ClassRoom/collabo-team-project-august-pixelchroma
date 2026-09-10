using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 단체입니다. 교리와 난이도가 유닛이 아니라 단체에 붙으므로,
/// 같은 캐릭터라도 소속 단체에 따라 다르게 행동합니다.
/// </summary>
[CreateAssetMenu(fileName = "EnemySquad", menuName = "Game/Enemy/Squad Data")]
public class EnemySquadData : ScriptableObject
{
    [Header("── 단체 정보 ──")]
    [SerializeField] private string squadId = "squad";
    [SerializeField] private string squadName = "능력자자유해방전선";
    [SerializeField, TextArea] private string description = "";

    [Header("── 교리 ──")]
    [Tooltip("Assault 돌격 / Hold 대기 / Snipe 저격 / Command 지휘")]
    [SerializeField] private SquadDoctrine doctrine = SquadDoctrine.Assault;
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Easy;
    [SerializeField] private AIWeightProfile weightProfile;

    [Header("── 교리 파라미터 ──")]
    [Tooltip("대기형 전용. 플레이어가 이 반경 안에 들어오기 전까지 움직이지 않습니다.")]
    [SerializeField, Min(0)] private int activationRange = 3;

    [Tooltip("지휘형 전용. 체력이 이 비율 이하로 떨어지면 후퇴로 전환합니다.")]
    [SerializeField, Range(0f, 1f)] private float retreatHpRatio = 0.4f;

    [Tooltip("어려움 난이도에서 단체가 표적을 나눠 맡을지 여부입니다.")]
    [SerializeField] private bool coordinateTargets = true;

    [Header("── 편성 ──")]
    [SerializeField] private List<SquadMember> members = new List<SquadMember>();

    [Header("── 배치 ──")]
    [Tooltip("배치 가능한 Y좌표 범위입니다. 기본은 적 구역(y 4~5)입니다.")]
    [SerializeField] private Vector2Int spawnRowRange = new Vector2Int(4, 5);

    public string SquadId => squadId;
    public string SquadName => squadName;
    public string Description => description;

    public SquadDoctrine Doctrine => doctrine;
    public AIDifficulty Difficulty => difficulty;
    public AIWeightProfile WeightProfile => weightProfile;

    public int ActivationRange => activationRange;
    public float RetreatHpRatio => retreatHpRatio;
    public bool CoordinateTargets => coordinateTargets;

    public IReadOnlyList<SquadMember> Members => members;
    public Vector2Int SpawnRowRange => spawnRowRange;

    /// <summary>이 단체가 전장에 세우는 총 유닛 수입니다.</summary>
    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (SquadMember member in members)
            {
                if (member.unit != null)
                    total += Mathf.Max(1, member.count);
            }
            return total;
        }
    }

    /// <summary>편성을 유닛 단위로 펼쳐 반환합니다. 스폰 시 사용합니다.</summary>
    public List<EnemyUnitData> BuildRoster()
    {
        var roster = new List<EnemyUnitData>();
        foreach (SquadMember member in members)
        {
            if (member.unit == null) continue;
            int count = Mathf.Max(1, member.count);
            for (int i = 0; i < count; i++)
                roster.Add(member.unit);
        }
        return roster;
    }

    /// <summary>이 유닛에 적용할 교리입니다. 유닛이 개별 오버라이드를 가지면 그쪽이 우선합니다.</summary>
    public SquadDoctrine GetDoctrineFor(EnemyUnitData unit)
        => unit != null && unit.OverrideDoctrine ? unit.DoctrineOverride : doctrine;

    [System.Serializable]
    public struct SquadMember
    {
        public EnemyUnitData unit;

        [Min(1)] public int count;

        [Tooltip("고정 배치 좌표입니다. 비워 두면 적 구역 안에서 무작위 배치합니다.")]
        public Vector2Int[] preferredCells;
    }
}
