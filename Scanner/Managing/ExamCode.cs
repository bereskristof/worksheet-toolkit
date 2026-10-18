namespace Scanner.Managing;

public readonly struct ExamCode : ICode<ExamCode>, IComparable<ExamCode>
{
    public static readonly ExamCode Empty = new();
    
    private enum Active {
        Null,
        LegacyUuid,
        SevenChar,
    }

    private static readonly Dictionary<char, char> ReplacementChars = new() {
        { 'B', '8' },
        { 'I', '1' },
        { 'O', '0' },
        { 'S', '5' },
    };
    
    /// Pseudo union tag
    private readonly Active _active = Active.Null;
    
    private readonly Guid _guid = Guid.Empty;
    private readonly string _sevenChar = string.Empty;

    public string Code => _active switch
    {
        Active.Null => string.Empty,
        Active.LegacyUuid => _guid.ToString(),
        Active.SevenChar => _sevenChar,
        _ => throw new ArgumentOutOfRangeException(nameof(_active), "ExamCode invariance was broken!"),
    };
    
    // Needed for default initialization.
    public ExamCode()
    { }

    private ExamCode(Guid guid)
    {
        _active = Active.LegacyUuid;
        _guid = guid;
    }

    private ExamCode(string sevenChar)
    {
        _active = Active.SevenChar;
        _sevenChar = NormalizeSevenChar(sevenChar);
    }

    public static ExamCode Create(string text)
    {
        if (IsSevenChar(text))
        {
            return new ExamCode(text);
        }
        if (IsUuid(text))
        {
            return new ExamCode(Guid.ParseExact(text, "N"));
        }
        return Empty;
    }
    
    /// Allows skipping the '-' character.
    /// Replaces 'B', 'I', 'S', and especially 'O' with their similar counterparts.
    private static bool IsSevenChar(string text)
    {
        text = NormalizeSevenChar(text);
        var isCorrectLen = text.Length == 8;
        var isAlphanumeric = text[..2].All(char.IsLetterOrDigit) && text[3..].All(char.IsLetterOrDigit);
        var isDashCorrect = text[2] == '-';

        return isCorrectLen && isAlphanumeric && isDashCorrect;
    }

    private static string NormalizeSevenChar(string text)
    {
        text = text.ToUpper();
        foreach (var (from, to) in ReplacementChars)
        {
            text = text.Replace(from, to);
        }
        if (text.Length == 7)
        {
            text = text[..2] + '-' + text[2..];
        }
        return text;
    }

    private static bool IsUuid(string text)
        => Guid.TryParseExact(text, "N", out _);

    public override string ToString()
    {
        return _active switch
        {
            Active.LegacyUuid => (_guid == Guid.Empty) ? string.Empty : _guid.ToString(),
            Active.SevenChar => _sevenChar,
            _ => throw new ArgumentOutOfRangeException(nameof(_active), "ExamCode invariant was broken!"),
        };
    }

    public int CompareTo(ExamCode other) => string.Compare(Code, other.Code, StringComparison.Ordinal);
}