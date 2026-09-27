using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple data-container component for the model-download progress bar UI.
/// Exposes the <see cref="Slider"/> and <see cref="ProgressText"/> references
/// so <see cref="QRNetworkHandler"/> can update them during download.
/// </summary>
public class QRProgressBarManager : MonoBehaviour
{
    /// <summary>The slider that visualises download progress (0–1).</summary>
    public Slider Slider;

    /// <summary>The text label that shows the percentage string.</summary>
    public TextMeshProUGUI ProgressText;
}
