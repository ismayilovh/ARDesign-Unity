using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Singleton registry for the loaded design model. Holds references to the
/// prefab (<see cref="Design"/>) and the currently instantiated scene object
/// (<see cref="ActiveDesign"/>), as well as the transform snapshot taken at
/// spawn time for use by <see cref="DesignTransformer.ResetRotation"/>.
/// </summary>
public class DesignManager : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="DesignManager"/>.</summary>
    public static DesignManager Instance;

    [SerializeField, FormerlySerializedAs("patent")] private GameObject design;
    private GameObject activeDesign;

    /// <summary>Fired when the active design is deleted.</summary>
    public event Action designDeletedEvent;

    /// <summary>Fired when a new design is spawned into the AR scene.</summary>
    public event Action designSpawnedEvent;

    /// <summary>Local scale of the design at the moment it was spawned.</summary>
    public Vector3 initialScale { get; private set; }

    /// <summary>World rotation of the design at the moment it was spawned.</summary>
    public Quaternion initialRotation { get; private set; }

    /// <summary>World position of the design at the moment it was spawned.</summary>
    public Vector3 initialPosition { get; private set; }

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Registers <see cref="OnScanEnter"/> on both scan states so any existing
    /// design is cleaned up whenever the user starts a new QR scan.
    /// </summary>
    private void Start()
    {
        StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE).OnStateEnter += OnScanEnter;
        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE).OnStateEnter += OnScanEnter;
    }

    private void OnDestroy()
    {
        if (StateMachine.Instance == null) return;

        StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE).OnStateEnter -= OnScanEnter;
        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE).OnStateEnter -= OnScanEnter;
    }

    /// <summary>
    /// The instantiated design GameObject currently in the AR scene.
    /// Setting this to <c>null</c> clears the active reference without destroying the object.
    /// </summary>
    public GameObject ActiveDesign
    {
        set => activeDesign = value;
        get => activeDesign;
    }

    /// <summary>
    /// The prefab or downloaded model assigned as the source for instantiation.
    /// </summary>
    public GameObject Design
    {
        set => design = value;
        get => design;
    }

    /// <summary>Destroys the active design instance and raises <see cref="designDeletedEvent"/>.</summary>
    public void DeleteActiveDesign()
    {
        if (activeDesign == null) return;

        GameObject designToDestroy = activeDesign;
        activeDesign = null;
        Destroy(designToDestroy);
        designDeletedEvent?.Invoke();
    }

    /// <summary>
    /// Called on scan-state entry to clean up any existing design so a new
    /// QR scan can begin.
    /// </summary>
    public void OnScanEnter()
    {
        DeleteActiveDesign();

        if (design != null)
        {
            DestroyGeneratedLodMeshes(design);
            Destroy(design);
            design = null;
        }
    }

    /// <summary>
    /// Releases meshes created by UnityMeshSimplifier for lower LOD levels.
    /// The source model and its spawned clone share these runtime mesh assets,
    /// so they are released once when both objects are retired for a new scan.
    /// </summary>
    private static void DestroyGeneratedLodMeshes(GameObject source)
    {
        var meshesToDestroy = new HashSet<Mesh>();
        LODGroup[] lodGroups = source.GetComponentsInChildren<LODGroup>(true);

        foreach (LODGroup lodGroup in lodGroups)
        {
            LOD[] lods = lodGroup.GetLODs();
            for (int lodIndex = 1; lodIndex < lods.Length; lodIndex++)
            {
                foreach (Renderer renderer in lods[lodIndex].renderers)
                {
                    Mesh mesh = renderer switch
                    {
                        SkinnedMeshRenderer skinnedRenderer => skinnedRenderer.sharedMesh,
                        MeshRenderer meshRenderer => meshRenderer.GetComponent<MeshFilter>()?.sharedMesh,
                        _ => null
                    };

                    if (mesh != null && meshesToDestroy.Add(mesh))
                        Destroy(mesh);
                }
            }
        }
    }

    /// <summary>
    /// Captures the active design's current transform as the initial snapshot
    /// used by <see cref="DesignTransformer.ResetRotation"/>.
    /// </summary>
    public void SetInitialTransform()
    {
        if (activeDesign == null) return;

        initialRotation = activeDesign.transform.rotation;
        initialScale = activeDesign.transform.localScale;
        initialPosition = activeDesign.transform.position;
    }

    /// <summary>Raises <see cref="designSpawnedEvent"/>.</summary>
    public void InvokeDesignSpawned()
    {
        designSpawnedEvent?.Invoke();
    }
}
