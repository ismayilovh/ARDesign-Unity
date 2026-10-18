using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using TMPro;

/// <summary>
/// Internal enum controlling where the model is placed when no AR plane is hit.
/// </summary>
enum PlacementMode
{
    /// <summary>Place the model in front of the camera at a fixed Z distance.</summary>
    Front,

    /// <summary>Place the model at the camera's position offset downward.</summary>
    Center,
}

/// <summary>
/// Listens for <see cref="RaycastHandler.clickEvent"/> and instantiates the
/// downloaded design model into the AR scene, either at a detected plane pose
/// or at a fixed camera-relative position when no plane is available.
/// Raises <see cref="objectSpawnedEvent"/> after placement.
/// </summary>
public class ObjectSpawnerAR : MonoBehaviour
{
    [SerializeField] private PlacementMode _placementMode = PlacementMode.Front;
    [SerializeField, FormerlySerializedAs("patent")] private GameObject design;
    [SerializeField] private float centerPlacementY = 1.1f;
    [SerializeField] private float frontPlacementZ = 2f;
    [SerializeField] private TextMeshProUGUI scaleText;
    [SerializeField] private TextMeshProUGUI positionText;

    /// <summary>
    /// Fired after a design is spawned. The <c>bool</c> parameter is
    /// <c>true</c> when placed on a detected AR plane, <c>false</c> otherwise.
    /// </summary>
    public static event Action<bool> objectSpawnedEvent;

    private GameObject activeDesign;

    /// <summary>When <c>true</c>, applies screen-fit scaling after spawning.</summary>
    public bool spawnWithinScreen = true;



  
    // private void OnEnable()
    // {
    //     RaycastHandler.clickEvent += SpawnObject;
    //     
    // }
    //
    // private void OnDisable()
    // {
    //     RaycastHandler.clickEvent -= SpawnObject;
    // }

    /// <summary>Subscribes to <see cref="RaycastHandler.clickEvent"/> to receive tap notifications.</summary>
    private void Start()
    {
        RaycastHandler.clickEvent += SpawnObject;
    }

    /// <summary>
    /// Handles a tap event: ignores it if no design is loaded or one is already
    /// placed, then delegates to <see cref="spawnWithNoRaycast"/> or
    /// <see cref="spawnWithRaycast"/> depending on whether plane hits were detected.
    /// </summary>
    /// <param name="hits">AR plane raycast hits, or <c>null</c> when plane detection is off.</param>
    void SpawnObject(List<ARRaycastHit> hits)
    {
        Debug.Log("trying to spawn");
        if (DesignManager.Instance.Design == null || DesignManager.Instance.ActiveDesign != null) return;
        if (hits == null)
        {
            spawnWithNoRaycast();
        }
        else
        {
            Pose hitPose = hits[0].pose;
            spawnWithRaycast(hitPose);
        }

    }

    /// <summary>
    /// Instantiates the design at a camera-relative position (center offset or
    /// directly in front) when no AR plane was detected.
    /// </summary>
    void spawnWithNoRaycast()
    {
        Vector3 position = Vector3.zero;
        Camera cam = Camera.main;
        switch (_placementMode)
        {
            case PlacementMode.Center:
                position = cam.transform.position;
                position.y -= centerPlacementY;
                break;
            case PlacementMode.Front:
                position = cam.transform.position + cam.transform.forward * frontPlacementZ;
                break;
        }

        SpawnDesign(position, DesignManager.Instance.Design.transform.rotation, false);
    }

    /// <summary>
    /// Instantiates the design at the pose returned by the AR plane raycast,
    /// aligning it to the detected surface.
    /// </summary>
    /// <param name="hitPose">The world-space pose of the first AR plane hit.</param>
    void spawnWithRaycast(Pose hitPose)
    {
        SpawnDesign(hitPose.position, DesignManager.Instance.Design.transform.rotation, true);
    }

    private void SpawnDesign(Vector3 position, Quaternion rotation, bool toGround)
    {
        if (DesignManager.Instance.ActiveDesign != null) return;

        GameObject spawnedContent = Instantiate(DesignManager.Instance.Design, position, rotation);
        GameObject modelRoot = PivotSetter.CreateModelRoot(spawnedContent);
        if (modelRoot == null) return;

        PivotSetter pivotSetter = modelRoot.GetComponent<PivotSetter>();
        pivotSetter.SetPlacementOnGround(toGround);

        DesignManager.Instance.ActiveDesign = modelRoot;
        modelRoot.SetActive(true);
        DesignManager.Instance.InvokeDesignSpawned();
        objectSpawnedEvent?.Invoke(toGround);
        DesignManager.Instance.SetInitialTransform();
    }

    // private void FitScreen()
    // {
    //     if (!spawnWithinScreen) return;
    //     float scaleAmount = ScreenScaler.instance.FitScreen(activeDesign);
    //     activeDesign.transform.localScale *= scaleAmount;
    //     scaleText.text ="Scale:          " + activeDesign.transform.localScale.y.ToString()  ;
    //     Debug.Log(scaleAmount);
    // }

    /// <summary>
    /// Assigns the given downloaded model as the design prefab to be instantiated on next tap.
    /// </summary>
    /// <param name="downloaded_design">The downloaded model GameObject.</param>
    public void SetActiveDesign(GameObject downloaded_design)
    {
        design = downloaded_design;
        Debug.Log("settedddd");
    }

    /// <summary>
    /// Returns the currently instantiated design instance in the AR scene.
    /// </summary>
    /// <returns>The active design <see cref="GameObject"/>, or <c>null</c> if none.</returns>
    public GameObject getActiveDesign()
    {
        return activeDesign;
    }

    /// <summary>
    /// Resets the design's scale to <see cref="Vector3.one"/> and repositions it
    /// using <see cref="PivotSetter.ChangeToOneOneMode"/>.
    /// </summary>
    public void ResetDesignTransform()
    {
        if (design == null) return;
        Camera cam = Camera.main;
        design.transform.localScale = Vector3.one;
        activeDesign.transform.localScale = Vector3.one;
        Vector3 offSet = PivotSetter.ChangeToOneOneMode(activeDesign,cam.transform,1f);
    }
    
    
    
    
}
