using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SquadSlotCard : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;

    public Image Portrait => portrait;
    public TMP_Text NameText => nameText;
    public TMP_Text DetailText => detailText;

    public void Show(string displayName, string detail, Sprite sprite, Color bgColor)
    {
        Image bg = GetComponent<Image>();
        if (bg != null) bg.color = bgColor;
        if (nameText != null) nameText.text = displayName;
        if (detailText != null) detailText.text = detail;
        if (portrait != null)
        {
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
        }
    }

    public void ShowEmpty()
    {
        Image bg = GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.45f, 0.47f, 0.52f, 1f);
        if (nameText != null) nameText.text = "빈 슬롯";
        if (detailText != null) detailText.text = "+";
        if (portrait != null) portrait.enabled = false;
    }
}
