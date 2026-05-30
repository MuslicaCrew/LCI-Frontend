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
            => _frame.Navigate(new HomePage(_frame));

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a CT scan",
                Filter = "MetaImage (*.mhd)|*.mhd"
            };

            if (dialog.ShowDialog() != true) return;

            FilePathText.Text = dialog.FileName;
            FilePathText.Foreground = System.Windows.Media.Brushes.White;

            try
            {
                var volume = MhdLoader.Load(dialog.FileName);
                _frame.Navigate(new ScanViewerPage(volume, _frame));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load scan:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
