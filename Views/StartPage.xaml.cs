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
        private readonly Frame frame;

        private float[]? patch;     
        private float[]? segMap;    
        public StartPage(Frame frame)
        {
            InitializeComponent();
            this.frame = frame;
        }

        private void SliceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (patch is null) return;
            int slice = (int)e.NewValue;
            BaseImage.Source = RenderGrayscale(patch, slice);
            OverlayImage.Source = segMap is null ? null : RenderHeatmap(segMap, slice);
            SliceText.Text = $"Szelet {slice + 1} / 64";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => this.frame.Navigate(new HomePage(this.frame));

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Folt kiválasztása",
                Filter = "Nyers folt (*.bin)|*.bin"
            };

            if (dialog.ShowDialog() != true) return;

            FilePathText.Text = dialog.FileName;
            FilePathText.Foreground = System.Windows.Media.Brushes.White;

            try
            {
                patch = PatchLoader.Load(dialog.FileName);
                var result = OnnxModelService.RunInference(
                    patch, new[] { 1, 1, PatchLoader.Size, PatchLoader.Size, PatchLoader.Size });

                if (result is null)
                {
                    ResultText.Text = "Megállapítás nem sikerült";
                    return;
                }

                Debug.WriteLine($"[Patch] {Path.GetFileName(dialog.FileName)} → cancer probability = {result.ClsProbability}");

                String amountOfRisk = result.ClsProbability switch
                {
                    > 0.70f => "Magas valószinűség",
                    > 0.35f => "Közepes valószinűség",
                    _ => "Alacsony valószinűség"
                };

                ResultText.Text = $"Daganat valószinűség: {result.ClsProbability:P2} | {amountOfRisk}";

                if (result.ClsProbability >= 0.35f)
                {
                    segMap = new float[result.SegLogits.Length];
                    for (int i = 0; i < result.SegLogits.Length; i++)
                    {
                        segMap[i] = 1f / (1f + MathF.Exp(-result.SegLogits[i]));
                    }
                }
                else
                {
                    segMap = null;
                }

                int initialSlice = 32;
                SliceSlider.IsEnabled = true;
                SliceSlider.Value = initialSlice;
                BaseImage.Source = RenderGrayscale(patch, initialSlice);
                OverlayImage.Source = segMap is not null ? RenderHeatmap(segMap, initialSlice) : null;




                //Debug.WriteLine($"[Patch] {Path.GetFileName(dialog.FileName)} → cancer probability = {result?.ClsProbability:P2} {amountOfRisk}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Patch] Failed: {ex.Message}");
            }
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
            const float MaxOpacity = 0.4f;
            int sliceOffset = sliceIndex * Size * Size;
            var pixels = new byte[Size * Size * 4];
            for (int i = 0; i < Size * Size; i++)
            {
                float p = Math.Clamp(segMap[sliceOffset + i], 0f, 1f);
                byte alpha = (byte)(p * MaxOpacity * 255f);
                int dst = i * 4;
                pixels[dst + 0] = 0;
                pixels[dst + 1] = 0;
                pixels[dst + 2] = 255;
                pixels[dst + 3] = alpha;
            }
            return BitmapSource.Create(Size, Size, 96, 96,
                PixelFormats.Bgra32, null, pixels, Size * 4);
        }

    }
}