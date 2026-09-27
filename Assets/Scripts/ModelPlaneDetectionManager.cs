using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Singleton manager for the in-AR plane-detection toggle.
/// Tracks the user's plane-detection preference and disables the toggle
/// automatically when 1:1 scale mode is active. Must be initialised
/// by calling <see cref="Initialize"/> (done by <see cref="MiscOptionsManager"/>).
/// </summary>
public class ModelPlaneDetectionManager : MonoBehaviour
{
    GameObject m_Parent;

    /// <summary>The single active instance of <see cref="ModelPlaneDetectionManager"/>.</summary>
    public static ModelPlaneDetectionManager Instance { get; private set; }

    /// <summary>
    /// Fired when plane detection is toggled on or off.
    /// The <c>bool</c> parameter is <c>true</c> when detection is enabled.
    /// </summary>
    public event UnityAction<bool> OnPlaneDetectionChange;

    private UIToggle m_UIToggleComponent;
    private Toggle planeToggle;
    private bool userPreference = false;

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Wires up toggle listeners and state-machine callbacks.
    /// Call once after all singleton instances are ready.
    /// </summary>
    public void Initialize()
    {
        planeToggle = GetComponent<Toggle>();
        m_UIToggleComponent = GetComponent<UIToggle>();

        m_UIToggleComponent.OnToggleValueChanged += (bool isOn) =>
        {
            OnPlaneDetectionChange?.Invoke(isOn);
            userPreference = !userPreference;
        };

        OnPlaneDetectionChange += (bool isOn) => { print("plane detection pressed");

            Debug.Log("user preference is : " + userPreference);
        };

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += () => { planeToggle.isOn = false; };
        ScalingModeController.Instance.modeChanged += ChangeButtonState;
    }

    /// <summary>
    /// Reacts to scaling-mode changes by enabling or disabling the plane-detection
    /// toggle and restoring the user's saved preference when possible.
    /// </summary>
    /// <param name="isOff">
    /// <c>true</c> when 1:1 mode is active (toggle must be disabled);
    /// <c>false</c> when FitScreen mode is active.
    /// </param>
    private void ChangeButtonState(bool isOff)
    {
        Debug.Log(!isOff + " : " + userPreference);
        planeToggle.SetIsOnWithoutNotify(!isOff && userPreference);
        planeToggle.interactable = !isOff;
        OnPlaneDetectionChange?.Invoke(!isOff && userPreference);
    }
}
