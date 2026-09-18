using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터를 선택하면 하단에 뜨는 스킬 패널입니다.
/// 초상화 / 스킬 버튼 / 일반 공격 버튼으로 구성됩니다.
/// </summary>
public class CharacterCommandPanelView : MonoBehaviour
{
    [Header("배경 · 초상화")]
    [SerializeField] private Image background;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;

    [Header("스킬 버튼")]
    [SerializeField] private Button skillButton;
    [Tooltip("스킬을 사용 대기 상태로 걸었을 때 켜지는 테두리")]
    [SerializeField] private GameObject skillArmedFrame;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillStateText;

    [Header("일반 공격 버튼")]
    [SerializeField] private Button attackButton;
    [Tooltip("일반 공격이 선택된 상태일 때 켜지는 테두리")]
    [SerializeField] private GameObject attackSelectedFrame;
    [SerializeField] private TMP_Text attackText;

    public Button SkillButton => skillButton;
    public Button AttackButton => attackButton;

    public void Show(
        CharacterData character, string skillName, string skillState,
        bool skillUsable, bool skillArmed)
    {
        gameObject.SetActive(true);

        if (portrait != null)
        {
            Sprite sprite = character != null ? character.IllustrationSprite : null;
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
            portrait.preserveAspect = true;
        }
        if (nameText != null)
            nameText.text = character != null ? character.DisplayName : "";

        if (skillNameText != null) skillNameText.text = skillName;
        if (skillStateText != null) skillStateText.text = skillState;
        if (skillButton != null) skillButton.interactable = skillUsable;
        if (skillArmedFrame != null) skillArmedFrame.SetActive(skillArmed);
        if (attackSelectedFrame != null) attackSelectedFrame.SetActive(!skillArmed);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
