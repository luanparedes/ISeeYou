using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ISeeYou.AI
{
    public class YoloDetector
    {
        #region Constants

        private const int ROWS = 8400;
        private const int DIMENSIONS = 84;

        #endregion

        #region Fields

        public List<(Rectangle Rect, string Label, int ClassId, float Conf)> Detections;

        private Net _net;
        private string[] _classNames;
        private readonly object _detectionLock = new object();

        #endregion

        #region Properties

        public float Threshold { get; set; } = 0.40f;
        public int FramesJumped { get; set; } = 10;
        public bool IsUsingGPU { get; set; } = true;
        public DepthType DepthTypeCv { get; set; } = DepthType.Cv8U;

        #endregion

        #region Constructors

        public YoloDetector(string modelPath, string namesPath)
        {
            Config(modelPath, namesPath);
        }

        #endregion

        #region Public Methods

        public void Detect(uint Height, uint Width, IntPtr bufferPtr, int Pitch)
        {
            FramesJumped++;

            using (Mat frameRGBA = new Mat((int)Height, (int)Width, DepthTypeCv, 4, bufferPtr, (int)Pitch))
            {
                if (FramesJumped % 10 == 0)
                {
                    using (Mat frameRGB = new Mat())
                    {
                        CvInvoke.CvtColor(frameRGBA, frameRGB, ColorConversion.Bgra2Rgb);
                        using (Mat blob = DnnInvoke.BlobFromImage(frameRGB, 1.0 / 255.0, new Size(640, 640), new MCvScalar(0, 0, 0), true, false))
                        {
                            _net.SetInput(blob);
                            using (Mat output = _net.Forward())
                            {
                                ProcessYoloOutput(output, (int)Height, (int)Width);
                            }
                        }
                    }
                }

                lock (_detectionLock)
                {
                    foreach (var det in Detections)
                    {
                        MCvScalar color = (det.ClassId == 2) ? new MCvScalar(255, 0, 0) : new MCvScalar(0, 255, 0);
                        CvInvoke.Rectangle(frameRGBA, det.Rect, color, 2);
                        CvInvoke.PutText(frameRGBA, $"{det.Label} ({det.Conf:P0})",
                            new Point(det.Rect.X, det.Rect.Y - 10), FontFace.HersheySimplex, 0.6, color, 2);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void Config(string modelPath, string namesPath)
        {
            _net = DnnInvoke.ReadNetFromONNX(modelPath);
            _classNames = File.ReadAllLines(namesPath);

            _net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
            _net.SetPreferableTarget(IsUsingGPU ? Target.OpenCL : Target.Cpu);

            Detections = new List<(Rectangle, string, int, float)>();
        }

        private void ProcessYoloOutput(Mat output, int Height, int Width)
        {
            float[] data = new float[DIMENSIONS * ROWS];
            Marshal.Copy(output.DataPointer, data, 0, data.Length);

            List<Rectangle> boxes = new List<Rectangle>();
            List<float> confidences = new List<float>();
            List<int> classIds = new List<int>();

            for (int i = 0; i < ROWS; i++)
            {
                float maxClassScore = 0;
                int classId = -1;

                for (int j = 4; j < DIMENSIONS; j++)
                {
                    float score = data[i + (j * ROWS)];
                    if (score > maxClassScore)
                    {
                        maxClassScore = score;
                        classId = j - 4;
                    }
                }

                if (maxClassScore > Threshold)
                {
                    float cx = data[i + (0 * ROWS)];
                    float cy = data[i + (1 * ROWS)];
                    float w = data[i + (2 * ROWS)];
                    float h = data[i + (3 * ROWS)];

                    int width = (int)(w * Width / 640.0);
                    int height = (int)(h * Height / 640.0);
                    int x = (int)((cx * Width / 640.0) - (width / 2));
                    int y = (int)((cy * Height / 640.0) - (height / 2));

                    boxes.Add(new Rectangle(x, y, width, height));
                    confidences.Add(maxClassScore);
                    classIds.Add(classId);
                }
            }

            int[] indices = DnnInvoke.NMSBoxes(boxes.ToArray(), confidences.ToArray(), Threshold, 0.45f);

            lock (_detectionLock)
            {
                Detections.Clear();
                foreach (int idx in indices)
                {
                    Detections.Add((
                        boxes[idx],
                        _classNames[classIds[idx]],
                        classIds[idx],
                        confidences[idx]
                    ));
                }
            }
        }

        #endregion
    }
}
