using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

/// <summary>
/// MonoBehaviour that automatically generates two LOD levels for its GameObject
/// using UnityMeshSimplifier's <see cref="LODGenerator"/> when requested.
/// LOD 0 keeps full-quality geometry; LOD 1 uses 65 % mesh quality with
/// combined meshes for better performance. The last LOD is culled by default.
/// Added dynamically to downloaded patent models by <see cref="QRNetworkHandler"/>.
/// </summary>
public class GenerateLOD : MonoBehaviour
{
    private LODLevel[] m_LodLevels;
    private bool m_AutoCollectRenderers;
    private SimplificationOptions m_SimplificationOptions;
    private const int LOD_COUNT = 2;
    private const float SIMPLIFIED_LOD_QUALITY = 0.65f;
    private const float SIMPLIFIED_LOD_TRANSITION_HEIGHT = 0.17f;

    [SerializeField, HideInInspector] private bool m_HasGenerated;
    private bool m_IsGenerating;

    /// <summary>
    /// Configures the LOD levels. Generation is started explicitly after model
    /// import so simplification can run asynchronously without blocking Awake.
    /// </summary>
    private void Awake()
    {
        ConfigureGeneration();
    }

    private void ConfigureGeneration()
    {
        m_LodLevels = new LODLevel[LOD_COUNT];
        m_LodLevels[0] = new LODLevel(0.5f, 1.0f)
        {
            CombineMeshes = false,
            CombineSubMeshes = false,
            SkinQuality = SkinQuality.Auto,
            ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            ReceiveShadows = true,
            SkinnedMotionVectors = true,
            LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
            ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes
        };

        // Build the LOD hierarchy and combined source mesh without simplifying
        // here. The costly simplification is performed by GenerateAsync below.
        m_LodLevels[1] = new LODLevel(SIMPLIFIED_LOD_TRANSITION_HEIGHT, 1.0f)
        {
            CombineMeshes = true,
            CombineSubMeshes = false,
            SkinQuality = SkinQuality.Auto,
            ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            ReceiveShadows = true,
            SkinnedMotionVectors = true,
            LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
            ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple
        };
        
        m_AutoCollectRenderers = true;
        m_SimplificationOptions = SimplificationOptions.Default;
    }

    /// <summary>
    /// Reuses precomputed LODGroups when the imported model already has them.
    /// Otherwise creates the LOD hierarchy on the main thread and simplifies
    /// each lower-LOD mesh on a worker thread.
    /// </summary>
    public async Task<bool> GenerateAsync()
    {
        if (m_IsGenerating)
            return false;

        if (m_HasGenerated || HasExistingLodGroups())
        {
            m_HasGenerated = true;
            return true;
        }

        m_IsGenerating = true;
        var generatedMeshes = new HashSet<Mesh>();
        try
        {
            if (m_LodLevels == null || m_SimplificationOptions.MaxIterationCount <= 0)
                ConfigureGeneration();

            // Remember imported meshes so temporary combined meshes can be
            // released after their simplified replacements are assigned.
            var importedMeshes = new HashSet<Mesh>();
            Renderer[] importedRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in importedRenderers)
            {
                Mesh mesh = GetSharedMesh(renderer);
                if (mesh != null)
                    importedMeshes.Add(mesh);
            }

            LODGroup lodGroup = LODGenerator.GenerateLODs(
                gameObject,
                m_LodLevels,
                m_AutoCollectRenderers,
                m_SimplificationOptions);

            if (lodGroup == null)
                return false;

            LOD[] lods = lodGroup.GetLODs();
            if (lods.Length < LOD_COUNT || lods[1].renderers == null)
            {
                m_HasGenerated = true;
                return true;
            }

            foreach (Renderer lowerRenderer in lods[1].renderers)
            {
                Mesh sourceMesh = GetSharedMesh(lowerRenderer);
                if (sourceMesh == null)
                    continue;

                bool sourceMeshIsImported = importedMeshes.Contains(sourceMesh);
                if (!sourceMeshIsImported)
                    generatedMeshes.Add(sourceMesh);

                // MeshSimplifier.Initialize reads Unity Mesh data, so keep it on
                // the main thread. SimplifyMesh operates on the simplifier's
                // managed data; ToMesh creates the Unity Mesh back on main.
                var simplifier = new MeshSimplifier
                {
                    SimplificationOptions = m_SimplificationOptions
                };
                simplifier.Initialize(sourceMesh);

                await Task.Run(() => simplifier.SimplifyMesh(SIMPLIFIED_LOD_QUALITY));

                if (this == null)
                {
                    DestroyGeneratedMeshes(generatedMeshes);
                    return false;
                }

                Mesh simplifiedMesh = simplifier.ToMesh();
                simplifiedMesh.name = sourceMesh.name + "_LOD1";
                generatedMeshes.Add(simplifiedMesh);
                SetSharedMesh(lowerRenderer, simplifiedMesh);

                if (!sourceMeshIsImported)
                {
                    Destroy(sourceMesh);
                    generatedMeshes.Remove(sourceMesh);
                }

                // Let Unity render and process other continuations between meshes.
                await Task.Yield();
            }

            m_HasGenerated = true;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LOD generation failed for {name}: {exception.Message}", this);
            LODGenerator.DestroyLODs(gameObject);
            DestroyGeneratedMeshes(generatedMeshes);
            return false;
        }
        finally
        {
            m_IsGenerating = false;
        }
    }

    private bool HasExistingLodGroups()
    {
        LODGroup[] existingLodGroups = GetComponentsInChildren<LODGroup>(true);
        return existingLodGroups.Length > 0;
    }

    private static Mesh GetSharedMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinnedRenderer)
            return skinnedRenderer.sharedMesh;

        if (renderer is MeshRenderer meshRenderer)
            return meshRenderer.GetComponent<MeshFilter>()?.sharedMesh;

        return null;
    }

    private static void SetSharedMesh(Renderer renderer, Mesh mesh)
    {
        if (renderer is SkinnedMeshRenderer skinnedRenderer)
        {
            skinnedRenderer.sharedMesh = mesh;
            return;
        }

        if (renderer is MeshRenderer meshRenderer)
        {
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null)
                meshFilter.sharedMesh = mesh;
        }
    }

    private static void DestroyGeneratedMeshes(HashSet<Mesh> meshes)
    {
        foreach (Mesh mesh in meshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }

    /// <summary>
    /// Attaches a convex <see cref="MeshCollider"/> to the Level01 mesh child
    /// generated by <see cref="LODGenerator"/>. Currently unused — kept for
    /// future collision support without re-deriving the child path logic.
    /// </summary>
    private void AttachLODCollider()
    {
        //LODGenerator algorithm will instantiate a new child of name _UMS_LODs_ .
        //This will have childs Level00 and Level01 representing LODs.
        // We will place the collider on the last LOD due to performance.


        Transform lastChild = transform.GetChild(transform.childCount - 1);
        Transform lodChild = lastChild.Find("Level01");
        Transform meshChild = lodChild.GetChild(0);

        MeshCollider meshCollider = meshChild.gameObject.AddComponent<MeshCollider>();
        MeshFilter meshFilter = meshChild.gameObject.GetComponent<MeshFilter>();

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
    }
}
