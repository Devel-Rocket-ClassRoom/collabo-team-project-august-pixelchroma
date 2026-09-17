using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UIScreenFlowValidator
{
    private const string CatalogPath = "Assets/Resources/UIScreenCatalog.asset";

    private static readonly string[] RequiredScenePaths =
    {
        "Assets/1.Sence/0.Tilte.unity",
        "Assets/1.Sence/1.MainMenu.unity",
        "Assets/1.Sence/2.Chapter Select.unity",
        "Assets/1.Sence/3.Stage List.unity",
        "Assets/1.Sence/3.5.Squad Select.unity",
        "Assets/1.Sence/4.MainGame.unity",
        "Assets/1.Sence/5.CharList.unity",
        "Assets/1.Sence/6.Status List.unity"
    };

    [MenuItem("Tools/SRPG UI/4차 화면 이동 연결 검사")]
    public static void Validate()
    {
        ValidateBuildScenes();

        UIScreenCatalog catalog = AssetDatabase.LoadAssetAtPath<UIScreenCatalog>(CatalogPath);
        if (catalog == null)
            throw new InvalidOperationException("UIScreenCatalog을 찾을 수 없습니다.");

        Require(catalog.TitleScreenPrefab, "타이틀 화면", "startButton");
        Require(catalog.HomeScreenPrefab, "메인 로비",
            "campaignButton", "squadButton", "agentsButton", "missionButton");
        Require(catalog.AgentListScreenPrefab, "요원 리스트",
            "cardPrefab", "cardContainer", "ownedCountText", "totalPowerText", "backButton",
            "detailCloseButton", "detailOpenButton", "detailPanel", "detailPortrait",
            "detailNameText", "detailRoleText", "detailStatsText", "detailDescriptionText");
        Require(catalog.AgentStatusScreenPrefab, "요원 상세",
            "backButton", "portrait", "roleAccent", "nameText", "roleText", "powerText",
            "statsText", "skillText", "descriptionText");
        RequireOperation(catalog.ChapterScreenPrefab, "챕터 선택", 3);
        RequireOperation(catalog.StageScreenPrefab, "스테이지 선택", 5);
        ValidateSquadScene();
        ValidateMainGameScene();

        Debug.Log("[SRPG UI] 4차 화면 이동/프리팹 연결 검사 통과");
    }

    private static void ValidateSquadScene()
    {
        EditorSceneManager.OpenScene("Assets/1.Sence/3.5.Squad Select.unity", OpenSceneMode.Single);
        SquadSelectionController controller = UnityEngine.Object.FindFirstObjectByType<SquadSelectionController>();
        if (controller == null)
            throw new InvalidOperationException("스쿼드 편성 씬에 SquadSelectionController가 없습니다.");

        SerializedObject serialized = new SerializedObject(controller);
        RequireReference(serialized, "squadUIPrefab", "스쿼드 편성 UI");
        if (serialized.FindProperty("previousSceneName")?.stringValue != "3.Stage List")
            throw new InvalidOperationException("스쿼드 편성 뒤로가기 씬 연결이 잘못됐습니다.");
        if (serialized.FindProperty("battleSceneName")?.stringValue != "4.MainGame")
            throw new InvalidOperationException("스쿼드 편성 전투 시작 씬 연결이 잘못됐습니다.");
    }

    private static void ValidateMainGameScene()
    {
        EditorSceneManager.OpenScene("Assets/1.Sence/4.MainGame.unity", OpenSceneMode.Single);
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
            throw new InvalidOperationException("메인게임 씬에 GameManager가 없습니다.");

        SerializedObject serialized = new SerializedObject(manager);
        RequireReference(serialized, "deploymentUIPrefab", "배치 UI");
        RequireReference(serialized, "attackPreviewUIPrefab", "공격 확인 UI");
        RequireReference(serialized, "battleHUDPrefab", "전투 HUD");
    }

    private static void RequireReference(SerializedObject serialized, string propertyName, string label)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
            throw new InvalidOperationException($"{label} 프리팹 연결이 비어 있습니다.");
    }

    private static void ValidateBuildScenes()
    {
        string[] enabled = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path.Replace('\\', '/'))
            .ToArray();

        foreach (string required in RequiredScenePaths)
        {
            if (!enabled.Contains(required))
                throw new InvalidOperationException($"빌드 목록에 씬이 없습니다: {required}");
        }
    }

    private static void Require(Component component, string label, params string[] propertyNames)
    {
        if (component == null)
            throw new InvalidOperationException($"{label} 프리팹이 연결되지 않았습니다.");

        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
                throw new InvalidOperationException($"{label}의 {propertyName} 연결이 비어 있습니다.");
        }
    }

    private static void RequireOperation(
        OperationSelectionScreenView component,
        string label,
        int expectedButtonCount)
    {
        Require(component, label, "backButton");

        SerializedObject serialized = new SerializedObject(component);
        if (expectedButtonCount == 5)
        {
            SerializedProperty title = serialized.FindProperty("selectionTitle");
            if (title == null || title.objectReferenceValue == null)
                throw new InvalidOperationException($"{label}의 selectionTitle 연결이 비어 있습니다.");
        }

        SerializedProperty buttons = serialized.FindProperty("selectionButtons");
        if (buttons == null || buttons.arraySize != expectedButtonCount)
            throw new InvalidOperationException(
                $"{label} 선택 버튼 수가 잘못됐습니다. 기대값: {expectedButtonCount}");

        for (int i = 0; i < buttons.arraySize; i++)
        {
            if (buttons.GetArrayElementAtIndex(i).objectReferenceValue == null)
                throw new InvalidOperationException($"{label} 선택 버튼 {i + 1} 연결이 비어 있습니다.");
        }
    }
}
