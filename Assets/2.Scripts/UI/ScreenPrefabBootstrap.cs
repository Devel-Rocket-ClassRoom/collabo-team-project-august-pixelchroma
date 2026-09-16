using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class ScreenPrefabBootstrap
{
    private const string CatalogResourceName = "UIScreenCatalog";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        InstallForScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForScene(Scene scene)
    {
        if (Object.FindFirstObjectByType<HomeScreenView>() != null ||
            Object.FindFirstObjectByType<AgentListScreenView>() != null ||
            Object.FindFirstObjectByType<OperationSelectionScreenView>() != null ||
            Object.FindFirstObjectByType<TitleScreenView>() != null ||
            Object.FindFirstObjectByType<AgentStatusScreenView>() != null)
            return;

        UIScreenCatalog catalog = Resources.Load<UIScreenCatalog>(CatalogResourceName);
        if (catalog == null) return;

        if (scene.name == "0.Tilte" && catalog.TitleScreenPrefab != null)
            Object.Instantiate(catalog.TitleScreenPrefab).name = "UI_타이틀";
        else if (scene.name == "1.MainMenu" && catalog.HomeScreenPrefab != null)
            Object.Instantiate(catalog.HomeScreenPrefab).name = "UI_메인 로비";
        else if (scene.name == "5.CharList" && catalog.AgentListScreenPrefab != null)
            Object.Instantiate(catalog.AgentListScreenPrefab).name = "UI_요원 리스트";
        else if (scene.name == "2.Chapter Select" && catalog.ChapterScreenPrefab != null)
            Object.Instantiate(catalog.ChapterScreenPrefab).name = "UI_챕터 선택";
        else if (scene.name == "3.Stage List" && catalog.StageScreenPrefab != null)
            Object.Instantiate(catalog.StageScreenPrefab).name = "UI_스테이지 선택";
        else if (scene.name == "6.Status List" && catalog.AgentStatusScreenPrefab != null)
            Object.Instantiate(catalog.AgentStatusScreenPrefab).name = "UI_요원 상세";
        else
            return;

        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
