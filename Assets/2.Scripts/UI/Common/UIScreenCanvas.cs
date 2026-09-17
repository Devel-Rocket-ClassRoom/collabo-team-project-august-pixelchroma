using UnityEngine;
using UnityEngine.UI;

// Root of a redesigned screen: enforces the 1080x1920 / Match 0.5 scaler and can hide the scene's legacy UI.
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class UIScreenCanvas : MonoBehaviour
{
    [SerializeField] private bool hideLegacySceneCanvases;

    private void Awake()
    {
        ApplyScaler();
    }

    private void Start()
    {
        // Re-applied in Start because UIBootstrap rescales canvases on sceneLoaded.
        ApplyScaler();
        if (hideLegacySceneCanvases)
            HideLegacySceneCanvases();
    }

    private void ApplyScaler()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UITheme.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = UITheme.MatchWidthOrHeight;
    }

    // Disables only the Canvas/Raycaster components so legacy objects (and any EventSystem under them) keep running.
    private void HideLegacySceneCanvases()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root == gameObject) continue;

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                if (canvas.GetComponent<UIScreenCanvas>() != null) continue;
                Transform parent = canvas.transform.parent;
                if (parent != null && parent.GetComponentInParent<Canvas>(true) != null) continue;

                canvas.enabled = false;
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
        }
    }
}
