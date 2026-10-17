namespace Scanner.Managing;

public interface ICode<out T> where T : ICode<T>
{
    public string Code { get; }

    public static abstract T Create(string text);
    
    public bool IsSet() => !string.IsNullOrEmpty(Code);
}

public static class CodeExtensions
{
    public static Valid<T>? TryValidate<T>(this T code) where T : ICode<T>
        => Valid<T>.TryValidate(code);
}