using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Storage;

public class CodeGenerator
{
    // To keep the characters equally distributed, B I O S are removed.
    // These characters were chosen as they are the most mistakable characters.
    private const string AllowedCharacters = "ACDEFGHJKLMNPQRTUVWXYZ0123456789";
    private const int CharacterCount = 32;

    private const int MaximumCharacterRepeat = 2;
    
    private readonly HashSet<string> _alreadyGeneratedCodes = [];

    public CodeGenerator(SqliteConnection connection)
    {
        Trace.Assert(AllowedCharacters.Length == CharacterCount);
        var existingCodes = connection.CreateCommand();
        existingCodes.CommandText = "SELECT DISTINCT Uuid FROM Solutions WHERE LENGTH(Uuid) = 8;";
        var reader = existingCodes.ExecuteReader();
        while (reader.Read())
        {
            var key = reader.GetString(0);
            var ret = _alreadyGeneratedCodes.Add(key);
            if (!ret)
            {
                Log.Write($"Duplicate key found: '{key}'", Log.Severity.Error);
            }
        }
    }
    
    public string GenerateCode()
    {
        while (true)
        {
            var code = GetCode();
            if (IsCodeTooRepeated(code)) continue;
            if (Profanity.ContainsProfanity(code.Replace("-", "")))
            {
                Log.Write($"GenerateCode: Profanity found '{code}'");
                continue;
            }
            if (!_alreadyGeneratedCodes.Add(code)) continue;
            return code;
        }
    }

    private static string GetCode()
    {
        var code = new char[7];
        var randomBytes = new byte[7];
        RandomNumberGenerator.Fill(randomBytes);
        for (var i = 0; i < 7; i++)
        {
            code[i] = AllowedCharacters[randomBytes[i] % CharacterCount];
        }
        return new string(code[..2]) + "-" + new string(code[2..]);
    }

    private bool IsCodeTooRepeated(string code)
    {
        var maximumRepeat = code.GroupBy(x => x).Select(group => group.Count()).Max();
        if (maximumRepeat > MaximumCharacterRepeat)
        {
            Log.Write($"GenerateCode: Too many characters repeating '{code}'");
        }
        return maximumRepeat > MaximumCharacterRepeat;
    }
}