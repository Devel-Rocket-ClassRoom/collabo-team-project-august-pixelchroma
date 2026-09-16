using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenView : MonoBehaviour
{
    [SerializeField] private Button campaignButton;
    [SerializeField] private Button squadButton;
    [SerializeField] private Button agentsButton;
    [SerializeField] private Button missionButton;

    private void Awake()
    {
        Bind(campaignButton, "2.Chapter Select");
        Bind(squadButton, "3.5.Squad Select");
        Bind(agentsButton, "5.CharList");
        Bind(missionButton, "3.Stage List");
    }

    private static void Bind(Button button, string sceneName)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }
}
