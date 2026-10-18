using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Stub component intended to calculate whether a detected AR plane is large
/// enough to accommodate a design model. Currently unimplemented.
/// </summary>
public class ARPlaneAreaCalculator : MonoBehaviour
{
    private ARPlaneManager planeManager;

    /// <summary>Locates and caches the scene's <see cref="ARPlaneManager"/>.</summary>
    private void Awake()
    {
        planeManager = FindFirstObjectByType<ARPlaneManager>();
    }

    /// <summary>
    /// Checks whether the bounding box of the given design collider fits within
    /// any tracked AR plane. Always returns <c>false</c> — implementation is incomplete.
    /// </summary>
    /// <param name="designCollider">The collider of the design model to test.</param>
    /// <returns><c>false</c> — not yet implemented.</returns>
    bool calculateArea(BoxCollider designCollider)
    {

        foreach (ARPlane plane in planeManager.trackables)
        {


        }

        return false;
    }
}
