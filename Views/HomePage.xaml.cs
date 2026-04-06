using System.Windows;
using System.Windows.Controls;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class HomePage : Page
    {
        private readonly Frame _frame;

        public HomePage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _frame.Navigate(new StartPage(_frame));
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            _frame.Navigate(new HelpPage(_frame));
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
