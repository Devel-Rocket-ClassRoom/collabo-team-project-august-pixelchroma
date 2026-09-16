using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgentStatusScreenView : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Image portrait;
    [SerializeField] private Image roleAccent;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text skillText;
    [SerializeField] private TMP_Text descriptionText;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneManager.LoadScene("5.CharList"));
        }

        CharacterData agent = AgentSelectionState.Current;
        if (agent == null)
        {
            OwnedAgentCatalog catalog = Resources.Load<OwnedAgentCatalog>("OwnedAgentCatalog");
            if (catalog != null && catalog.OwnedAgents.Count > 0)
                agent = catalog.OwnedAgents[0];
        }

        Bind(agent);
    }

    private void Bind(CharacterData agent)
    {
        if (agent == null) return;
        if (nameText != null) nameText.text = agent.DisplayName;
        if (roleText != null) roleText.text = AgentCardView.GetPatternName(agent.AttackPattern);
        if (powerText != null) powerText.text = $"전투력  {AgentCardView.GetPower(agent):N0}";
        if (roleAccent != null) roleAccent.color = agent.TeamColor;
        if (portrait != null)
        {
            portrait.sprite = agent.BattleSprite;
            portrait.color = agent.BattleSprite != null ? Color.white : agent.TeamColor;
            portrait.preserveAspect = true;
        }

        if (statsText != null)
        {
            statsText.text =
                $"체력\n{agent.MaxHP}\n\n공격력\n{agent.AttackPower}\n\n이동\n{agent.MoveRange}\n\n사거리\n{agent.AttackRange}";
        }

        if (skillText != null)
        {
            string skillName = agent.SkillData != null ? agent.SkillData.DisplayName : AgentCardView.GetPatternName(agent.AttackPattern);
            skillText.text =
                $"특수 공격  {skillName}\n" +
                $"위력 {agent.SpecialPower}   재사용 {agent.CooldownTurns}턴\n" +
                $"공격 유형  {AgentCardView.GetPatternName(agent.AttackPattern)}";
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(agent.Description) ? "등록된 요원 설명이 없습니다." : agent.Description;
    }
}
