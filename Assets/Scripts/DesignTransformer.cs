using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles touch gestures (drag-to-rotate, pinch-to-scale) and mouse fallback
/// to transform the active design model. Smoothly interpolates towards
/// <c>targetRotation</c> and <c>targetScale</c> each frame.
/// Reacts to <see cref="ScalingModeController.modeChanged"/>,
/// <see cref="ModelTransformManager.OnLockModel"/>, and
/// <see cref="ObjectSpawnerAR.objectSpawnedEvent"/>.
/// </summary>
public class DesignTransformer : MonoBehaviour
{
    private DesignManager designManager;

    [SerializeField] private InputActionProperty dragDeltaAction;
    [SerializeField] private InputActionProperty pinchDeltaAction;
    [SerializeField] private InputActionProperty twistDeltaAction;

    [SerializeField] private float dragRotationSpeed = 0.12f;
    [SerializeField] private float twistRotationSpeed = 0.5f;

    [SerializeField] private float pinchScaleSpeed = 0.05f;
    [SerializeField] private float minScale = 0.01f;
    [SerializeField] private float maxScale = 2f;

    [SerializeField] private bool useMouseFallback = true;
    [SerializeField] private float mouseDragToRotate = 0.2f;
    [SerializeField] private float mouseScrollToScale = 0.1f;

    [SerializeField] private float smoothSpeed = 2f;

    private Quaternion targetRotation;
    private Vector3 targetScale;
    private bool transformInputsEnabled;
    private Transform activeDesignTransform;
    private Camera mainCamera;
    
    /// <summary>Caches the <see cref="DesignManager"/> singleton reference.</summary>
    private void Start()
    {
        designManager = DesignManager.Instance;
        mainCamera = Camera.main;

    }

    /// <summary>
    /// Each frame, smoothly interpolates the active design's scale toward
    /// <c>targetScale</c> and its rotation toward <c>targetRotation</c> using
    /// Slerp, keeping the model orbiting around its rotation center.
    /// </summary>
    private void Update()
    {
        if (activeDesignTransform == null) return;

        float interpolation = Time.deltaTime * smoothSpeed;
        if ((activeDesignTransform.localScale - targetScale).sqrMagnitude > 0.00000001f)
        {
            Vector3 nextScale = Vector3.Lerp(activeDesignTransform.localScale, targetScale, interpolation);
            activeDesignTransform.localScale = (nextScale - targetScale).sqrMagnitude <= 0.00000001f
                ? targetScale
                : nextScale;
        }

        if (Quaternion.Angle(activeDesignTransform.rotation, targetRotation) > 0.01f)
        {
            activeDesignTransform.rotation = Quaternion.Slerp(
                activeDesignTransform.rotation,
                targetRotation,
                interpolation);
        }
    }

    /// <summary>Registers all input action callbacks and controller event subscriptions.</summary>
    private void OnEnable()
    {
        EnableTransformInputs();
        EnableControllers();
    }

    /// <summary>Removes all input action callbacks and controller event subscriptions.</summary>
    private void OnDisable()
    {
        DisableTransformInputs();
        DisableControllers();
    }


    /// <summary>
    /// Subscribes the drag and pinch input actions and the pivot-change event
    /// so the model responds to touch gestures.
    /// </summary>
    private void EnableTransformInputs()
    {
        if (transformInputsEnabled) return;

        if (dragDeltaAction.action != null)
        {
            dragDeltaAction.action.performed += OnDragDelta;

        }


        if (pinchDeltaAction.action != null)
        {
            pinchDeltaAction.action.performed += OnPinchDelta;
        }

        transformInputsEnabled = true;
    }

    /// <summary>
    /// Unsubscribes the drag and pinch input actions and the pivot-change event
    /// so gestures no longer affect the model (used when locked or in 1:1 mode).
    /// </summary>
    private void DisableTransformInputs()
    {
        if (!transformInputsEnabled) return;

        if (dragDeltaAction.action != null)
        {
            dragDeltaAction.action.performed -= OnDragDelta;

        }



        if (pinchDeltaAction.action != null)
            pinchDeltaAction.action.performed -= OnPinchDelta;

        transformInputsEnabled = false;
    }

    /// <summary>
    /// Applies mouse-drag rotation to <c>targetRotation</c> when
    /// <c>useMouseFallback</c> is enabled and the left mouse button is held.
    /// Rotates around the world Y axis (horizontal drag) and the camera's right
    /// axis (vertical drag).
    /// </summary>
    private void UpdateMouseRotation()
    {
        if (!useMouseFallback) return;
        if (designManager.ActiveDesign == null) return;
        if (!Input.GetMouseButton(0)) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (mouseX == 0f && mouseY == 0f) return;

        float yDegrees = -mouseX * mouseDragToRotate;
        float xDegrees =  mouseY * mouseDragToRotate;

        Quaternion rotY = Quaternion.AngleAxis(yDegrees, Vector3.up);
        Quaternion rotX = Quaternion.AngleAxis(xDegrees, mainCamera.transform.right);

        targetRotation = rotX * rotY * targetRotation;
    }


    /// <summary>
    /// Subscribes to scaling-mode, reset, lock, and spawn events from the
    /// manager singletons and <see cref="ObjectSpawnerAR"/>.
    /// </summary>
    private void EnableControllers()
    {
        if (ScalingModeController.Instance != null)
            ScalingModeController.Instance.modeChanged += OnModeChange;

        if (ModelTransformManager.Instance != null)
        {
            ModelTransformManager.Instance.OnResetModelTransform += ResetRotation;
            ModelTransformManager.Instance.OnLockModel += ChangeTransformInputState;
        }

        ObjectSpawnerAR.objectSpawnedEvent += HandleSpawnPosition;
    }

    /// <summary>
    /// Unsubscribes from all manager and spawner events registered in
    /// <see cref="EnableControllers"/>.
    /// </summary>
    private void DisableControllers()
    {
        if (ScalingModeController.Instance != null)
            ScalingModeController.Instance.modeChanged -= OnModeChange;

        if (ModelTransformManager.Instance != null)
        {
            ModelTransformManager.Instance.OnResetModelTransform -= ResetRotation;
            ModelTransformManager.Instance.OnLockModel -= ChangeTransformInputState;
        }

        ObjectSpawnerAR.objectSpawnedEvent -= HandleSpawnPosition;
    }

    /// <summary>
    /// Enables or disables touch/mouse transform inputs based on the lock state
    /// received from <see cref="ModelTransformManager.OnLockModel"/>.
    /// </summary>
    /// <param name="isLocked"><c>true</c> to lock (disable inputs); <c>false</c> to unlock.</param>
    private void ChangeTransformInputState(bool isLocked)
    {
        if (isLocked) DisableTransformInputs();
        else          EnableTransformInputs();
    }


    /// <summary>
    /// Input action callback for one-finger drag. Converts the 2D screen-space
    /// delta into a combined Y-axis (yaw) and camera-right-axis (pitch) rotation
    /// and accumulates it into <c>targetRotation</c>.
    /// </summary>
    /// <param name="ctx">The input callback context providing the drag delta as <see cref="Vector2"/>.</param>
    private void OnDragDelta(InputAction.CallbackContext ctx)
    {
        if (designManager.ActiveDesign == null) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
        float yDegrees = -delta.x * dragRotationSpeed;
        float xDegrees =  delta.y * dragRotationSpeed;

        Quaternion rotY = Quaternion.AngleAxis(yDegrees, Vector3.up);
        Quaternion rotX = Quaternion.AngleAxis(xDegrees, mainCamera.transform.right);

        targetRotation = rotX * rotY * targetRotation;
    }

    /// <summary>
    /// Input action callback for two-finger pinch. Converts the pinch magnitude
    /// into a uniform scale factor and clamps the result between
    /// <c>minScale</c> and <c>maxScale</c>.
    /// </summary>
    /// <param name="ctx">The input callback context providing the pinch delta as <see cref="float"/>.</param>
    private void OnPinchDelta(InputAction.CallbackContext ctx)
    {
        if (designManager.ActiveDesign == null) return;

        float pinchDelta  = ctx.ReadValue<float>();
        float scaleFactor = 1f + pinchDelta * pinchScaleSpeed;

        Vector3 newScale = targetScale * scaleFactor;
        float   clamped  = Mathf.Clamp(newScale.x, minScale, maxScale);
        targetScale = new Vector3(clamped, clamped, clamped);
    }


    /// <summary>
    /// Reacts to a scaling mode change by switching between
    /// <see cref="OneToOneScale"/> and <see cref="FitToScreenScale"/>.
    /// </summary>
    /// <param name="isOneToOne"><c>true</c> to enter 1:1 scale mode.</param>
    public void OnModeChange(bool isOneToOne)
    {
        if (isOneToOne) OneToOneScale();
        else            FitToScreenScale();
    }

    /// <summary>
    /// Called when the design is first spawned. Adjusts <c>pinchScaleSpeed</c>
    /// relative to the model's largest dimension, applies the initial screen-fit
    /// scale, and snaps the Y position to the spawn surface.
    /// </summary>
    /// <param name="toGround">
    /// <c>true</c> if the model was placed on a detected plane (snap to ground);
    /// <c>false</c> if placed without plane detection.
    /// </param>
    public void HandleSpawnPosition(bool toGround)
    {
        GameObject activeDesign = designManager.ActiveDesign;
        if (activeDesign == null) return;
        activeDesignTransform = activeDesign.transform;
        Bounds bounds = PivotSetter.GetSpawnBounds(activeDesign);
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        pinchScaleSpeed *= 3 / maxSize;
        Debug.Log("Max size is : " + maxSize);
        PivotSetter pivotSetter = activeDesign.GetComponent<PivotSetter>();
        bool useCalculatedPivot = PivotSettingManager.Instance != null &&
                                  PivotSettingManager.Instance.UseCalculatedPivot;
        if (pivotSetter != null)
        {
            pivotSetter.SetPlacementOnGround(toGround);
            pivotSetter.SetUseCalculatedPivot(useCalculatedPivot);
        }

        float scale = ScreenScaler.instance.FitScreen(activeDesign, true);
        if (scale > 0) activeDesign.transform.localScale *= scale;
        SyncTargets();
    }

    /// <summary>
    /// Resets the active design's rotation, scale, and position to the values
    /// captured at spawn time.
    /// </summary>
    public void ResetRotation()
    {
        if (designManager.ActiveDesign == null) return;
        targetRotation = designManager.initialRotation;
        targetScale    = designManager.initialScale;
        designManager.ActiveDesign.transform.position = designManager.initialPosition;
    }

    /// <summary>
    /// Switches to FitScreen mode: resets the model transform and re-enables
    /// touch/mouse transform inputs.
    /// </summary>
    public void FitToScreenScale()
    {
        if (designManager.Design == null || designManager.ActiveDesign == null) return;
        ResetRotation();
        EnableTransformInputs();
    }

    /// <summary>
    /// Switches to 1:1 real-world scale mode: sets scale to
    /// <see cref="Vector3.one"/>, resets rotation, pushes the model out of the
    /// camera's view via <see cref="PivotSetter.ChangeToOneOneMode"/>, and
    /// disables touch/mouse transform inputs.
    /// </summary>
    public void OneToOneScale()
    {
        if (designManager.Design == null || designManager.ActiveDesign == null) return;
        targetScale    = Vector3.one;
        targetRotation = designManager.initialRotation;
        DesignManager.Instance.ActiveDesign.transform.rotation = DesignManager.Instance.initialRotation;
        PivotSetter.ChangeToOneOneMode(designManager.ActiveDesign, mainCamera.transform, 1);
        DisableTransformInputs();
    }


    /// <summary>
    /// Snaps <c>targetRotation</c> and <c>targetScale</c> to the active design's
    /// current transform values so the interpolation loop starts from the correct
    /// baseline (called after spawn-time scaling is applied).
    /// </summary>
    private void SyncTargets()
    {
        if (designManager.ActiveDesign == null) return;
        targetRotation = designManager.ActiveDesign.transform.rotation;
        targetScale    = designManager.ActiveDesign.transform.localScale;
    }
}
