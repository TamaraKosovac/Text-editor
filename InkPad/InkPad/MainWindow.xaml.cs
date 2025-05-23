using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using Path = System.IO.Path;
using Button = System.Windows.Controls.Button;

namespace InkPad;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
        NewTabButton_Click(this, new RoutedEventArgs());
        if (EditorTabControl.Items.Count > 0)
        {
            EditorTabControl.SelectedItem = EditorTabControl.Items[EditorTabControl.Items.Count - 1];
        }
        foreach (var fontFamily in Fonts.SystemFontFamilies)
        {
            var item = new ComboBoxItem();
            item.Content = fontFamily.Source;
            item.FontFamily = fontFamily;
            FontComboBox.Items.Add(item);
        }
    }


    private void RoseThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSettings.IsChecked = false;
        Application.Current.Resources.MergedDictionaries.Clear();

        var lightColors = new ResourceDictionary
        {
            Source = new Uri("Themes/RoseTheme.xaml", UriKind.Relative)
        };
        ChangeTheme("Themes/RoseTheme.xaml");
        var sharedStyles = new ResourceDictionary
        {
            Source = new Uri("Resources/Styles.xaml", UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Add(lightColors);
        Application.Current.Resources.MergedDictionaries.Add(sharedStyles);
    }

    private void PurpleThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSettings.IsChecked = false;
        Application.Current.Resources.MergedDictionaries.Clear();

        var darkColors = new ResourceDictionary
        {
            Source = new Uri("Themes/PurpleTheme.xaml", UriKind.Relative)
        };
        ChangeTheme("Themes/PurpleTheme.xaml");
        var sharedStyles = new ResourceDictionary
        {
            Source = new Uri("Resources/Styles.xaml", UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Add(darkColors);
        Application.Current.Resources.MergedDictionaries.Add(sharedStyles);
    }


    private void NewTabButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;
        RichTextBox richTextBox = new RichTextBox
        {
            FontSize = 14,
            AcceptsReturn = true,
            AcceptsTab = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)Application.Current.Resources["BackgroundColor"],
            Foreground = (Brush)Application.Current.Resources["FontColor"],
            BorderThickness = new Thickness(0),
            Document = new FlowDocument(),
            IsUndoEnabled = true,
            UndoLimit = 100
        };

        TextBlock statusTextLeft = new TextBlock
        {
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = (Brush)Application.Current.Resources["FontColor"]
        };
        TextBlock statusTextRight = new TextBlock
        {
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = (Brush)Application.Current.Resources["FontColor"]
        };
        Grid grid = new Grid();
        grid.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            Direction = 320,
            ShadowDepth = 5,
            Opacity = 0.3,
            BlurRadius = 10
        };
        grid.Margin = new Thickness(1);
        grid.Background = (Brush)Application.Current.Resources["BackgroundColor"];
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); 

        Grid.SetRow(richTextBox, 0);
        Grid.SetRow(statusTextLeft, 1);
        Grid.SetRow(statusTextRight, 1);

        Grid.SetColumn(statusTextLeft, 0);
        Grid.SetColumn(statusTextRight, 1);

        grid.Children.Add(richTextBox);
        grid.Children.Add(statusTextLeft);
        grid.Children.Add(statusTextRight);

        statusTextRight.MinWidth = 100; 

        Grid.SetColumnSpan(richTextBox, 2);

        richTextBox.SelectionChanged += (s, args) =>
        {
            TextPointer caretPos = richTextBox.CaretPosition;
            TextPointer start = richTextBox.Document.ContentStart;

            int charCount = new TextRange(start, caretPos).Text.Length;

            int line = 1;
            int column = 1;

            foreach (var block in richTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    TextPointer paragraphStart = paragraph.ContentStart;
                    TextPointer paragraphEnd = paragraph.ContentEnd;

                    if (paragraphEnd.CompareTo(caretPos) >= 0)
                    {
                        column = new TextRange(paragraphStart, caretPos).Text.Length + 1;
                        break;
                    }

                    line++;
                }
            }

            statusTextLeft.Text = $"Characters: {charCount}  |  Ln: {line}  Col: {column}";
            statusTextRight.Text = $"Windows (CRLF)  |  Unicode (UTF-8)";
        };

        richTextBox.SelectionChanged += Delete_SelectionChanged;
        richTextBox.SelectionChanged += Copy_SelectionChanged;

        richTextBox.TextChanged += (s, args) =>
        {
            foreach (Paragraph paragraph in richTextBox.Document.Blocks.OfType<Paragraph>())
            {
                if (paragraph.Margin != new Thickness(0))
                    paragraph.Margin = new Thickness(0);
            }

            if (EditorTabControl.SelectedItem is TabItem currentTab)
            {
                TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                UpdateTabHeaderButtonsVisibility(currentTab, range.Text);
            }

            UpdateUndoRedoButtons(richTextBox);
        };

        Update_SelectionChanged();
        UpdateUndoRedoButtons(richTextBox);


        var newTab = new TabItem
        {
            Header = $"Untitled",
            HeaderTemplate = (DataTemplate)FindResource("TabHeaderTemplate"),
            Content = grid
        };

        EditorTabControl.Items.Add(newTab);
        EditorTabControl.SelectedItem = newTab;

        newTab.Loaded += (senderTab, args) =>
        {
            TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            UpdateTabHeaderButtonsVisibility(newTab, range.Text);
        };

        Update_SelectionChanged();
    }


    private void NewWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;
        MainWindow newWindow = new MainWindow
        {
            Left = this.Left + 30,
            Top = this.Top + 30
        };

        newWindow.Show();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "Open Text File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            string fileContent;
            string fileName = Path.GetFileName(filePath);

            string lineEnding = DetectLineEnding(filePath);
            string encoding = DetectFileEncoding(filePath);
            fileContent = File.ReadAllText(filePath);

            RichTextBox richTextBox = new RichTextBox
            {
                FontSize = 14,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = (Brush)Application.Current.Resources["BackgroundColor"],
                Foreground = (Brush)Application.Current.Resources["FontColor"],
                BorderThickness = new Thickness(0),
                AcceptsTab = true,
                IsUndoEnabled = true,
                UndoLimit = 100
            };

            string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                richTextBox.Document.Blocks.Add(new Paragraph(new Run(line)));
            }

            TextBlock statusTextLeft = new TextBlock
            {
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = (Brush)Application.Current.Resources["FontColor"]
            };

            TextBlock statusTextRight = new TextBlock
            {
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = (Brush)Application.Current.Resources["FontColor"]
            };
            statusTextRight.Text = $"{lineEnding}  |  {encoding}";

            Grid grid = new Grid();
            grid.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 320,
                ShadowDepth = 5,
                Opacity = 0.3,
                BlurRadius = 10
            };
            grid.Margin = new Thickness(1);
            grid.Background = (Brush)Application.Current.Resources["BackgroundColor"];

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetRow(richTextBox, 0);
            Grid.SetRow(statusTextLeft, 1);
            Grid.SetRow(statusTextRight, 1);

            Grid.SetColumn(statusTextLeft, 0);
            Grid.SetColumn(statusTextRight, 1);

            Grid.SetColumnSpan(richTextBox, 2);

            grid.Children.Add(richTextBox);
            grid.Children.Add(statusTextLeft);
            grid.Children.Add(statusTextRight);

            statusTextRight.MinWidth = 100;

            richTextBox.SelectionChanged += (s, args) =>
            {
                TextPointer caretPos = richTextBox.CaretPosition;
                TextPointer start = richTextBox.Document.ContentStart;

                int line = 1;
                int column = 1;

                foreach (var block in richTextBox.Document.Blocks)
                {
                    if (block is Paragraph paragraph)
                    {
                        TextPointer paragraphStart = paragraph.ContentStart;
                        TextPointer paragraphEnd = paragraph.ContentEnd;

                        if (paragraphEnd.CompareTo(caretPos) >= 0)
                        {
                            column = new TextRange(paragraphStart, caretPos).Text.Length + 1;
                            break;
                        }

                        line++;
                    }
                }
                statusTextLeft.Text = $"Characters: {new TextRange(start, caretPos).Text.Length}  |  Ln: {line}  Col: {column}";
            };

            richTextBox.SelectionChanged += Delete_SelectionChanged;
            richTextBox.SelectionChanged += Copy_SelectionChanged;

            richTextBox.TextChanged += (s, args) =>
            {
                foreach (Paragraph paragraph in richTextBox.Document.Blocks.OfType<Paragraph>())
                {
                    if (paragraph.Margin != new Thickness(0))
                        paragraph.Margin = new Thickness(0);
                }

                if (EditorTabControl.SelectedItem is TabItem currentTab)
                {
                    TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    UpdateTabHeaderButtonsVisibility(currentTab, range.Text);
                }

                UpdateUndoRedoButtons(richTextBox);
            };

            Update_SelectionChanged();
            UpdateUndoRedoButtons(richTextBox);

            var newTab = new TabItem
            {
                Header = fileName,
                HeaderTemplate = (DataTemplate)FindResource("TabHeaderTemplate"),
                Content = grid
            };

            EditorTabControl.Items.Add(newTab);
            EditorTabControl.SelectedItem = newTab;

            newTab.Loaded += (senderTab, args) =>
            {
                TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                UpdateTabHeaderButtonsVisibility(newTab, range.Text);
            };

            Update_SelectionChanged();
        }
    }


    private string DetectLineEnding(string filePath)
    {
        string fileContent = File.ReadAllText(filePath);
        if (fileContent.Contains("\r\n"))
            return "Windows (CRLF)";
        else if (fileContent.Contains("\n"))
            return "Unix (LF)";
        else
            return "Unknown";
    }

    private string DetectFileEncoding(string filePath)
    {
        using (var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
        {
            return reader.CurrentEncoding.EncodingName;
        }
    }

    private T? FindChildControl<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;

            var result = FindChildControl<T>(child);
            if (result != null)
                return result;
        }

        return null;
    }


    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                if (selectedTab.Tag is string currentFilePath && File.Exists(currentFilePath))
                {
                    string content = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                    File.WriteAllText(currentFilePath, content);

                    var circleButton = FindChild<Button>(selectedTab, "CircleButton");
                    var closeButton = FindChild<Button>(selectedTab, "CloseButton");

                    if (circleButton != null && closeButton != null)
                    {
                        circleButton.Visibility = Visibility.Collapsed;
                        closeButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    SaveAsButton_Click(sender, e);
                }
            }
        }
    }


    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
           selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Text File",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"{selectedTab.Header}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string content = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                    File.WriteAllText(filePath, content);

                    string fileNameWithoutExtension = Path.GetFileName(filePath);
                    if (!fileNameWithoutExtension.EndsWith(".txt"))
                        fileNameWithoutExtension += ".txt";

                    selectedTab.Header = fileNameWithoutExtension;
                    selectedTab.Tag = filePath;

                    var circleButton = FindChild<Button>(selectedTab, "CircleButton");
                    var closeButton = FindChild<Button>(selectedTab, "CloseButton");

                    if (circleButton != null && closeButton != null)
                    {
                        circleButton.Visibility = Visibility.Collapsed;
                        closeButton.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    private void SaveAllButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;

        foreach (var item in EditorTabControl.Items)
        {
            if (item is TabItem tabItem &&
                tabItem.Content is Grid grid)
            {
                var richTextBox = FindChildControl<RichTextBox>(grid);
                if (richTextBox != null)
                {
                    EditorTabControl.SelectedItem = tabItem;
                    SaveButton_Click(sender, e);
                }
            }
        }
    }


    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;

        if (sender is Button closeButton)
        {
            TabItem? tabItem = FindParent<TabItem>(closeButton);

            if (tabItem != null && tabItem.Content is Grid grid)
            {
                var richTextBox = FindChildControl<RichTextBox>(grid);
                if (richTextBox != null)
                {
                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    if (!string.IsNullOrWhiteSpace(textRange.Text) && tabItem.Tag == null)
                    {
                        MessageBoxResult result = MessageBoxWindow.Show("Do you want to save changes?", "Save Changes");

                        if (result == MessageBoxResult.Yes)
                        {
                            SaveAsButton_Click(sender, e);
                            EditorTabControl.Items.Remove(tabItem);
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            EditorTabControl.Items.Remove(tabItem);
                        }
                    }
                    else
                    {
                        EditorTabControl.Items.Remove(tabItem);
                    }
                }
            }
        }

        Update_SelectionChanged();
    }


    private void CloseAllTabsButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;
        for (int i = EditorTabControl.Items.Count - 1; i >= 0; i--)
        {
            var tabItem = EditorTabControl.Items[i] as TabItem;

            if (tabItem != null && tabItem.Content is Grid grid)
            {
                var richTextBox = FindChildControl<RichTextBox>(grid);
                if (richTextBox != null)
                {
                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    if (!string.IsNullOrWhiteSpace(textRange.Text) && tabItem.Tag == null)
                    {
                        MessageBoxResult result = MessageBoxWindow.Show("Do you want to save changes?", "Save Changes");

                        if (result == MessageBoxResult.Yes)
                        {
                            EditorTabControl.SelectedItem = tabItem;
                            SaveAsButton_Click(sender, e);
                            EditorTabControl.Items.RemoveAt(i);
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            EditorTabControl.Items.RemoveAt(i);
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            break;
                        }
                    }
                    else
                    {
                        EditorTabControl.Items.RemoveAt(i);
                    }
                }
            }
        }
        Update_SelectionChanged();
    }


    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                if (!string.IsNullOrWhiteSpace(textRange.Text) && selectedTab.Tag == null)
                {
                    MessageBoxResult result = MessageBoxWindow.Show("Do you want to save changes?", "Save Changes");

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveAsButton_Click(sender, e);
                        EditorTabControl.Items.Remove(selectedTab);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        EditorTabControl.Items.Remove(selectedTab);
                    }
                }
                else
                {
                    EditorTabControl.Items.Remove(selectedTab);
                }
            }
        }

        Update_SelectionChanged();
    }


    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFile.IsChecked = false;
        this.Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }


    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var currentTextBox = FindChildControl<RichTextBox>(grid);

            if (currentTextBox != null)
            {
                var selection = currentTextBox.Selection;

                if (!selection.IsEmpty)
                {
                    EditingCommands.ToggleBold.Execute(null, currentTextBox);
                }
                else
                {
                    var caretPos = currentTextBox.CaretPosition;
                    TextRange range = new TextRange(caretPos, caretPos);
                    var currentWeight = range.GetPropertyValue(TextElement.FontWeightProperty);
                    var newWeight = (currentWeight != DependencyProperty.UnsetValue && currentWeight.Equals(FontWeights.Bold))
                        ? FontWeights.Normal
                        : FontWeights.Bold;

                    currentTextBox.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, newWeight);
                    currentTextBox.Focus();
                }
            }
        }
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var currentTextBox = FindChildControl<RichTextBox>(grid);

            if (currentTextBox != null)
            {
                var selection = currentTextBox.Selection;

                if (!selection.IsEmpty)
                {
                    EditingCommands.ToggleItalic.Execute(null, currentTextBox);
                }
                else
                {
                    var caret = currentTextBox.CaretPosition;
                    var range = new TextRange(caret, caret);

                    var currentStyle = range.GetPropertyValue(TextElement.FontStyleProperty);
                    var newStyle = (currentStyle != DependencyProperty.UnsetValue && currentStyle.Equals(FontStyles.Italic))
                        ? FontStyles.Normal
                        : FontStyles.Italic;

                    currentTextBox.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, newStyle);
                    currentTextBox.Focus();
                }
            }
        }
    }

    private void UnderlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var currentTextBox = FindChildControl<RichTextBox>(grid);

            if (currentTextBox != null)
            {
                var selection = currentTextBox.Selection;

                if (!selection.IsEmpty)
                {
                    EditingCommands.ToggleUnderline.Execute(null, currentTextBox);
                }
                else
                {
                    var caret = currentTextBox.CaretPosition;
                    var range = new TextRange(caret, caret);

                    var currentDeco = range.GetPropertyValue(Inline.TextDecorationsProperty);
                    var isUnderlined = currentDeco != DependencyProperty.UnsetValue &&
                                       currentDeco.Equals(TextDecorations.Underline);

                    currentTextBox.Selection.ApplyPropertyValue(
                        Inline.TextDecorationsProperty,
                        isUnderlined ? null : TextDecorations.Underline
                    );

                    currentTextBox.Focus();
                }
            }
        }
    }


    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox.SelectedItem is string selectedFont)
        {
            if (EditorTabControl.SelectedItem is TabItem selectedTab)
            {
                var grid = FindParent<Grid>(selectedTab);

                if (grid != null)
                {
                    var richTextBox = FindChildControl<RichTextBox>(grid);

                    if (richTextBox != null)
                    {
                        richTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(selectedFont));

                        TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                        textRange.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(selectedFont));
                    }
                }
            }
        }
    }



    private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeComboBox.SelectedItem != null)
        {
            double selectedSize = (double)FontSizeComboBox.SelectedItem;

            if (EditorTabControl.SelectedItem is TabItem selectedTab)
            {
                var grid = FindParent<Grid>(selectedTab);

                if (grid != null)
                {
                    var richTextBox = FindChildControl<RichTextBox>(grid);

                    if (richTextBox != null)
                    {
                        richTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, selectedSize);
                        richTextBox.Focus();
                        richTextBox.CaretPosition = richTextBox.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                        richTextBox.Selection.Select(richTextBox.CaretPosition, richTextBox.CaretPosition);
                    }
                }
            }
        }
    }



    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                TextSelection selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.Text = string.Empty;
                }
            }
        }
    }

    private void CutButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null && !richTextBox.Selection.IsEmpty)
            {
                string selectedText = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End).Text;
                Clipboard.SetText(selectedText);
                richTextBox.Selection.Text = string.Empty;
            }
        }
    }


    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                if (!richTextBox.Selection.IsEmpty)
                {
                    string selectedText = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End).Text;
                    Clipboard.SetText(selectedText);
                }
                else
                {
                    string allText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                    Clipboard.SetText(allText);
                }
            }
        }
    }

    private void PasteButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null && Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                richTextBox.Selection.Text = clipboardText;
            }
        }
    }


    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null && richTextBox.CanUndo)
            {
                richTextBox.Undo();
            }
        }
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab &&
            selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null && richTextBox.CanRedo)
            {
                richTextBox.Redo();
            }
        }
    }


    private void ChangeTheme(string themeUri)
    {
        var theme = new ResourceDictionary();
        theme.Source = new Uri(themeUri, UriKind.Relative);

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(theme);
        foreach (TabItem tabItem in EditorTabControl.Items)
        {
            if (tabItem.Content is RichTextBox richTextBox)
            {
                richTextBox.Background = (Brush)Application.Current.Resources["BackgroundColor"];
                richTextBox.Foreground = (Brush)Application.Current.Resources["FontColor"];
            }
        }
    }

    private void UpdateUndoRedoButtons(RichTextBox richTextBox)
    {
        UndoShortcutButton.IsEnabled = richTextBox.CanUndo;
        UndoButton.IsEnabled = richTextBox.CanUndo;
        RedoShortcutButton.IsEnabled = richTextBox.CanRedo;
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSearch.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab)
        {
            var grid = FindParent<Grid>(selectedTab);

            if (grid != null)
            {
                var rtb = FindChildControl<RichTextBox>(grid);

                if (rtb != null)
                {
                    var findWindow = new FindReplaceWindow(rtb, FindReplaceMode.Find)
                    {
                        Owner = this
                    };
                    findWindow.ShowDialog();
                }
            }
        }
    }

    private void ReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSearch.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab)
        {
            var grid = FindParent<Grid>(selectedTab);

            if (grid != null)
            {
                var rtb = FindChildControl<RichTextBox>(grid);

                if (rtb != null)
                {
                    var replaceWindow = new FindReplaceWindow(rtb, FindReplaceMode.Replace)
                    {
                        Owner = this
                    };
                    replaceWindow.ShowDialog();
                }
            }
        }
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null)
            {
                richTextBox.FontSize += 2;
                richTextBox.Focus();
            }
        }
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditorTabControl.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);
            if (richTextBox != null && richTextBox.FontSize > 6)
            {
                richTextBox.FontSize -= 2;
                richTextBox.Focus();
            }
        }
    }



    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);

            if (richTextBox != null)
            {
                richTextBox.SelectAll();
                richTextBox.Focus();
            }
        }
    }


    private void TimeAndDateButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEdit.IsChecked = false;

        if (EditorTabControl.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid grid)
        {
            var richTextBox = FindChildControl<RichTextBox>(grid);

            if (richTextBox != null)
            {
                string dateTimeString = DateTime.Now.ToString("HH:mm d.M.yyyy.");
                richTextBox.Selection.Text = dateTimeString;
                richTextBox.Focus();
            }
        }
    }



    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FontSizeComboBox.Items.Clear();
        double[] fontSizes = new double[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
        foreach (double size in fontSizes)
        {
            FontSizeComboBox.Items.Add(size);
        }

        FontSizeComboBox.SelectedIndex = fontSizes.ToList().IndexOf(12);

        FontComboBox.Items.Clear();
        bool defaultFontFound = false;
        foreach (FontFamily font in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
        {
            FontComboBox.Items.Add(font.Source);
            if (font.Source.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                defaultFontFound = true;
            }
        }
        if (!defaultFontFound)
        {
            FontComboBox.Items.Insert(0, "Arial");
        }

        FontComboBox.SelectedItem = "Arial";

        if (EditorTabControl.SelectedItem is TabItem selectedTab)
        {
            var grid = FindParent<Grid>(selectedTab);

            if (grid != null)
            {
                var currentTextBox = FindChildControl<RichTextBox>(grid);

                if (currentTextBox != null)
                {
                    currentTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Arial"));
                }
            }
        }
    }


    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);

        while (parent != null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as T;
    }

    private void Update_SelectionChanged()
    {
        CloseAllShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        CloseButton.IsEnabled = EditorTabControl.Items.Count > 0;
        CloseShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SaveButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SaveAssButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SaveAllButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SaveShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SaveAllShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        BoldShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        ItalicShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        UnderlineShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        PasteButton.IsEnabled = EditorTabControl.Items.Count > 0;
        PasteShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        FindShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        ReplaceShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        FindButton.IsEnabled = EditorTabControl.Items.Count > 0;
        ReplaceButton.IsEnabled = EditorTabControl.Items.Count > 0;
        ZoomInShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        ZoomOutShortcutButton.IsEnabled = EditorTabControl.Items.Count > 0;
        TimeAndDateButton.IsEnabled = EditorTabControl.Items.Count > 0;
        SelectAllButton.IsEnabled = EditorTabControl.Items.Count > 0;
    }

    private void Delete_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            DeleteButton.IsEnabled = !richTextBox.Selection.IsEmpty;
        }
    }

    private void Copy_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            CopyButton.IsEnabled = !richTextBox.Selection.IsEmpty;
            CopyShortcutButton.IsEnabled = !richTextBox.Selection.IsEmpty;
            CutShortcutButton.IsEnabled = !richTextBox.Selection.IsEmpty;
            CutButton.IsEnabled = !richTextBox.Selection.IsEmpty;
        }
    }


    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            string text = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;

            if (EditorTabControl.SelectedItem is TabItem currentTab)
            {
                UpdateTabHeaderButtonsVisibility(currentTab, text);
            }
        }
    }


    private void UpdateTabHeaderButtonsVisibility(TabItem tab, string text)
    {
        var circleButton = FindVisualChild<Button>(tab, "CircleButton");
        var closeButton = FindVisualChild<Button>(tab, "CloseButton");

        if (circleButton != null && closeButton != null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                circleButton.Visibility = Visibility.Collapsed;
                closeButton.Visibility = Visibility.Visible;
            }
            else
            {
                circleButton.Visibility = Visibility.Visible;
                closeButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private T? FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                var frameworkElement = child as FrameworkElement;
                if (frameworkElement?.Name == name)
                {
                    return typedChild;
                }
            }

            var result = FindVisualChild<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    private T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is T tChild && (child as FrameworkElement)?.Name == childName)
            {
                return tChild;
            }

            T? result = FindChild<T>(child, childName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}