using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Add Model button that allows the user to scan a new QR code.
/// The button toggles between an "add" icon (shows a confirmation popup) and a
/// "cancel" icon (cancels an in-progress QR scan). Reacts to
/// <see cref="StateMachine"/> state changes to keep its state consistent.
/// </summary>
public class AddModelManager : MonoBehaviour
{
    private GameObject m_AddModelPopupWindow;

    private Button m_AddModelButton;

    /// <summary>Icon sprite shown when the button will cancel an active QR scan.</summary>
    public Sprite CancelSprite;

    /// <summary>Icon sprite shown when the button will start a new QR scan.</summary>
    public Sprite AddSprite;

    private Image m_ButtonIcon;

    /// <summary>
    /// Resolves the button and icon image, configures initial state, and wires
    /// state-machine callbacks so the button resets and disables appropriately.
    /// </summary>
    void Start()
    {
        m_AddModelButton = GetComponent<Button>();
        m_ButtonIcon = GetComponentsInChildren<Image>()[1];

        SetButtonToDisplayAddModelPopup();

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += SetButtonToDisplayAddModelPopup;

        StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE)
            .OnStateEnter += SetButtonToDisplayAddModelPopup;

        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_COMPLETE_STATE)
            .OnStateEnter += () => { m_AddModelButton.interactable = false; };

        m_AddModelPopupWindow = null;
    }

    /// <summary>
    /// Configures the button to show the "add model" confirmation popup:
    /// sets the add icon and replaces any existing click listener.
    /// </summary>
    private void SetButtonToDisplayAddModelPopup()
    {
        m_AddModelButton.interactable = true;
        m_ButtonIcon.sprite = AddSprite;
        m_AddModelButton.onClick.RemoveAllListeners();
        m_AddModelButton.onClick.AddListener(DisplayAddModelPopup);
    }

    /// <summary>
    /// Configures the button to cancel an in-progress QR scan:
    /// sets the cancel icon and replaces the click listener with a stop-scanner action.
    /// </summary>
    private void SetButtonToCancelQRScan()
    {
        m_AddModelButton.interactable = true;
        m_ButtonIcon.sprite = CancelSprite;
        m_AddModelButton.onClick.RemoveAllListeners();
        m_AddModelButton.onClick.AddListener(() => {
            StateMachine.Instance.SetState(StateMachine.States.NO_MODEL_VIEW_STATE);
            QRScanner.Instance.StopScanner();
        });
    }

    /// <summary>
    /// Shows the "add model" confirmation popup, creating it on first call
    /// and reusing it on subsequent calls.
    /// </summary>
    private void DisplayAddModelPopup()
    {
        m_AddModelButton.interactable = false;

        if (m_AddModelPopupWindow != null)
        {
            m_AddModelPopupWindow.SetActive(true);
            return;
        }

        PopupBuilder builder = PopupBuilder.Create();

        m_AddModelPopupWindow = builder
            .WithContentText("Do you want to view another model?")
            .WithContentText("You will need to scan the QR code of the model.")
            .WithHeader("INFO")
            .WithFooterButton("Scan QR Code", PopupWindow.BUTTON_COLOR_OK, OnAddModelPopupOK)
            .WithFooterButton("Cancel", PopupWindow.BUTTON_COLOR_CANCEL, OnAddModelPopupCancel)
            .Get();
    }

    /// <summary>
    /// Confirms the add-model action: transitions to QR scan state, hides the
    /// popup, and reconfigures the button to act as a scan-cancel button.
    /// </summary>
    private void OnAddModelPopupOK()
    {
        StateMachine.Instance.SetState(StateMachine.States.QR_SCAN_STATE);
        m_AddModelPopupWindow.SetActive(false);
        SetButtonToCancelQRScan();
    }

    /// <summary>Cancels the add-model flow and restores button interactivity.</summary>
    private void OnAddModelPopupCancel()
    {
        m_AddModelButton.interactable = true;
        m_AddModelPopupWindow.SetActive(false);
    }
}
