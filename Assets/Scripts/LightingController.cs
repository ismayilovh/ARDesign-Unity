using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Toggles AR lighting estimation and URP main-light rendering in response to
/// <see cref="PopupMenuManager.OnEnableLightning"/>. Uses reflection to set
/// the private <c>m_MainLightRenderingMode</c> field on the URP asset because
/// no public API is available in URP 17.
/// </summary>
public class LightingController : MonoBehaviour
{
    [SerializeField] private PopupMenuManager popupMenuManager;
    [SerializeField] private ARCameraManager arCameraManager;

    private LightEstimation _savedLightEstimation;
    private CameraFacingDirection _savedFacingDirection;
    private AmbientMode _savedAmbientMode;
    private Color _savedAmbientLight;

    /// <summary>
    /// Subscribes to the lighting toggle event, snapshots the current URP and
    /// ambient lighting settings for later restoration, and disables lighting by default.
    /// </summary>
    private void Start()
    {
        if (popupMenuManager != null)
            popupMenuManager.OnEnableLightning += SetLighting;
        else
            Debug.LogError("Pop up manager is null");

        if (arCameraManager != null)
        {
            _savedLightEstimation = arCameraManager.requestedLightEstimation;
            _savedFacingDirection = arCameraManager.requestedFacingDirection;
        }

        _savedAmbientMode = RenderSettings.ambientMode;
        _savedAmbientLight = RenderSettings.ambientLight;

        SetLighting(false);
    }

    /// <summary>Unsubscribes from the lighting toggle event.</summary>
    private void OnDestroy()
    {
        if (popupMenuManager != null)
            popupMenuManager.OnEnableLightning -= SetLighting;
    }

    /// <summary>
    /// Enables or disables per-pixel main-light rendering on the URP asset via
    /// reflection, and toggles AR camera light estimation and ambient lighting.
    /// </summary>
    /// <param name="enable"><c>true</c> to enable full lighting; <c>false</c> to disable it.</param>
    private void SetLighting(bool enable)
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogError("Current render pipeline is not URP.");
            return;
        }

        var field = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightRenderingMode",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogError("Could not find m_MainLightRenderingMode field via reflection.");
            return;
        }

        field.SetValue(urpAsset, enable ? LightRenderingMode.PerPixel : LightRenderingMode.Disabled);

        RenderSettings.ambientMode = enable ? _savedAmbientMode : AmbientMode.Flat;
        RenderSettings.ambientLight = enable ? _savedAmbientLight : Color.white;

        if (arCameraManager != null)
        {
            arCameraManager.requestedLightEstimation = enable ? _savedLightEstimation : LightEstimation.None;
            arCameraManager.requestedFacingDirection = _savedFacingDirection;
        }
    }
}
