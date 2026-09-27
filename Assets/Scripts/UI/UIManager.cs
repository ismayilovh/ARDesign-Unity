using System;

using UnityEngine;

/// <summary>
/// Legacy coordinator for the older UI layer. Bridges
/// <see cref="PlaneDetectionController"/> toggles and <see cref="ResetPatentButton"/>
/// clicks to <see cref="RaycastHandler"/>, <see cref="ScreenScaler"/>, and
/// <see cref="ObjectSpawnerAR"/>. Superseded by the New UI system in
/// <c>Scripts/UI/New UI/</c>.
/// </summary>
public class UIManager : MonoBehaviour
{

    [SerializeField] private PlaneDetectionController planeDetectionController;
    [SerializeField] private PlaneDetectionController screenFitController;
    [SerializeField] private ResetPatentButton _resetPatentButton;
    private RaycastHandler raycastHandler;
    private ObjectSpawnerAR _objectSpawnerAR;

    /// <summary>Locates and caches the <see cref="RaycastHandler"/> and <see cref="ObjectSpawnerAR"/> singletons.</summary>
    private void Awake()
    {
        raycastHandler = FindFirstObjectByType<RaycastHandler>();
        _objectSpawnerAR = FindFirstObjectByType<ObjectSpawnerAR>();
    }

    /// <summary>Subscribes to all legacy UI control events.</summary>
    private void OnEnable()
    {
        planeDetectionController.planeDetectionToggleChange += HandlePlaneDetectionChange;
        screenFitController.planeDetectionToggleChange += HandleScreenFitDetectionChange;
        _resetPatentButton.resetPatentTransformAction += ResetPatentTransform;
    }

    /// <summary>Unsubscribes from all legacy UI control events.</summary>
    private void OnDisable()
    {
        planeDetectionController.planeDetectionToggleChange -= HandlePlaneDetectionChange;
        screenFitController.planeDetectionToggleChange -= HandleScreenFitDetectionChange;
        _resetPatentButton.resetPatentTransformAction -= ResetPatentTransform;
    }

    /// <summary>Forwards the plane-detection toggle state to <see cref="RaycastHandler.changePlaneDetection"/>.</summary>
    /// <param name="isOn">New toggle state.</param>
    void HandlePlaneDetectionChange(bool isOn)
    {
        Debug.Log("hello from canvasbaba");
        raycastHandler.changePlaneDetection(isOn);
    }

    /// <summary>Delegates to <see cref="ObjectSpawnerAR.ResetPatentTransform"/>.</summary>
    void ResetPatentTransform()
    {
        _objectSpawnerAR.ResetPatentTransform();
    }

    /// <summary>Forwards the screen-fit toggle state to <see cref="ScreenScaler.SetScreenFit"/>.</summary>
    /// <param name="isOn">New toggle state.</param>
    void HandleScreenFitDetectionChange(bool isOn)
    {
        ScreenScaler.instance.SetScreenFit(isOn);
    }
}
