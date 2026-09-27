using UnityEngine;

/// <summary>
/// Owns the reversible pivot offset for a spawned patent and also provides the
/// bounds and positioning helpers used when placing or scaling a patent in AR.
/// </summary>
public class PivotSetter : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private Vector3 calculatedLocalPosition;
    private bool hasOriginalState;
    private bool hasCalculatedState;
    private bool placeOnGround;

    /// <summary>Whether the active instance currently uses the calculated bounds pivot.</summary>
    public bool UsesCalculatedPivot { get; private set; }

    /// <summary>
    /// Increments whenever the content's local pivot transform is applied so
    /// bounds consumers can invalidate cached hierarchy calculations cheaply.
    /// </summary>
    public int BoundsVersion { get; private set; }

    /// <summary>
    /// Creates an unchanged transform root around the imported instance so pivot
    /// changes can be applied to the content without moving the root manipulated
    /// by <see cref="PatentTransformer"/>.
    /// </summary>
    public static GameObject CreateModelRoot(GameObject spawnedContent)
    {
        if (spawnedContent == null) return null;

        Transform sourceTransform = spawnedContent.transform;
        string modelName = spawnedContent.name;
        if (modelName.EndsWith("(Clone)"))
            modelName = modelName.Substring(0, modelName.Length - "(Clone)".Length).TrimEnd();

        GameObject root = new GameObject(modelName);
        root.SetActive(false);
        root.layer = spawnedContent.layer;
        root.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        root.transform.localScale = sourceTransform.localScale;

        sourceTransform.SetParent(root.transform, true);
        spawnedContent.name = modelName + " Content";
        spawnedContent.SetActive(true);

        PivotSetter pivotSetter = root.AddComponent<PivotSetter>();
        pivotSetter.Initialize(sourceTransform);
        return root;
    }

    /// <summary>Records whether the calculated pivot should use bottom-center or center.</summary>
    public void SetPlacementOnGround(bool toGround)
    {
        if (placeOnGround == toGround && hasCalculatedState) return;

        placeOnGround = toGround;
        hasCalculatedState = false;

        if (UsesCalculatedPivot)
            ApplyCalculatedPivot();
    }

    /// <summary>
    /// Switches between the exact imported content transform and the calculated
    /// bounds pivot. Assigning exact cached values makes repeated calls drift-free.
    /// </summary>
    public void SetUseCalculatedPivot(bool useCalculated)
    {
        UsesCalculatedPivot = useCalculated;

        if (contentRoot == null) return;

        CacheOriginalPivotState();
        if (useCalculated)
            ApplyCalculatedPivot();
        else
            ApplyOriginalPivot();
    }

    private void Initialize(Transform content)
    {
        contentRoot = content;
        CacheOriginalPivotState();
        ApplyOriginalPivot();
    }

    private void CacheOriginalPivotState()
    {
        if (hasOriginalState || contentRoot == null) return;

        originalLocalPosition = contentRoot.localPosition;
        originalLocalRotation = contentRoot.localRotation;
        originalLocalScale = contentRoot.localScale;
        hasOriginalState = true;
    }

    private void CalculatePivotIfNeeded()
    {
        if (hasCalculatedState || !hasOriginalState || contentRoot == null) return;

        ApplyOriginalPivot();
        Physics.SyncTransforms();

        Bounds bounds = GetBounds(gameObject);
        Vector3 calculatedPivot = new Vector3(
            bounds.center.x,
            placeOnGround ? bounds.min.y : bounds.center.y,
            bounds.center.z);

        calculatedLocalPosition = originalLocalPosition - calculatedPivot;
        hasCalculatedState = true;
    }

    private void ApplyOriginalPivot()
    {
        if (!hasOriginalState || contentRoot == null) return;

        contentRoot.localPosition = originalLocalPosition;
        contentRoot.localRotation = originalLocalRotation;
        contentRoot.localScale = originalLocalScale;
        BoundsVersion++;
    }

    private void ApplyCalculatedPivot()
    {
        CalculatePivotIfNeeded();
        if (!hasCalculatedState || contentRoot == null) return;

        contentRoot.localPosition = calculatedLocalPosition;
        contentRoot.localRotation = originalLocalRotation;
        contentRoot.localScale = originalLocalScale;
        BoundsVersion++;
    }

    /// <summary>
    /// Repositions <paramref name="patent"/> so its bounding box no longer
    /// overlaps the camera position in the XZ plane, pushing it along the
    /// camera's forward direction when the camera is inside the model bounds.
    /// Used when switching to 1:1 real-world scale mode.
    /// </summary>
    /// <param name="patent">The root GameObject of the patent model.</param>
    /// <param name="camTransform">The AR camera transform.</param>
    /// <param name="offSet">Extra clearance distance in metres beyond the model edge.</param>
    /// <returns>
    /// The calculated offset vector (currently always <see cref="Vector3.zero"/>),
    /// or <see cref="Vector3.positiveInfinity"/> if <paramref name="patent"/> is null.
    /// </returns>
    public static Vector3 ChangeToOneOneMode(GameObject patent ,Transform camTransform, float offSet)
    {
        if (patent == null)
        {
            Debug.Log("Patent is null");
            return Vector3.positiveInfinity;
        }

        Physics.SyncTransforms();
            
        Vector3 calculatedOffSet = Vector3.zero;
        patent.transform.localScale = Vector3.one;
        Bounds bounds = GetSpawnBounds(patent);
        
        Vector3 mins = bounds.min;
        Vector3 maxs = bounds.max;
        Vector3 center = bounds.center;
        
        float minX = mins.x;
        float maxX = maxs.x;
        float minZ = mins.z;
        float maxZ = maxs.z;

        float camX = camTransform.position.x;
        float camZ = camTransform.position.z;
        
        
        bool isInsideX = camX >= minX && camX <= maxX;
        bool isInsideZ = camZ >= minZ && camZ <= maxZ;

        if (isInsideX && isInsideZ)
        {

            Vector2 boundsCenterXZ = new Vector2(center.x, center.z);
            Vector2 cameraXZ = new Vector2(camX, camZ);
            
           // Vector2 pushDir = (boundsCenterXZ - cameraXZ).normalized;
           Vector2 pushDir = new Vector2(camTransform.forward.x, camTransform.forward.z).normalized;
            if (pushDir == Vector2.zero) pushDir = new Vector2(camTransform.forward.x, camTransform.forward.z).normalized;
            float distToEdgeX = (Mathf.Abs(pushDir.x) > 0.001f) 
                ? bounds.extents.x / Mathf.Abs(pushDir.x) 
                : float.MaxValue;

            
            float distToEdgeZ = (Mathf.Abs(pushDir.y) > 0.001f) 
                ? bounds.extents.z / Mathf.Abs(pushDir.y) 
                : float.MaxValue;
            
            float exactDistanceToEdge = Mathf.Min(distToEdgeX, distToEdgeZ);
            float targetDistance = exactDistanceToEdge + offSet;
            
            Vector2 newCenterXZ = cameraXZ + (pushDir * targetDistance);
            Vector2 movementDelta = newCenterXZ - boundsCenterXZ;
            
            patent.transform.position += new Vector3(movementDelta.x, 0, movementDelta.y);
        }
        
        return calculatedOffSet;
    }

    /// <summary>
    /// Computes the world-space axis-aligned bounding box that encapsulates all
    /// <see cref="MeshRenderer"/> components in <paramref name="patent"/>'s hierarchy.
    /// </summary>
    /// <param name="patent">The root GameObject whose renderers are measured.</param>
    /// <returns>
    /// The encapsulating <see cref="Bounds"/> in world space, or a zero-size
    /// bounds at the object's position if no renderers are found.
    /// </returns>
    public static Bounds GetSpawnBounds(GameObject patent)
    {
        Renderer[] renderers = patent.GetComponentsInChildren<Renderer>();
        bool initialized = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return initialized ? bounds : new Bounds(patent.transform.position, Vector3.zero);
    }

    /// <summary>
    /// Computes the local-space axis-aligned bounding box of <paramref name="patent"/>
    /// by transforming all mesh-corner vertices into the root object's local space.
    /// More accurate than <see cref="GetSpawnBounds"/> for rotated hierarchies.
    /// </summary>
    /// <param name="patent">The root GameObject whose mesh filters are measured.</param>
    /// <returns>The local-space <see cref="Bounds"/>, or a small default if no meshes exist.</returns>
    public static Bounds GetBounds(GameObject patent)
    {
        Matrix4x4 worldToRoot = patent.transform.worldToLocalMatrix;
        bool initialized = false;
        Bounds result = default;

        MeshFilter[] filters = patent.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in filters)
        {
            if (meshFilter.sharedMesh == null) continue;

            Matrix4x4 meshToRoot = worldToRoot * meshFilter.transform.localToWorldMatrix;
            EncapsulateBounds(meshFilter.sharedMesh.bounds, meshToRoot, ref result, ref initialized);
        }

        SkinnedMeshRenderer[] skinnedRenderers = patent.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            if (skinnedRenderer.sharedMesh == null) continue;

            Matrix4x4 meshToRoot = worldToRoot * skinnedRenderer.transform.localToWorldMatrix;
            EncapsulateBounds(skinnedRenderer.localBounds, meshToRoot, ref result, ref initialized);
        }

        return initialized ? result : new Bounds(Vector3.zero, Vector3.one * 0.1f);
    }

    private static void EncapsulateBounds(
        Bounds sourceBounds,
        Matrix4x4 sourceToRoot,
        ref Bounds result,
        ref bool initialized)
    {
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 corner = new Vector3(
                x == 0 ? sourceBounds.min.x : sourceBounds.max.x,
                y == 0 ? sourceBounds.min.y : sourceBounds.max.y,
                z == 0 ? sourceBounds.min.z : sourceBounds.max.z);

            Vector3 rootCorner = sourceToRoot.MultiplyPoint3x4(corner);
            if (!initialized)
            {
                result = new Bounds(rootCorner, Vector3.zero);
                initialized = true;
            }
            else
            {
                result.Encapsulate(rootCorner);
            }
        }
    }

    /// <summary>
    /// Adjusts the Y position of <paramref name="patent"/> so its bottom face sits
    /// on the detected AR plane (when <paramref name="toGround"/> is <c>true</c>)
    /// or its center aligns with the spawn Y coordinate.
    /// Also re-centres the XZ pivot relative to the bounds center.
    /// </summary>
    /// <param name="patent">The root GameObject to reposition.</param>
    /// <param name="toGround">
    /// <c>true</c> to snap the bottom of the model to the plane surface;
    /// <c>false</c> to align the center.
    /// </param>
    /// <param name="usePivot">
    /// <c>true</c> to treat the transform position as the pivot point;
    /// <c>false</c> to use the bounds center.
    /// </param>
    public static void SnapToYOffset(GameObject patent , bool toGround , bool usePivot)
    {
        if (patent == null)
        {
            Debug.Log("patent is null from pivot setter");
            return;
        }

        Bounds bounds = GetSpawnBounds(patent);
        Vector3 center = usePivot ? patent.transform.position : bounds.center;
        float target = toGround ? bounds.min.y : center.y;
        Vector2 xzCenter = new Vector2(center.x, center.z);
        Transform t = patent.transform;
        t.position = new Vector3(2*t.position.x - xzCenter.x, 2 * t.position.y - target, 2*t.position.z - xzCenter.y);
    }



}
