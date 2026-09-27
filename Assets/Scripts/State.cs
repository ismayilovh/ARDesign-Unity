using System;
using UnityEngine;

/// <summary>
/// Represents a single state within the app's finite state machine.
/// Subscribers attach behaviour to <see cref="OnStateEnter"/>,
/// <see cref="OnStateUpdate"/>, and <see cref="OnStateExit"/> and
/// <see cref="StateMachine"/> calls <see cref="Enter"/>, <see cref="Update"/>,
/// and <see cref="Exit"/> to drive those callbacks.
/// </summary>
public class State
{
    /// <summary>Fired once when this state becomes active.</summary>
    public event Action OnStateEnter;

    /// <summary>Fired every frame while this state is active.</summary>
    public event Action OnStateUpdate;

    /// <summary>Fired once when this state is deactivated.</summary>
    public event Action OnStateExit;

    /// <summary>Activates the state and invokes <see cref="OnStateEnter"/>.</summary>
    public void Enter()
    {
        OnStateEnter?.Invoke();
    }

    /// <summary>Deactivates the state and invokes <see cref="OnStateExit"/>.</summary>
    public void Exit()
    {
        OnStateExit?.Invoke();
    }

    /// <summary>Called every frame by <see cref="StateMachine"/>; invokes <see cref="OnStateUpdate"/>.</summary>
    public void Update()
    {
        OnStateUpdate?.Invoke();
    }
}
