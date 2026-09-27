using System;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles AR screen-tap input and fires <see cref="clickEvent"/> with raycast
/// results against detected AR planes. Also manages ARPlaneManager detection
/// state, toggling it on or off in response to <see cref="ScalingModeController"/>
/// and <see cref="ModelPlaneDetectionManager"/> events.
/// </summary>
public class RaycastHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private InputActionProperty tapAction;
    [SerializeField] private InputActionProperty positionAction;
    [SerializeField] private bool usePlaneDetection = true;
    [SerializeField] private bool showDetectedPlanes = false;
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    /// <summary>
    /// Raised each time a valid tap is processed.
    /// Passes the list of <see cref="ARRaycastHit"/> results, or <c>null</c>
    /// when plane-detection is disabled.
    /// </summary>
    public static event Action<List<ARRaycastHit>> clickEvent;

    private Vector2 touchPosition;
    private bool touched = false;
    private int touchCount = 0;
    /// <summary>
    /// Caches the <see cref="ARRaycastManager"/> and <see cref="ARPlaneManager"/>
    /// components and sets initial plane-detection visibility.
    /// </summary>
    private void Awake()
    {

        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
        planeManager.enabled = showDetectedPlanes;

    }

    /// <summary>
    /// Subscribes to scaling-mode and plane-detection change events, then
    /// disables plane detection by default.
    /// </summary>
    private void Start()
    {
        if (ScalingModeController.Instance != null)
        {
            ScalingModeController.Instance.modeChanged += OnModeChange;
        }

        if (ModelPlaneDetectionManager.Instance != null)
        {
            ModelPlaneDetectionManager.Instance.OnPlaneDetectionChange += changePlaneDetection;
        }


        changePlaneDetection(false);
    }

    /// <summary>Subscribes the tap input action callback.</summary>
    private void OnEnable()
    {

        tapAction.action.performed += CheckInput;
    }

    /// <summary>Unsubscribes the tap input action callback.</summary>
    private void OnDisable()
    {
        tapAction.action.performed -= CheckInput;
    }

    /// <summary>
    /// Called when the scaling mode changes. Reserved for future plane-detection
    /// adjustments based on whether the 1:1 mode is active.
    /// </summary>
    /// <param name="isOneToOne"><c>true</c> when 1:1 scale mode is active.</param>
    public void OnModeChange(bool isOneToOne)
    {
        //if(isOneToOne) changePlaneDetection(false);

    }

    /// <summary>Each frame, flushes any pending tap into a raycast event.</summary>
    void Update()
    {
        HandleClick();
    }

    /// <summary>
    /// Consumes a buffered tap flag and triggers <see cref="RaiseClickEvent"/>.
    /// Decoupling input capture from processing ensures raycasts run on the main thread.
    /// </summary>
    void HandleClick()
    {
        if (touched)
        {
            touched = false;
            Debug.Log("Clicked");
            RaiseClickEvent();
        }
    }

    /// <summary>
    /// Input action callback that records the tap position and sets the
    /// <c>touched</c> flag, while ignoring taps that land on UI elements.
    /// </summary>
    /// <param name="ctx">The input callback context from the tap action.</param>
    void CheckInput(InputAction.CallbackContext ctx)
    {

        int pointerId = -1;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            Debug.Log("UI Click Detected - Ignoring Scene Raycast");
            return;
        }

        touchCount++;

        if (positionAction.action != null)
        {
            touchPosition = positionAction.action.ReadValue<Vector2>();
        }
        else
        {
            touchPosition = Mouse.current.position.ReadValue();
        }

        touched = true;

    }

    /// <summary>
    /// Performs an AR raycast at the buffered touch position and fires
    /// <see cref="clickEvent"/> with hits when plane detection is on,
    /// or with <c>null</c> when plane detection is off.
    /// </summary>
    void RaiseClickEvent()
    {
        if (usePlaneDetection && raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("plane detected raycast");
            clickEvent?.Invoke(hits);
        }
        else if(!usePlaneDetection)
        {
            Debug.Log("non plane detection raycast");
            clickEvent?.Invoke(null);
        }
    }

    /// <summary>
    /// Enables or disables horizontal AR plane detection and toggles the
    /// visibility of all currently tracked planes.
    /// </summary>
    /// <param name="isOn"><c>true</c> to enable plane detection; <c>false</c> to disable.</param>
    public void changePlaneDetection(bool isOn)
    {
        if (isOn == usePlaneDetection) return;

        usePlaneDetection = isOn;
        if (isOn)
        {
            planeManager.subsystem?.Start();
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }
        else
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
        }
        foreach (ARPlane plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(isOn);

        }
    }

   

}
