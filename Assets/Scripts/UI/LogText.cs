using System;
using TMPro;
using UnityEngine;

/// <summary>
/// A pooled, animated log notification text element managed by <see cref="LogManager"/>.
/// Slides from a start position to an end position over time, fades in while
/// moving, lingers for a configurable duration, then returns itself to the pool.
/// </summary>
public class LogText : MonoBehaviour
{
    private TextMeshProUGUI text;
    [SerializeField] private float dropSpeed = 0.2f;
    [SerializeField] private float stayDuration = 2f;
    [SerializeField] private float fadeSpeedMultiplier;

    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color normalColor  = Color.black;

    [SerializeField] private float endPointThreshold = 0.1f;
    private Vector2 startPoint;
    private Vector2 endPoint;

    private bool isStarted = false;
    private float fadeSpeed;
    private float remainingStayDuration;
    private Vector2 targetPos;

    private Color currentColor;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Initialises the log animation from <paramref name="_startPoint"/> to
    /// <paramref name="_endPoint"/> and begins the slide-and-fade sequence.
    /// </summary>
    /// <param name="_startPoint">Local-space UI start position.</param>
    /// <param name="_endPoint">Local-space UI end position.</param>
    public void StartLog(Vector2 _startPoint, Vector2 _endPoint)
    {
        startPoint = _startPoint;
        endPoint = _endPoint;
        transform.localPosition = startPoint;
        isStarted = true;
        fadeSpeed = Mathf.Abs(_endPoint.y - _startPoint.y)/dropSpeed;
        remainingStayDuration = stayDuration;
    }

    /// <summary>
    /// Sets the displayed text and its colour based on severity.
    /// </summary>
    /// <param name="_text">The message to display.</param>
    /// <param name="isError"><c>true</c> to use the error colour; <c>false</c> for normal.</param>
    public void SetText(String _text , bool isError)
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();
        if (text == null) return;

        text.text = _text;
        Color color = isError ? errorColor : normalColor;
        color.a = 0;
        currentColor = color;
        text.color = color;
    }

    /// <summary>
    /// Advances the log animation. Called each frame by <see cref="LogManager.UpdateLogs"/>.
    /// </summary>
    public void UpdateLog()
    {
        if (isStarted)
        {
            MoveLog();
        }
    }

    /// <summary>Stops this notification and returns it to the pool immediately.</summary>
    public void CloseImmediately()
    {
        if (!isStarted) return;

        isStarted = false;
        if (LogManager.Instance != null)
            LogManager.Instance.EnqueueLog(this);
        else
            gameObject.SetActive(false);
    }

    /// <summary>Clears animation state before this element is stored in the pool.</summary>
    public void ResetForPool()
    {
        isStarted = false;
        remainingStayDuration = 0f;
    }

    /// <summary>
    /// Moves the element toward <c>endPoint</c>, fades the alpha in, and counts
    /// down the linger duration once the end point is reached. Returns the element
    /// to the <see cref="LogManager"/> pool when the linger expires.
    /// </summary>
    private void MoveLog()
    {
        if (Mathf.Abs(transform.localPosition.y - endPoint.y) <= endPointThreshold)
        {
            remainingStayDuration -= Time.deltaTime;
            if (remainingStayDuration <= 0)
            {
                isStarted = false;
                if (LogManager.Instance != null)
                    LogManager.Instance.EnqueueLog(this);
            }
        }
        else
        {
            transform.localPosition = Vector2.MoveTowards(transform.localPosition , endPoint , dropSpeed*Time.deltaTime );
        }
        currentColor.a = Mathf.Min(1, currentColor.a + fadeSpeed * Time.deltaTime);
        text.color = currentColor;
    }

}
