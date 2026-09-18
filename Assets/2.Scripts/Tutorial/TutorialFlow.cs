using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

/// <summary>
/// 첫 플레이 튜토리얼 흐름입니다.
/// 타이틀 → 1.2 튜토리얼 스토리(세계관) → 1.3 튜토리얼 전투(조작) → 완료 기록 → 메인 메뉴
/// 튜토리얼 씬·대사·가이드 데이터·가이드 UI는 모두 Addressables(그룹 "Tutorial")로 불러옵니다.
/// 주소는 Tools/POLTERGEIST/Tutorial/Addressables 설정 메뉴가 등록합니다.
/// </summary>
public static class TutorialFlow
{
    public const string StorySceneKey = "Tutorial/Scene/Story";
    public const string BattleSceneKey = "Tutorial/Scene/Battle";
    public const string WorldStoryKey = "Tutorial/Story/World";
    public const string BattleGuideKey = "Tutorial/Battle/Guide";
    public const string GuideViewKey = "Tutorial/UI/Guide";

    public const string AfterTutorialScene = "1.MainMenu";

    /// <summary>완료 파일(Tutorial Clear.json)이 없으면 튜토리얼을 보여줍니다.</summary>
    public static bool ShouldPlay => !TutorialProgress.IsCleared;

    public static void LoadStoryScene() => LoadScene(StorySceneKey);

    public static void LoadBattleScene() => LoadScene(BattleSceneKey);

    /// <summary>튜토리얼을 끝까지 마쳤을 때 완료 파일을 만들고 메인 메뉴로 갑니다.</summary>
    public static void Complete()
    {
        TutorialProgress.MarkCleared();
        SceneManager.LoadScene(AfterTutorialScene);
    }

    private static void LoadScene(string key)
    {
        Time.timeScale = 1f;
        Addressables.LoadSceneAsync(key, LoadSceneMode.Single).Completed += handle =>
        {
            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                // 주소를 못 찾으면 튜토리얼을 건너뛰고 메인 메뉴로 보냅니다. (완료 기록은 남기지 않음)
                Debug.LogError($"[Tutorial] Addressable 씬 '{key}'을(를) 불러오지 못했습니다. 메인 메뉴로 이동합니다.");
                SceneManager.LoadScene(AfterTutorialScene);
            }
        };
    }
}
