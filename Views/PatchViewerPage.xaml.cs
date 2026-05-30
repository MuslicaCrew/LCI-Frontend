using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LungCancerIdentifierFrontEnd.Services;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class PatchViewerPage : Page
    {
        private readonly Frame _frame;
        private readonly float[] _patch;
        private readonly string _name;
        private int _currentSlice;

        private const int Size = PatchLoader.Size;

        public PatchViewerPage(float[] patch, string name, Frame frame)
        {
            InitializeComponent();
            _patch = patch;
            _name = name;
            _frame = frame;

            TitleText.Text = name;

            // Ground truth from filename convention (pos_NNN.bin / neg_NNN.bin)
            if (name.StartsWith("pos_", StringComparison.OrdinalIgnoreCase))
                GroundTruthText.Text = "Ground truth: POSITIVE (nodule)";
            else if (name.StartsWith("neg_", StringComparison.OrdinalIgnoreCase))
                GroundTruthText.Text = "Ground truth: NEGATIVE";
            else
                GroundTruthText.Text = "Ground truth: unknown";

            SliceSlider.Minimum = 0;
            SliceSlider.Maximum = Size - 1;
            SliceSlider.Value = Size / 2;
        }

        private void SliceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currentSlice = (int)e.NewValue;
            SliceText.Text = $"{_currentSlice + 1} / {Size}";
            RenderSlice();
        }

        private void RenderSlice()
        {
            var pixels = new byte[Size * Size];
            int offset = _currentSlice * Size * Size;
            for (int i = 0; i < pixels.Length; i++)
            {
                float v = Math.Clamp(_patch[offset + i], 0f, 1f);
                pixels[i] = (byte)(v * 255);
            }
            SliceImage.Source = BitmapSource.Create(
                Size, Size, 96, 96, PixelFormats.Gray8, null, pixels, Size);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var result = OnnxModelService.RunInference(
                _patch, new[] { 1, 1, Size, Size, Size });

            if (result is null)
            {
                ResultText.Text = "Inference failed — see Output window.";
                return;
            }

            int positive = result.SegLogits.Count(v => v > 0f);
            int total = result.SegLogits.Length;

            ResultText.Text =
                $"Cancer probability: {result.ClsProbability:P2}   " +
                $"Seg voxels above threshold: {positive} / {total} ({positive * 100.0 / total:F2}%)";
        }

        private void Back_Click(object sender, RoutedEventArgs e) => _frame.GoBack();
    }
}