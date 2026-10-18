using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Controls a lightweight runtime diagnostics overlay.</summary>
public class DebugModeManager : MonoBehaviour
{
    [SerializeField] private GameObject debugRoot;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private UIVisibilityManager uiVisibilityManager;
    [SerializeField] private float refreshInterval = 0.25f;

    private readonly StringBuilder textBuilder = new StringBuilder(192);
    private bool globallyVisible = true;
    private float elapsedTime;
    private int frameCount;
    private float framesPerSecond;

    /// <summary>Whether runtime debug mode is enabled for this session.</summary>
    public bool IsDebugModeEnabled { get; private set; }

    private void Awake()
    {
        if (debugRoot != null)
            debugRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (uiVisibilityManager != null)
        {
            globallyVisible = uiVisibilityManager.UIVisible;
            uiVisibilityManager.OnUIVisibilityChanged += HandleUIVisibilityChanged;
        }

        ApplyRootVisibility();
    }

    private void OnDisable()
    {
        if (uiVisibilityManager != null)
            uiVisibilityManager.OnUIVisibilityChanged -= HandleUIVisibilityChanged;

        if (debugRoot != null)
            debugRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsDebugModeEnabled || !globallyVisible) return;

        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;
        if (elapsedTime < Mathf.Max(0.05f, refreshInterval)) return;

        framesPerSecond = elapsedTime > 0f ? frameCount / elapsedTime : 0f;
        elapsedTime = 0f;
        frameCount = 0;
        RefreshText();
    }

    /// <summary>Enables or disables the diagnostics overlay without changing app behavior.</summary>
    public void SetDebugModeEnabled(bool enabled)
    {
        if (IsDebugModeEnabled == enabled)
        {
            ApplyRootVisibility();
            return;
        }

        IsDebugModeEnabled = enabled;
        elapsedTime = 0f;
        frameCount = 0;
        framesPerSecond = 0f;
        ApplyRootVisibility();

        if (enabled)
            RefreshText();
    }

    private void HandleUIVisibilityChanged(bool visible)
    {
        globallyVisible = visible;
        ApplyRootVisibility();
    }

    private void ApplyRootVisibility()
    {
        if (debugRoot != null)
            debugRoot.SetActive(IsDebugModeEnabled && globallyVisible && isActiveAndEnabled);
    }

    private void RefreshText()
    {
        if (debugText == null) return;

        StateMachine stateMachine = StateMachine.Instance;
        DesignManager designManager = DesignManager.Instance;
        GameObject activeModel = designManager != null ? designManager.ActiveDesign : null;

        textBuilder.Clear();
        textBuilder.AppendLine("DEBUG");
        textBuilder.Append("State: ")
            .Append(stateMachine != null ? stateMachine.CurrentStateName.ToString() : "Unavailable")
            .AppendLine();
        textBuilder.AppendFormat(CultureInfo.InvariantCulture, "FPS: {0:0.0}\n", framesPerSecond);
        textBuilder.Append("Model loaded: ")
            .Append(designManager != null && designManager.Design != null ? "Yes" : "No")
            .AppendLine();
        textBuilder.Append("Model spawned: ")
            .Append(activeModel != null ? "Yes" : "No");

        if (activeModel != null)
            textBuilder.AppendLine().Append("Active model: ").Append(activeModel.name);

        debugText.text = textBuilder.ToString();
    }
}
