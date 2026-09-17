using UnityEngine;
using UnityEngine.UI;

public class StorySkipController : MonoBehaviour
{
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private ScenarioController scenarioController;
    [SerializeField] private Button skipButton;
    [SerializeField] private UIPanelTransition confirmPopup;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        skipButton.onClick.AddListener(OpenConfirm);
        confirmButton.onClick.AddListener(ConfirmSkip);
        cancelButton.onClick.AddListener(CancelSkip);
        confirmPopup.gameObject.SetActive(false);
    }

    private void OpenConfirm()
    {
        // Pauses typing and input so the dialogue doesn't advance behind the popup.
        chatManager.isPausedByMenu = true;
        confirmPopup.Open();
    }

    private void CancelSkip()
    {
        confirmPopup.Close();
        chatManager.isPausedByMenu = false;
    }

    private void ConfirmSkip()
    {
        skipButton.interactable = false;
        confirmButton.interactable = false;
        cancelButton.interactable = false;
        scenarioController.SkipStory();
    }
}
