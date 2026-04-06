using System.Windows;
using LungCancerIdentifierFrontEnd.Views;

namespace LungCancerIdentifierFrontEnd
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Start on the Home page
            MainFrame.Navigate(new HomePage(MainFrame));
        }
    }
}
