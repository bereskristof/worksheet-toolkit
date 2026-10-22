using System.Windows;
using System.Windows.Controls;
using Storage.Task;

namespace Interface.Sheet;

public partial class SheetPage
{
    TaskElement? _questionSelectionTarget;
    
    private static QuestionList Questions => Bindings.Instance.Questions;
    
    public SheetPage()
    {
        InitializeComponent();
        TaskList.DataContext = Questions;
        RootSelector.Node = Bindings.Instance.SheetRoot;
    }

    private void SheetEditor_OnRequestsTaskSelection(object? sender, EventArgs e)
    {
        if (sender is not TaskElement taskElement)
            return;
        SwapToQuestionSelection(taskElement);
    }

    private void SwapToQuestionSelection(TaskElement target)
    {
        TaskList.IsEnabled = true;
        SheetTreeView.IsEnabled = false;
        _questionSelectionTarget = target;
        TaskList.SelectedItem = target.Node.Question;
    }

    private void SwapToSheetEditor()
    {
        TaskList.IsEnabled = false;
        SheetTreeView.IsEnabled = true;
        _questionSelectionTarget = null;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        if (button.DataContext is not Question question)
            return;
        _questionSelectionTarget?.UpdateQuestion(question);
        SwapToSheetEditor();
    }
}