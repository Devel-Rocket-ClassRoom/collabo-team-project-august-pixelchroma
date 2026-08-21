using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private float dragThresholdPixels = 10f;

    private Camera cam;
    private bool isPressed;
    private bool isDragging;
    private Vector2 pressStartPos;
    private Vector2 lastPointerPos;

    private Plane groundPlane;
    private Vector3 boundsMin;
    private Vector3 boundsMax;
    private bool hasBounds;

    public System.Action<Vector2> OnTap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    public void SetBounds(Vector3 gridMin, Vector3 gridMax, float padding, float groundY)
    {
        groundPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
        boundsMin = new Vector3(gridMin.x - padding, 0, gridMin.z - padding);
        boundsMax = new Vector3(gridMax.x + padding, 0, gridMax.z + padding);
        hasBounds = true;
    }

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            pressStartPos = pointer.position.ReadValue();
            lastPointerPos = pressStartPos;
            isPressed = true;
            isDragging = false;
        }

        if (isPressed && pointer.press.isPressed)
        {
            Vector2 currentPos = pointer.position.ReadValue();

            if (!isDragging &&
                (currentPos - pressStartPos).sqrMagnitude > dragThresholdPixels * dragThresholdPixels)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Ray rayBefore = cam.ScreenPointToRay(lastPointerPos);
                Ray rayAfter = cam.ScreenPointToRay(currentPos);
                float distBefore, distAfter;

                if (groundPlane.Raycast(rayBefore, out distBefore) &&
                    groundPlane.Raycast(rayAfter, out distAfter))
                {
                    Vector3 worldBefore = rayBefore.GetPoint(distBefore);
                    Vector3 worldAfter = rayAfter.GetPoint(distAfter);
                    Vector3 delta = worldBefore - worldAfter;
                    delta.y = 0f;

                    Vector3 newPos = cam.transform.position + delta;
                    if (hasBounds) newPos = ClampToGroundBounds(newPos);
                    cam.transform.position = newPos;
                }
            }

            lastPointerPos = currentPos;
        }

        if (isPressed && pointer.press.wasReleasedThisFrame)
        {
            isPressed = false;
            if (!isDragging)
                OnTap?.Invoke(pressStartPos);
        }
    }

    private Vector3 ClampToGroundBounds(Vector3 camPos)
    {
        Ray forwardRay = new Ray(camPos, cam.transform.forward);
        float dist;
        if (!groundPlane.Raycast(forwardRay, out dist)) return camPos;

        Vector3 lookAt = forwardRay.GetPoint(dist);
        Vector3 clamped = new Vector3(
            Mathf.Clamp(lookAt.x, boundsMin.x, boundsMax.x),
            lookAt.y,
            Mathf.Clamp(lookAt.z, boundsMin.z, boundsMax.z));

        return camPos + (clamped - lookAt);
    }
}
