using UnityEngine;

[CreateAssetMenu(fileName = "UIScreenCatalog", menuName = "Game/UI/Screen Catalog")]
public class UIScreenCatalog : ScriptableObject
{
    [SerializeField] private HomeScreenView homeScreenPrefab;
    [SerializeField] private AgentListScreenView agentListScreenPrefab;
    [SerializeField] private OperationSelectionScreenView chapterScreenPrefab;
    [SerializeField] private OperationSelectionScreenView stageScreenPrefab;
    [SerializeField] private TitleScreenView titleScreenPrefab;
    [SerializeField] private AgentStatusScreenView agentStatusScreenPrefab;

    public HomeScreenView HomeScreenPrefab => homeScreenPrefab;
    public AgentListScreenView AgentListScreenPrefab => agentListScreenPrefab;
    public OperationSelectionScreenView ChapterScreenPrefab => chapterScreenPrefab;
    public OperationSelectionScreenView StageScreenPrefab => stageScreenPrefab;
    public TitleScreenView TitleScreenPrefab => titleScreenPrefab;
    public AgentStatusScreenView AgentStatusScreenPrefab => agentStatusScreenPrefab;
}
