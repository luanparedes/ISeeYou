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
using Emgu.CV;
using Emgu.CV.Dnn;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

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
        private int frameCounter = 0;

        private Net _yoloNet;
        private string[] _classNames;
        private bool _aiInitialized = false;

        public MainWindow()
        {
            InitializeComponent();

            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _bitmap = new WriteableBitmap((int)Width, (int)Height);

            CameraStreamImage.Source = _bitmap;

            _lockObject = new object();

            this.InitializeYolo();
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

            frameCounter++;

            //if (frameCounter % 3 == 0)
            //{
                using (Mat frameRGBA = new Mat((int)Height, (int)Width, DepthType.Cv8U, 4, _bufferPtr, (int)Pitch))
                {
                    // 2. CONVERSÃO: Cria uma versão RGB (3 canais) apenas para a IA
                    using (Mat frameRGB = new Mat())
                    {
                        CvInvoke.CvtColor(frameRGBA, frameRGB, ColorConversion.Rgba2Rgb);

                        // 3. Criar o Blob a partir do frameRGB (YOLO precisa de 3 canais)
                        using (Mat blob = DnnInvoke.BlobFromImage(frameRGB, 1.0 / 255.0, new Size(640, 640), new MCvScalar(0, 0, 0), true, false))
                        {
                            _yoloNet.SetInput(blob);

                            // 4. Rodar a inferência (Aqui estava o erro de canais)
                            using (Mat output = _yoloNet.Forward())
                            {
                                // Isso vai nos dizer se ele te dá 100 linhas, 8400 linhas, etc.
                                Debug.WriteLine($"Dimensões do YOLO26: {output.Size.Width} (Colunas) x {output.Size.Height} (Linhas)");

                                // Vamos ver os primeiros valores para entender a escala (se é 0-1 ou 0-640)
                                float[] dump = new float[6];
                                Marshal.Copy(output.DataPointer, dump, 0, 6);
                                Debug.WriteLine($"Amostra de dados: {dump[0]}, {dump[1]}, {dump[2]}, {dump[3]}, {dump[4]}, {dump[5]}");

                                // 5. --- A MÁGICA DO DESENHO ESTÁ AQUI ---
                                // Chamamos o método que lê o tensor de saída e desenha
                                // Passamos 'frameRGBA' para que os desenhos apareçam na tela do WinUI!
                                ProcessYoloOutput(output, frameRGBA);
                            }
                        }
                    }
                //}

                frameCounter = 0; // Resetamos o contador para processar a cada 3 frames
            }

            //// --- PROCESSAMENTO DE VISÃO COMPUTACIONAL ---
            //// Criamos um Mat que aponta DIRETAMENTE para o endereço de memória do buffer do VLC
            //// Isso evita uma cópia desnecessária de memória (Zero-copy approach)
            //using (Mat frame = new Mat((int)Height, (int)Width, DepthType.Cv8U, (int)BytePerPixel, _bufferPtr, (int)Pitch))
            //{
            //    // 1. Converter para Cinza para o Detector (Haar Cascades pedem cinza)
            //    using (Mat gray = new Mat())
            //    {
            //        CvInvoke.CvtColor(frame, gray, ColorConversion.Bgr2Gray);

            //        // 2. Detectar Rostos
            //        var faces = _faceDetector.DetectMultiScale(gray, 1.1, 4);

            //        // 3. Desenhar no frame (isso altera o _bufferPtr que será exibido depois)
            //        foreach (var face in faces)
            //        {
            //            // Verde neon para dar aquele ar de "hacker"
            //            CvInvoke.Rectangle(frame, face, new MCvScalar(255, 255, 0), 0);
            //            CvInvoke.PutText(frame, "Human detected", new Point(face.X - 20, face.Y - 20),
            //                FontFace.HersheySimplex, 0.8, new MCvScalar(0, 255, 0), 2);
            //        }
            //    }
            //}

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

        private void ProcessYoloOutput(Mat output, Mat frameToDrawOn)
        {
            // YOLOv8 output: [1, 84, 8400]
            // O Mat do Emgu pode interpretar isso de formas variadas.
            // Vamos garantir que pegamos os dados brutos e tratamos como uma matriz 84 x 8400

            int dimensions = 84; // 4 coord + 80 classes
            int rows = 8400;     // número de predições

            float[] data = new float[dimensions * rows];
            Marshal.Copy(output.DataPointer, data, 0, data.Length);

            List<Rectangle> boxes = new List<Rectangle>();
            List<float> confidences = new List<float>();
            List<int> classIds = new List<int>();

            float confThreshold = 0.45f; // Ajuste para 0.45 para evitar os falsos positivos das folhas

            for (int i = 0; i < rows; i++)
            {
                float maxClassScore = 0;
                int classId = -1;

                // Procurar qual classe tem o maior score para esta predição
                // No YOLOv8, as classes começam no índice 4 de cada coluna
                for (int j = 4; j < dimensions; j++)
                {
                    // A matriz está transposta: a classe 'j' da predição 'i'
                    float score = data[i + (j * rows)];
                    if (score > maxClassScore)
                    {
                        maxClassScore = score;
                        classId = j - 4;
                    }
                }

                if (maxClassScore > confThreshold)
                {
                    // Coordenadas centrais (cx, cy, w, h)
                    float cx = data[i + (0 * rows)];
                    float cy = data[i + (1 * rows)];
                    float w = data[i + (2 * rows)];
                    float h = data[i + (3 * rows)];

                    // Converter para coordenadas de tela (x, y, w, h)
                    // Multiplicamos pela proporção da sua câmera (1920/640 e 1080/640)
                    int width = (int)(w * Width / 640.0);
                    int height = (int)(h * Height / 640.0);
                    int x = (int)((cx * Width / 640.0) - (width / 2));
                    int y = (int)((cy * Height / 640.0) - (height / 2));

                    boxes.Add(new Rectangle(x, y, width, height));
                    confidences.Add(maxClassScore);
                    classIds.Add(classId);
                }
            }

            // NMS (Non-Maximum Suppression) para não desenhar 500 quadrados no mesmo carro
            int[] indices = DnnInvoke.NMSBoxes(boxes.ToArray(), confidences.ToArray(), confThreshold, 0.45f);

            foreach (int idx in indices)
            {
                Rectangle box = boxes[idx];
                string label = $"{_classNames[classIds[idx]]} ({confidences[idx]:P0})";

                // Azul para carros (ID 2), Verde para o resto (pessoas, etc)
                MCvScalar color = (classIds[idx] == 2) ? new MCvScalar(255, 0, 0) : new MCvScalar(0, 255, 0);

                CvInvoke.Rectangle(frameToDrawOn, box, color, 2);
                CvInvoke.PutText(frameToDrawOn, label, new Point(box.X, box.Y - 10),
                    FontFace.HersheySimplex, 0.6, color, 2);
            }
        }

        private void InitializeYolo()
        {
            try
            {
                string modelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "yolov8n.onnx");
                string namesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "coco.names");

                _yoloNet = DnnInvoke.ReadNetFromONNX(modelPath);
                _classNames = File.ReadAllLines(namesPath);

                // Como você tem uma RTX 9060 XT, tente usar o backend padrão.
                // Se der erro, mude para Backend.Default e Target.Cpu
                _yoloNet.SetPreferableBackend(Emgu.CV.Dnn.Backend.Default);
                _yoloNet.SetPreferableTarget(Target.Cpu); // Mude para Cuda se tiver configurado

                _aiInitialized = true;
                Debug.WriteLine("YOLOv8 Inicializado com sucesso!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar YOLO: {ex.Message}");
            }
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
