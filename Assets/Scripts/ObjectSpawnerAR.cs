using System;
using System.Collections.Generic;
using UnityEngine;
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
/// downloaded patent model into the AR scene, either at a detected plane pose
/// or at a fixed camera-relative position when no plane is available.
/// Raises <see cref="objectSpawnedEvent"/> after placement.
/// </summary>
public class ObjectSpawnerAR : MonoBehaviour
{
    [SerializeField] private PlacementMode _placementMode = PlacementMode.Front;
    [SerializeField] private GameObject patent;
    [SerializeField] private float centerPlacementY = 1.1f;
    [SerializeField] private float frontPlacementZ = 2f;
    [SerializeField] private TextMeshProUGUI scaleText;
    [SerializeField] private TextMeshProUGUI positionText;

    /// <summary>
    /// Fired after a patent is spawned. The <c>bool</c> parameter is
    /// <c>true</c> when placed on a detected AR plane, <c>false</c> otherwise.
    /// </summary>
    public static event Action<bool> objectSpawnedEvent;

    private GameObject activePatent;

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
    /// Handles a tap event: ignores it if no patent is loaded or one is already
    /// placed, then delegates to <see cref="spawnWithNoRaycast"/> or
    /// <see cref="spawnWithRaycast"/> depending on whether plane hits were detected.
    /// </summary>
    /// <param name="hits">AR plane raycast hits, or <c>null</c> when plane detection is off.</param>
    void SpawnObject(List<ARRaycastHit> hits)
    {
        Debug.Log("trying to spawn");
        if (PatentManager.Instance.Patent == null || PatentManager.Instance.ActivePatent != null) return;
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
    /// Instantiates the patent at a camera-relative position (center offset or
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

        SpawnPatent(position, PatentManager.Instance.Patent.transform.rotation, false);
    }

    /// <summary>
    /// Instantiates the patent at the pose returned by the AR plane raycast,
    /// aligning it to the detected surface.
    /// </summary>
    /// <param name="hitPose">The world-space pose of the first AR plane hit.</param>
    void spawnWithRaycast(Pose hitPose)
    {
        SpawnPatent(hitPose.position, PatentManager.Instance.Patent.transform.rotation, true);
    }

    private void SpawnPatent(Vector3 position, Quaternion rotation, bool toGround)
    {
        if (PatentManager.Instance.ActivePatent != null) return;

        GameObject spawnedContent = Instantiate(PatentManager.Instance.Patent, position, rotation);
        GameObject modelRoot = PivotSetter.CreateModelRoot(spawnedContent);
        if (modelRoot == null) return;

        PivotSetter pivotSetter = modelRoot.GetComponent<PivotSetter>();
        pivotSetter.SetPlacementOnGround(toGround);

        PatentManager.Instance.ActivePatent = modelRoot;
        modelRoot.SetActive(true);
        PatentManager.Instance.InvokePatentSpawned();
        objectSpawnedEvent?.Invoke(toGround);
        PatentManager.Instance.SetInitialTransform();
    }

    // private void FitScreen()
    // {
    //     if (!spawnWithinScreen) return;
    //     float scaleAmount = ScreenScaler.instance.FitScreen(activePatent);
    //     activePatent.transform.localScale *= scaleAmount;
    //     scaleText.text ="Scale:          " + activePatent.transform.localScale.y.ToString()  ;
    //     Debug.Log(scaleAmount);
    // }

    /// <summary>
    /// Assigns the given downloaded model as the patent prefab to be instantiated on next tap.
    /// </summary>
    /// <param name="downloaded_patent">The downloaded model GameObject.</param>
    public void SetActivePatent(GameObject downloaded_patent)
    {
        patent = downloaded_patent;
        Debug.Log("settedddd");
    }

    /// <summary>
    /// Returns the currently instantiated patent instance in the AR scene.
    /// </summary>
    /// <returns>The active patent <see cref="GameObject"/>, or <c>null</c> if none.</returns>
    public GameObject getActivePatent()
    {
        return activePatent;
    }

    /// <summary>
    /// Resets the patent's scale to <see cref="Vector3.one"/> and repositions it
    /// using <see cref="PivotSetter.ChangeToOneOneMode"/>.
    /// </summary>
    public void ResetPatentTransform()
    {
        if (patent == null) return;
        Camera cam = Camera.main;
        patent.transform.localScale = Vector3.one;
        activePatent.transform.localScale = Vector3.one;
        Vector3 offSet = PivotSetter.ChangeToOneOneMode(activePatent,cam.transform,1f);
    }
    
    
    
    
}
