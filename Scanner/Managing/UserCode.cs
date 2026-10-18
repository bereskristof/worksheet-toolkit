namespace Scanner.Managing;

public readonly struct UserCode : ICode<UserCode>, IComparable<UserCode>
{
    public string Code { get; } = string.Empty;

    public static UserCode Empty => new();

    // Needed for default initialization.
    public UserCode()
    { }

    private UserCode(string text)
    {
        Code = text.ToUpper();
    }

    public static UserCode Create(string text)
    {
        if (IsUserCode(text))
        {
            return new UserCode(text);
        }
        return Empty;
    }
    
    private static bool IsUserCode(string text)
        => text.Length == 6 && text.All(char.IsLetterOrDigit);

    public int CompareTo(UserCode other) => string.Compare(Code, other.Code, StringComparison.Ordinal);
}