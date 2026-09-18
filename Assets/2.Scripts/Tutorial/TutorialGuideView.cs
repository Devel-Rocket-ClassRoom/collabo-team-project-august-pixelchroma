using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 전투에서 안내 대사와 목표를 보여주는 UI입니다.
/// 모양은 프리팹(3.Prefabs/1.3.Tutorial/TutorialGuide (PlayHere))에서 수정합니다.
/// </summary>
public class TutorialGuideView : MonoBehaviour
{
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private Image portrait;
    [Tooltip("초상화가 있을 때 오른쪽으로 밀려나는 글자 영역 (이름 + 대사)")]
    [SerializeField] private RectTransform textArea;
    [SerializeField, Range(0f, 0.5f)] private float textLeftWithPortrait = 0.26f;
    [SerializeField, Range(0f, 0.5f)] private float textLeftWithoutPortrait = 0.04f;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("목표 표시")]
    [SerializeField] private GameObject objectiveChip;
    [SerializeField] private TMP_Text objectiveText;

    [Header("건너뛰기")]
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipButtonText;

    public event Action NextPressed;
    public event Action SkipConfirmed;

    private bool skipArmed;

    private void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(() => NextPressed?.Invoke());
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipPressed);
    }

    /// <summary>대사를 띄웁니다. buttonLabel이 비어 있으면 다음 버튼을 숨깁니다(행동으로 넘어가는 단계).</summary>
    public void Show(Sprite speakerPortrait, string speaker, string body, string objective, string buttonLabel)
    {
        gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);

        if (portrait != null)
        {
            portrait.sprite = speakerPortrait;
            portrait.gameObject.SetActive(speakerPortrait != null);
        }
        if (textArea != null)
        {
            float left = speakerPortrait != null ? textLeftWithPortrait : textLeftWithoutPortrait;
            textArea.anchorMin = new Vector2(left, textArea.anchorMin.y);
        }
        if (speakerText != null) speakerText.text = speaker;
        if (bodyText != null) bodyText.text = body;

        bool hasButton = !string.IsNullOrEmpty(buttonLabel);
        if (nextButton != null) nextButton.gameObject.SetActive(hasButton);
        if (nextButtonText != null && hasButton) nextButtonText.text = buttonLabel;

        bool hasObjective = !string.IsNullOrEmpty(objective);
        if (objectiveChip != null) objectiveChip.SetActive(hasObjective);
        if (objectiveText != null) objectiveText.text = objective;
    }

    /// <summary>대사창은 접고 목표만 남깁니다. (자유 전투 구간)</summary>
    public void ShowObjectiveOnly(string objective)
    {
        gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (objectiveChip != null) objectiveChip.SetActive(!string.IsNullOrEmpty(objective));
        if (objectiveText != null) objectiveText.text = objective;
    }

    private void OnSkipPressed()
    {
        // 실수로 누르는 걸 막기 위해 두 번 눌러야 건너뜁니다.
        if (!skipArmed)
        {
            skipArmed = true;
            if (skipButtonText != null) skipButtonText.text = "한 번 더 누르면 건너뜀";
            CancelInvoke(nameof(DisarmSkip));
            Invoke(nameof(DisarmSkip), 2.5f);
            return;
        }
        SkipConfirmed?.Invoke();
    }

    private void DisarmSkip()
    {
        skipArmed = false;
        if (skipButtonText != null) skipButtonText.text = "튜토리얼 건너뛰기";
    }
}
