using UnityEngine;

/// <summary>
/// Container component for the miscellaneous options panel. On Start it
/// initialises its child <see cref="UIVisibilityManager"/>,
/// <see cref="ModelPlaneDetectionManager"/>, and <see cref="PivotSettingManager"/>
/// components (which require manual initialisation after singletons are ready),
/// then hides the panel until it is needed.
/// </summary>
public class MiscOptionsManager : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private UIToggle notificationsToggle;
    [SerializeField] private UIToggle debugModeToggle;
    [SerializeField] private LogManager logManager;
    [SerializeField] private DebugModeManager debugModeManager;

    private bool initialized;
    private bool callbacksRegistered;

    private void Start()
    {
        GameObject optionsPanel = panelRoot != null ? panelRoot : gameObject;

        UIVisibilityManager visibilityManager = optionsPanel.GetComponentInChildren<UIVisibilityManager>(true);
        ModelPlaneDetectionManager planeDetectionManager = optionsPanel.GetComponentInChildren<ModelPlaneDetectionManager>(true);
        PivotSettingManager pivotSettingManager = optionsPanel.GetComponentInChildren<PivotSettingManager>(true);

        visibilityManager?.Initialize();
        planeDetectionManager?.Initialize();
        pivotSettingManager?.Initialize();

        if (logManager == null)
            logManager = LogManager.Instance;

        notificationsToggle?.SetValueWithoutNotify(logManager == null || logManager.NotificationsEnabled);
        debugModeToggle?.SetValueWithoutNotify(debugModeManager != null && debugModeManager.IsDebugModeEnabled);

        initialized = true;
        RegisterCallbacks();
        optionsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (initialized)
            RegisterCallbacks();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered) return;

        if (notificationsToggle != null)
            notificationsToggle.OnToggleValueChanged += SetNotificationsEnabled;
        if (debugModeToggle != null)
            debugModeToggle.OnToggleValueChanged += SetDebugModeEnabled;

        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered) return;

        if (notificationsToggle != null)
            notificationsToggle.OnToggleValueChanged -= SetNotificationsEnabled;
        if (debugModeToggle != null)
            debugModeToggle.OnToggleValueChanged -= SetDebugModeEnabled;

        callbacksRegistered = false;
    }

    private void SetNotificationsEnabled(bool enabled)
    {
        logManager?.SetNotificationsEnabled(enabled);
    }

    private void SetDebugModeEnabled(bool enabled)
    {
        debugModeManager?.SetDebugModeEnabled(enabled);
    }
}
