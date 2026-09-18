using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 1.3 튜토리얼 전투 씬 전용. GameManager를 튜토리얼 모드로 돌리고,
/// 가이드 데이터(Addressable "Tutorial/Battle/Guide")의 단계를 하나씩 보여주며
/// 플레이어가 이동 → 공격 → 반격 확인 → 위협 범위 확인을 직접 해 보게 합니다.
/// 전투에서 이기면 완료 파일(Tutorial Clear.json)을 만들고 메인 메뉴로 갑니다.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TutorialBattleDirector : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("튜토리얼 전투 설정 (임시)")]
    [Tooltip("튜토리얼에서 배치할 수 있는 요원. 타이틀에서 바로 들어와 스쿼드 편성이 없으므로 여기서 정합니다.")]
    [SerializeField] private List<CharacterData> tutorialRoster = new List<CharacterData>();
    [Tooltip("튜토리얼에서 아군 요원의 최대 체력. 0이면 캐릭터 원래 체력을 씁니다.")]
    [SerializeField, Min(0)] private int playerMaxHp = 10;

    private AsyncOperationHandle<TutorialGuideData> guideHandle;
    private AsyncOperationHandle<GameObject> viewHandle;
    private TutorialGuideData guide;
    private TutorialGuideView view;
    private int stepIndex = -1;
    private bool finished;

    private TutorialStep CurrentStep =>
        guide != null && stepIndex >= 0 && stepIndex < guide.steps.Count ? guide.steps[stepIndex] : null;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[Tutorial] 씬에 GameManager가 없습니다.");
            return;
        }

        gameManager.TutorialMode = true;
        gameManager.RosterOverride = tutorialRoster;
        gameManager.PlayerMaxHpOverride = playerMaxHp;
        // 위협 범위는 튜토리얼에서 버튼을 눌러 직접 켜 보도록 꺼 둔 채 시작합니다.
        gameManager.SetThreatRangeVisible(false);
        gameManager.BattleStarted += OnBattleStarted;
        gameManager.PlayerUnitSelected += OnUnitSelected;
        gameManager.PlayerUnitMoved += OnUnitMoved;
        gameManager.PlayerAttackFinished += OnAttackFinished;
        gameManager.ThreatRangeToggled += OnThreatToggled;
        gameManager.BattleEnded += OnBattleEnded;
        gameManager.TutorialResultConfirmed += OnResultConfirmed;
    }

    private IEnumerator Start()
    {
        guideHandle = Addressables.LoadAssetAsync<TutorialGuideData>(TutorialFlow.BattleGuideKey);
        viewHandle = Addressables.InstantiateAsync(TutorialFlow.GuideViewKey);
        yield return guideHandle;
        yield return viewHandle;

        if (guideHandle.Status != AsyncOperationStatus.Succeeded ||
            viewHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[Tutorial] 튜토리얼 가이드 데이터/UI를 불러오지 못했습니다. 안내 없이 전투만 진행합니다.");
            yield break;
        }

        guide = guideHandle.Result;
        view = viewHandle.Result.GetComponent<TutorialGuideView>();
        view.name = "TutorialGuide (PlayHere)";
        view.NextPressed += OnNextPressed;
        view.SkipConfirmed += SkipTutorial;

        GoToStep(0);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.BattleStarted -= OnBattleStarted;
            gameManager.PlayerUnitSelected -= OnUnitSelected;
            gameManager.PlayerUnitMoved -= OnUnitMoved;
            gameManager.PlayerAttackFinished -= OnAttackFinished;
            gameManager.ThreatRangeToggled -= OnThreatToggled;
            gameManager.BattleEnded -= OnBattleEnded;
            gameManager.TutorialResultConfirmed -= OnResultConfirmed;
        }
        if (viewHandle.IsValid()) Addressables.ReleaseInstance(viewHandle);
        if (guideHandle.IsValid()) Addressables.Release(guideHandle);
    }

    // ── 단계 진행 ──

    private void GoToStep(int index)
    {
        if (finished || guide == null || view == null) return;
        stepIndex = index;

        TutorialStep step = CurrentStep;
        if (step == null)
        {
            // 모든 안내가 끝나면 목표만 남기고 자유롭게 싸웁니다.
            view.ShowObjectiveOnly("목표: 적을 모두 쓰러뜨리세요");
            return;
        }

        if (step.goal == TutorialStepGoal.WinBattle)
        {
            view.Show(guide.speakerPortrait, step.speaker, step.text, step.objective, "확인");
            return;
        }

        string button = step.goal == TutorialStepGoal.PressNext ? "다음" : null;
        view.Show(guide.speakerPortrait, step.speaker, step.text, step.objective, button);
    }

    private void Complete(TutorialStepGoal goal)
    {
        TutorialStep step = CurrentStep;
        if (step == null || step.goal != goal) return;
        GoToStep(stepIndex + 1);
    }

    private void OnNextPressed()
    {
        if (finished)
        {
            return;
        }

        TutorialStep step = CurrentStep;
        if (step == null) return;

        if (step.goal == TutorialStepGoal.PressNext)
            GoToStep(stepIndex + 1);
        else if (step.goal == TutorialStepGoal.WinBattle)
            view.ShowObjectiveOnly(string.IsNullOrEmpty(step.objective) ? "목표: 적을 모두 쓰러뜨리세요" : step.objective);
    }

    private void OnBattleStarted() => Complete(TutorialStepGoal.StartBattle);
    private void OnUnitSelected(Unit unit) => Complete(TutorialStepGoal.SelectUnit);
    private void OnUnitMoved(Unit unit) => Complete(TutorialStepGoal.MoveUnit);
    private void OnAttackFinished(Unit attacker, Unit target, bool counter) => Complete(TutorialStepGoal.Attack);
    private void OnThreatToggled(bool visible) => Complete(TutorialStepGoal.ToggleThreatRange);

    // ── 결과 ──

    private void OnBattleEnded(bool victory)
    {
        if (guide == null || view == null) return;
        finished = true;
        view.Show(guide.speakerPortrait, "키타노 유키",
            victory ? guide.victoryText : guide.defeatText,
            victory ? "결과 화면의 버튼을 눌러 훈련을 마치세요" : "결과 화면의 버튼을 눌러 다시 도전하세요",
            null);
    }

    private void OnResultConfirmed(bool victory)
    {
        if (victory) TutorialFlow.Complete();
        else TutorialFlow.LoadBattleScene();
    }

    private void SkipTutorial()
    {
        finished = true;
        TutorialFlow.Complete();
    }
}
