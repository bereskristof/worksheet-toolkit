using System.Windows;
using System.Windows.Controls;

namespace Interface.Data;

public partial class AnswerKeyRow
{
    public enum Answer
    {
        None = -1,
        A = 0,
        B = 1,
        C = 2,
        D = 3,
        E = 4,
    }

    private readonly RadioButton[] _buttons;
    private readonly int _index;
    
    public AnswerKeyRow(int index, Answer answer)
    {
        InitializeComponent();
        _buttons = [ButtonA, ButtonB, ButtonC, ButtonD, ButtonE];
        _index = index + 1;
        UpdateAnswer(answer);
    }

    internal void UpdateAnswer(Answer answer)
    {
        Visibility = (answer == Answer.None) ? Visibility.Hidden : Visibility.Visible;
        RowLabel.Content = $"{_index,2}. {answer.ToString()}";
        foreach (var button in _buttons)
            button.IsChecked = false;
        if (answer != Answer.None)
            _buttons[(int)answer].IsChecked = true;
    }
}

public static class AnswerKeyRowExtensions
{
    public static AnswerKeyRow.Answer ToAnswer(this long answer) => (AnswerKeyRow.Answer)answer;
}