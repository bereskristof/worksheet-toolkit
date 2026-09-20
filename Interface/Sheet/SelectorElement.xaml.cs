using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Storage.Sheet;

namespace Interface.Sheet;

public partial class SelectorElement : ICanRequestDeletion
{
    public event EventHandler? RequestsDeletion;
    
    public event EventHandler? RequestsTaskSelection;
    
    public SelectorNode Node { get; set; }
    
    private readonly int _depth;
    
    public SelectorElement() : this(Bindings.Instance.SheetRoot) { } // Not exactly a good idea

    private SelectorElement(SelectorNode node, int depth = 0)
    {
        _depth = depth;
        Node = node;
        InitializeComponent();
        FormatDesignByIndent(_depth);
    }

    private void OptionListOrdered_OnSelected(object sender, RoutedEventArgs e)
        => Node.Type = SelectorNode.SelectorType.Sequential;

    private void OptionListShuffled_OnSelected(object sender, RoutedEventArgs e) 
        => Node.Type = SelectorNode.SelectorType.Shuffled;

    private void OptionPickShuffled_OnSelected(object sender, RoutedEventArgs e) 
        => Node.Type = SelectorNode.SelectorType.Random;

    private void ButtonHide_OnClick(object sender, RoutedEventArgs e) 
        => ToggleHide();

    private void ButtonAddTask_OnClick(object sender, RoutedEventArgs e)
        => AddChildTask();

    private void ButtonAddContainer_OnClick(object sender, RoutedEventArgs e)
        => AddChildSelector();

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
        => Delete(true);
    
    private void FormatDesignByIndent(int indentLevel)
    {
        MainBorder.Background = DepthFormatting.GetBrushFromIndent(indentLevel);
        if (indentLevel == 0) 
            FormatRoot();
    }

    private void FormatRoot()
    {
        MainBorder.Background = Brushes.Transparent;
        MainBorder.BorderBrush = Brushes.Transparent;
        MainBorder.BorderThickness = new Thickness(0);
        MainBorder.Padding = new Thickness(0);
        MainBorder.Margin = new Thickness(0, 0, 3, 0);
        TitleTextBlock.Text = Interface.Resources.Lang.Sheet_RootTitle;
        DeleteButton.Visibility = Visibility.Collapsed;
        HideButton.Visibility = Visibility.Collapsed;
    }

    private void ToggleHide()
    {
        ContentList.Visibility = ContentList.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        ManageButtons.Visibility = ManageButtons.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        BehaviourBox.IsEnabled = !BehaviourBox.IsEnabled;
        HideButton.Content = ContentList.Visibility == Visibility.Collapsed ? Interface.Resources.Lang.Sheet_Show : Interface.Resources.Lang.Sheet_Hide;
    }
    
    private void AddChildTask()
    {
        var node = new TaskNode { Question = null };
        Node.Children.Add(node);
        var task = new TaskElement(node, _depth + 1);
        ContentList.Children.Add(task);
        task.RequestsDeletion += DeleteChild;
        task.RequestsTaskSelection += BubbleTaskSelection;
    }

    private void AddChildSelector()
    {
        var node = new SelectorNode();
        Node.Children.Add(node);
        var selector = new SelectorElement(node, _depth + 1);
        ContentList.Children.Add(selector);
        selector.RequestsDeletion += DeleteChild;
        selector.RequestsTaskSelection += BubbleTaskSelection;
    }

    private void BubbleTaskSelection(object? sender, EventArgs e) 
        => RequestsTaskSelection?.Invoke(sender, e);

    private void DeleteChild(object? sender, EventArgs e)
    {
        if (sender is TaskElement task)
            Node.Children.Remove(task.Node);
        ContentList.Children.Remove(sender! as Control);
    }

    public void Delete(bool root = false)
    {
        foreach (var child in ContentList.Children)
        {
            switch (child)
            {
                case SelectorElement selectorElement:
                    selectorElement.Delete();
                    break;
                case TaskElement taskElement:
                    taskElement.Delete();
                    Node.Children.Remove(taskElement.Node);
                    break;
            }
        }

        if (!root) return;
        RequestsDeletion?.Invoke(this, EventArgs.Empty);
    }
}