using LungCancerIdentifierFrontEnd.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class ScanViewerPage : Page
    {
        private readonly Frame _frame;
        private readonly Volume _volume;
        private int _currentSlice;
        private int? _clickX, _clickY, _clickZ;

        private const float WindowMin = -1000f; // HU values for display windowing only
        private const float WindowMax = 400f;
        private const int Patch = PatchExtractor.PatchSize;

        public ScanViewerPage(Volume volume, Frame frame)
        {
            InitializeComponent();
            _volume = volume;
            _frame = frame;

            StatusText.Text =
                $"Volume {_volume.Width}×{_volume.Height}×{_volume.Depth}, " +
                $"spacing {_volume.Spacing.X:F2}×{_volume.Spacing.Y:F2}×{_volume.Spacing.Z:F2} mm";

            SliceSlider.Minimum = 0;
            SliceSlider.Maximum = _volume.Depth - 1;
            SliceSlider.Value = _volume.Depth / 2;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => _frame.GoBack();

        private void SliceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _currentSlice = (int)e.NewValue;
            SliceText.Text = $"{_currentSlice + 1} / {_volume.Depth}";
            RenderSlice();
            DrawOverlay();
        }

        private void ImageHost_SizeChanged(object sender, SizeChangedEventArgs e) => DrawOverlay();

        private void RenderSlice()
        {
            int w = _volume.Width;
            int h = _volume.Height;
            var pixels = new byte[w * h];
            long sliceOffset = (long)_currentSlice * w * h;

            for (int i = 0; i < pixels.Length; i++)
            {
                float hu = _volume.Data[sliceOffset + i];
                float clipped = Math.Clamp(hu, WindowMin, WindowMax);
                pixels[i] = (byte)((clipped - WindowMin) / (WindowMax - WindowMin) * 255);
            }

            SliceImage.Source = BitmapSource.Create(
                w, h, 96, 96, PixelFormats.Gray8, null, pixels, w);
        }

        private void SliceImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SliceImage.Source is not BitmapSource src) return;

            var pos = e.GetPosition(ImageHost);          // <-- was SliceImage
            var (scale, offX, offY) = GetDisplayTransform(src);

            int vx = (int)((pos.X - offX) / scale);
            int vy = (int)((pos.Y - offY) / scale);

            if ((uint)vx >= _volume.Width || (uint)vy >= _volume.Height) return;

            _clickX = vx;
            _clickY = vy;
            _clickZ = _currentSlice;
            RunButton.IsEnabled = true;
            ResultText.Text = $"Selected voxel ({vx}, {vy}, {_currentSlice})";
            DrawOverlay();
        }

        private (double scale, double offX, double offY) GetDisplayTransform(BitmapSource src)
        {
            double sx = ImageHost.ActualWidth / src.PixelWidth;     // <-- was SliceImage.ActualWidth
            double sy = ImageHost.ActualHeight / src.PixelHeight;   // <-- was SliceImage.ActualHeight
            double s = Math.Min(sx, sy);
            return (s,
                (ImageHost.ActualWidth - src.PixelWidth * s) / 2,
                (ImageHost.ActualHeight - src.PixelHeight * s) / 2);
        }

        private void DrawOverlay()
        {
            OverlayCanvas.Children.Clear();
            if (_clickX is null || _clickZ != _currentSlice) return;
            if (SliceImage.Source is not BitmapSource src) return;

            var (scale, offX, offY) = GetDisplayTransform(src);
            int half = Patch / 2;
            double boxSize = Patch * scale;

            var rect = new Rectangle
            {
                Width = boxSize,
                Height = boxSize,
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };
            Canvas.SetLeft(rect, offX + (_clickX!.Value - half) * scale);
            Canvas.SetTop(rect, offY + (_clickY!.Value - half) * scale);
            OverlayCanvas.Children.Add(rect);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clickX is null || _clickY is null || _clickZ is null) return;

            var patch = PatchExtractor.ExtractAndNormalize(
                _volume, _clickX.Value, _clickY.Value, _clickZ.Value);

            var result = OnnxModelService.RunInference(
                patch, new[] { 1, 1, Patch, Patch, Patch });

            if (result is null)
            {
                ResultText.Text = "Inference failed — see Output window.";
                return;
            }

            // Count seg voxels above threshold (sigmoid(0) = 0.5, so logit > 0 ⇔ prob > 0.5)
            int positive = result.SegLogits.Count(v => v > 0f);
            int total = result.SegLogits.Length;

            ResultText.Text =
                $"Cancer probability: {result.ClsProbability:P2}   " +
                $"Seg voxels above threshold: {positive} / {total} ({positive * 100.0 / total:F2}%)";
        }
    }
}