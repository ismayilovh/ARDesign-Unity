using UnityEngine;

/// <summary>
/// UI button handler that toggles between QR-scanning and AR patent-viewing
/// modes by calling <see cref="GameManager.ChangeMode"/> when clicked.
/// </summary>
public class SceneTransitionButton : MonoBehaviour
{
    /// <summary>
    /// Called by the button's OnClick event. Delegates to <see cref="GameManager.ChangeMode"/>.
    /// </summary>
    public void OnSceneChangeClicked()
    {
        GameManager.instance.ChangeMode();
    }
}
