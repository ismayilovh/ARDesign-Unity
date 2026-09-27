using System;
using UnityEngine;

/// <summary>
/// Internal enum for the two available model scaling modes.
/// </summary>
enum ScalingMode
{
    /// <summary>Scales the model to fit the device screen.</summary>
    FitScreen,

    /// <summary>Renders the model at real-world 1:1 metric scale.</summary>
    OneToOne,
}

/// <summary>
/// Singleton controller that tracks and broadcasts the active scaling mode.
/// Subscribes to <see cref="ModelViewManager"/> toggle events and raises
/// <see cref="modeChanged"/> so other systems can react to mode switches.
/// </summary>
public class ScalingModeController : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="ScalingModeController"/>.</summary>
    public static ScalingModeController Instance;

    [SerializeField] private ScalingMode mode = ScalingMode.FitScreen;

    /// <summary>
    /// Fired whenever the scaling mode changes.
    /// The <c>bool</c> parameter is <c>true</c> for <see cref="ScalingMode.OneToOne"/>
    /// and <c>false</c> for <see cref="ScalingMode.FitScreen"/>.
    /// </summary>
    public event Action<bool> modeChanged;

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Subscribes to <see cref="ModelViewManager"/> toggle events so mode changes
    /// from the UI are forwarded to <see cref="modeChanged"/> subscribers.
    /// </summary>
    private void Start()
    {
        if (ModelViewManager.Instance != null)
        {
            ModelViewManager.Instance.OnOneToOneViewToggle += ChangeModeToOneToOne;
            ModelViewManager.Instance.OnNormalViewToggle += ChangeModeToNormalView;
        }


    }

    /// <summary>Switches the scaling mode to <see cref="ScalingMode.OneToOne"/>.</summary>
    public void ChangeModeToOneToOne()
    {
        ChangeMode(true);
    }

    /// <summary>Switches the scaling mode to <see cref="ScalingMode.FitScreen"/>.</summary>
    public void ChangeModeToNormalView()
    {
        ChangeMode(false);
    }

    /// <summary>
    /// Changes the scaling mode and fires <see cref="modeChanged"/> if the mode actually changed.
    /// </summary>
    /// <param name="isOneToOne">
    /// <c>true</c> to switch to <see cref="ScalingMode.OneToOne"/>;
    /// <c>false</c> to switch to <see cref="ScalingMode.FitScreen"/>.
    /// </param>
    public void ChangeMode(bool isOneToOne)
    {
        if (isOneToOne && mode != ScalingMode.OneToOne)
        {
            mode = ScalingMode.OneToOne;
            modeChanged?.Invoke(true);
        }
        else if (!isOneToOne && mode != ScalingMode.FitScreen)
        {
            mode = ScalingMode.FitScreen;
            modeChanged?.Invoke(false);
        }

    }
}
