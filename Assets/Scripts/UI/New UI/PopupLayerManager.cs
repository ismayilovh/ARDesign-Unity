using UnityEngine;

/// <summary>
/// Registers this GameObject as <see cref="PopupWindow.PopupLayer"/> on Awake
/// and hides it by default. The popup layer is a full-screen overlay that blocks
/// interaction with the AR scene while a popup is open.
/// </summary>
public class PopupLayerManager : MonoBehaviour
{
    /// <summary>Registers this GameObject as the global popup layer and hides it by default.</summary>
    void Awake()
    {
        PopupWindow.PopupLayer = gameObject;
        gameObject.SetActive(false);
    }
}
