using System;
using UnityEngine;

/// <summary>
/// Marks this GameObject to persist across scene loads by calling
/// <see cref="UnityEngine.Object.DontDestroyOnLoad"/> on itself during Awake.
/// Attach to any root object that must survive scene transitions.
/// </summary>
public class DontDestroyObjects : MonoBehaviour
{
    /// <summary>Calls <see cref="UnityEngine.Object.DontDestroyOnLoad"/> so this object survives scene loads.</summary>
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
