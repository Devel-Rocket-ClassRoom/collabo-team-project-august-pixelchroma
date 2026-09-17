using TMPro;
using UnityEngine;

public class CommonTopBarView : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private UICurrencyItemView[] currencies;

    public void SetPlayer(int level, string title)
    {
        if (levelText != null) levelText.text = $"LV.{level}";
        if (titleText != null) titleText.text = title;
    }

    public void SetCurrency(int index, string value)
    {
        if (currencies != null && index >= 0 && index < currencies.Length && currencies[index] != null)
            currencies[index].SetValue(value);
    }
}
