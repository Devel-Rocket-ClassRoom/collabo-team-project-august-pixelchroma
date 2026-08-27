using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingPopup : MonoBehaviour
{
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float offScreenY = -2500f;

    private GameObject settingPanel;
    private RectTransform panelRect;
    private Tween currentTween;

    void Awake()
    {
        Transform canvas = transform.root;
        Transform found = canvas.Find("Setting_Panel");
        if (found == null)
        {
            Debug.LogError("[SettingPopup] Setting_Panel을 찾을 수 없음!");
            return;
        }

        settingPanel = found.gameObject;

        Transform panel = found.Find("Panel");
        if (panel != null)
            panelRect = panel.GetComponent<RectTransform>();

        // option_Button 자신의 Button에 Open 연결
        Button myButton = GetComponent<Button>();
        if (myButton != null)
            myButton.onClick.AddListener(Open);

        // Setting_Panel 안의 닫기 버튼들 자동 연결
        Button exitBtn = FindButtonInChildren(found, "Game_Exit_Button");
        if (exitBtn != null)
            exitBtn.onClick.AddListener(Close);

        Button closeBtn = FindButtonInChildren(found, "Close_Button");
        if (closeBtn != null)
            closeBtn.onClick.AddListener(Close);
    }

    Button FindButtonInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
            Button found = FindButtonInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public void Open()
    {
        if (settingPanel == null || panelRect == null) return;

        currentTween?.Kill();
        settingPanel.SetActive(true);
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, offScreenY);
        currentTween = panelRect.DOAnchorPosY(0f, slideDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void Close()
    {
        if (settingPanel == null || panelRect == null) return;

        currentTween?.Kill();
        currentTween = panelRect.DOAnchorPosY(offScreenY, slideDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => settingPanel.SetActive(false));
    }
}
