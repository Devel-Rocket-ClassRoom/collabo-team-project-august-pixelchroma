using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip lobbyBGM;
    [SerializeField] private AudioClip battleBGM;

    [Header("Settings")]
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float fadeSpeed = 1f;

    private AudioSource audioSource;
    private AudioClip currentClip;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    private void PlayBGMForScene(string sceneName)
    {
        AudioClip clip = GetClipForScene(sceneName);

        if (clip == null)
        {
            audioSource.Stop();
            currentClip = null;
            return;
        }

        if (currentClip == clip)
            return;

        currentClip = clip;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private AudioClip GetClipForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "0.Tilte":
                return titleBGM;

            case "1.MainMenu":
            case "2.Chapter Select":
            case "3.Stage List":
                return lobbyBGM;

            case "3.5.Squad Select":
            case "4.MainGame":
                return battleBGM;

            default:
                return lobbyBGM;
        }
    }
}
