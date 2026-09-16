using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BottomNavigationView : MonoBehaviour
{
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button agentsButton;
    [SerializeField] private Button squadButton;
    [SerializeField] private Button operationButton;

    private void Awake()
    {
        Bind(lobbyButton, "1.MainMenu");
        Bind(agentsButton, "5.CharList");
        Bind(squadButton, "3.5.Squad Select");
        Bind(operationButton, "2.Chapter Select");
    }

    private static void Bind(Button button, string sceneName)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }
}
