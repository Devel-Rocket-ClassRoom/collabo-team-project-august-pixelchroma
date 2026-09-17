using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject filledGroup;
    [SerializeField] private GameObject emptyGroup;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text traitText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text rarityText;

    public Button Button => button;

    public void Show(string displayName, Sprite portraitSprite, string trait, string role, int level, string rarity)
    {
        filledGroup.SetActive(true);
        emptyGroup.SetActive(false);
        nameText.text = displayName;
        levelText.text = $"Lv.{level}";
        traitText.text = trait;
        roleText.text = role;
        rarityText.text = rarity;
        portrait.sprite = portraitSprite;
        portrait.enabled = portraitSprite != null;
    }

    public void ShowEmpty()
    {
        filledGroup.SetActive(false);
        emptyGroup.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        selectionFrame.SetActive(selected);
    }
}
