using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Storage.Sheet;

namespace Interface.Data;

public partial class DataPage
{
    private const uint MaxAnswerCount = SheetTreeInfo.MaximumNumberOfQuestions;
    
    private List<AnswerKeyRow> _rows = [];
    public ObservableCollection<string> ExamKeys { get; private set; } = [];

    public DataPage()
    {
        InitializeComponent();
        GenerateAnswerGrid();
    }

    private void GenerateAnswerGrid()
    {
        for (var i = 0; i < MaxAnswerCount; i++)
        {
            var row = new AnswerKeyRow(i, AnswerKeyRow.Answer.None);
            row.SetValue(Grid.RowProperty, i + 1);
            _rows.Add(row);
            AnswerGrid.Children.Add(row);
            var rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            AnswerGrid.RowDefinitions.Add(rowDefinition);
        }
    }

    /// Reload the key list
    private void ReloadExamKeys()
    {
        ExamKeys.Clear();
        var iterator = Storage.ExamResultObtainer.ObtainExamKeys();
        foreach (var key in iterator)
            ExamKeys.Add(key);
    }

    private void ReloadButton_OnClick(object sender, RoutedEventArgs e) => ReloadExamKeys();

    private void AnswerList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;
        DisplayAnswers(e.AddedItems[0]?.ToString() ?? throw new InvalidOperationException());
    }

    private void DisplayAnswers(string examKey)
    {
        ExamCodeHeader.Visibility = Visibility.Visible;
        ExamCodeLabel.Content = examKey;
        var answers = Storage.ExamResultObtainer.ObtainCorrectAnswers(examKey);
        for (var i = 0; i < answers.Length; i++)
            _rows[i].UpdateAnswer(answers[i].AnswerNumber.ToAnswer());
        for (var i = answers.Length; i < MaxAnswerCount; i++)
            _rows[i].UpdateAnswer(AnswerKeyRow.Answer.None);
    }
}