using System.Windows;
using LungCancerIdentifierFrontEnd.Views;

namespace LungCancerIdentifierFrontEnd
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage(MainFrame));           
        }
    }
}
