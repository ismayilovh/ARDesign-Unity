using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fluent builder for creating <see cref="PopupWindow"/> instances. Instantiates
/// the <c>Popup Window</c> prefab from Resources, attaches it to the
/// <c>PopupParent</c> tagged object, and configures it via chained method calls.
/// </summary>
/// <example>
/// <code>
/// GameObject popup = PopupBuilder.Create()
///     .WithHeader("WARNING")
///     .WithContentText("Are you sure?")
///     .WithFooterButton("OK", PopupWindow.BUTTON_COLOR_OK, OnOK)
///     .Get();
/// </code>
/// </example>
public class PopupBuilder
{
    private GameObject m_PopupWindowPrefab;
    private PopupWindow m_PopupWindowScript;

    private const string m_PopupWindowPrefabPath = "Popup Window";

    /// <summary>Creates and returns a new <see cref="PopupBuilder"/> instance.</summary>
    /// <returns>A new <see cref="PopupBuilder"/>.</returns>
    public static PopupBuilder Create()
    {
        return new PopupBuilder();
    }

    private PopupBuilder()
    {
        m_PopupWindowPrefab = Resources.Load<GameObject>(m_PopupWindowPrefabPath);
        m_PopupWindowPrefab = GameObject.Instantiate(m_PopupWindowPrefab, GameObject.FindWithTag("PopupParent").transform);

        m_PopupWindowScript = m_PopupWindowPrefab.GetComponent<PopupWindow>();
        PopupWindow.PopupLayer = GameObject.FindWithTag("PopupLayer");
    }

    /// <summary>Adds a text paragraph to the popup's content area.</summary>
    /// <param name="text">The text to display.</param>
    /// <returns>This builder for chaining.</returns>
    public PopupBuilder WithContentText(string text)
    {
        m_PopupWindowScript.AddContentText(text);
        return this;
    }

    /// <summary>Sets the popup's header text.</summary>
    /// <param name="text">The header string to display in bold.</param>
    /// <returns>This builder for chaining.</returns>
    public PopupBuilder WithHeader(string text)
    {
        m_PopupWindowScript.AddHeader(text);
        return this;
    }

    /// <summary>Adds a footer action button.</summary>
    /// <param name="buttonText">Label displayed on the button.</param>
    /// <param name="color">Background color of the button.</param>
    /// <param name="callback">Action invoked when the button is clicked.</param>
    /// <returns>This builder for chaining.</returns>
    public PopupBuilder WithFooterButton(string buttonText, Color color, UnityAction callback)
    {
        m_PopupWindowScript.AddFooterButton(buttonText, color, callback);
        return this;
    }

    /// <summary>Places an arbitrary <see cref="GameObject"/> into the popup's content area.</summary>
    /// <param name="content">The content object to embed.</param>
    /// <returns>This builder for chaining.</returns>
    public PopupBuilder WithContent(GameObject content)
    {
        m_PopupWindowScript.AddContent(content);
        return this;
    }

    /// <summary>
    /// Returns the fully configured popup <see cref="GameObject"/> ready for display.
    /// </summary>
    /// <returns>The instantiated popup window.</returns>
    public GameObject Get()
    {
        return m_PopupWindowPrefab;
    }

}
