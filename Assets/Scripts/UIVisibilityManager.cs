using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Hides or shows a collection of AR UI panels based on the current app state
/// and a hide-all toggle. Must be initialised manually by calling
/// <see cref="Initialize"/> (done by <see cref="MiscOptionsManager"/>).
/// </summary>
public class UIVisibilityManager : MonoBehaviour
{
    List<GameObject> m_ObjectsToModifyVisibility;
    private UIToggle m_UIToggleComponent;

    /// <summary>Fired with <c>true</c> when globally controlled UI is visible.</summary>
    public event UnityAction<bool> OnUIVisibilityChanged;

    /// <summary>Whether globally controlled UI is currently visible.</summary>
    public bool UIVisible { get; private set; } = true;

    /// <summary>
    /// Wires up state-machine callbacks and toggle listeners.
    /// Call once after all singleton instances are ready.
    /// </summary>
    public void Initialize()
    {
        m_UIToggleComponent = GetComponent<UIToggle>();

        GameObject modelViewObject = ModelViewManager.Instance.gameObject;
        GameObject modelTransformObject = ModelTransformManager.Instance.gameObject;
        GameObject miscOptionsObject = ModelPlaneDetectionManager.Instance.gameObject.transform.parent.gameObject;

        GameObject modelPlaneDetectionObject = ModelPlaneDetectionManager.Instance.gameObject;

        m_ObjectsToModifyVisibility = new List<GameObject> {
            modelViewObject,
            modelTransformObject,
            modelPlaneDetectionObject
        };

        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE)
            .OnStateEnter += () =>
            {
                modelViewObject.SetActive(false);
                modelTransformObject.SetActive(false);
                miscOptionsObject.SetActive(false);
            };

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE)
            .OnStateEnter += () =>
            {
                modelViewObject.SetActive(true);
                modelTransformObject.SetActive(true);
                miscOptionsObject.SetActive(true);
                GetComponent<Toggle>().isOn = false;
            };

        m_UIToggleComponent.OnToggleValueChanged += (bool isOn) => { SetUIObjectVisibility(isOn); };
    }

    /// <summary>
    /// Shows or hides all tracked UI objects.
    /// </summary>
    /// <param name="isOn">
    /// When <c>true</c> the panels are hidden; when <c>false</c> they are shown.
    /// </param>
    private void SetUIObjectVisibility(bool isOn)
    {
        UIVisible = !isOn;
        foreach (GameObject obj in m_ObjectsToModifyVisibility)
        {
            obj.SetActive(!isOn);
        }

        OnUIVisibilityChanged?.Invoke(UIVisible);
    }

}
