using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton manager for the calculated-pivot toggle and its session preference.
/// When disabled the imported content transform is restored exactly. Must be initialised by calling
/// <see cref="Initialize"/> (done by <see cref="MiscOptionsManager"/>).
/// </summary>
public class PivotSettingManager : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="PivotSettingManager"/>.</summary>
    public static PivotSettingManager Instance { get; private set; }

    /// <summary>
    /// Fired when the calculated-pivot preference changes.
    /// Parameter is <c>true</c> when the calculated pivot should be used.
    /// </summary>
    public event UnityAction<bool> OnPivotUseChange;

    [SerializeField] private bool useCalculatedPivot;

    private UIToggle m_UIToggleComponent;
    private PivotSetter activePivotSetter;
    private bool initialized;
    private bool callbacksRegistered;

    /// <summary>Whether current and future models should use the calculated pivot.</summary>
    public bool UseCalculatedPivot => useCalculatedPivot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        
    }
    
    /// <summary>
    /// Wires up the toggle listener and model lifecycle callbacks.
    /// Call once after all singleton instances are ready.
    /// </summary>
    public void Initialize()
    {
        if (initialized) return;

        m_UIToggleComponent = GetComponent<UIToggle>();
        initialized = true;

        m_UIToggleComponent.SetValueWithoutNotify(useCalculatedPivot);
        RegisterCallbacks();
        BindActiveModel();
    }

    private void OnEnable()
    {
        if (!initialized) return;

        RegisterCallbacks();
        m_UIToggleComponent.SetValueWithoutNotify(useCalculatedPivot);
        BindActiveModel();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered) return;

        m_UIToggleComponent.OnToggleValueChanged += HandleToggleChanged;
        if (DesignManager.Instance != null)
        {
            DesignManager.Instance.designSpawnedEvent += BindActiveModel;
            DesignManager.Instance.designDeletedEvent += HandleDesignDeleted;
        }

        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered) return;

        if (m_UIToggleComponent != null)
            m_UIToggleComponent.OnToggleValueChanged -= HandleToggleChanged;

        if (DesignManager.Instance != null)
        {
            DesignManager.Instance.designSpawnedEvent -= BindActiveModel;
            DesignManager.Instance.designDeletedEvent -= HandleDesignDeleted;
        }

        callbacksRegistered = false;
    }

    private void HandleToggleChanged(bool isOn)
    {
        SetUseCalculatedPivot(isOn);
    }

    private void BindActiveModel()
    {
        GameObject activeDesign = DesignManager.Instance?.ActiveDesign;
        activePivotSetter = activeDesign != null ? activeDesign.GetComponent<PivotSetter>() : null;
        activePivotSetter?.SetUseCalculatedPivot(useCalculatedPivot);
    }

    private void HandleDesignDeleted()
    {
        activePivotSetter = null;
    }

    /// <summary>
    /// Programmatically sets the calculated-pivot preference and fires
    /// <see cref="OnPivotUseChange"/>.
    /// </summary>
    /// <param name="value"><c>true</c> for the calculated pivot; <c>false</c> for the imported pivot.</param>
    public void SetUseCalculatedPivot(bool value)
    {
        useCalculatedPivot = value;
        if (m_UIToggleComponent != null)
            m_UIToggleComponent.SetValueWithoutNotify(value);

        activePivotSetter?.SetUseCalculatedPivot(value);
        OnPivotUseChange?.Invoke(value);
    }

    /// <summary>Compatibility alias for existing callers.</summary>
    public void SetUsePivot(bool value)
    {
        SetUseCalculatedPivot(value);
    }
}
