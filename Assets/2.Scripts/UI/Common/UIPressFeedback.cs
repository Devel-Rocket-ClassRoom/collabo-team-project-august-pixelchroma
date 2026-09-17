using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.1f;

    private Selectable selectable;
    private Vector3 restScale;
    private Tween tween;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        restScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable()) return;
        Animate(restScale * pressedScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Animate(restScale, releaseDuration);
    }

    private void Animate(Vector3 target, float duration)
    {
        tween?.Kill();
        tween = transform.DOScale(target, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        tween?.Kill();
        transform.localScale = restScale;
    }
}
