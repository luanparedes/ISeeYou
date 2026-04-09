using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ISeeYou
{
    public sealed partial class MainWindow : Window
    {
        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _player;
        private DispatcherQueue _dispatcherQueue;
        private WriteableBitmap _bitmap;
        private bool isPaused;
        private IntPtr _bufferPtr = IntPtr.Zero; // Nosso "Canvas" na memória
        private object _lockObject;
        private byte[] _managedBuffer; // Reusable managed buffer to avoid per-frame allocations
        private const uint Width = 1920;
        private const uint Height = 1080;
        private const uint BytePerPixel = 4; // RGBA
        private const uint Pitch = Width * BytePerPixel;
        private const uint Lines = Height;

        public MainWindow()
        {
            InitializeComponent();

            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _bitmap = new WriteableBitmap((int)Width, (int)Height);

            CameraStreamImage.Source = _bitmap;

            _lockObject = new object();

            this.InitializeVlc();
        }

        private void InitializeVlc()
        {
            string libPath = System.IO.Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64");

            Core.Initialize("C:\\Developer\\Softwares\\C#\\ISeeYou\\ISeeYou (Package)\\bin\\x64\\Debug\\AppX\\libvlc\\win-x64");

            _bufferPtr = Marshal.AllocHGlobal((int)(Pitch * Lines));
            _managedBuffer = new byte[(int)(Pitch * Lines)];
            _libVLC = new LibVLC("--no-osd", "--embedded-video", "--rtsp-tcp");

            _player = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _player.SetVideoFormat("RGBA", Width, Height, Pitch);
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



        private void Display(IntPtr opaque, IntPtr picture)
        {
            if (isPaused || _dispatcherQueue == null) 
                return;

            int byteCount = (int)(Pitch * Lines);

            _dispatcherQueue.TryEnqueue(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        Marshal.Copy(_bufferPtr, _managedBuffer, 0, byteCount);

                        using (var stream = _bitmap.PixelBuffer.AsStream())
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.Write(_managedBuffer, 0, byteCount);
                        }

                        _bitmap.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Erro no frame: " + ex.Message);
                    }
                }
            });
        }

        private void ButtonPlayPauseClick(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
        }

        private void ButtonMuteDesmuteClick(object sender, RoutedEventArgs e)
        {
            _player.Mute = !_player.Mute;
        }
    }
}

public class CameraInfo
{
    public string? Name { get; set; }
    public string? Model { get; set; }
    public string? RemoteAddress { get; set; } // IP from discovery if available
    public IEnumerable<string> XAddrs { get; set; } = Array.Empty<string>();
    public string? DeviceServiceUrl { get; set; } // first XAddr usually
    public string? MediaXAddr { get; set; } // resolved via GetCapabilities
    public bool MediaRequiresAuth { get; set; }
}
