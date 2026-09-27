using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy toggle component used in the older UI layer. Changes its label colour
/// and fires <see cref="planeDetectionToggleChange"/> when the toggle state changes.
/// Used by <see cref="UIManager"/> for plane-detection and screen-fit controls.
/// </summary>
public class PlaneDetectionController : MonoBehaviour
{

    private Text toggleText;
    private Toggle toggle;

    /// <summary>Fired when the toggle value changes. Parameter is the new state.</summary>
    public event Action<bool> planeDetectionToggleChange;

    /// <summary>
    /// Resolves the child text label and the toggle component, registers the
    /// value-changed listener, and sets the initial label colour to green.
    /// </summary>
    private void Awake()
    {
        toggleText = GetComponentInChildren<Text>();
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnPlaneDetectionToggleChange);
        toggleText.color = Color.green;
    }

    /// <summary>
    /// Called by the toggle's onValueChanged event. Updates the label colour and
    /// raises <see cref="planeDetectionToggleChange"/>.
    /// </summary>
    /// <param name="isOn">The new toggle state passed by Unity's event system.</param>
    public void OnPlaneDetectionToggleChange(bool isOn)
    {
        bool t = toggle.isOn;
        Debug.Log("hello from toggle : " + t.ToString());
        toggleText.color = t ? Color.green : Color.red;
        planeDetectionToggleChange?.Invoke(t);
    }

}
