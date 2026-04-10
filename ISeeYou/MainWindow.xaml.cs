using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using ISeeYou.AI;

namespace ISeeYou
{
    public sealed partial class MainWindow : Window
    {
        #region Constants

        private const uint Width = 1920;
        private const uint Height = 1080;
        private const uint BytePerPixel = 4; // RGBA
        private const uint Pitch = Width * BytePerPixel;
        private const uint Lines = Height;

        #endregion

        #region Fields
        
        private YoloDetector _yoloDetector;
        private LibVLC _libVLC;
        private MediaPlayer _player;
        private DispatcherQueue _dispatcherQueue;
        private WriteableBitmap _bitmap;

        private bool isPaused;
        private IntPtr _bufferPtr = IntPtr.Zero;
        private byte[] _managedBuffer;
        private object _lockObject;
        private readonly object _detectionLock = new object();

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bitmap = new WriteableBitmap((int)Width, (int)Height);

            CameraStreamImage.Source = _bitmap;

            _lockObject = new object();

            string modelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "yolov8n.onnx");
            string namesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "coco.names");

            _yoloDetector = new YoloDetector(modelPath, namesPath);
            this.InitializeVlc();
        }

        #endregion

        #region Private Methods

        private void Display(IntPtr opaque, IntPtr picture)
        {
            if (isPaused || _dispatcherQueue == null) return;

            int byteCount = (int)(Pitch * Lines);

            _yoloDetector.Detect((int)Height, (int)Width, _bufferPtr, (int)Pitch);

            _dispatcherQueue.TryEnqueue(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        Marshal.Copy(_bufferPtr, _managedBuffer, 0, byteCount);

                        using (var stream = _bitmap.PixelBuffer.AsStream())
                        {
                            stream.Write(_managedBuffer, 0, byteCount);
                        }
                        _bitmap.Invalidate();
                    }
                    catch (Exception ex) { Debug.WriteLine("Erro: " + ex.Message); }
                }
            });
        }

        private void InitializeVlc()
        {
            string libPath = System.IO.Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64");

            Core.Initialize("C:\\Developer\\Softwares\\C#\\ISeeYou\\ISeeYou (Package)\\bin\\x64\\Debug\\AppX\\libvlc\\win-x64");

            _bufferPtr = Marshal.AllocHGlobal((int)(Pitch * Lines));
            _managedBuffer = new byte[(int)(Pitch * Lines)];
            _libVLC = new LibVLC("--no-osd", "--embedded-video", "--rtsp-tcp");

            _player = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _player.SetVideoFormat("RV32", Width, Height, Pitch);
            _player.SetVideoCallbacks(Lock, null, Display);

            string rtspUrl = "rtsp://admin:Paredes2026@192.168.0.5:8554/Streaming/Channels/101";
            var media = new Media(_libVLC, new Uri(rtspUrl));

            _player.Play(media);
        }

        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            if (_bufferPtr == IntPtr.Zero) return IntPtr.Zero;

            Marshal.WriteIntPtr(planes, _bufferPtr);

            return _bufferPtr;
        }

        #endregion

        #region Handlers

        private void ButtonPlayPauseClick(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
        }

        private void ButtonMuteDesmuteClick(object sender, RoutedEventArgs e)
        {
            _player.Mute = !_player.Mute;
        }

        #endregion
    }
}