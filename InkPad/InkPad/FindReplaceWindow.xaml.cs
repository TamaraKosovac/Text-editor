using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace InkPad
{
    public partial class FindReplaceWindow : Window, INotifyPropertyChanged
    {
        private string _titleText;
        private readonly RichTextBox _targetRichTextBox;
        private readonly FindReplaceMode _mode;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TitleText
        {
            get => _titleText;
            set
            {
                _titleText = value;
                OnPropertyChanged();
            }
        }

        public FindReplaceWindow(RichTextBox targetRichTextBox, FindReplaceMode mode)
        {
            InitializeComponent();
            _targetRichTextBox = targetRichTextBox;
            DataContext = this;

            _mode = mode;

            if (mode == FindReplaceMode.Find)
            {
                _titleText = "Find";
                TitleText = _titleText;
                ReplacePanel.Visibility = Visibility.Collapsed;
                ReplaceButton.Visibility = Visibility.Collapsed;
                FindButton.Visibility = Visibility.Visible;
            }
            else
            {
                _titleText = "Find and replace";
                TitleText = _titleText;
                ReplacePanel.Visibility = Visibility.Visible;
                ReplaceButton.Visibility = Visibility.Visible;
                FindButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private void Find_Click(object sender, RoutedEventArgs e)
        {
            string pattern = PatternTextBox.Text;
            if (!string.IsNullOrEmpty(pattern))
            {
                string text = new TextRange(_targetRichTextBox.Document.ContentStart, _targetRichTextBox.Document.ContentEnd).Text;
                int count = Regex.Matches(text, Regex.Escape(pattern)).Count;

                var customMessageBox = new ConfirmationMessageWindow($"Pattern \"{pattern}\" found {count} times.");
                customMessageBox.WindowClosed += CustomMessageBox_WindowClosed; 
                customMessageBox.ShowDialog();
            }
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            string pattern = PatternTextBox.Text;
            string replacement = ReplaceTextBox.Text;

            if (!string.IsNullOrEmpty(pattern)) 
            {
                string text = new TextRange(_targetRichTextBox.Document.ContentStart, _targetRichTextBox.Document.ContentEnd).Text;

                if (Regex.IsMatch(text, Regex.Escape(pattern)))
                {
                    string replacedText = Regex.Replace(text, Regex.Escape(pattern), replacement);
                    _targetRichTextBox.Document.Blocks.Clear();
                    _targetRichTextBox.Document.Blocks.Add(new Paragraph(new Run(replacedText)));

                    var customMessageBox = new ConfirmationMessageWindow($"Replaced \"{pattern}\" with \"{replacement}\".");
                    customMessageBox.ShowDialog();
                }
                else
                {
                    var customMessageBox = new ConfirmationMessageWindow($"Pattern \"{pattern}\" was not found.");
                    customMessageBox.ShowDialog();
                }

                this.Close();
            }
            else
            {
                var customMessageBox = new ConfirmationMessageWindow("Please enter a pattern to replace.");
                customMessageBox.ShowDialog();
            }
        }


        private void CustomMessageBox_WindowClosed()
        {
            this.Close();  
        }
    }
}