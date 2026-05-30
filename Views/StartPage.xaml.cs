using LungCancerIdentifierFrontEnd.Services;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class StartPage : Page
    {
        private readonly Frame _frame;

        private float[]? _patch;     // <-- add
        private float[]? _segMap;    // <-- add

        public StartPage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
        }

        private void SliceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_patch is null) return;
            int slice = (int)e.NewValue;
            BaseImage.Source = RenderGrayscale(_patch, slice);
            OverlayImage.Source = _segMap is null ? null : RenderHeatmap(_segMap, slice);
            SliceText.Text = $"Slice {slice + 1} / 64";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => _frame.Navigate(new HomePage(_frame));

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a patch",
                Filter = "Raw patch (*.bin)|*.bin"
            };

            if (dialog.ShowDialog() != true) return;

            FilePathText.Text = dialog.FileName;
            FilePathText.Foreground = System.Windows.Media.Brushes.White;

            try
            {
                _patch = PatchLoader.Load(dialog.FileName);
                var result = OnnxModelService.RunInference(
                    _patch, new[] { 1, 1, PatchLoader.Size, PatchLoader.Size, PatchLoader.Size });

                if (result is null)
                {
                    ResultText.Text = "Inference failed.";
                    return;
                }

                String amountOfRisk = result.ClsProbability switch
                {
                    > 0.5f => "High probability",
                    > 0.221f => "Moderate probability",
                    _ => "Low probability"
                };

                ResultText.Text = $"Cancer probability: {result.ClsProbability:P2} {amountOfRisk}";

                if (result.ClsProbability >= 0.5f)
                {
                    // Positive: build segmap and show the most active slice with overlay
                    _segMap = new float[result.SegLogits.Length];
                    for (int i = 0; i < result.SegLogits.Length; i++)
                    {
                        _segMap[i] = 1f / (1f + MathF.Exp(-result.SegLogits[i]));
                    }
                    Debug.WriteLine($"max seg p = {_segMap.Max()}");
                    Debug.WriteLine($"average seg p = {_segMap.Average()}");
                    Debug.WriteLine($"min seg p = {_segMap.Min()}");
                    //int bestSlice = FindMostActiveSlice(_segMap);
                    //BaseImage.Source = RenderGrayscale(_patch, bestSlice);
                    //OverlayImage.Source = RenderHeatmap(_segMap, bestSlice);

                    int initialSlice = _segMap is not null ? FindMostActiveSlice(_segMap) : PatchLoader.Size / 2;
                    SliceSlider.IsEnabled = true;
                    SliceSlider.Value = initialSlice;
                    // Force render in case the slider value didn't change between loads
                    BaseImage.Source = RenderGrayscale(_patch, initialSlice);
                    OverlayImage.Source = _segMap is not null ? RenderHeatmap(_segMap, initialSlice) : null;
                }
                else
                { 
                    //BaseImage.Source = RenderGrayscale(_patch, PatchLoader.Size / 2);
                    //OverlayImage.Source = null;
                    int initialSlice = _segMap is not null ? FindMostActiveSlice(_segMap) : PatchLoader.Size / 2;
                    SliceSlider.IsEnabled = true;
                    SliceSlider.Value = initialSlice;
                    // Force render in case the slider value didn't change between loads
                    BaseImage.Source = RenderGrayscale(_patch, initialSlice);
                    OverlayImage.Source = _segMap is not null ? RenderHeatmap(_segMap, initialSlice) : null;
                }




                Debug.WriteLine($"[Patch] {Path.GetFileName(dialog.FileName)} → cancer probability = {result?.ClsProbability:P2} {amountOfRisk}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Patch] Failed: {ex.Message}");
            }


        }

        private static int FindMostActiveSlice(float[] segMap)
        {
            const int Size = 64;
            int bestSlice = Size / 2;
            float bestSum = -1f;

            for (int z = 0; z < Size; z++)
            {
                float sum = 0f;
                int offset = z * Size * Size;
                for (int i = 0; i < Size * Size; i++)
                    sum += segMap[offset + i];
                if (sum > bestSum) { bestSum = sum; bestSlice = z; }
            }
            return bestSlice;
        }

        private static BitmapSource RenderGrayscale(float[] patch, int sliceIndex)
        {
            const int Size = 64;
            int sliceOffset = sliceIndex * Size * Size;
            var pixels = new byte[Size * Size];

            for (int i = 0; i < Size * Size; i++)
            {
                float v = Math.Clamp(patch[sliceOffset + i], 0f, 1f);
                pixels[i] = (byte)(v * 255);
            }

            return BitmapSource.Create(Size, Size, 96, 96,
                PixelFormats.Gray8, null, pixels, Size);
        }

        private static BitmapSource RenderHeatmap(float[] segMap, int sliceIndex)
        {
            const int Size = 64;
            const float OverlayThreshold = 0.3f;
            const float MaxOpacity = 0.3f;

            int sliceOffset = sliceIndex * Size * Size;
            var pixels = new byte[Size * Size * 4];

            for (int i = 0; i < Size * Size; i++)
            {
                float p = segMap[sliceOffset + i];
                float t = Math.Clamp((p - OverlayThreshold) / (1f - OverlayThreshold), 0f, 1f);
                byte alpha = (byte)(t * MaxOpacity * 255);

                int dst = i * 4;
                pixels[dst + 0] = 0;        // B
                pixels[dst + 1] = 0;        // G
                pixels[dst + 2] = 255;      // R = pure red
                pixels[dst + 3] = alpha;    // A scales with seg probability
            }

            return BitmapSource.Create(Size, Size, 96, 96,
                PixelFormats.Bgra32, null, pixels, Size * 4);
        }

    }
}