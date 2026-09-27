using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton object-pool manager for <see cref="LogText"/> notification elements.
/// Pre-allocates a fixed pool and recycles entries when their animations finish.
/// Notifications can be suppressed globally with <see cref="SetNotificationsEnabled"/>.
/// </summary>
public class LogManager : MonoBehaviour
{
    /// <summary>The single active instance of <see cref="LogManager"/>.</summary>
    public static LogManager Instance { get; private set; }

    [SerializeField] private int poolSize = 5;
    [SerializeField] private LogText logPrefab;

    private Queue<LogText> logPool;
    private List<LogText> activeLogs;
    private HashSet<LogText> pooledLogs;
    private bool poolInitialized;
    
    
    [SerializeField] private float logDistanceY = 0.5f;
    [SerializeField] private Vector2 startPoint;
    [SerializeField] private Vector2 endPoint;
    
    //[SerializeField] private int maxTextSize = 30;
   
    
    
    /// <summary>Initialises the singleton and allocates the pool and active-log collections.</summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        logPool = new Queue<LogText>(poolSize);
        activeLogs = new List<LogText>(poolSize);
        pooledLogs = new HashSet<LogText>();
    }

    private void Start()
    {
        InitializeLogPool();
    }

    /// <summary>
    /// Instantiates <see cref="poolSize"/> inactive <see cref="LogText"/> prefabs
    /// and enqueues them into <c>logPool</c> for later reuse.
    /// </summary>
    private void InitializeLogPool()
    {
        if (poolInitialized) return;
        poolInitialized = true;

        if (logPrefab == null)
        {
            Debug.LogWarning("LogManager has no LogText prefab assigned.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            LogText log = Instantiate(logPrefab, transform);
            log.gameObject.SetActive(false);
            logPool.Enqueue(log);
            pooledLogs.Add(log);
        }
    }

    /// <summary>
    /// Returns a finished <see cref="LogText"/> to the pool so it can be reused.
    /// Called automatically by <see cref="LogText"/> when its animation completes.
    /// </summary>
    /// <param name="log">The <see cref="LogText"/> to recycle.</param>
    public void EnqueueLog(LogText log)
    {
        if (log == null)
        {
            Debug.Log("Log is null");
            return;
        }
        if (!pooledLogs.Add(log)) return;

        log.ResetForPool();
        activeLogs.Remove(log);
        log.gameObject.SetActive(false);
        logPool.Enqueue(log);
    }

    /// <summary>
    /// Whether log notifications are currently shown. Defaults to <c>true</c>.
    /// </summary>
    public bool NotificationsEnabled { get; private set; } = true;

    /// <summary>
    /// Enables or disables all log notifications globally.
    /// </summary>
    /// <param name="enabled"><c>true</c> to show notifications; <c>false</c> to suppress them.</param>
    public void SetNotificationsEnabled(bool enabled)
    {
        if (NotificationsEnabled == enabled) return;

        NotificationsEnabled = enabled;
        if (!enabled)
            CloseAllNotifications();
    }

    /// <summary>
    /// Dequeues a pooled <see cref="LogText"/>, positions it, and starts its
    /// animation. Does nothing if notifications are disabled or the pool is empty.
    /// </summary>
    /// <param name="logText">The message string to display.</param>
    public void DequeueLog(String logText)
    {
        DequeueLog(logText, false);
    }

    /// <summary>Displays a normal or error notification when one is available.</summary>
    public void DequeueLog(String logText, bool isError)
    {
        if (!NotificationsEnabled) return;
        if (logPool == null || logPool.Count == 0) return;

        LogText log = logPool.Dequeue();
        pooledLogs.Remove(log);
        log.SetText(logText, isError);
        log.gameObject.SetActive(true);
        log.StartLog(startPoint , new Vector2(endPoint.x , endPoint.y + (logDistanceY*activeLogs.Count)));
        activeLogs.Add(log);
    }

    /// <summary>Immediately recycles every notification currently on screen.</summary>
    public void CloseAllNotifications()
    {
        for (int i = activeLogs.Count - 1; i >= 0; i--)
            activeLogs[i].CloseImmediately();
    }
    

    /// <summary>Calls <see cref="LogText.UpdateLog"/> on every currently active log element.</summary>
    private void UpdateLogs()
    {
        for (int i = activeLogs.Count - 1; i >= 0; i--)
        {
            activeLogs[i].UpdateLog();
        }
    }

    /// <summary>Each frame, advances all active log animations via <see cref="UpdateLogs"/>.</summary>
    void Update()
    {
        UpdateLogs();
    }
}
