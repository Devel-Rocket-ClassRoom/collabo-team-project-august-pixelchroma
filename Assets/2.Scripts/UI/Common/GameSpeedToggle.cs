using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 누를 때마다 1배속 ↔ 2배속을 바꾸는 버튼입니다. Time.timeScale로 속도를 바꾸므로
/// 대사 타이핑·유닛 이동·적 턴 대기가 함께 빨라집니다.
/// 선택한 배속은 prefsKey별로 기억하고, 버튼이 사라지면(씬 전환) 1배속으로 되돌립니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameSpeedToggle : MonoBehaviour
{
    [SerializeField] private string prefsKey = "GameSpeed";
    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private TMP_Text label;
    [Tooltip("2배속일 때 강조색으로 바뀌는 테두리입니다.")]
    [SerializeField] private Graphic highlight;
    [SerializeField] private string normalText = "배속 x1";
    [SerializeField] private string fastText = "배속 x2";

    private Color normalHighlightColor;
    private bool isFast;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
        if (highlight != null) normalHighlightColor = highlight.color;
    }

    private void OnEnable()
    {
        isFast = PlayerPrefs.GetInt(prefsKey, 0) == 1;
        Apply();
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    public void Toggle()
    {
        isFast = !isFast;
        PlayerPrefs.SetInt(prefsKey, isFast ? 1 : 0);
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = isFast ? fastSpeed : 1f;
        if (label != null) label.text = isFast ? fastText : normalText;
        if (highlight != null) highlight.color = isFast ? UITheme.Accent : normalHighlightColor;
    }
}
