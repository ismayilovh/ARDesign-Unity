using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Singleton manager for model transform controls: lock/unlock movement,
/// reset transform, and delete the active patent model.
/// Drives confirmation popups via <see cref="PopupBuilder"/> before
/// executing destructive actions.
/// </summary>
public class ModelTransformManager : MonoBehaviour
{
    /// <summary>Toggle that locks or unlocks model interaction.</summary>
    public UIToggle LockModelUIToggle;

    /// <summary>Button that resets the model to its initial transform.</summary>
    public Button ResetModelTransformButton;

    /// <summary>Button that deletes the currently active patent model.</summary>
    public Button DeleteModelTransformButton;

    /// <summary>Underlying <see cref="Toggle"/> component for the lock control.</summary>
    public Toggle LockModelTransformToggle;

    /// <summary>The single active instance of <see cref="ModelTransformManager"/>.</summary>
    public static ModelTransformManager Instance { get; private set; }

    /// <summary>Fired when the lock toggle changes. Parameter is <c>true</c> when locked.</summary>
    public event UnityAction<bool> OnLockModel;

    /// <summary>Fired after the user confirms a transform reset.</summary>
    public event UnityAction OnResetModelTransform;

    /// <summary>Fired after the user confirms model deletion.</summary>
    public event UnityAction OnDeleteModel;


    private GameObject m_ResetTransformPopupWindow;
    private GameObject m_DeleteModelPopupWindow;

    /// <summary>Initialises the singleton and clears cached popup references.</summary>
    private void Awake()
    {
        m_ResetTransformPopupWindow = null;
        m_DeleteModelPopupWindow = null;
        Instance = this;
    }

    /// <summary>
    /// Wires button and toggle listeners, subscribes to scaling-mode changes to
    /// auto-lock controls in 1:1 mode, and hides the panel until a model is active.
    /// </summary>
    void Start()
    {

        LockModelUIToggle.OnToggleValueChanged += (bool isOn) => { OnLockModel?.Invoke(isOn); };
        ResetModelTransformButton.onClick.AddListener(DisplayResetTransformPopup);
        DeleteModelTransformButton.onClick.AddListener(DisplayDeleteModelPopup);

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += () => { LockModelUIToggle.GetComponent<Toggle>().isOn = false; };
        ScalingModeController.Instance.modeChanged += LockLockToggle;
        ScalingModeController.Instance.modeChanged += LockResetButton;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Forces the lock toggle on and makes it non-interactable when 1:1 mode is active,
    /// ensuring the model cannot be moved while at real-world scale.
    /// </summary>
    /// <param name="isOn"><c>true</c> when 1:1 mode is active.</param>
    private void LockLockToggle(bool isOn)
    {
        LockModelTransformToggle.isOn = isOn;
        LockModelTransformToggle.interactable = !isOn;
    }

    /// <summary>
    /// Disables the reset button in 1:1 mode because resetting scale would conflict
    /// with real-world positioning.
    /// </summary>
    /// <param name="isOn"><c>true</c> when 1:1 mode is active.</param>
    private void LockResetButton(bool isOn)
    {
        ResetModelTransformButton.interactable = !isOn;
    }

    /// <summary>
    /// Shows a confirmation popup before resetting the model transform.
    /// Creates the popup on first call; reuses it on subsequent calls.
    /// </summary>
    private void DisplayResetTransformPopup()
    {
        ResetModelTransformButton.interactable = false;

        if (m_ResetTransformPopupWindow != null)
        {
            m_ResetTransformPopupWindow.SetActive(true);
            return;
        }

        PopupBuilder builder = PopupBuilder.Create();

        m_ResetTransformPopupWindow = builder
            .WithContentText("This will reset your model back to the original scale.")
            .WithHeader("WARNING")
            .WithFooterButton("OK", PopupWindow.BUTTON_COLOR_OK, OnResetTransformPopupOK)
            .WithFooterButton("Cancel", PopupWindow.BUTTON_COLOR_CANCEL, OnResetTransformPopupCancel)
            .Get();
    }

    /// <summary>
    /// Confirms the reset: restores button interactivity, hides the popup,
    /// and fires <see cref="OnResetModelTransform"/>.
    /// </summary>
    private void OnResetTransformPopupOK()
    {
        ResetModelTransformButton.interactable = true;
        m_ResetTransformPopupWindow.SetActive(false);
        OnResetModelTransform?.Invoke();
    }

    /// <summary>Cancels the reset and restores button interactivity.</summary>
    private void OnResetTransformPopupCancel()
    {
        ResetModelTransformButton.interactable = true;
        m_ResetTransformPopupWindow.SetActive(false);
    }

    /// <summary>
    /// Shows a confirmation popup before deleting the active patent model.
    /// Creates the popup on first call; reuses it on subsequent calls.
    /// </summary>
    private void DisplayDeleteModelPopup()
    {
        DeleteModelTransformButton.interactable = false;

        if (m_DeleteModelPopupWindow != null)
        {
            m_DeleteModelPopupWindow.SetActive(true);
            return;
        }

        PopupBuilder builder = PopupBuilder.Create();

        m_DeleteModelPopupWindow = builder
            .WithContentText("Your model will be deleted.")
            .WithHeader("WARNING")
            .WithFooterButton("OK", PopupWindow.BUTTON_COLOR_OK, OnDeleteModelPopupOK)
            .WithFooterButton("Cancel", PopupWindow.BUTTON_COLOR_CANCEL, OnDeleteModelPopupCancel)
            .Get();
    }

    /// <summary>
    /// Confirms deletion: restores button interactivity, hides the popup,
    /// calls <see cref="PatentManager.DeleteActivePatent"/>, and fires <see cref="OnDeleteModel"/>.
    /// </summary>
    private void OnDeleteModelPopupOK()
    {
        DeleteModelTransformButton.interactable = true;
        m_DeleteModelPopupWindow.SetActive(false);
        PatentManager.Instance.DeleteActivePatent();
        OnDeleteModel?.Invoke();
    }

    /// <summary>Cancels the deletion and restores button interactivity.</summary>
    private void OnDeleteModelPopupCancel()
    {
        DeleteModelTransformButton.interactable = true;
        m_DeleteModelPopupWindow.SetActive(false);
    }
}
