using UnityEngine;

/// <summary>
/// Keeps a 2D battle character facing the gameplay camera while preserving an
/// upright silhouette, similar to a 2.5D tactical game presentation.
/// </summary>
[DisallowMultipleComponent]
public class SpriteBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null) return;

        transform.rotation = targetCamera.transform.rotation;
    }
}
