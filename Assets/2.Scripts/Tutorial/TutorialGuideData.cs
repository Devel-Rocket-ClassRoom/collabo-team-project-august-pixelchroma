using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 한 단계가 끝나는 조건입니다.</summary>
public enum TutorialStepGoal
{
    [InspectorName("다음 버튼 누르기")] PressNext,
    [InspectorName("배치 후 작전 개시")] StartBattle,
    [InspectorName("아군 선택")] SelectUnit,
    [InspectorName("아군 이동")] MoveUnit,
    [InspectorName("적 공격")] Attack,
    [InspectorName("위협 범위 버튼 누르기")] ToggleThreatRange,
    [InspectorName("전투 승리")] WinBattle
}

[System.Serializable]
public class TutorialStep
{
    public string speaker = "키타노 유키";
    [TextArea(2, 6)] public string text;
    public TutorialStepGoal goal = TutorialStepGoal.PressNext;
    [Tooltip("화면에 작게 띄우는 목표 문구. 비우면 표시하지 않습니다.")]
    public string objective;
}

/// <summary>
/// 1.3 튜토리얼 전투에서 순서대로 보여줄 안내입니다. (Addressable "Tutorial/Battle/Guide")
/// 대사와 순서는 이 에셋만 고치면 바뀝니다.
/// </summary>
[CreateAssetMenu(fileName = "TutorialGuide", menuName = "Game/Tutorial Guide Data")]
public class TutorialGuideData : ScriptableObject
{
    public Sprite speakerPortrait;
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("결과")]
    [TextArea(2, 6)] public string victoryText = "훈련 종료. 수고하셨습니다, 국장님.";
    [TextArea(2, 6)] public string defeatText = "괜찮습니다. 모의 훈련이니 다시 해 보죠.";
}
