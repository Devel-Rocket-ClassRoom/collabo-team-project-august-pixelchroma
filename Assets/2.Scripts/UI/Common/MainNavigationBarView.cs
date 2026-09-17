using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainNavigationBarView : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public Button button;
        public Graphic icon;
        public TMP_Text label;
        public GameObject indicator;
        public string sceneName;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private int selectedIndex;

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => OnTabClicked(index));
        }
        Refresh();
    }

    public void SetSelected(int index)
    {
        selectedIndex = index;
        Refresh();
    }

    private void OnTabClicked(int index)
    {
        if (index == selectedIndex) return;

        string sceneName = tabs[index].sceneName;
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            string label = tabs[index].label != null ? tabs[index].label.text : sceneName;
            Debug.Log($"[Navigation] '{label}' 화면은 아직 준비 중입니다.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    private void Refresh()
    {
        Color muted = UITheme.WithAlpha(UITheme.LightGray, 0.55f);
        for (int i = 0; i < tabs.Length; i++)
        {
            bool selected = i == selectedIndex;
            Color color = selected ? UITheme.Accent : muted;
            if (tabs[i].icon != null) tabs[i].icon.color = color;
            if (tabs[i].label != null) tabs[i].label.color = color;
            if (tabs[i].indicator != null) tabs[i].indicator.SetActive(selected);
        }
    }
}
