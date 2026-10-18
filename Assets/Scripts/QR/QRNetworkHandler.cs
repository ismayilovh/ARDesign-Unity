using GLTFast;
using GLTFast.Logging;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton that downloads a glTF/GLB model from the URL encoded in the
/// scanned QR code and loads it into the scene using GLTFast. Shows a
/// progress-bar popup during download and an error popup on failure.
/// On success it sets <see cref="DesignManager.Design"/> and transitions to
/// <see cref="StateMachine.States.MODEL_VIEW_STATE"/>.
/// </summary>
public class QRNetworkHandler : MonoBehaviour
{
    GameObject m_QRModelDownloadScreen;
    StateMachine m_StateMachine;

    /// <summary>Temporary reference to the loaded glTF scene root while it is prepared as <see cref="DesignManager.Design"/>.</summary>
    public GameObject LoadedModel;

    private ObjectSpawnerAR _objectSpawnerAR;

    private const string ProgressBarPath = "Progress Bar";
    private GameObject m_ProgressBarPrefab;
    private QRProgressBarManager m_ProgressBarManager;
    private float m_LastDisplayedProgress = -1f;
    private GltfImport m_LoadedGltf;
    private int m_LoadGeneration;

    /// <summary>Transform under which the GLTFast importer instantiates the model temporarily.</summary>
    public Transform ModelLoadTransform;

    /// <summary>The single active instance of <see cref="QRNetworkHandler"/>.</summary>
    public static QRNetworkHandler Instance
    {
        get; private set;
    }

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        m_LoadGeneration++;
        m_LoadedGltf?.Dispose();
        m_LoadedGltf = null;
    }

    /// <summary>
    /// Sets up the model load transform, wires state-machine callbacks to trigger
    /// download and hide the progress screen, and pre-loads the progress bar prefab.
    /// </summary>
    private void Start()
    {
        ModelLoadTransform = transform;
        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_COMPLETE_STATE).
            OnStateEnter += () => StartDownloadingModel();

        StateMachine.Instance.GetState(StateMachine.States.MODEL_VIEW_STATE).
            OnStateEnter += () => { if (m_QRModelDownloadScreen != null) m_QRModelDownloadScreen.SetActive(false); };

        StateMachine.Instance.GetState(StateMachine.States.NO_MODEL_VIEW_STATE).
            OnStateEnter += () => { if (m_QRModelDownloadScreen != null) m_QRModelDownloadScreen.SetActive(false); };

        _objectSpawnerAR = FindAnyObjectByType<ObjectSpawnerAR>();
        LoadedModel = null;

        m_ProgressBarPrefab = Resources.Load<GameObject>(ProgressBarPath);

    }

    /// <summary>
    /// Shows the download-progress popup, creating it on first call.
    /// </summary>
    public void DisplayModelDownloadScreen()
    {
        if (m_QRModelDownloadScreen != null)
        {
            m_QRModelDownloadScreen.SetActive(true);
            m_LastDisplayedProgress = -1f;
            OnDownloadProgress(0f);
            return;
        }

        PopupBuilder builder = PopupBuilder.Create();

        GameObject progressBar = Instantiate(m_ProgressBarPrefab);
        progressBar.SetActive(false);
        m_ProgressBarManager = progressBar.GetComponent<QRProgressBarManager>();
        m_LastDisplayedProgress = -1f;

        m_QRModelDownloadScreen = builder
            .WithHeader("DOWNLOADING MODEL")
            .WithFooterButton("Cancel", PopupWindow.BUTTON_COLOR_CANCEL, CancelDownload)
            .WithContent(progressBar)
            .Get();
    }

    /// <summary>
    /// Resets handler state and starts downloading the model whose URL was
    /// decoded by <see cref="QRScanner"/>.
    /// </summary>
    public void StartDownloadingModel()
    {
        ResetNetworkHandler();
        int loadGeneration = ++m_LoadGeneration;
        StartCoroutine(DownloadModel(QRScanner.Instance.QrCode, loadGeneration));
    }

    /// <summary>
    /// Stops all download coroutines, returns to <see cref="StateMachine.States.NO_MODEL_VIEW_STATE"/>,
    /// and destroys the download popup. The popup is destroyed (not hidden) because
    /// either a progress or error variant may exist.
    /// </summary>
    private void CancelDownload()
    {
        m_LoadGeneration++;
        StopAllCoroutines();
        StateMachine.Instance.SetState(StateMachine.States.NO_MODEL_VIEW_STATE);
        Destroy(m_QRModelDownloadScreen);
    }

    /// <summary>
    /// Updates the progress bar slider and percentage label during an active download.
    /// </summary>
    /// <param name="progress">Download progress in the range 0–1.</param>
    private void OnDownloadProgress(float progress)
    {
        if (m_ProgressBarManager == null ||
            (progress < 1f && progress - m_LastDisplayedProgress < 0.005f))
            return;

        m_LastDisplayedProgress = progress;
        m_ProgressBarManager.Slider.value = progress;
        m_ProgressBarManager.ProgressText.SetText("{0:F2}%", progress * 100f);
    }

    /// <summary>Clears the <see cref="LoadedModel"/> reference to prepare for a new download.</summary>
    private void ResetNetworkHandler()
    {
        LoadedModel = null;
        m_LoadedGltf?.Dispose();
        m_LoadedGltf = null;
    }

    /// <summary>
    /// Asynchronously parses raw GLB bytes with GLTFast, instantiates the scene
    /// under this transform, prepares it as the inactive
    /// <see cref="DesignManager.Design"/> template, and adds <see cref="GenerateLOD"/>.
    /// </summary>
    /// <param name="data">Raw GLB file bytes returned by the web request.</param>
    private async Task<bool> LoadGLBObject(byte[] data, int loadGeneration)
    {
        var logger = new CollectingLogger();
        var gltf = new GltfImport(logger: logger);
        bool keepImporter = false;
        Transform spawnParent = transform;
        int childCountBeforeImport = spawnParent.childCount;

        try
        {
            bool success = await gltf.Load(data);
            if (loadGeneration != m_LoadGeneration)
                return false;

            if (success)
            {
                success = await gltf.InstantiateMainSceneAsync(spawnParent);
                if (loadGeneration != m_LoadGeneration)
                    return false;

                if (success && spawnParent.childCount > childCountBeforeImport)
                {
                    Debug.Log("Model loaded successfully!");
                    LoadedModel = spawnParent.GetChild(childCountBeforeImport).gameObject;

                    // Prepare the inactive template before publishing it. Existing
                    // imported LODGroups are reused; otherwise simplification runs
                    // on a worker so it does not block the main thread.
                    if (!LoadedModel.TryGetComponent<GenerateLOD>(out _))
                        LoadedModel.AddComponent<GenerateLOD>();

                    GenerateLOD lodGenerator = LoadedModel.GetComponent<GenerateLOD>();
                    LoadedModel.SetActive(false);

                    await lodGenerator.GenerateAsync();
                    if (loadGeneration != m_LoadGeneration)
                        return false;

                    DesignManager.Instance.Design = LoadedModel;
                    m_LoadedGltf = gltf;
                    keepImporter = true;
                    LoadedModel = null;

                    return true;
                }
            }

            if (logger.Items != null)
            {
                foreach (var item in logger.Items)
                {
                    Debug.LogError(item.ToString());
                }
            }

            Debug.LogError("Model load failed");
            return false;
        }
        finally
        {
            if (!keepImporter)
            {
                DestroyImportedChildren(spawnParent, childCountBeforeImport);
                gltf.Dispose();
            }
        }
    }

    private static void DestroyImportedChildren(Transform parent, int firstImportedChild)
    {
        for (int i = parent.childCount - 1; i >= firstImportedChild; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Destroys the progress popup and replaces it with an error popup that
    /// displays the failure message and an OK button to dismiss.
    /// </summary>
    /// <param name="error">The error message from the failed web request.</param>
    private void OnDownloadError(string error)
    {
        Debug.LogError("Error: " + error);

        Destroy(m_QRModelDownloadScreen);

        PopupBuilder builder = PopupBuilder.Create();

        m_QRModelDownloadScreen = builder
            .WithHeader("ERROR")
            .WithFooterButton("OK", PopupWindow.BUTTON_COLOR_OK, CancelDownload)
            .WithContentText(string.Format("An error occured: {0}", error))
            .Get();
    }

    /// <summary>
    /// Coroutine that downloads the model at <paramref name="uri"/>, streams progress
    /// to the UI, then calls
    /// <see cref="LoadGLBObject"/> before transitioning to
    /// <see cref="StateMachine.States.MODEL_VIEW_STATE"/>.
    /// </summary>
    /// <param name="uri">The URL of the GLB file to download.</param>
    private IEnumerator DownloadModel(string uri, int loadGeneration)
    {
        DisplayModelDownloadScreen();
        
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            www.SendWebRequest();
            
            while (www.downloadProgress < 1)
            {
                OnDownloadProgress(www.downloadProgress);
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError ||
                www.result == UnityWebRequest.Result.DataProcessingError)
            {
                OnDownloadError(www.error);
            } else
            {
                byte[] modelData = www.downloadHandler.data;
                Task<bool> loadTask = LoadGLBObject(modelData, loadGeneration);

                while (!loadTask.IsCompleted)
                {
                    yield return null;
                }

                if (loadGeneration != m_LoadGeneration)
                    yield break;

                if (loadTask.IsFaulted)
                {
                    OnDownloadError(loadTask.Exception?.GetBaseException().Message ?? "Model load failed");
                }
                else if (loadTask.Result)
                {
                    StateMachine.Instance.SetState(StateMachine.States.MODEL_VIEW_STATE);
                }
                else
                {
                    OnDownloadError("Model load failed");
                }
            }
        }        
    }
}
