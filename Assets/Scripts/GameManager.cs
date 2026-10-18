using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Internal enum representing which high-level mode the app is currently in.
/// </summary>
enum ArMode
{
    /// <summary>The AR design-viewing mode is active.</summary>
    Design,

    /// <summary>The QR-code scanning mode is active.</summary>
    Qr
}

/// <summary>
/// Singleton bootstrap manager that routes the app between QR scanning and
/// AR design-viewing by toggling scene root GameObjects and wiring up
/// <see cref="StateMachine"/> state-enter callbacks.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="GameManager"/>.</summary>
    public static GameManager instance;

    [SerializeField] private GameObject qrScene;

    [SerializeField, FormerlySerializedAs("patentScene")] private GameObject designScene;
    [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
    private RaycastHandler raycastHandler;
    private ArMode currMode = ArMode.Qr;

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Destroy(this);
        }


    }

    /// <summary>
    /// Locates the <see cref="RaycastHandler"/>, wires state-machine enter callbacks,
    /// and starts the app in QR mode.
    /// </summary>
    void Start()
    {
        raycastHandler = FindAnyObjectByType<RaycastHandler>();
        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE).OnStateEnter += ActivateDesignParts;
        StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE).OnStateEnter += ActivateQrParts;
        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE).OnStateEnter += ActivateQrParts;
        ActivateQrParts();

    }

    /// <summary>Reserved for future enable-time setup.</summary>
    private void OnEnable()
    {

    }

    // private void OnDisable()
    // {
    //     StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE).OnStateEnter -= ActivateDesignParts;
    //     StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE).OnStateEnter -= ActivateQrParts;
    //
    // }

    /// <summary>
    /// Toggles between QR and design modes based on the current <see cref="ArMode"/>.
    /// </summary>
    public void ChangeMode()
    {
        if (currMode == ArMode.Design)
        {
            ActivateQrParts();
        }
        else if (currMode == ArMode.Qr)
        {
            ActivateDesignParts();
        }
    }

    /// <summary>
    /// Hides the design scene, disables plane detection, shows the QR scene,
    /// and sets the current mode to <see cref="ArMode.Qr"/>.
    /// </summary>
    private void ActivateQrParts()
    {
        designScene.SetActive(false);
        setDesignScripts(false);
        raycastHandler.changePlaneDetection(false);
        qrScene.SetActive(true);
        currMode = ArMode.Qr;
    }

    /// <summary>
    /// Hides the QR scene, enables design scripts, shows the design scene,
    /// and sets the current mode to <see cref="ArMode.Design"/>.
    /// </summary>
    private void ActivateDesignParts()
    {
        qrScene.SetActive(false);
        setDesignScripts(true);
        designScene.SetActive(true);
        currMode = ArMode.Design;
    }

    /// <summary>
    /// Enables or disables the <see cref="RaycastHandler"/> component to prevent
    /// AR raycasts from firing outside of design-viewing mode.
    /// </summary>
    /// <param name="active"><c>true</c> to enable; <c>false</c> to disable.</param>
    private void setDesignScripts(bool active)
    {
       // raycastHandler.changePlaneDetection(active);
        raycastHandler.enabled = active;
    }

}
