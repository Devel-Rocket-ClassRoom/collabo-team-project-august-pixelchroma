using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIAlphaPulse : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [SerializeField] private float halfPeriod = 1.5f;

    private Tween tween;

    private void OnEnable()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        group.alpha = maxAlpha;
        tween = group.DOFade(minAlpha, halfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        tween?.Kill();
    }
}
