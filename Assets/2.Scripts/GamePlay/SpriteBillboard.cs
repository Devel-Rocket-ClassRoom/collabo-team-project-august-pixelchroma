using UnityEngine;

/// <summary>
/// Keeps a 2D battle character facing the gameplay camera while preserving an
/// upright silhouette, similar to a 2.5D tactical game presentation.
/// </summary>
[DisallowMultipleComponent]
public class SpriteBillboard : MonoBehaviour
{
    [Tooltip("카메라를 향한 뒤 이미지 방향을 보정할 각도입니다.")]
    [SerializeField] private Vector3 rotationOffset;

    private void LateUpdate()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null) return;

        // Keep the sprite parallel to the screen. Attach this component only to
        // the visual child, never to the grid-aligned root/collider object.
        transform.rotation = targetCamera.transform.rotation *
                             Quaternion.Euler(rotationOffset);
    }
}
