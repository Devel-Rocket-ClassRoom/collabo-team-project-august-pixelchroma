using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AgentCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image portrait;
    [SerializeField] private Image roleAccent;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text levelText;

    public void Bind(CharacterData data, Action<CharacterData> onSelected)
    {
        if (data == null) return;

        if (nameText != null) nameText.text = data.DisplayName;
        if (roleText != null) roleText.text = GetPatternName(data.AttackPattern);
        if (levelText != null) levelText.text = $"전투력 {GetPower(data):N0}";
        if (roleAccent != null) roleAccent.color = data.TeamColor;
        if (portrait != null)
        {
            portrait.sprite = data.BattleSprite;
            portrait.color = data.BattleSprite != null ? Color.white : Color.Lerp(data.TeamColor, Color.black, 0.25f);
            portrait.preserveAspect = true;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(data));
        }
    }

    public static int GetPower(CharacterData data)
    {
        return data.MaxHP * 120 + data.AttackPower * 260 + data.MoveRange * 80 + data.AttackRange * 110 + data.SpecialPower * 90;
    }

    public static string GetPatternName(CharacterAttackPattern pattern)
    {
        switch (pattern)
        {
            case CharacterAttackPattern.CrossArea: return "십자 범위";
            case CharacterAttackPattern.DiamondArea: return "광역 폭발";
            case CharacterAttackPattern.PiercingLine: return "관통 저격";
            case CharacterAttackPattern.Cone: return "부채꼴 제압";
            case CharacterAttackPattern.Chain: return "연쇄 공격";
            default: return "단일 공격";
        }
    }
}
