using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioController : MonoBehaviour
{
    private const string StageListScene = "3.Stage List";

    public DialogueDataSO currentScenario;
    [Tooltip("마지막 챕터(nextStorySO 없음)가 끝나면 이동할 씬. 비워두면 이동하지 않습니다.")]
    public string sceneOnFinished = "3.5.Squad Select";
    public int currentGroupIndex =0;
    public int currentEntryIndex =0;

    // Awake so the stage's scenario is in place before ChatManager.Start checks it.
    void Awake()
    {
        if (StorySelection.Scenario != null)
        {
            currentScenario = StorySelection.Scenario;
            if (StorySelection.NoBattle)
                sceneOnFinished = StageListScene;
            StorySelection.Scenario = null;
            StorySelection.NoBattle = false;
        }
    }

    public void SkipStory()
    {
        if (!string.IsNullOrEmpty(sceneOnFinished))
            SceneManager.LoadScene(sceneOnFinished);
    }

    void Start()
    {
        if(currentScenario !=null)
        {
            StartChapter(currentScenario);
        }
    }
    public void StartChapter(DialogueDataSO newSO)
    {
        if (newSO == null) return;

        currentScenario = newSO;
        currentGroupIndex = 0;
        currentEntryIndex = 0;

        Debug.Log($"<color=pink>{newSO.name} 파트를 시작합니다!</color>");


        ChatManager chatManager = Object.FindAnyObjectByType<ChatManager>();
        if (chatManager == null) return;

        // 첫 대사가 없는 챕터는 여기서 걸러냄 (빈 그룹이면 아래에서 터짐)
        if (newSO.groups.Count == 0 || newSO.groups[0].entries.Count == 0)
        {
            Debug.LogWarning($"{newSO.name} 에 재생할 대사가 없습니다!");
            return;
        }

        // ChatManager는 자기 currentScenario에서 ID를 찾으므로 여기서 같이 바꿔줘야
        // 다음 챕터로 넘어갔을 때 이전 SO를 계속 뒤지지 않음
        chatManager.currentScenario = newSO;

        int firstID = newSO.groups[0].entries[0].id;
        chatManager.StartCoroutine(chatManager.PlayDialogue(firstID));
    }

   

    public void RequestNextDialogue()
    {
        
        if (currentScenario.groups.Count == 0) return;

       
        DialogueGroup targetGroup = currentScenario.groups[currentGroupIndex];

      
        if (currentEntryIndex < targetGroup.entries.Count)
        {
            DialogueEntry data = targetGroup.entries[currentEntryIndex];

          
            currentEntryIndex++;
        }
        else
        {
          
            OnGroupFinished();
        }
    }

    public void EndOfDialogue()
    {
        if(currentScenario.nextStorySO !=null)
        {
            StartChapter(currentScenario.nextStorySO);
        }
        else if (!string.IsNullOrEmpty(sceneOnFinished))
        {
            SceneManager.LoadScene(sceneOnFinished);
        }

    }

    void OnGroupFinished()
    {
        Debug.Log($"{currentScenario.groups[currentGroupIndex].GroupName} 그룹의 대사가 끝났습니다.");
      
    }
}
