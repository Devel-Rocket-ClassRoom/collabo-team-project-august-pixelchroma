using UnityEngine;
using DG.Tweening;

public class MainMenuAnimator : MonoBehaviour
{
    [Header("Middle Upper (Y축 이동)")]
    [SerializeField] private RectTransform middleUpper;
    [SerializeField] private float upperStartY = 200f;
    [SerializeField] private float upperEndY = -150f;
    [SerializeField] private float upperDuration = 0.6f;
    [SerializeField] private float upperDelay = 0f;
    [SerializeField] private Ease upperEase = Ease.OutBack;

    [Header("Right Middle Button (X축 이동)")]
    [SerializeField] private RectTransform rightMiddleButton;
    [SerializeField] private float rightStartX = 300f;
    [SerializeField] private float rightEndX = -300f;
    [SerializeField] private float rightDuration = 0.6f;
    [SerializeField] private float rightDelay = 0.2f;
    [SerializeField] private Ease rightEase = Ease.OutBack;

    private void OnEnable()
    {
        PlayIntro();
    }

    public void PlayIntro()
    {
        if (middleUpper != null)
        {
            middleUpper.anchoredPosition = new Vector2(
                middleUpper.anchoredPosition.x, upperStartY);
            middleUpper.DOAnchorPosY(upperEndY, upperDuration)
                .SetDelay(upperDelay)
                .SetEase(upperEase);
        }

        if (rightMiddleButton != null)
        {
            rightMiddleButton.anchoredPosition = new Vector2(
                rightStartX, rightMiddleButton.anchoredPosition.y);
            rightMiddleButton.DOAnchorPosX(rightEndX, rightDuration)
                .SetDelay(rightDelay)
                .SetEase(rightEase);
        }
    }

    private void OnDisable()
    {
        if (middleUpper != null) middleUpper.DOKill();
        if (rightMiddleButton != null) rightMiddleButton.DOKill();
    }
}
