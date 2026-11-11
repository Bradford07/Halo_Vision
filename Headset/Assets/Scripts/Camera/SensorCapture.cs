// Author: Cal Peters
// Finds and activates the HoloLens 2 colored camera using window API dependencies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;

// Enable live camera APIs
// Anything inside #if ENABLE_WIND_SUPPORT will not run outside the headset as it requires Microsoft's UWP 
#if ENABLE_WINMD_SUPPORT
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.MediaProperties;
#endif

public class SensorCapture : MonoBehaviour
{
    public RawImage cameraPreview;
    public TextMeshProUGUI cameraStatus;

    // Limit status updates to once per second
    private float nextStatusUpdateTime = 0f;
    private string statusMessage = "Camera Status: Waiting...";

    #if ENABLE_WINMD_SUPPORT
        // HoloLens camera capture components
        private MediaCapture mediaCapture;
        private MediaFrameReader frameReader;
        private MediaFrameSource cameraSource;

        private bool isInitializing = false;
        private bool isShuttingDown = false;

        private int frameCount = 0;
        private int nullFrameCount = 0;
        private int bitmapCount = 0;

        // Camera initialization and cleanup can occur on different threads
        // Protects shared camera state from concurrent changes
        private readonly object cameraLock = new object();
    #endif

    // Start is called before the first frame update
    void Start()
    {
        if(cameraPreview != null)
        {
            cameraPreview.color = Color.white;
        }
        
        #if ENABLE_WINMD_SUPPORT
            InitializeCamera();
        #else
            statusMessage = "Camera Status: UWP required";
        #endif

    }

    // Update is called once per frame
    // UI updates here so it stays on main thread
    void Update()
    {   
        // Refresh diagnostic text once per second instead of every frame.
        if(Time.unscaledTime < nextStatusUpdateTime)
            return;

        nextStatusUpdateTime = Time.unscaledTime + 1f;

        if(cameraStatus == null) return;

        // Volatile.Read allows reading values from other threads safely
        #if ENABLE_WINMD_SUPPORT
            cameraStatus.text =
                Volatile.Read(ref statusMessage) +
                "\nValid Frames: " + Volatile.Read(ref frameCount) +
                "\nNull Frames: " + Volatile.Read(ref nullFrameCount) +
                "\nSoftwareBitmaps: " + Volatile.Read(ref bitmapCount);
        #else
            cameraStatus.text = Volatile.Read(ref statusMessage);
        #endif

    }

    // Initialize the live camera and display its status
    #if ENABLE_WINMD_SUPPORT
    private void InitializeCamera()
    {

        // Prevent duplicate initialization or startup druing shutdown
        lock(cameraLock)
        {
            if(isInitializing || isShuttingDown || mediaCapture != null)
            {
                return;
            }

            isInitializing = true;
        }
        Volatile.Write(ref statusMessage, "Camera Status: Initializing...");

        MediaCapture capture = null;

        try
        {
            capture = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                // CPU memory allows inspection of SoftwareBitmap frames
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            lock(cameraLock)
            {
                mediaCapture = capture;
            }

            // Camera initialization is asynchronous and must finish before searching for camera sources
            var initialization = mediaCapture.InitializeAsync(settings);


            initialization.Completed = (operation, status) =>
            {
          
                bool shouldDispose;

                lock(cameraLock)
                {
                    isInitializing = false;

                    // Check if camera was shut down while initializing
                    shouldDispose = isShuttingDown || status != AsyncStatus.Completed;

                    if(shouldDispose)
                    {
                        mediaCapture = null;
                    }
                }

                if(shouldDispose)
                {
                    capture.Dispose();
                    if(status != AsyncStatus.Completed)
                    {
                        Volatile.Write(ref statusMessage, "Camera Error: Initialization failed");
                    }
                }
                else
                {
                    // Initialization succeeded now locate a compatible color camera
                    FindCameraSource();
                }
            };

        }
        catch(System.Exception ex)
        {
            lock(cameraLock)
            {
                isInitializing = false;
                mediaCapture = null;
            }

            if(capture != null)
                capture.Dispose();

            Volatile.Write(ref statusMessage, "Camera Error: " + ex.Message);
        }
    }



    private void FindCameraSource()
    {
        // Find a color camera source that supports the NV12 format
        foreach(var source in mediaCapture.FrameSources.Values)
        {
            if(source.Info.SourceKind != MediaFrameSourceKind.Color)
                continue;

            foreach(var format in source.SupportedFormats)
            {
                if(format.VideoFormat == null)
                    continue;

                if(!string.Equals(format.Subtype, MediaEncodingSubtypes.Nv12, System.StringComparison.OrdinalIgnoreCase)) 
                    continue;
                
                //Use smaller camera format to lessen memory size from higher resolutions
                if(format.VideoFormat.Width == 896 && format.VideoFormat.Height == 504)
                {
                    cameraSource = source;

                    Volatile.Write(ref statusMessage, "Camera Status: Selecting 896x504...");

                    try
                    {
                        // Configure the selected camera format asynchronously 
                        var operation = source.SetFormatAsync(format);

                        operation.Completed = (asyncOperation, status) =>
                        {
                            if(status == AsyncStatus.Completed)
                            {
                                // Shutdown may have occured during formatting
                                lock(cameraLock)
                                {
                                    if(isShuttingDown)
                                        return;
                                }

                                CreateFrameReader();
                            }
                            else
                            {
                                Volatile.Write(ref statusMessage, "Camera Error: Format selection failed");
                            }
                        };
                    }
                    catch (System.Exception ex)
                    {
                        Volatile.Write(ref statusMessage, "Format Error: " + ex.Message);
                    }

                    return;
                }
            }
        }

        Volatile.Write(ref statusMessage,"Camera Error: 896x504 NV12 not found");
    }

    private void CreateFrameReader()
    {
        Volatile.Write(ref statusMessage, "Camera Status: Creating frame reader...");

        try
        {
            // Create a reader for the selected camera source
            var operation = mediaCapture.CreateFrameReaderAsync(cameraSource);

            operation.Completed = (asyncOperation, status) =>
            {
                if(status != AsyncStatus.Completed)
                {
                    Volatile.Write(ref statusMessage, "Camera Error: Failed to create frame reader");
                    return;
                }

                MediaFrameReader reader = null;

                try
                {
                    reader = asyncOperation.GetResults();

                    lock(cameraLock)
                    {
                        // Check if shutdown was initiated drung frame reader creation
                        if(isShuttingDown)
                        {
                            reader.Dispose();
                            return;
                        }

                        frameReader = reader;
                    }

                    // Register the callback that receives incoming camera frames
                    reader.FrameArrived += OnFrameArrived;

                    StartFrameReader(reader);
                }
                catch(System.Exception ex)
                {
                    Volatile.Write(ref statusMessage, "Reader Error: " + ex.Message);
                }
            };
        }
        catch(System.Exception ex)
        {
            Volatile.Write(ref statusMessage, "Reader Error: " + ex.Message);
        }
    }

    private void StartFrameReader(MediaFrameReader reader)
    {
        Volatile.Write(ref statusMessage, "Camera Status: Starting frame reader...");

        try
        {
            // Start receiving camera frames asynchronously
            var operation = reader.StartAsync();

            operation.Completed = (asyncOperation, status) =>
            {
                // Operation succesful at API level
                if(status != AsyncStatus.Completed)
                {
                    Volatile.Write(ref statusMessage, "Camera Error: Failed to start reader");
                    return;
                }

                var result = asyncOperation.GetResults();

                // Frame reader started successfully
                if(result == MediaFrameReaderStartStatus.Success)
                {
                    Volatile.Write(ref statusMessage,"Camera Status: Reader started!");
                }
                else
                {
                    Volatile.Write(ref statusMessage,"Camera Error: Reader start status " + result);
                }
            };
        }
        catch(System.Exception ex)
        {
            Volatile.Write(ref statusMessage,"Reader Error: " + ex.Message);
        }
    }

    // Currently verifying successful receiving of camera data
    // Swap to displaying frames later
    private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // Get the newest available frame from the camera
        using (var frame = sender.TryAcquireLatestFrame())
        {
            if(frame == null)
            {
                // Safely increment frame counts
                Interlocked.Increment(ref nullFrameCount);
                return;
            }

            Interlocked.Increment(ref frameCount);

            var videoFrame = frame.VideoMediaFrame;

            if(videoFrame == null)
                return;

            SoftwareBitmap bitmap = videoFrame.SoftwareBitmap;

            if(bitmap == null)
                return;

            // Check bitmap and release it after
            // Bitmaps will be used for image rendering later
            // Use "using" to ensure Dispose() is called after block finishes
            using (bitmap)
            {
                int count = Interlocked.Increment(ref bitmapCount);

                // Record the camera format after receiving 30 valid bitmaps
                // Used to make sure bitmaps were being received
                if(count == 30)
                {
                    Volatile.Write(ref statusMessage,
                        "Camera Format: " + bitmap.BitmapPixelFormat +
                        "\nResolution: " + bitmap.PixelWidth + " x " + bitmap.PixelHeight
                    );
                }
            }
        }
    }


    #endif

    // When the GameObject is destroyed, release the headset camera
    void OnDestroy()
    {
        #if ENABLE_WINMD_SUPPORT
            MediaCapture captureToDispose = null;
            MediaFrameReader readerToDispose = null;

            lock(cameraLock)
            {
                // Prevent async camera operations from continuing during shutdown
                isShuttingDown = true;

                readerToDispose = frameReader;
                frameReader = null;

                if(!isInitializing)
                {
                    captureToDispose = mediaCapture;
                    mediaCapture = null;
                }
            }

            if(readerToDispose != null)
            {
                // Stop getting frame events before releasing reader
                readerToDispose.FrameArrived -= OnFrameArrived;
                readerToDispose.Dispose();
            }

            if(captureToDispose != null)
            {
                captureToDispose.Dispose();
            }
        #endif
    }
}
