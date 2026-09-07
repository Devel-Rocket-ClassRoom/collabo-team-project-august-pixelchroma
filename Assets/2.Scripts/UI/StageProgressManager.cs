using UnityEngine;
using UnityEngine.UI;

public class StageProgressManager : MonoBehaviour
{
    private const string CLEARED_KEY = "ClearedStage";

    public static int CurrentStageIndex { get; set; }

    [SerializeField] private Transform stageContent;
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float lockedAlpha = 0.4f;

    private void Start()
    {
        if (stageContent == null)
        {
            GameObject found = GameObject.Find("Stage_Content");
            if (found != null) stageContent = found.transform;
        }
        if (stageContent == null) return;

        int cleared = PlayerPrefs.GetInt(CLEARED_KEY, 0);
        int buttonIndex = 0;

        for (int i = 0; i < stageContent.childCount; i++)
        {
            Transform child = stageContent.GetChild(i);
            Button btn = child.GetComponent<Button>();
            if (btn == null) continue;

            bool unlocked = buttonIndex == 0 || buttonIndex <= cleared;
            btn.interactable = unlocked;

            if (!unlocked)
            {
                ColorBlock cb = btn.colors;
                cb.disabledColor = lockedColor;
                btn.colors = cb;

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = lockedAlpha;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            int idx = buttonIndex;
            btn.onClick.AddListener(() => CurrentStageIndex = idx);
            buttonIndex++;
        }
    }

    public static void UnlockNextStage()
    {
        int cleared = PlayerPrefs.GetInt(CLEARED_KEY, 0);
        if (CurrentStageIndex >= cleared)
        {
            PlayerPrefs.SetInt(CLEARED_KEY, CurrentStageIndex + 1);
            PlayerPrefs.Save();
        }
    }
}
