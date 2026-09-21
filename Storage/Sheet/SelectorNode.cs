using System.Security.Cryptography;
using Storage.Task;

namespace Storage.Sheet;

public class SelectorNode : ISheetNode
{
    public enum SelectorType
    {
        Random,
        Sequential,
        Shuffled,
    }
    
    public List<ISheetNode> Children { get; set; } = [];

    public SelectorType Type { get; set; } = SelectorType.Shuffled;
    
    public Question[] GetQuestions()
    {
        switch (Type)
        {
            case SelectorType.Random:
                var i = RandomNumberGenerator.GetInt32(0, Children.Count);
                return Children[i].GetQuestions();
            case SelectorType.Sequential:
                return Children.SelectMany(child => child.GetQuestions()).ToArray();
            case SelectorType.Shuffled:
                var arrayOfArrays = Children.Select(child => child.GetQuestions()).ToArray(); // Keeps order of children
                RandomNumberGenerator.Shuffle<Question[]>(arrayOfArrays);
                var array = arrayOfArrays.SelectMany(arrayOfArray => arrayOfArray).ToArray();
                return array;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public SheetTreeInfo GetSheetInfo()
    {
        var elementZero = new SheetTreeInfo(null);
        return Type switch
        {
            SelectorType.Random => Children
                .Select(node => node.GetSheetInfo())
                .Aggregate(elementZero, SheetTreeInfo.CombineOr),
            SelectorType.Sequential or SelectorType.Shuffled => Children
                .Select(node => node.GetSheetInfo())
                .Aggregate(elementZero, SheetTreeInfo.CombineAnd),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}