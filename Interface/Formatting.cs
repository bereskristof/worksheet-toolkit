using System.Collections;
using System.Text.RegularExpressions;
using Interface.Export;
using Storage.Sheet;

namespace Interface;

public static partial class Formatting
{
    public enum Subject
    {
        Singular,
        Plural,
    }

    [GeneratedRegex(@"\((.*?)(?:\|(.*?))?\)", RegexOptions.Compiled)]
    private static partial Regex SubjectRegex();

    /// Replaces combined forms: 'ha(ve|s)' with proper forms: 'has or have'.
    public static string FormatPlurality(string src, Subject subject)
    {
        var regex = SubjectRegex();
        foreach (Match match in regex.Matches(src))
        {
            var plural = match.Groups[1].Value;
            var singular = (match.Groups.Count > 1) ? match.Groups[2].Value : "";
            src = src.Replace(match.Value, (subject == Subject.Plural) ? plural : singular);
        }
        return src;
    }

    /// Replaces combined forms: 'ha(ve|s)' with proper forms: 'has or have'.
    /// Subject form depends on if `subjects.Count` is greater than 1.
    public static string FormatPlurality(string src, ICollection subjects)
    {
        var subject = CountableToSubject(subjects);
        return FormatPlurality(src, subject);
    }

    /// Generate a subject from a collection.
    /// Returns singular if count is 0 or 1, plural otherwise.
    public static Subject CountableToSubject(ICollection countable)
        => (countable.Count <= 1) ? Subject.Singular : Subject.Plural;
}