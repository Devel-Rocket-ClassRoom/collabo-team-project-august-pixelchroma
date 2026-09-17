using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgentListScreenView : MonoBehaviour
{
    [SerializeField] private AgentCardView cardPrefab;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private TMP_Text totalPowerText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button detailCloseButton;
    [SerializeField] private Button detailOpenButton;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailPortrait;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailRoleText;
    [SerializeField] private TMP_Text detailStatsText;
    [SerializeField] private TMP_Text detailDescriptionText;

    private OwnedAgentCatalog catalog;

    private void Awake()
    {
        catalog = Resources.Load<OwnedAgentCatalog>("OwnedAgentCatalog");
        if (backButton != null)
            backButton.onClick.AddListener(() => SceneManager.LoadScene("1.MainMenu"));
        if (detailCloseButton != null)
            detailCloseButton.onClick.AddListener(HideDetail);
        if (detailOpenButton != null)
            detailOpenButton.onClick.AddListener(OpenStatus);

        HideDetail();
        Populate();
    }

    private void Populate()
    {
        if (catalog == null || cardPrefab == null || cardContainer == null) return;

        int totalPower = 0;
        int count = 0;
        foreach (CharacterData agent in catalog.OwnedAgents)
        {
            if (agent == null) continue;
            AgentCardView card = Instantiate(cardPrefab, cardContainer);
            card.name = $"요원 카드_{agent.CharacterId}";
            card.gameObject.SetActive(true);
            card.Bind(agent, ShowDetail);
            totalPower += AgentCardView.GetPower(agent);
            count++;
        }

        if (ownedCountText != null) ownedCountText.text = $"보유 요원  {count}명";
        if (totalPowerText != null) totalPowerText.text = $"총 전투력  {totalPower:N0}";
    }

    private void ShowDetail(CharacterData agent)
    {
        if (agent == null || detailPanel == null) return;
        AgentSelectionState.Current = agent;
        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();

        if (detailPortrait != null)
        {
            detailPortrait.sprite = agent.IllustrationSprite;
            detailPortrait.color = agent.IllustrationSprite != null ? Color.white : agent.TeamColor;
            detailPortrait.preserveAspect = true;
        }
        if (detailNameText != null) detailNameText.text = agent.DisplayName;
        if (detailRoleText != null) detailRoleText.text = AgentCardView.GetPatternName(agent.AttackPattern);
        if (detailStatsText != null)
        {
            detailStatsText.text =
                $"체력  {agent.MaxHP}     공격력  {agent.AttackPower}\n" +
                $"이동  {agent.MoveRange}     사거리  {agent.AttackRange}\n" +
                $"전투력  {AgentCardView.GetPower(agent):N0}";
        }
        if (detailDescriptionText != null)
            detailDescriptionText.text = string.IsNullOrWhiteSpace(agent.Description) ? "등록된 요원 설명이 없습니다." : agent.Description;
    }

    private void OpenStatus()
    {
        if (AgentSelectionState.Current != null)
            SceneManager.LoadScene("6.Status List");
    }

    private void HideDetail()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
    }
}
