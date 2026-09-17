using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelTransition : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float openDuration = 0.2f;
    [SerializeField] private float closeDuration = 0.15f;
    [SerializeField] private float slideOffset = 30f;

    private CanvasGroup group;
    private Vector2 restPosition;
    private Tween tween;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (content == null) content = (RectTransform)transform;
        restPosition = content.anchoredPosition;
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        tween?.Kill();
    }

    public void Open()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (playOnEnable) return;
        }
        Play();
    }

    public void Close()
    {
        tween?.Kill();
        group.interactable = false;
        group.blocksRaycasts = false;
        tween = group.DOFade(0f, closeDuration)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void Play()
    {
        tween?.Kill();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;
        content.anchoredPosition = restPosition + Vector2.down * slideOffset;
        tween = DOTween.Sequence()
            .Join(group.DOFade(1f, openDuration))
            .Join(content.DOAnchorPos(restPosition, openDuration).SetEase(Ease.OutCubic))
            .SetUpdate(true);
    }
}
