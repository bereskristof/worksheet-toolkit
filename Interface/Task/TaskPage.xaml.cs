using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Storage.Task;

namespace Interface.Task;

public partial class TaskPage
{
    private Question? _currentQuestion;

    private static QuestionList Questions => Bindings.Instance.Questions;

    public TaskPage()
    {
        InitializeComponent();
        TaskList.DataContext = Questions;
    }

    private void TaskList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;
        if (grid.SelectedItem is not Question question)
            return;
        ChangeSelectedTask(question);
    }

    private void ChangeSelectedTask(Question question)
    {
        _currentQuestion = question;
        AnswerListPanel.ItemsSource = _currentQuestion.Answers;
        UpdateImagePreview();
        // Update bindings
        Binding questionBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Text"), 
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay,
        };
        BindingOperations.SetBinding(QuestionBox, TextBox.TextProperty, questionBinding);
        Binding scoreBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Points"), 
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay,
        };
        BindingOperations.SetBinding(ScoreBox, TextBox.TextProperty, scoreBinding);
        TaskList.Items.Refresh();
        TaskList.SelectedItem = question;
        SetRightControls();
    }

    private void UpdateImagePreview()
    {
        if (_currentQuestion == null) return;
        ImagePanel.Visibility = Visibility.Collapsed;
        ImageProgressBar.Visibility = Visibility.Visible;
        ImageButton.IsEnabled = false;
        BackgroundWorker asyncImageLoader = new();
        asyncImageLoader.DoWork += BackgroundLoader_DoWork;
        asyncImageLoader.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        asyncImageLoader.RunWorkerAsync(_currentQuestion.Id);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        long id = (long)(e.Argument ?? -1);
        if (_currentQuestion == null) return;
        var stream = _currentQuestion.FetchImageStream();
        e.Result = new Tuple<long, MemoryStream?>(id, stream);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        ImageProgressBar.Visibility = Visibility.Collapsed;
        if (e.Result is not Tuple<long, MemoryStream?> result) return;
        if (result.Item1 != _currentQuestion?.Id) return; // Skip if the question has changed
        if (result.Item2 == null)
        {
            ImagePreview.Source = null;
            ImagePanel.Visibility = Visibility.Collapsed;
            ImageButton.IsEnabled = true;
            return;
        }
        ImagePreview.Source = BitmapFrame.Create(result.Item2, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        ImagePanel.Visibility = Visibility.Visible;
    }

    private void SetRightControls(bool enable = true)
    {
        QuestionBox.IsEnabled = enable;
        ScoreBox.IsEnabled = enable;
        // ImageButton.IsEnabled = true; // Handled elsewhere
        AnswerAddButton.IsEnabled = enable;
        AnswerDeleteButton.IsEnabled = enable;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var question = Questions.Add();
        _currentQuestion = question;
        ChangeSelectedTask(question);
        TaskList.ScrollIntoView(TaskList.Items[^1]!);
    }

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentQuestion == null) return; // Does nothing if no question is selected
        Questions.Remove(_currentQuestion);
        SetRightControls(false);
        ImageButton.IsEnabled = false;
    }

    private void ButtonDeleteImage_OnClick(object sender, RoutedEventArgs e)
    {
        _currentQuestion?.DeleteImage();
        UpdateImagePreview();
    }

    private void ButtonAddAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        _currentQuestion?.Answers.Add(_currentQuestion);
    }

    private void ButtonDeleteAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        if (AnswerListPanel.SelectedItem is not Answer answer)
            return;
        _currentQuestion?.Answers.Remove(answer);
    }

    private void ImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetLoadPath(Interface.Resources.Lang.Browse_ImageImportTitle);
        _currentQuestion?.StoreImageFromPath(path);
        UpdateImagePreview();
    }
    
    private static string GetLoadPath(string title)
    {
        OpenFileDialog openDialog = new OpenFileDialog
        {
            Filter = "Supported files|*.png;*.jpg;*.jpeg;*.pdf|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }

    private void TaskList_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        TaskList.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }
}