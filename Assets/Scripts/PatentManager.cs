using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton registry for the loaded patent model. Holds references to the
/// prefab (<see cref="Patent"/>) and the currently instantiated scene object
/// (<see cref="ActivePatent"/>), as well as the transform snapshot taken at
/// spawn time for use by <see cref="PatentTransformer.ResetRotation"/>.
/// </summary>
public class PatentManager : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="PatentManager"/>.</summary>
    public static PatentManager Instance;

    [SerializeField] private GameObject patent;
    private GameObject activePatent;

    /// <summary>Fired when the active patent is deleted.</summary>
    public event Action patentDeletedEvent;

    /// <summary>Fired when a new patent is spawned into the AR scene.</summary>
    public event Action patentSpawnedEvent;

    /// <summary>Local scale of the patent at the moment it was spawned.</summary>
    public Vector3 initialScale { get; private set; }

    /// <summary>World rotation of the patent at the moment it was spawned.</summary>
    public Quaternion initialRotation { get; private set; }

    /// <summary>World position of the patent at the moment it was spawned.</summary>
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
    /// patent is cleaned up whenever the user starts a new QR scan.
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
    /// The instantiated patent GameObject currently in the AR scene.
    /// Setting this to <c>null</c> clears the active reference without destroying the object.
    /// </summary>
    public GameObject ActivePatent
    {
        set => activePatent = value;
        get => activePatent;
    }

    /// <summary>
    /// The prefab or downloaded model assigned as the source for instantiation.
    /// </summary>
    public GameObject Patent
    {
        set => patent = value;
        get => patent;
    }

    /// <summary>Destroys the active patent instance and raises <see cref="patentDeletedEvent"/>.</summary>
    public void DeleteActivePatent()
    {
        if (activePatent == null) return;

        GameObject patentToDestroy = activePatent;
        activePatent = null;
        Destroy(patentToDestroy);
        patentDeletedEvent?.Invoke();
    }

    /// <summary>
    /// Called on scan-state entry to clean up any existing patent so a new
    /// QR scan can begin.
    /// </summary>
    public void OnScanEnter()
    {
        DeleteActivePatent();

        if (patent != null)
        {
            DestroyGeneratedLodMeshes(patent);
            Destroy(patent);
            patent = null;
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
    /// Captures the active patent's current transform as the initial snapshot
    /// used by <see cref="PatentTransformer.ResetRotation"/>.
    /// </summary>
    public void SetInitialTransform()
    {
        if (activePatent == null) return;

        initialRotation = activePatent.transform.rotation;
        initialScale = activePatent.transform.localScale;
        initialPosition = activePatent.transform.position;
    }

    /// <summary>Raises <see cref="patentSpawnedEvent"/>.</summary>
    public void InvokePatentSpawned()
    {
        patentSpawnedEvent?.Invoke();
    }
}
