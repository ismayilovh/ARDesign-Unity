using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a miniature preview of the loaded patent model into a
/// <see cref="RenderTexture"/> and displays the result in the UI as a
/// <see cref="Sprite"/>. Triggered when entering
/// <see cref="StateMachine.States.MODEL_VIEW_STATE"/> and reset on QR scan.
/// Requires a Camera component inside the loaded model's hierarchy.
/// </summary>
public class MiniModelDisplayManager : MonoBehaviour
{
    private Image m_MiniDisplayImage;
    private int m_DisplayWidth;
    private int m_DisplayHeight;

    /// <summary>Layer mask used to isolate the model for the preview camera.</summary>
    public LayerMask ModelImageLayer;

    private Sprite m_DefaultSprite;

    /// <summary>
    /// Subscribes to state-machine events and caches the image component,
    /// default sprite, and display dimensions.
    /// </summary>
    void Start()
    {
        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += () => DisplayMiniObject();

        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE)
            .OnStateEnter += () => ResetDisplay();

        m_MiniDisplayImage = GetComponent<Image>();
        m_DefaultSprite = m_MiniDisplayImage.sprite;

        RectTransform rt = GetComponent<RectTransform>();
        m_DisplayHeight = (int)rt.rect.height;
        m_DisplayWidth = (int)rt.rect.width;
    }

    /// <summary>Restores the default placeholder sprite, clearing any model preview.</summary>
    private void ResetDisplay()
    {
        m_MiniDisplayImage.sprite = m_DefaultSprite;
    }

    /// <summary>
    /// Renders the loaded model's embedded camera into a <see cref="RenderTexture"/>,
    /// reads the pixels into a <see cref="Texture2D"/>, and assigns it as a
    /// <see cref="Sprite"/> to the display image.
    /// </summary>
    private void DisplayMiniObject()
    {
        //TODO: add camera to models that dont have one
        //TODO: better adjust size of camera to perfectly fit model, now it is up to the model's own config.
        int width = Screen.width;
        int height = Screen.height;

        GameObject loadedModel = QRNetworkHandler.Instance.LoadedModel;
        loadedModel.SetLayerRecursively(ModelImageLayer);
        
        Camera renderCamera = loadedModel.GetComponentInChildren<Camera>(true);
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.cullingMask = 1 << ModelImageLayer;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 1.5f;

        RenderTexture cameraRender = new RenderTexture(width, height, 24);
        renderCamera.targetTexture = cameraRender;
        renderCamera.Render();

        RenderTexture.active = cameraRender;
        Texture2D displayTexture = new Texture2D(width,height, TextureFormat.ARGB32, false);
        displayTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        displayTexture.Apply();

        Sprite sprite = Sprite.Create(displayTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        m_MiniDisplayImage.sprite = sprite;
        
    }
}
