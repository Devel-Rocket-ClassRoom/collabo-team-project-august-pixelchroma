using UnityEngine;
using DG.Tweening;

public class MainMenuAnimator : MonoBehaviour
{
    public enum AnimAxis { X, Y }

    [Header("이동 설정")]
    [SerializeField] private AnimAxis axis = AnimAxis.Y;
    [SerializeField] private float startValue = 200f;
    [SerializeField] private float endValue = -150f;

    [Header("타이밍")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        if (rect == null) return;

        Vector2 pos = rect.anchoredPosition;
        if (axis == AnimAxis.X)
            pos.x = startValue;
        else
            pos.y = startValue;
        rect.anchoredPosition = pos;

        if (axis == AnimAxis.X)
            rect.DOAnchorPosX(endValue, duration).SetDelay(delay).SetEase(ease);
        else
            rect.DOAnchorPosY(endValue, duration).SetDelay(delay).SetEase(ease);
    }

    private void OnDisable()
    {
        if (rect != null) rect.DOKill();
    }
}
