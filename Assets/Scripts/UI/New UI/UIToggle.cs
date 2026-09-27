using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Wrapper around Unity's <see cref="Toggle"/> that adds visual color feedback
/// (green when on, default color when off) and exposes a typed
/// <see cref="OnToggleValueChanged"/> event for other components to subscribe to.
/// </summary>
public class UIToggle : MonoBehaviour
{
    private Toggle m_Toggle;

    /// <summary>Fired when the toggle value changes. Parameter is the new state.</summary>
    public event UnityAction<bool> OnToggleValueChanged;

    private Image m_Image;
    private Color m_DefaultColor;

    /// <summary>The current toggle value.</summary>
    public bool IsOn => m_Toggle != null && m_Toggle.isOn;

    /// <summary>
    /// Caches the <see cref="Image"/> component and its default color, then
    /// registers <see cref="OnTogglePress"/> as the toggle's value-changed listener.
    /// </summary>
    void Awake()
    {
        m_Image = GetComponent<Image>();
        m_DefaultColor = m_Image.color;

        m_Toggle = GetComponent<Toggle>();

        m_Toggle.onValueChanged.AddListener(OnTogglePress);
    }

    private void OnDestroy()
    {
        if (m_Toggle != null)
            m_Toggle.onValueChanged.RemoveListener(OnTogglePress);
    }

    /// <summary>
    /// Updates the toggle and its visual state without publishing
    /// <see cref="OnToggleValueChanged"/>.
    /// </summary>
    public void SetValueWithoutNotify(bool isOn)
    {
        if (m_Toggle == null) return;

        m_Toggle.SetIsOnWithoutNotify(isOn);
        ChangeColor(isOn);
    }

    /// <summary>
    /// Internal listener called by Unity's toggle system. Applies the color
    /// change and then forwards the new state via <see cref="OnToggleValueChanged"/>.
    /// </summary>
    /// <param name="isOn">The new toggle state.</param>
    private void OnTogglePress(bool isOn)
    {
        ChangeColor(isOn);
        OnToggleValueChanged?.Invoke(isOn);
    }

    /// <summary>
    /// Sets the background image to green when <paramref name="isOn"/> is <c>true</c>
    /// and restores the original color when <c>false</c>.
    /// </summary>
    /// <param name="isOn"><c>true</c> when the toggle is active.</param>
    private void ChangeColor(bool isOn)
    {
        if (isOn)
        {
            m_Image.color = Color.green;
        }
        else
        {
            m_Image.color = m_DefaultColor;
        }
    }
}
