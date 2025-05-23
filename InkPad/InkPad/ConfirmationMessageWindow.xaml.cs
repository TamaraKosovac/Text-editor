using System.Windows;
using System.Windows.Input;

namespace InkPad
{
    public partial class ConfirmationMessageWindow : Window
    {
        public event Action WindowClosed = delegate { };
        public ConfirmationMessageWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            WindowClosed?.Invoke();
            this.Close();
        }
    }
}
