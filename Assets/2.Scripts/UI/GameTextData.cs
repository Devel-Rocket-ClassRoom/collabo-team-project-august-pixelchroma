using UnityEngine;

[CreateAssetMenu(fileName = "GameTextData", menuName = "Game/TextData")]
public class GameTextData : ScriptableObject
{
    [Header("Deployment")]
    [Tooltip("{0} = deployed count, {1} = max units")]
    public string deployFormat = "배치: {0} / {1}\n초록색 칸을 누르세요";

    [Header("Ready")]
    public string readyToStart = "모든 유닛이 준비되었습니다!";

    [Header("Player Turn")]
    [Tooltip("{0} = turn count")]
    public string playerTurnHeader = "아군 턴 - {0}턴";
    public string idle = "행동할 유닛을 선택하세요";
    public string unitSelected = "파랑=이동  빨강=공격";
    public string unitMoved = "빨강=공격  되돌리기/대기";

    [Tooltip("{0} = HP, {1} = ATK, {2} = MOV")]
    public string unitStatsFormat = "\n체력:{0}  공격:{1}  이동:{2}";

    [Header("Enemy Turn")]
    public string enemyTurn = "적군 턴...";

    [Header("Battle Result")]
    public string victory = "승리!";
    public string defeat = "패배...";
}
