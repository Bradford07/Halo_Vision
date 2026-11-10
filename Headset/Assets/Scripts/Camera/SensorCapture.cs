using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;

// Enable live camera api's
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

    private Texture2D cameraTexture;
    private byte[] latestFramePixels;
    private int frameWidth;
    private int frameHeight;
    private readonly object frameLock = new object();
    private bool newFrameAvailable = false;
    private float nextStatusUpdateTime = 0f;
    private string statusMessage = "Camera Status: Waiting...";

    #if ENABLE_WINMD_SUPPORT
        private MediaCapture mediaCapture;
        private MediaFrameReader frameReader;
        private MediaFrameSource cameraSource;
        private bool isInitializing = false;
        private bool isShuttingDown = false;
        private int frameCount = 0;
        private int nullFrameCount = 0;
        private int bitmapCount = 0;
        private string[] supportedNv12Resolutions = new string[0];

        // Protects camera state shared with the Windows callback
        private readonly object cameraLock = new object();
    #endif

    // Start is called before the first frame update
    void Start()
    {
        cameraPreview.color = Color.white;

        #if ENABLE_WINMD_SUPPORT
            InitializeCamera();
        #else
            statusMessage = "Camera Status: UWP required";
        #endif

    }

    // Update is called once per frame

    void Update()
    {
        if (Time.unscaledTime < nextStatusUpdateTime)
            return;

        nextStatusUpdateTime = Time.unscaledTime + 1f;


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
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            lock(cameraLock)
            {
                mediaCapture = capture;
            }

            var initialization = mediaCapture.InitializeAsync(settings);


            initialization.Completed = (operation, status) =>
            {
          
                bool shouldDispose;

                lock(cameraLock)
                {
                    isInitializing = false;
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
        foreach (var source in mediaCapture.FrameSources.Values)
        {
            if (source.Info.SourceKind != MediaFrameSourceKind.Color)
                continue;

            foreach (var format in source.SupportedFormats)
            {
                if (format.VideoFormat == null)
                    continue;

                if (!string.Equals(
                    format.Subtype,
                    MediaEncodingSubtypes.Nv12,
                    System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (format.VideoFormat.Width == 896 &&
                    format.VideoFormat.Height == 504)
                {
                    cameraSource = source;

                    Volatile.Write(
                        ref statusMessage,
                        "Camera Status: Selecting 896x504..."
                    );

                    try
                    {
                        var operation = source.SetFormatAsync(format);

                        operation.Completed = (asyncOperation, status) =>
                        {
                            if (status == AsyncStatus.Completed)
                            {
                                lock (cameraLock)
                                {
                                    if (isShuttingDown)
                                        return;
                                }

                                CreateFrameReader();
                            }
                            else
                            {
                                Volatile.Write(
                                    ref statusMessage,
                                    "Camera Error: Format selection failed"
                                );
                            }
                        };
                    }
                    catch (System.Exception ex)
                    {
                        Volatile.Write(
                            ref statusMessage,
                            "Format Error: " + ex.Message
                        );
                    }

                    return;
                }
            }
        }

        Volatile.Write(
            ref statusMessage,
            "Camera Error: 896x504 NV12 not found"
        );
    }




    // private void FindCameraSource()
    // {
    //     foreach(var source in mediaCapture.FrameSources.Values)
    //     {
    //         if(source.Info.SourceKind == MediaFrameSourceKind.Color)
    //         {
    //             cameraSource = source;
    //             Volatile.Write(ref statusMessage, "Camera Status: Color source found");
                
    //             CreateFrameReader();
                
    //             return;
    //         }
    //     }

    //     Volatile.Write(ref statusMessage, "Camera Error: No color source found");
    // }

    private void CreateFrameReader()
    {
        Volatile.Write(ref statusMessage, "Camera Status: Creating frame reader...");

        try
        {
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
                        if(isShuttingDown)
                        {
                            reader.Dispose();
                            return;
                        }

                        frameReader = reader;
                    }

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
            var operation = reader.StartAsync();

            operation.Completed = (asyncOperation, status) =>
            {
                if(status != AsyncStatus.Completed)
                {
                    Volatile.Write(ref statusMessage, "Camera Error: Failed to start reader");
                    return;
                }

                var result = asyncOperation.GetResults();

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


    private void OnFrameArrived(
        MediaFrameReader sender,
        MediaFrameArrivedEventArgs args)
    {
        using (var frame = sender.TryAcquireLatestFrame())
        {
            if (frame == null)
            {
                Interlocked.Increment(ref nullFrameCount);
                return;
            }

            Interlocked.Increment(ref frameCount);

            var videoFrame = frame.VideoMediaFrame;

            if (videoFrame == null)
                return;

            SoftwareBitmap bitmap = videoFrame.SoftwareBitmap;

            if (bitmap == null)
                return;

            using (bitmap)
            {
                int count = Interlocked.Increment(ref bitmapCount);

                if (count == 30)
                {
                    Volatile.Write(
                        ref statusMessage,
                        "Camera Format: " + bitmap.BitmapPixelFormat +
                        "\nResolution: " + bitmap.PixelWidth +
                        " x " + bitmap.PixelHeight
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
