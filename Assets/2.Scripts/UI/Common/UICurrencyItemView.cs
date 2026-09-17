using TMPro;
using UnityEngine;

public class UICurrencyItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    public void SetValue(string value)
    {
        if (valueText != null) valueText.text = value;
    }
}
