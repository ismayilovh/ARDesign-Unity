using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton finite state machine that drives the overall app flow.
/// Subscribe to individual <see cref="State"/> events via
/// <see cref="GetState"/> and trigger transitions with <see cref="SetState"/>.
/// </summary>
public class StateMachine : MonoBehaviour
{

    /// <summary>The single active instance of <see cref="StateMachine"/>.</summary>
    public static StateMachine Instance
    {
        get; private set;
    }

    /// <summary>
    /// Enumeration of all application states.
    /// </summary>
    public enum States
    {
        /// <summary>Default state: no model loaded, QR scanner inactive.</summary>
        NO_MODEL_VIEW_STATE,

        /// <summary>Camera is actively scanning for a QR code.</summary>
        QR_SCAN_STATE,

        /// <summary>QR code decoded; model download in progress.</summary>
        QR_SCAN_COMPLETE_STATE,

        /// <summary>Model placed in AR scene; interaction enabled.</summary>
        MODEL_VIEW_STATE
    }

    private Dictionary<States, State> m_StateMap = new();

    private State m_CurrentState;

    /// <summary>The name of the currently active state.</summary>
    public States CurrentStateName;

    /// <summary>
    /// Initialises the singleton, creates a <see cref="State"/> object for each
    /// <see cref="States"/> value, and enters <see cref="States.NO_MODEL_VIEW_STATE"/>.
    /// </summary>
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

        m_StateMap[States.NO_MODEL_VIEW_STATE] = new State();
        m_StateMap[States.QR_SCAN_STATE] = new State();
        m_StateMap[States.QR_SCAN_COMPLETE_STATE] = new State();
        m_StateMap[States.MODEL_VIEW_STATE] = new State();

        SetState(States.NO_MODEL_VIEW_STATE);
    }

    /// <summary>
    /// Exits the current state and enters <paramref name="stateName"/>.
    /// </summary>
    /// <param name="stateName">The state to transition into.</param>
    public void SetState(States stateName)
    {
        m_CurrentState?.Exit();

        m_CurrentState = m_StateMap[stateName];
        CurrentStateName = stateName;

        m_CurrentState.Enter();
    }

    /// <summary>
    /// Returns the <see cref="State"/> object associated with <paramref name="stateName"/>
    /// so callers can subscribe to its lifecycle events.
    /// </summary>
    /// <param name="stateName">The state whose object should be retrieved.</param>
    /// <returns>The <see cref="State"/> for the given <paramref name="stateName"/>.</returns>
    public State GetState(States stateName)
    {
        return m_StateMap[stateName];
    }

    /// <summary>Forwards Unity's Update tick to the currently active <see cref="State"/>.</summary>
    void Update()
    {
        m_CurrentState.Update();
    }
}
