using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Runtime popup window component. Manages a header, scrollable content area,
/// and a footer with one or more action buttons. The <see cref="PopupLayer"/>
/// overlay is automatically shown and hidden with this window.
/// Construct instances through <see cref="PopupBuilder"/> rather than directly.
/// </summary>
public class PopupWindow : MonoBehaviour
{
    [SerializeField] private GameObject m_Content;
    [SerializeField] private GameObject m_Header;
    [SerializeField] private GameObject m_Footer;

    [SerializeField] private GameObject m_ButtonSample;

    /// <summary>Preset button color for destructive/cancel actions (red).</summary>
    public static Color BUTTON_COLOR_CANCEL = new (0.7529412f, 0.2235294f, 0.1686275f);

    /// <summary>Preset button color for confirmation/OK actions (green).</summary>
    public static Color BUTTON_COLOR_OK = new (0.1529412f, 0.682353f, 0.3764706f);

    /// <summary>
    /// The full-screen overlay GameObject shown while any popup is visible.
    /// Set by <see cref="PopupLayerManager"/> and <see cref="PopupBuilder"/>.
    /// </summary>
    public static GameObject PopupLayer;

    private void OnEnable()
    {
        PopupLayer.SetActive(true);
    }

    private void OnDisable()
    {
        PopupLayer.SetActive(false);
    }

    /// <summary>
    /// Creates a <see cref="TextMeshProUGUI"/> label inside the content area.
    /// </summary>
    /// <param name="content">The text string to display.</param>
    public void AddContentText(string content)
    {
        GameObject textObject = new GameObject();
        textObject.transform.SetParent(m_Content.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(500f, rectTransform.sizeDelta.y);

        ContentSizeFitter contentSizeFitter = textObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>
    /// Creates a bold <see cref="TextMeshProUGUI"/> label inside the header area.
    /// </summary>
    /// <param name="headerText">The header string to display.</param>
    public void AddHeader(string headerText)
    {
        GameObject textObject = new GameObject();
        textObject.transform.SetParent(m_Header.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = headerText;
        text.fontStyle = FontStyles.Bold;

        ContentSizeFitter contentSizeFitter = textObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>
    /// Parents an arbitrary <see cref="GameObject"/> into the content area and
    /// activates it if necessary.
    /// </summary>
    /// <param name="content">The GameObject to embed in the content area.</param>
    public void AddContent(GameObject content)
    {
        if (!content.activeSelf) content.SetActive(true);
        content.transform.SetParent(m_Content.transform, false);
    }

    /// <summary>
    /// Instantiates a button from the sample prefab, sets its color, label, and
    /// click callback, then places it in the footer area.
    /// </summary>
    /// <param name="buttonText">Label to display on the button.</param>
    /// <param name="buttonColor">Background color of the button image.</param>
    /// <param name="callback">Action to invoke when the button is clicked.</param>
    public void AddFooterButton(string buttonText, Color buttonColor, UnityAction callback)
    {
        GameObject button = Instantiate(m_ButtonSample, m_Footer.transform, false);

        //GameObject button = new GameObject();
        button.transform.SetParent(m_Footer.transform, false);
        Image imageComponent = button.GetComponent<Image>();

        //UnityEngine.UI.Image imageComponent = button.AddComponent<UnityEngine.UI.Image>();
        imageComponent.color = buttonColor;
        //imageComponent.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150f, 50f);

        Button buttonComponent = button.GetComponent<Button>();

        buttonComponent.onClick.AddListener(callback);


        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        textComponent.alignment = TextAlignmentOptions.Midline;
        textComponent.enableAutoSizing = true;

        rectTransform = textComponent.rectTransform;

        rectTransform.anchorMin = Vector2.zero; 
        rectTransform.anchorMax = Vector2.one;  
        
        rectTransform.offsetMin = Vector2.zero; 
        rectTransform.offsetMax = Vector2.zero;

        textComponent.text = buttonText;
    }

}
