using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy UI button that fires <see cref="resetDesignTransformAction"/> when clicked,
/// allowing subscribers to reset the design's transform. Part of the older UI layer
/// in <c>Scripts/UI/</c>; prefer <see cref="ModelTransformManager"/> in new UI code.
/// </summary>
public class ResetDesignButton : MonoBehaviour
{
    private Button btn;

    /// <summary>Fired when the button is clicked to request a transform reset.</summary>
    public event Action resetDesignTransformAction;

    /// <summary>Caches the <see cref="Button"/> component.</summary>
    private void Awake()
    {
        btn = GetComponent<Button>();

    }

    /// <summary>Registers the click listener when the object becomes active.</summary>
    private void OnEnable()
    {
        btn.onClick.AddListener(ResetTransform);
    }

    /// <summary>Removes the click listener when the object is deactivated.</summary>
    private void OnDisable()
    {
        btn.onClick.RemoveListener(ResetTransform);
    }

    /// <summary>Fires <see cref="resetDesignTransformAction"/> in response to a button click.</summary>
    private void ResetTransform()
    {
        resetDesignTransformAction?.Invoke();
    }
}
