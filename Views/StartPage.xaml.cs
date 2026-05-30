using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LungCancerIdentifierFrontEnd.Services;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class StartPage : Page
    {
        private readonly Frame _frame;

        public StartPage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _frame.Navigate(new HomePage(_frame));
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a file",
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathText.Text = dialog.FileName;
                FilePathText.Foreground = System.Windows.Media.Brushes.White;

                RunInferenceOnFile(dialog.FileName);
            }
        }

        private void RunInferenceOnFile(string path)
        {
            var (data, shape) = LoadFileAsTensor(path);
            if (data is null) return;

            OnnxModelService.RunInference(data, shape);
        }

        /// <summary>
        /// Reads the file into a float[] in the shape your ONNX model expects.
        /// For your 3D UNet that's [1, 1, D, H, W] matching the patch size from training.
        /// </summary>
        private (float[]? data, int[] shape) LoadFileAsTensor(string path)
        {
            // Example assuming you export patches from Python as raw float32 .bin files:
            //
            // const int D = 64, H = 64, W = 64;   // match your training patch size
            // var bytes = File.ReadAllBytes(path);
            // if (bytes.Length != D * H * W * sizeof(float))
            // {
            //     Debug.WriteLine($"[ONNX] Unexpected file size: {bytes.Length} bytes.");
            //     return (null, Array.Empty<int>());
            // }
            // var floats = new float[D * H * W];
            // Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            // return (floats, new[] { 1, 1, D, H, W });

            Debug.WriteLine("[ONNX] LoadFileAsTensor is a stub — implement based on your file format.");
            return (null, Array.Empty<int>());
        }
    }
}