namespace Scanner.Codes;

public readonly struct UserCode
{
    public string Code { get; } = string.Empty;

    public static UserCode Empty => default;

    private UserCode(string text)
    {
        Code = text.ToUpper();
    }

    public static UserCode? TryCreate(string text)
    {
        if (IsUserCode(text))
        {
            return new UserCode(text);
        }
        return null;
    }
    
    private static bool IsUserCode(string text)
        => text.Length == 6 && text.All(char.IsLetterOrDigit);
}