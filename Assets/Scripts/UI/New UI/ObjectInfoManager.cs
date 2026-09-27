using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays periodically refreshed transform and bounds data for the active
/// patent while caching geometry statistics once per spawned model.
/// </summary>
public class ObjectInfoManager : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private UIToggle visibilityToggle;
    [SerializeField] private float refreshInterval = 0.2f;

    private readonly List<MeshEntry> meshEntries = new List<MeshEntry>();
    private readonly StringBuilder textBuilder = new StringBuilder(384);
    private GameObject activeModel;
    private long vertexCount;
    private long triangleCount;
    private float refreshTimer;
    private bool toggleCallbackRegistered;
    private PatentManager subscribedPatentManager;

    /// <summary>Whether the object-information panel is selected for display.</summary>
    public bool IsVisible { get; private set; }

    private struct MeshEntry
    {
        public Renderer Renderer;
        public Bounds LocalBounds;
    }

    private void Awake()
    {
        if (visibilityToggle == null)
            visibilityToggle = GetComponent<UIToggle>();
    }

    private void Start()
    {
        RegisterCallbacks();
        BindActiveModel();
        visibilityToggle?.SetValueWithoutNotify(IsVisible);
        ApplyPanelVisibility();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
        BindActiveModel();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void Update()
    {
        if (!IsVisible) return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < Mathf.Max(0.05f, refreshInterval)) return;

        refreshTimer = 0f;
        RefreshText();
    }

    /// <summary>Shows or hides the information panel without disabling this manager.</summary>
    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        visibilityToggle?.SetValueWithoutNotify(visible);
        ApplyPanelVisibility();

        if (visible)
        {
            refreshTimer = 0f;
            RefreshText();
        }
    }

    private void RegisterCallbacks()
    {
        if (!toggleCallbackRegistered && visibilityToggle != null)
        {
            visibilityToggle.OnToggleValueChanged += SetVisible;
            toggleCallbackRegistered = true;
        }

        PatentManager patentManager = PatentManager.Instance;
        if (subscribedPatentManager == patentManager) return;

        if (subscribedPatentManager != null)
        {
            subscribedPatentManager.patentSpawnedEvent -= BindActiveModel;
            subscribedPatentManager.patentDeletedEvent -= HandleModelDeleted;
        }

        subscribedPatentManager = patentManager;
        if (subscribedPatentManager != null)
        {
            subscribedPatentManager.patentSpawnedEvent += BindActiveModel;
            subscribedPatentManager.patentDeletedEvent += HandleModelDeleted;
        }
    }

    private void UnregisterCallbacks()
    {
        if (toggleCallbackRegistered && visibilityToggle != null)
            visibilityToggle.OnToggleValueChanged -= SetVisible;
        toggleCallbackRegistered = false;

        if (subscribedPatentManager != null)
        {
            subscribedPatentManager.patentSpawnedEvent -= BindActiveModel;
            subscribedPatentManager.patentDeletedEvent -= HandleModelDeleted;
        }

        subscribedPatentManager = null;
    }

    private void ApplyPanelVisibility()
    {
        if (panelRoot != null)
            panelRoot.SetActive(IsVisible);
    }

    private void BindActiveModel()
    {
        GameObject model = PatentManager.Instance?.ActivePatent;
        if (model == activeModel) return;

        activeModel = model;
        CacheGeometry();

        if (IsVisible)
            RefreshText();
    }

    private void HandleModelDeleted()
    {
        activeModel = null;
        meshEntries.Clear();
        vertexCount = 0L;
        triangleCount = 0L;

        if (IsVisible)
            RefreshText();
    }

    private void CacheGeometry()
    {
        meshEntries.Clear();
        vertexCount = 0L;
        triangleCount = 0L;

        if (activeModel == null) return;

        HashSet<Renderer> lodManagedRenderers = new HashSet<Renderer>();
        HashSet<Renderer> lod0Renderers = new HashSet<Renderer>();
        LODGroup[] lodGroups = activeModel.GetComponentsInChildren<LODGroup>(true);

        foreach (LODGroup lodGroup in lodGroups)
        {
            LOD[] lods = lodGroup.GetLODs();
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                Renderer[] renderers = lods[lodIndex].renderers;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;

                    lodManagedRenderers.Add(renderer);
                    if (lodIndex == 0)
                        lod0Renderers.Add(renderer);
                }
            }
        }

        Renderer[] modelRenderers = activeModel.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in modelRenderers)
        {
            if (renderer == null) continue;
            if (lodManagedRenderers.Contains(renderer) && !lod0Renderers.Contains(renderer)) continue;
            if (!renderer.enabled && !lod0Renderers.Contains(renderer)) continue;

            Mesh mesh = null;
            Bounds localBounds = default;

            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                    mesh = meshFilter.sharedMesh;

                if (mesh != null)
                    localBounds = mesh.bounds;
            }
            else
            {
                SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
                if (skinnedRenderer != null)
                {
                    mesh = skinnedRenderer.sharedMesh;
                    localBounds = skinnedRenderer.localBounds;
                }
            }

            if (mesh == null) continue;

            meshEntries.Add(new MeshEntry
            {
                Renderer = renderer,
                LocalBounds = localBounds
            });
            vertexCount += mesh.vertexCount;
            triangleCount += CountTriangles(mesh);
        }
    }

    private void RefreshText()
    {
        if (infoText == null) return;

        if (activeModel == null)
        {
            infoText.text = "No model loaded";
            return;
        }

        Transform modelTransform = activeModel.transform;
        Vector3 position = modelTransform.position;
        Vector3 rotation = modelTransform.localEulerAngles;
        Vector3 scale = modelTransform.localScale;
        Vector3 size = ComputeWorldBounds().size;
        PivotSetter pivotSetter = activeModel.GetComponent<PivotSetter>();
        string pivotMode = pivotSetter != null && pivotSetter.UsesCalculatedPivot
            ? "Calculated"
            : "Original";

        textBuilder.Clear();
        textBuilder.Append("Model: ").Append(activeModel.name).AppendLine();
        textBuilder.AppendFormat(CultureInfo.InvariantCulture,
            "Position: X {0:0.00}  Y {1:0.00}  Z {2:0.00}\n", position.x, position.y, position.z);
        textBuilder.AppendFormat(CultureInfo.InvariantCulture,
            "Rotation: X {0:0.0}°  Y {1:0.0}°  Z {2:0.0}°\n", rotation.x, rotation.y, rotation.z);
        textBuilder.AppendFormat(CultureInfo.InvariantCulture,
            "Scale: X {0:0.00}  Y {1:0.00}  Z {2:0.00}\n", scale.x, scale.y, scale.z);
        textBuilder.AppendFormat(CultureInfo.InvariantCulture,
            "Size: X {0:0.00}  Y {1:0.00}  Z {2:0.00}\n", size.x, size.y, size.z);
        textBuilder.Append("Meshes: ").Append(meshEntries.Count.ToString("N0", CultureInfo.InvariantCulture)).AppendLine();
        textBuilder.Append("Vertices: ").Append(vertexCount.ToString("N0", CultureInfo.InvariantCulture)).AppendLine();
        textBuilder.Append("Triangles (LOD0): ").Append(triangleCount.ToString("N0", CultureInfo.InvariantCulture)).AppendLine();
        textBuilder.Append("Pivot: ").Append(pivotMode);

        infoText.text = textBuilder.ToString();
    }

    private Bounds ComputeWorldBounds()
    {
        bool initialized = false;
        Bounds result = new Bounds(activeModel != null ? activeModel.transform.position : Vector3.zero, Vector3.zero);

        foreach (MeshEntry entry in meshEntries)
        {
            if (entry.Renderer == null) continue;

            Matrix4x4 localToWorld = entry.Renderer.transform.localToWorldMatrix;
            Bounds localBounds = entry.LocalBounds;

            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? localBounds.min.x : localBounds.max.x,
                    y == 0 ? localBounds.min.y : localBounds.max.y,
                    z == 0 ? localBounds.min.z : localBounds.max.z);
                Vector3 worldCorner = localToWorld.MultiplyPoint3x4(corner);

                if (!initialized)
                {
                    result = new Bounds(worldCorner, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(worldCorner);
                }
            }
        }

        return result;
    }

    private static long CountTriangles(Mesh mesh)
    {
        long count = 0L;
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                count += (long)mesh.GetIndexCount(subMesh) / 3L;
        }

        return count;
    }
}
