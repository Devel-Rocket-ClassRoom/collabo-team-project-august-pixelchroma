using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenView : MonoBehaviour
{
    private const string NextScene = "1.MainMenu";

    [SerializeField] private Button startButton;

    [Header("Access Sequence (optional)")]
    [SerializeField] private CanvasGroup accessOverlay;
    [SerializeField] private TMP_Text accessStatusText;
    [SerializeField] private RectTransform accessProgress;

    [Header("Build Info (optional)")]
    [SerializeField] private TMP_Text statusVersionText;
    [SerializeField] private TMP_Text footerVersionText;

    private bool isStarting;

    private void Awake()
    {
        if (statusVersionText != null) statusVersionText.text = $"v{Application.version}";
        if (footerVersionText != null) footerVersionText.text = $"UID: ----------\nVersion: {Application.version}";

        if (startButton == null) return;
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartPressed);
    }

    private void OnStartPressed()
    {
        if (isStarting) return;
        isStarting = true;

        if (accessOverlay == null || accessStatusText == null)
        {
            GoNext();
            return;
        }
        StartCoroutine(PlayAccessSequence());
    }

    // 첫 플레이(튜토리얼 완료 파일 없음)면 튜토리얼 스토리로, 아니면 메인 메뉴로 갑니다.
    private void GoNext()
    {
        if (TutorialFlow.ShouldPlay) TutorialFlow.LoadStoryScene();
        else SceneManager.LoadScene(NextScene);
    }

    private IEnumerator PlayAccessSequence()
    {
        accessOverlay.blocksRaycasts = true;
        accessStatusText.text = "AUTHENTICATING...";
        accessStatusText.color = UITheme.White;
        if (accessProgress != null) accessProgress.localScale = new Vector3(0f, 1f, 1f);

        accessOverlay.DOFade(1f, 0.12f).SetUpdate(true);
        if (accessProgress != null)
            accessProgress.DOScaleX(1f, 0.5f).SetEase(Ease.InOutQuad).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.55f);

        accessStatusText.text = "ACCESS GRANTED";
        accessStatusText.color = UITheme.Accent;
        yield return new WaitForSecondsRealtime(0.25f);

        GoNext();
    }
}
