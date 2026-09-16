using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenView : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton == null) return;
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => SceneManager.LoadScene("1.MainMenu"));
    }
}
