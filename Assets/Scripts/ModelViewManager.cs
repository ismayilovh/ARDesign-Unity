using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton manager that controls the model-view mode toggles (Normal vs 1:1 scale).
/// Exposes <see cref="OnNormalViewToggle"/> and <see cref="OnOneToOneViewToggle"/>
/// events that other systems subscribe to in order to react to view-mode changes.
/// </summary>
public class ModelViewManager : MonoBehaviour
{
    /// <summary>The <see cref="UIToggle"/> component for the normal (FitScreen) view button.</summary>
    public UIToggle NormalViewUIToggle;

    /// <summary>The <see cref="UIToggle"/> component for the 1:1 real-world scale view button.</summary>
    public UIToggle OneToOneViewUIToggle;

    private Toggle NormalViewToggle;
    private Toggle OneToOneViewToggle;

    /// <summary>The single active instance of <see cref="ModelViewManager"/>.</summary>
    public static ModelViewManager Instance { get; private set; }

    /// <summary>Fired when the user switches to Normal (FitScreen) view.</summary>
    public event Action OnNormalViewToggle;

    /// <summary>Fired when the user switches to 1:1 real-world scale view.</summary>
    public event Action OnOneToOneViewToggle;

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Resolves the underlying <see cref="Toggle"/> components, wires toggle
    /// callbacks, and hides the panel until the model-view state is active.
    /// </summary>
    void Start()
    {
        NormalViewToggle = NormalViewUIToggle.gameObject.GetComponent<Toggle>();
        OneToOneViewToggle = OneToOneViewUIToggle.gameObject.GetComponent<Toggle>();

        NormalViewUIToggle.OnToggleValueChanged += (bool isOn) => { SetNormalView(isOn); };
        OneToOneViewUIToggle.OnToggleValueChanged += (bool isOn) => { SetOneToOneView(isOn); };

        OnNormalViewToggle += () => { print("switch normal view"); };
        OnOneToOneViewToggle += () => { print("switch 11 view"); };

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += () => { NormalViewToggle.isOn = true; };

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Activates Normal view: turns off the 1:1 toggle, fires <see cref="OnNormalViewToggle"/>,
    /// and adjusts toggle interactability so only one can be active at a time.
    /// </summary>
    /// <param name="isOn"><c>true</c> when this toggle was just turned on.</param>
    private void SetNormalView(bool isOn)
    {
        if (isOn)
        {
            OneToOneViewToggle.isOn = false;
            OnNormalViewToggle?.Invoke();
            NormalViewToggle.interactable = false;
            OneToOneViewToggle.interactable = true;
        }
    }

    /// <summary>
    /// Activates 1:1 view: turns off the Normal toggle, fires <see cref="OnOneToOneViewToggle"/>,
    /// and adjusts toggle interactability so only one can be active at a time.
    /// </summary>
    /// <param name="isOn"><c>true</c> when this toggle was just turned on.</param>
    private void SetOneToOneView(bool isOn)
    {
        if (isOn)
        {
            NormalViewToggle.isOn = false;
            OnOneToOneViewToggle?.Invoke();
            OneToOneViewToggle.interactable = false;
            NormalViewToggle.interactable = true;
        }
    }
}
