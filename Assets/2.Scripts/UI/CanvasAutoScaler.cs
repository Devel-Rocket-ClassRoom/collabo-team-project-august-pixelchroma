using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class CanvasAutoScaler : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 2220f);
    [SerializeField] private float matchWidthOrHeight = 0f;

    private void Awake()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
