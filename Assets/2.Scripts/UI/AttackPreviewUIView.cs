using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackPreviewUIView : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("체력 바")]
    [Tooltip("공격 후 남는 체력 구간")]
    [SerializeField] private RectTransform healthFill;
    [Tooltip("이번 공격으로 깎이는 구간")]
    [SerializeField] private RectTransform healthDamage;
    [SerializeField] private TMP_Text healthText;

    public RectTransform Panel => panel;
    public TMP_Text PreviewText => previewText;
    public Button CancelButton => cancelButton;
    public Button ConfirmButton => confirmButton;

    /// <summary>대상의 체력과 이번 공격으로 깎일 양을 바로 표시합니다.</summary>
    public void SetHealth(int current, int max, int predictedDamage)
    {
        if (max <= 0) max = Mathf.Max(1, current);

        int after = Mathf.Clamp(current - Mathf.Max(0, predictedDamage), 0, max);
        float currentRatio = Mathf.Clamp01((float)current / max);
        float afterRatio = Mathf.Clamp01((float)after / max);

        // 남는 체력 구간과, 그 뒤에 이어지는 깎이는 구간으로 나눠 그립니다.
        SetRange(healthFill, 0f, afterRatio);
        SetRange(healthDamage, afterRatio, currentRatio);

        if (healthText != null) healthText.text = $"{current} → {after}";
    }

    private static void SetRange(RectTransform rect, float from, float to)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(from, 0f);
        rect.anchorMax = new Vector2(Mathf.Max(from, to), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.gameObject.SetActive(to > from + 0.0001f);
    }
}
