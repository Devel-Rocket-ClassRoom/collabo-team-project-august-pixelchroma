using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 1.2 튜토리얼 스토리 씬 전용. 세계관 대사(Addressable "Tutorial/Story/World")를 불러와 재생하고,
/// 끝나거나 스킵하면 1.3 튜토리얼 전투로 넘어갑니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TutorialStoryDirector : MonoBehaviour
{
    [SerializeField] private ScenarioController scenarioController;
    [SerializeField] private ChatManager chatManager;

    private AsyncOperationHandle<DialogueDataSO> storyHandle;

    private void Awake()
    {
        if (scenarioController == null) scenarioController = FindAnyObjectByType<ScenarioController>();
        if (chatManager == null) chatManager = FindAnyObjectByType<ChatManager>();

        // 씬에 들어 있는 다른 대사가 먼저 재생되지 않도록 비워 두고, Addressable 대사를 불러온 뒤 시작합니다.
        StorySelection.Scenario = null;
        StorySelection.NoBattle = false;
        if (scenarioController != null)
        {
            scenarioController.currentScenario = null;
            scenarioController.onFinished = TutorialFlow.LoadBattleScene;
        }
        if (chatManager != null) chatManager.currentScenario = null;
    }

    private IEnumerator Start()
    {
        storyHandle = Addressables.LoadAssetAsync<DialogueDataSO>(TutorialFlow.WorldStoryKey);
        yield return storyHandle;

        if (storyHandle.Status != AsyncOperationStatus.Succeeded || scenarioController == null)
        {
            Debug.LogError($"[Tutorial] 세계관 대사 '{TutorialFlow.WorldStoryKey}'를 불러오지 못해 전투 튜토리얼로 넘어갑니다.");
            TutorialFlow.LoadBattleScene();
            yield break;
        }

        scenarioController.StartChapter(storyHandle.Result);
    }

    private void OnDestroy()
    {
        if (storyHandle.IsValid()) Addressables.Release(storyHandle);
    }
}
