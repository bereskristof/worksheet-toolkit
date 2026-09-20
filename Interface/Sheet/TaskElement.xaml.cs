using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Storage.Sheet;
using Storage.Task;

namespace Interface.Sheet;

public partial class TaskElement : ICanRequestDeletion
{
    public event EventHandler? RequestsDeletion;
    
    public event EventHandler? RequestsTaskSelection;
    
    public TaskNode Node { get; set; }

    public TaskElement() : this(new TaskNode { Question = null }) { }

    internal TaskElement(TaskNode node, int depth = 0)
    {
        Node = node;
        InitializeComponent();
        FormatDesignByIndent(depth);
    }

    private void ButtonSelect_OnClick(object sender, RoutedEventArgs e)
        => SelectQuestion();

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e) 
        => Delete(true);

    private void FormatDesignByIndent(int indentLevel)
    {
        MainBorder.Background = DepthFormatting.GetBrushFromIndent(indentLevel);
    }
    
    private void SelectQuestion() 
        => RequestsTaskSelection?.Invoke(this, EventArgs.Empty);

    public void Delete(bool root = false)
    {
        if (!root) return;
        RequestsDeletion?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateQuestion(Question question)
    {
        Binding questionBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Text"), 
            UpdateSourceTrigger = UpdateSourceTrigger.Default,
            Mode = BindingMode.OneWay,
        };
        BindingOperations.SetBinding(QuestionPreview, TextBlock.TextProperty, questionBinding);
        Node.Question = question;
    }
}