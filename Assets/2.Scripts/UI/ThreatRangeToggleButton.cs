using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 위협 범위를 한 번에 켜고 끄는 HUD 버튼입니다. (미션 필수: 한 번의 토글로 확인)
/// </summary>
[RequireComponent(typeof(Button))]
public class ThreatRangeToggleButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("위협 범위가 켜져 있을 때 강조색으로 바뀌는 테두리")]
    [SerializeField] private Graphic highlight;
    [SerializeField] private string onText = "위협 범위 ON";
    [SerializeField] private string offText = "위협 범위 OFF";

    private Color normalHighlightColor;
    private GameManager boundManager;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
        if (highlight != null) normalHighlightColor = highlight.color;
    }

    private void OnEnable() => TryBind();

    private void Update()
    {
        // GameManager가 HUD보다 늦게 생길 수 있어 연결될 때까지 확인합니다.
        if (boundManager == null) TryBind();
    }

    private void OnDisable()
    {
        if (boundManager != null) boundManager.ThreatRangeToggled -= Refresh;
        boundManager = null;
    }

    private void TryBind()
    {
        if (boundManager != null || GameManager.Instance == null) return;
        boundManager = GameManager.Instance;
        boundManager.ThreatRangeToggled += Refresh;
        Refresh(boundManager.ShowingThreatRange);
    }

    private void OnClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ToggleThreatRange();
    }

    private void Refresh(bool visible)
    {
        if (label != null) label.text = visible ? onText : offText;
        if (highlight != null) highlight.color = visible ? UITheme.Warning : normalHighlightColor;
    }
}
