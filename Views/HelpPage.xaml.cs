using System.Windows;
using System.Windows.Controls;

namespace LungCancerIdentifierFrontEnd.Views
{
    public partial class HelpPage : Page
    {
        private readonly Frame _frame;

        public HelpPage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _frame.Navigate(new HomePage(_frame));
        }
    }
}
