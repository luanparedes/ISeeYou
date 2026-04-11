using ISeeYou.AI;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using ISeeYou.Core.Services;

namespace ISeeYou.MainApp.ViewModels
{
    public class HomePageViewModel : ObservableObject
    {
        #region Constants

        private const uint Width = 1920;
        private const uint Height = 1080;
        private const uint BytePerPixel = 4;
        private const uint Pitch = Width * BytePerPixel;
        private const uint Lines = Height;

        #endregion

        #region Fields

        private YoloDetector _yoloDetector;
        private LibVLC _libVLC;
        private MediaPlayer _player;
        private DispatcherQueue _dispatcherQueue;

        private bool isPaused;
        private IntPtr _bufferPtr = IntPtr.Zero;
        private byte[] _managedBuffer;
        private object _lockObject;
        private readonly object _detectionLock = new object();

        private WriteableBitmap _bitmap;
        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            set => SetProperty(ref _bitmap, value);
        }

        #endregion

        #region Services

        private readonly ILogService _applicationLog;

        #endregion

        #region Constructors

        public HomePageViewModel(ILogService applicationLog)
        {
            _applicationLog = applicationLog;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bitmap = new WriteableBitmap((int)Width, (int)Height);

            _lockObject = new object();

            string modelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "yolov8n.onnx");
            string namesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "coco.names");

            _yoloDetector = new YoloDetector(modelPath, namesPath);
            this.InitializeVlc();
        }

        #endregion

        #region Private Methods

        //private void Display(IntPtr opaque, IntPtr picture)
        //{
        //    if (isPaused || _dispatcherQueue == null) return;

        //    int byteCount = (int)(Pitch * Lines);

        //    Marshal.Copy(_bufferPtr, _managedBuffer, 0, byteCount);

        //    _yoloDetector.Detect((int)Height, (int)Width, _bufferPtr, (int)Pitch);

        //    Marshal.Copy(_bufferPtr, _managedBuffer, 0, byteCount);

        //    _dispatcherQueue.TryEnqueue(() =>
        //    {
        //        lock (_lockObject)
        //        {
        //            try
        //            {
        //                using (var stream = _bitmap.PixelBuffer.AsStream())
        //                {
        //                    stream.Position = 0;
        //                    stream.Write(_managedBuffer, 0, byteCount);
        //                }
        //                _bitmap.Invalidate();
        //            }
        //            catch (Exception ex) { Debug.WriteLine("Erro render: " + ex.Message); }
        //        }
        //    });
        //}

        private void Display(IntPtr opaque, IntPtr picture)
        {
            if (isPaused || _dispatcherQueue == null)
                return;

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
                        OnPropertyChanged(nameof(Bitmap));
                    }
                    catch (Exception ex) { Debug.WriteLine("Erro: " + ex.Message); }
                }
            });
        }

        private void InitializeVlc()
        {
            _applicationLog.Info("Initializing VLC...");

            string libPath = System.IO.Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64");

            LibVLCSharp.Shared.Core.Initialize("C:\\Developer\\Softwares\\C#\\ISeeYou\\ISeeYou (Package)\\bin\\x64\\Debug\\AppX\\libvlc\\win-x64");

            _bufferPtr = Marshal.AllocHGlobal((int)(Pitch * Lines));
            _managedBuffer = new byte[(int)(Pitch * Lines)];
            _libVLC = new LibVLC("--no-osd", "--embedded-video", "--rtsp-tcp");

            _player = new MediaPlayer(_libVLC);
            _player.SetVideoFormat("RV32", Width, Height, Pitch);
            _player.SetVideoCallbacks(Lock, null, Display);

            string rtspUrl = "rtsp://admin:Paredes2026@192.168.0.5:8554/Streaming/Channels/101";
            var media = new Media(_libVLC, new Uri(rtspUrl));

            _applicationLog.Info("Starting video playback...");

            _player.Play(media);

            _applicationLog.Info("VLC initialized and video playback started.");
        }

        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            if (_bufferPtr == IntPtr.Zero) 
                return IntPtr.Zero;

            Marshal.WriteIntPtr(planes, _bufferPtr);

            return _bufferPtr;
        }

        #endregion

        #region Handlers

        public void ButtonPlayPauseClick(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            _applicationLog.Info(isPaused ? "Video paused." : "Video resumed.");
        }

        public void ButtonMuteDesmuteClick(object sender, RoutedEventArgs e)
        {
            _player.Mute = !_player.Mute;
            _applicationLog.Info(_player.Mute ? "Video muted." : "Video unmuted.");
        }

        #endregion
    }
}
