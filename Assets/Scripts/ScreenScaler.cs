using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton utility that calculates the uniform scale factor needed to fit a
/// patent model within a configurable fraction of the device screen.
/// Uses the camera's FOV and the model's world-space bounds from
/// <see cref="PivotSetter.GetSpawnBounds"/>.
/// </summary>
public class ScreenScaler : MonoBehaviour
{

    /// <summary>The single active instance of <see cref="ScreenScaler"/>.</summary>
    public static ScreenScaler instance;

    /// <summary>Optional debug text field for displaying scale info.</summary>
    public TextMeshProUGUI txt;

    private Camera cam;
    private Vector3 originalSize = Vector3.negativeInfinity;
    private Vector3 originalCenter = Vector3.negativeInfinity;
    private bool isScreenFitOn = true;

    [SerializeField] [Range(0.1f, 1f)] private float scaleAmount = 0.6f;
    [SerializeField] private Slider slider;

    /// <summary>Initialises the singleton and caches <see cref="Camera.main"/>.</summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            cam = Camera.main;
        }
        else if(instance != this)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Enables or disables the screen-fit calculation.
    /// When disabled <see cref="FitScreen"/> returns <c>-1</c>.
    /// </summary>
    /// <param name="isOn"><c>true</c> to enable screen fitting.</param>
    public void SetScreenFit(bool isOn)
    {
        isScreenFitOn = isOn;
    }

    // void Update()
    // {
    //     scaleAmount = slider.value;
    // }

    /// <summary>
    /// Calculates the uniform scale multiplier that makes <paramref name="patent"/>
    /// fill <c>scaleAmount</c> of the camera's view frustum at its current distance.
    /// </summary>
    /// <param name="patent">The root GameObject of the patent model to size.</param>
    /// <param name="usePivot">
    /// When <c>true</c>, uses the object's transform position as the reference
    /// center; otherwise uses the bounds center.
    /// </param>
    /// <returns>
    /// The scale multiplier to apply, or <c>-1</c> if the calculation cannot
    /// proceed (null inputs or screen fit disabled).
    /// </returns>
    public float FitScreen(GameObject patent , bool usePivot)
    {
        if (patent == null || cam == null || !isScreenFitOn)
        {
            Debug.Log("bir şeyler null kardeşim");
            return -1f;
        }
        // BoxCollider collider = patent.GetComponentInChildren<BoxCollider>();
        patent.transform.localScale = Vector3.one;
        Bounds bounds = PivotSetter.GetSpawnBounds(patent);
        if (originalCenter == Vector3.negativeInfinity && originalSize == Vector3.negativeInfinity)
        {
            originalCenter = usePivot? patent.transform.position : bounds.center;
            originalSize = bounds.size;
        }
        float projectedDistance = Vector3.Distance(cam.transform.position, patent.transform.position);
        float verticalFOVRadians = cam.fieldOfView * Mathf.Deg2Rad;
        float viewHeightWorld = 2.0f * projectedDistance * Mathf.Tan(verticalFOVRadians * 0.5f);
        float viewWidthWorld = viewHeightWorld * cam.aspect;
        float scaleNeededX = (viewWidthWorld * scaleAmount) / Math.Max(bounds.size.z , bounds.size.x);
        float scaleNeededY = (viewHeightWorld * scaleAmount) / bounds.size.y;
        float smallestScale = Mathf.Min(scaleNeededX, scaleNeededY);
        return smallestScale;
    }


}
