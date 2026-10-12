using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using ZXing;
using ZXing.Common;

/// <summary>
/// Singleton component that samples the AR camera's CPU luminance plane every N
/// frames and decodes it as a QR code using the ZXing library. When a code is
/// found the state machine transitions to
/// <see cref="StateMachine.States.QR_SCAN_COMPLETE_STATE"/> and the decoded URL
/// is available via <see cref="QrCode"/>.
/// </summary>
public class QRScanner : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField, Min(1)] private int scanIntervalFrames = 60;
    [SerializeField, Min(128)] private int maxScanDimension = 640;

    private byte[] luminanceBuffer;
    private Coroutine scanCoroutine;
    private Task<string> activeDecodeTask;
    private int scanSessionId;
    private int activeDecodeSessionId;

    /// <summary>The decoded QR code string, or <see cref="string.Empty"/> when no code has been found.</summary>
    public string QrCode { get; private set; }

    /// <summary>The single active instance of <see cref="QRScanner"/>.</summary>
    public static QRScanner Instance
    {
        get; private set;
    }

    IBarcodeReader m_BarcodeReader;

    /// <summary>Initialises the singleton instance.</summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Creates the ZXing barcode reader, subscribes to the QR scan state,
    /// and caches the screen resolution used for pixel capture.
    /// </summary>
    private void Start()
    {
        m_BarcodeReader = new BarcodeReader
        {
            AutoRotate = false,
            Options = new DecodingOptions
            {
                TryHarder = false,
                TryInverted = false,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<ARCameraManager>();

        StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE)
            .OnStateEnter += StartScanner;

        QrCode = string.Empty;
    }

    private void OnDestroy()
    {
        scanSessionId++;

        if (StateMachine.Instance != null)
            StateMachine.Instance.GetState(StateMachine.States.QR_SCAN_STATE).OnStateEnter -= StartScanner;

        StopScannerCoroutine();
    }

    /// <summary>
    /// Resets the scanner state and starts a new scan coroutine.
    /// Safe to call while a scan is already running.
    /// </summary>
    public void StartScanner()
    {
        StopScannerCoroutine();
        scanSessionId++;
        QrCode = string.Empty;
        scanCoroutine = StartCoroutine(ScanFrameForQRCode(scanIntervalFrames, scanSessionId));
    }

    /// <summary>
    /// Stops any active scan coroutine and clears the last decoded result.
    /// </summary>
    public void StopScanner()
    {
        scanSessionId++;
        QrCode = string.Empty;
        StopScannerCoroutine();
    }

    private void StopScannerCoroutine()
    {
        if (scanCoroutine == null) return;

        StopCoroutine(scanCoroutine);
        scanCoroutine = null;
    }

    /// <summary>
    /// Coroutine that captures a screen frame every <paramref name="waitframeCount"/>
    /// frames and attempts to decode it as a QR code. Stops automatically once a
    /// code is found or the state changes away from
    /// <see cref="StateMachine.States.QR_SCAN_STATE"/>.
    /// </summary>
    /// <param name="waitframeCount">Number of frames to skip between each decode attempt.</param>
    IEnumerator ScanFrameForQRCode(int waitframeCount, int sessionId)
    {
        while (sessionId == scanSessionId && string.IsNullOrEmpty(QrCode) &&
               StateMachine.Instance.CurrentStateName == StateMachine.States.QR_SCAN_STATE)
        {
            for (int i = 0; i < waitframeCount; i++)
                yield return null;

            if (sessionId != scanSessionId ||
                StateMachine.Instance.CurrentStateName != StateMachine.States.QR_SCAN_STATE)
                break;

            // A stopped coroutine cannot cancel an in-progress synchronous ZXing decode.
            // Wait for it before reusing the reader, and discard its result if it belongs
            // to an earlier scan session.
            if (activeDecodeTask != null)
            {
                Task<string> pendingTask = activeDecodeTask;
                while (!pendingTask.IsCompleted)
                    yield return null;

                if (ReferenceEquals(activeDecodeTask, pendingTask))
                    activeDecodeTask = null;

                if (pendingTask.IsFaulted)
                {
                    Debug.LogWarning(pendingTask.Exception?.GetBaseException().Message);
                }
                else if (pendingTask.Status == TaskStatus.RanToCompletion &&
                         activeDecodeSessionId == sessionId &&
                         StateMachine.Instance.CurrentStateName == StateMachine.States.QR_SCAN_STATE)
                {
                    QrCode = pendingTask.Result;
                }

                if (!string.IsNullOrEmpty(QrCode))
                    break;

                // The completed task may have come from a previous scan session.
                if (activeDecodeSessionId != sessionId)
                    continue;
            }

            try
            {
                activeDecodeSessionId = sessionId;
                activeDecodeTask = DecodeLatestCameraImage();
            }
            catch (Exception ex) { Debug.LogWarning(ex.Message); }

            if (activeDecodeTask != null)
            {
                Task<string> pendingTask = activeDecodeTask;
                while (!pendingTask.IsCompleted)
                    yield return null;

                if (ReferenceEquals(activeDecodeTask, pendingTask))
                    activeDecodeTask = null;

                if (pendingTask.IsFaulted)
                {
                    Debug.LogWarning(pendingTask.Exception?.GetBaseException().Message);
                }
                else if (pendingTask.Status == TaskStatus.RanToCompletion &&
                         sessionId == scanSessionId &&
                         StateMachine.Instance.CurrentStateName == StateMachine.States.QR_SCAN_STATE)
                {
                    QrCode = pendingTask.Result;
                }
            }
        }

        if (sessionId == scanSessionId)
            scanCoroutine = null;

        if (sessionId == scanSessionId && !string.IsNullOrEmpty(QrCode) &&
            StateMachine.Instance.CurrentStateName == StateMachine.States.QR_SCAN_STATE)
        {
            StateMachine.Instance.SetState(StateMachine.States.QR_SCAN_COMPLETE_STATE);
        }
    }

    /// <summary>
    /// Captures a downsampled grayscale CPU camera image on the main thread,
    /// then schedules ZXing decoding of the copied pixels on a worker. Reading
    /// the luminance plane avoids full-resolution GPU readback, Texture2D
    /// allocation, and ARGB conversion on every scan attempt.
    /// </summary>
    private Task<string> DecodeLatestCameraImage()
    {
        if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return Task.FromResult(string.Empty);

        try
        {
            if (image.planeCount == 0) return Task.FromResult(string.Empty);

            XRCpuImage.Plane luminancePlane = image.GetPlane(0);
            int largestDimension = Mathf.Max(image.width, image.height);
            int sampleStep = Mathf.Max(1, Mathf.CeilToInt(largestDimension / (float)maxScanDimension));
            int scanWidth = Mathf.Max(1, image.width / sampleStep);
            int scanHeight = Mathf.Max(1, image.height / sampleStep);
            int requiredLength = scanWidth * scanHeight;

            // Only one decode task may be active; the coroutine waits for it to finish
            // before the next capture, so this buffer can be reused safely.
            if (luminanceBuffer == null || luminanceBuffer.Length != requiredLength)
                luminanceBuffer = new byte[requiredLength];

            for (int y = 0; y < scanHeight; y++)
            {
                int sourceRow = y * sampleStep * luminancePlane.rowStride;
                int destinationRow = y * scanWidth;

                for (int x = 0; x < scanWidth; x++)
                {
                    int sourceIndex = sourceRow + x * sampleStep * luminancePlane.pixelStride;
                    luminanceBuffer[destinationRow + x] = luminancePlane.data[sourceIndex];
                }
            }

            IBarcodeReader barcodeReader = m_BarcodeReader;
            byte[] pixelBuffer = luminanceBuffer;
            return Task.Run(() => DecodeLuminance(barcodeReader, pixelBuffer, scanWidth, scanHeight));
        }
        finally
        {
            image.Dispose();
        }
    }

    /// <summary>
    /// Runs ZXing against a managed buffer. This method intentionally uses no
    /// Unity or AR Foundation APIs and is safe to execute on a worker thread.
    /// </summary>
    private static string DecodeLuminance(IBarcodeReader barcodeReader, byte[] luminanceBuffer, int width, int height)
    {
        Result result = barcodeReader.Decode(
            luminanceBuffer,
            width,
            height,
            RGBLuminanceSource.BitmapFormat.Gray8);
        return result?.Text ?? string.Empty;
    }
}
