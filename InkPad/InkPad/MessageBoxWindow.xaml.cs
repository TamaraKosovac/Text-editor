using System.Windows;
using System.Windows.Input;

namespace InkPad
{
    public partial class MessageBoxWindow : Window
    {
        public string Message { get; set; }
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public MessageBoxWindow(string message, string title)
        {
            InitializeComponent();
            DataContext = this;
            Message = message;
            Title = title;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        public static MessageBoxResult Show(string message, string title)
        {
            MessageBoxWindow msgBox = new MessageBoxWindow(message, title);
            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}
