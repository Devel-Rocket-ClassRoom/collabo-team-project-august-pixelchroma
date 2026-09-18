using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 저장된 전투가 있을 때 이어할지 묻는 팝업입니다.
/// 화면 구성은 Resources/BattleResumePrompt 프리팹에서 수정합니다.
/// </summary>
public class BattleResumePrompt : MonoBehaviour
{
    // Assets/3.Prefabs/4.MainGame/Resources 안에 있는 프리팹입니다.
    public const string ResourcePath = "BattleResumePrompt (PlayHere)";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYes;
    private Action onNo;

    /// <summary>프리팹을 띄웁니다. 프리팹이 없으면 null을 돌려줍니다.</summary>
    public static BattleResumePrompt Show(string title, string body, Action onYes, Action onNo)
    {
        BattleResumePrompt prefab = Resources.Load<BattleResumePrompt>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[BattleResumePrompt] Resources/{ResourcePath} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        BattleResumePrompt prompt = Instantiate(prefab);
        prompt.name = "BattleResumePrompt (PlayHere)";
        prompt.Bind(title, body, onYes, onNo);
        EnsureEventSystem();
        return prompt;
    }

    private void Bind(string title, string body, Action yes, Action no)
    {
        onYes = yes;
        onNo = no;

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(() => Answer(true));
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(() => Answer(false));
        }
    }

    private void Answer(bool resume)
    {
        Action callback = resume ? onYes : onNo;
        onYes = null;
        onNo = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
