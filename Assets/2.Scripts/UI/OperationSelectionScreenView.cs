using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OperationSelectionScreenView : MonoBehaviour
{
    public enum ScreenKind
    {
        Chapter,
        Stage
    }

    [SerializeField] private ScreenKind screenKind;
    [SerializeField] private Button backButton;
    [SerializeField] private Button[] selectionButtons;
    [SerializeField] private TMP_Text selectionTitle;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
                SceneManager.LoadScene(screenKind == ScreenKind.Chapter ? "1.MainMenu" : "2.Chapter Select"));
        }

        if (selectionButtons == null) return;
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            int index = i;
            Button button = selectionButtons[i];
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Select(index));
        }
    }

    private void Select(int index)
    {
        if (screenKind == ScreenKind.Chapter)
        {
            PlayerPrefs.SetInt("SelectedChapter", index);
            SceneManager.LoadScene("3.Stage List");
            return;
        }

        StageProgressManager.CurrentStageIndex = index;
        if (selectionTitle != null)
            selectionTitle.text = $"선택 작전  01-{index + 1:00}";
        SceneManager.LoadScene("3.5.Squad Select");
    }
}
