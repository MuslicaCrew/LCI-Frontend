using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

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
            }
        }
    }
}
