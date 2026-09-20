using System.Windows.Media;

namespace Interface.Sheet;

public static class DepthFormatting
{
    private static readonly Tuple<byte, byte, byte>[] IndentColors =
    [
        new(244, 244, 226),
        new(244, 244, 244),
        new(226, 226, 244),
        new(226, 244, 226),
    ];
    
    internal static Brush GetBrushFromIndent(int indentLevel)
    {
        var indCol = IndentColors.ElementAt(indentLevel % IndentColors.Length);
        return new SolidColorBrush(Color.FromRgb(indCol.Item1, indCol.Item2, indCol.Item3));
    }
}