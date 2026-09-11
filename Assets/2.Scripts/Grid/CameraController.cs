using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private float dragThresholdPixels = 10f;
    [Header("Two Finger Rotation")]
    [SerializeField] private bool enableTwoFingerRotation = true;
    [SerializeField, Range(0.1f, 3f)] private float rotationSensitivity = 1f;
    [SerializeField, Min(0f)] private float rotationThresholdDegrees = 0.15f;

    [Header("Focus / Zoom")]
    [SerializeField] private float focusZoomFOV = 38f;
    [SerializeField] private float focusDuration = 0.35f;
    [SerializeField] private float focusHeightOffset = 2f;

    [Header("Building Visibility")]
    [SerializeField] private bool fadeBuildingsOnContact = true;
    [SerializeField, Min(0.05f)] private float buildingContactRadius = 0.75f;
    [SerializeField, Range(0.05f, 1f)] private float buildingFadeAlpha = 0.2f;
    [SerializeField, Min(0.1f)] private float buildingFadeSpeed = 4f;
    [SerializeField] private LayerMask buildingContactMask = ~0;

    private Camera cam;
    private bool isPressed;
    private bool isDragging;
    private Vector2 pressStartPos;
    private Vector2 lastPointerPos;
    private bool isMultiTouchGesture;
    private bool multiTouchStartedOverUI;

    private Plane groundPlane;
    private Vector3 boundsMin;
    private Vector3 boundsMax;
    private bool hasBounds;

    private float defaultFOV;
    private float targetFOV;
    private Vector3 focusTargetPos;
    private bool isFocusing;
    private float focusLerp = 1f;
    private Vector3 focusStartPos;
    private float focusStartFOV;

    private Transform followTarget;
    private float followSmooth = 8f;

    private readonly Collider[] buildingContactBuffer = new Collider[64];
    private readonly HashSet<Transform> contactedBuildings = new HashSet<Transform>();
    private readonly Dictionary<Transform, BuildingFadeState> fadedBuildings =
        new Dictionary<Transform, BuildingFadeState>();
    private readonly List<Transform> restoredBuildings = new List<Transform>();

    public bool IsFocused { get; private set; }
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
        defaultFOV = cam.fieldOfView;
        targetFOV = defaultFOV;
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
        UpdateFocus();

        if (HandleTwoFingerRotation())
            return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            Vector2 pointerPosition = pointer.position.ReadValue();
            if (IsPointerOverUI(pointerPosition))
            {
                // A previous map press must never survive into a UI interaction.
                isPressed = false;
                isDragging = false;
                return;
            }

            pressStartPos = pointerPosition;
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
            Vector2 releasePosition = pointer.position.ReadValue();
            if (!isDragging && !IsPointerOverUI(releasePosition))
                OnTap?.Invoke(pressStartPos);
        }
    }

    private void LateUpdate()
    {
        UpdateBuildingVisibility();
    }

    private void OnDisable()
    {
        RestoreAllBuildings();
    }

    private void OnDestroy()
    {
        RestoreAllBuildings();
        if (Instance == this)
            Instance = null;
    }

    private bool HandleTwoFingerRotation()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (!enableTwoFingerRotation || touchscreen == null)
            return false;

        var activeTouches = new List<UnityEngine.InputSystem.Controls.TouchControl>(2);
        foreach (var touch in touchscreen.touches)
        {
            if (touch.press.isPressed)
            {
                activeTouches.Add(touch);
                if (activeTouches.Count == 2) break;
            }
        }

        if (activeTouches.Count < 2)
        {
            if (isMultiTouchGesture)
            {
                // Do not let the remaining finger turn the completed rotation into
                // a drag or a map tap. Normal input resumes after every finger lifts.
                if (activeTouches.Count == 0)
                {
                    isMultiTouchGesture = false;
                    multiTouchStartedOverUI = false;
                }
                return true;
            }
            return false;
        }

        var firstTouch = activeTouches[0];
        var secondTouch = activeTouches[1];
        Vector2 firstPosition = firstTouch.position.ReadValue();
        Vector2 secondPosition = secondTouch.position.ReadValue();

        if (!isMultiTouchGesture)
        {
            isMultiTouchGesture = true;
            isPressed = false;
            isDragging = false;
            multiTouchStartedOverUI =
                IsPointerOverUI(firstPosition) || IsPointerOverUI(secondPosition);
        }

        if (multiTouchStartedOverUI)
            return true;

        Vector2 previousFirst = firstPosition - firstTouch.delta.ReadValue();
        Vector2 previousSecond = secondPosition - secondTouch.delta.ReadValue();
        Vector2 previousDirection = previousSecond - previousFirst;
        Vector2 currentDirection = secondPosition - firstPosition;

        if (previousDirection.sqrMagnitude < 1f || currentDirection.sqrMagnitude < 1f)
            return true;

        float twistDegrees = Vector2.SignedAngle(previousDirection, currentDirection);
        if (Mathf.Abs(twistDegrees) < rotationThresholdDegrees)
            return true;

        Vector3 pivot = GetCurrentGroundFocus();
        cam.transform.RotateAround(
            pivot,
            Vector3.up,
            -twistDegrees * rotationSensitivity);

        return true;
    }

    private Vector3 GetCurrentGroundFocus()
    {
        Ray forwardRay = new Ray(cam.transform.position, cam.transform.forward);
        if (groundPlane.Raycast(forwardRay, out float distance))
            return forwardRay.GetPoint(distance);

        return hasBounds ? (boundsMin + boundsMax) * 0.5f : Vector3.zero;
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var eventData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        var raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(eventData, raycastResults);
        return raycastResults.Count > 0;
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

    public void FocusOn(Vector3 worldPosition)
    {
        Vector3 offset = cam.transform.position - GetCurrentGroundFocus();
        focusTargetPos = worldPosition + offset;
        focusTargetPos.y = cam.transform.position.y;
        if (hasBounds) focusTargetPos = ClampToGroundBounds(focusTargetPos);

        focusStartPos = cam.transform.position;
        focusStartFOV = cam.fieldOfView;
        targetFOV = focusZoomFOV;
        focusLerp = 0f;
        isFocusing = true;
        IsFocused = true;
    }

    public void ResetFocus()
    {
        if (!IsFocused) return;
        focusStartPos = cam.transform.position;
        focusStartFOV = cam.fieldOfView;
        targetFOV = defaultFOV;
        focusTargetPos = cam.transform.position;
        focusLerp = 0f;
        isFocusing = true;
        IsFocused = false;
    }

    public Coroutine FocusOnAndWait(Vector3 worldPosition)
    {
        FocusOn(worldPosition);
        return StartCoroutine(WaitForFocus());
    }

    private System.Collections.IEnumerator WaitForFocus()
    {
        while (isFocusing)
            yield return null;
    }

    public void StartFollow(Transform target)
    {
        followTarget = target;
    }

    public void StopFollow()
    {
        followTarget = null;
    }

    private void UpdateFocus()
    {
        if (isFocusing)
        {
            focusLerp += Time.deltaTime / Mathf.Max(0.01f, focusDuration);
            if (focusLerp >= 1f)
            {
                focusLerp = 1f;
                isFocusing = false;
            }

            float t = Mathf.SmoothStep(0f, 1f, focusLerp);
            cam.transform.position = Vector3.Lerp(focusStartPos, focusTargetPos, t);
            cam.fieldOfView = Mathf.Lerp(focusStartFOV, targetFOV, t);
        }

        if (followTarget != null && !isFocusing)
        {
            Vector3 offset = cam.transform.position - GetCurrentGroundFocus();
            Vector3 desired = followTarget.position + offset;
            desired.y = cam.transform.position.y;
            if (hasBounds) desired = ClampToGroundBounds(desired);
            cam.transform.position = Vector3.Lerp(
                cam.transform.position, desired, Time.deltaTime * followSmooth);
        }
    }

    private void UpdateBuildingVisibility()
    {
        if (!fadeBuildingsOnContact)
        {
            RestoreAllBuildings();
            return;
        }

        contactedBuildings.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            buildingContactRadius,
            buildingContactBuffer,
            buildingContactMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = buildingContactBuffer[i];
            buildingContactBuffer[i] = null;
            if (hit == null) continue;

            Transform building = FindBuildingRoot(hit.transform);
            if (building == null || !contactedBuildings.Add(building))
                continue;

            if (!fadedBuildings.ContainsKey(building))
            {
                BuildingFadeState state = new BuildingFadeState(building);
                if (state.HasRenderers)
                    fadedBuildings.Add(building, state);
            }
        }

        restoredBuildings.Clear();
        foreach (KeyValuePair<Transform, BuildingFadeState> pair in fadedBuildings)
        {
            bool touching = pair.Key != null && contactedBuildings.Contains(pair.Key);
            float targetAlpha = touching ? buildingFadeAlpha : 1f;
            bool restored = pair.Value.MoveTowards(
                targetAlpha,
                buildingFadeSpeed * Time.deltaTime);

            if (!touching && restored)
                restoredBuildings.Add(pair.Key);
        }

        foreach (Transform building in restoredBuildings)
        {
            if (fadedBuildings.TryGetValue(building, out BuildingFadeState state))
                state.Restore();
            fadedBuildings.Remove(building);
        }
    }

    private static Transform FindBuildingRoot(Transform hit)
    {
        Transform building = null;
        Transform current = hit;

        while (current != null)
        {
            string objectName = current.name.ToLowerInvariant();
            if (objectName.Contains("building") ||
                objectName.Contains("apartment") ||
                objectName.Contains("skyscraper"))
            {
                building = current;
            }

            if (objectName.StartsWith("background_") || objectName == "city_all")
                break;

            current = current.parent;
        }

        return building;
    }

    private void RestoreAllBuildings()
    {
        foreach (BuildingFadeState state in fadedBuildings.Values)
            state.Restore();

        fadedBuildings.Clear();
        contactedBuildings.Clear();
        restoredBuildings.Clear();
    }

    private sealed class BuildingFadeState
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

        private readonly List<RendererMaterials> renderers = new List<RendererMaterials>();
        private float currentAlpha = 1f;
        private bool isRestored;

        public bool HasRenderers => renderers.Count > 0;

        public BuildingFadeState(Transform root)
        {
            Renderer[] childRenderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in childRenderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;

                Material[] originals = renderer.sharedMaterials;
                Material[] instances = new Material[originals.Length];
                Color[] colors = new Color[originals.Length];
                bool hasMaterial = false;

                for (int i = 0; i < originals.Length; i++)
                {
                    Material original = originals[i];
                    if (original == null) continue;

                    Material instance = new Material(original)
                    {
                        name = original.name + " (Camera Fade)"
                    };
                    instances[i] = instance;
                    colors[i] = GetColor(instance);
                    ConfigureTransparent(instance);
                    hasMaterial = true;
                }

                if (!hasMaterial)
                    continue;

                renderer.sharedMaterials = instances;
                renderers.Add(new RendererMaterials(renderer, originals, instances, colors));
            }
        }

        public bool MoveTowards(float targetAlpha, float maxDelta)
        {
            if (isRestored) return true;

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, maxDelta);
            foreach (RendererMaterials entry in renderers)
                entry.SetAlpha(currentAlpha);

            return Mathf.Approximately(currentAlpha, 1f) &&
                   Mathf.Approximately(targetAlpha, 1f);
        }

        public void Restore()
        {
            if (isRestored) return;
            isRestored = true;

            foreach (RendererMaterials entry in renderers)
                entry.Restore();
            renderers.Clear();
        }

        private static Color GetColor(Material material)
        {
            if (material.HasProperty(BaseColorId))
                return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId))
                return material.GetColor(ColorId);
            return Color.white;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty(SurfaceId))
                material.SetFloat(SurfaceId, 1f);
            if (material.HasProperty(BlendId))
                material.SetFloat(BlendId, 0f);
            if (material.HasProperty(SrcBlendId))
                material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendId))
                material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteId))
                material.SetFloat(ZWriteId, 0f);
            if (material.HasProperty(OutlineEnabledId))
                material.SetFloat(OutlineEnabledId, 0f);

            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_OUTLINE_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private sealed class RendererMaterials
        {
            private readonly Renderer renderer;
            private readonly Material[] originals;
            private readonly Material[] instances;
            private readonly Color[] colors;

            public RendererMaterials(
                Renderer renderer,
                Material[] originals,
                Material[] instances,
                Color[] colors)
            {
                this.renderer = renderer;
                this.originals = originals;
                this.instances = instances;
                this.colors = colors;
            }

            public void SetAlpha(float alpha)
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    Material material = instances[i];
                    if (material == null) continue;

                    Color color = colors[i];
                    color.a *= alpha;
                    if (material.HasProperty(BaseColorId))
                        material.SetColor(BaseColorId, color);
                    else if (material.HasProperty(ColorId))
                        material.SetColor(ColorId, color);
                }
            }

            public void Restore()
            {
                if (renderer != null)
                    renderer.sharedMaterials = originals;

                foreach (Material material in instances)
                {
                    if (material != null)
                        Object.Destroy(material);
                }
            }
        }
    }
}
