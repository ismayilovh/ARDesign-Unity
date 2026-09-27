using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple button handler that toggles a popup menu panel open and closed.
/// Requires a <see cref="Button"/> on the same GameObject.
/// </summary>
[RequireComponent(typeof(Button))]
public class PopupMenuHandler : MonoBehaviour
{
    [SerializeField] private GameObject popupMenu;
    private Button button;
    private bool isOpen = false;

    /// <summary>Caches the <see cref="Button"/> component and registers the click listener.</summary>
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(EnablePopupMenu);
    }

    /// <summary>Toggles the visibility of <see cref="popupMenu"/>.</summary>
    void EnablePopupMenu()
    {
        isOpen = !isOpen;
        popupMenu.SetActive(isOpen);
    }
}
