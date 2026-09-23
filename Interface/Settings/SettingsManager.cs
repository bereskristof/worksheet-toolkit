using System.Globalization;
using Microsoft.Win32;
using Storage;

namespace Interface.Settings;

public static class SettingsManager
{
    public enum Language
    {
        // NOTE: Ordering should match the setting menu dropdown order!
        En = 0,
        Hu = 1,
    }

    /// Convert the language enum to a culture
    public static CultureInfo ToCulture(this Language language) => CultureInfo.GetCultureInfo(language switch
    {
        Language.Hu => "hu-HU",
        _ => "en-US",
    });

    /// Convert the language enum to a culture name (e.g.: "en-US")
    public static string ToCultureName(this Language language) => language switch
    {
        Language.Hu => "hu-HU",
        _ => "en-US",
    };
    
    private const string KeyPath = @"SOFTWARE\WorksheetToolkit";
    private const string ProgramLocaleKey = "ProgramLocale";
    private const string TexPathKey = "TexPath";
    private const string CsvHeadersKey = "CsvHeaders";

    /// Used to avoid needlessly updating the registry multiple times.
    private static string _texPathUpdateCache = string.Empty;

    private static readonly RegistryKey Key =
        Registry.CurrentUser.OpenSubKey(KeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree) ??
        throw new RegistryInaccessibleException();

    /// Get the currently set language/locale from the registry.
    /// Falls back to the systems locale if the registry is empty.
    internal static Language GetLanguage()
    {
        var language = Key.GetValue(ProgramLocaleKey) as string ?? GetLanguageFromSystemCulture();
        Log.Write($"Culture obtained: {language}");
        return language switch
        {
            "hu-HU" => Language.Hu,
            _ => Language.En,
        };
    }

    private static string GetLanguageFromSystemCulture()
    {
        var language = CultureInfo.CurrentCulture.Name;
        Log.Write($"Culture obtained from system: {language}");
        return language;
    }
    
    /// Set the programs' language.
    internal static void SetLanguage(Language newLanguage)
    {
        var newLanguageName = newLanguage.ToCultureName();
        Key.SetValue(ProgramLocaleKey, newLanguageName);
        Log.Write($"Culture set in registry: {newLanguageName}");
    }

    /// Get the currently set path for LaTeX binaries.
    internal static string? GetTexPath()
    {
        var texPath = Key.GetValue(TexPathKey) as string;
        _texPathUpdateCache = texPath ?? string.Empty;
        Log.Write($"Tex path obtained: {texPath}");
        return texPath;
    }

    /// Set the LaTeX path to search if PATH fails.
    internal static void SetTexPath(string texPath)
    {
        if (_texPathUpdateCache == texPath)
            return;
        Key.SetValue(TexPathKey, texPath);
        Log.Write($"Tex path set in registry: {texPath}");
    }

    /// Get whether headers are enabled for CSV exports.
    internal static bool GetCsvHeaders()
    {
        var def = Convert.ToInt32(true);
        var isEnabled = Convert.ToBoolean(Key.GetValue(CsvHeadersKey) as int? ?? def);
        Log.Write($"Csv header settings obtained: {isEnabled}");
        return isEnabled;
    }

    /// Set whether headers are enabled for CSV exports.
    internal static void SetCsvHeaders(bool isEnabled)
    {
        Key.SetValue(CsvHeadersKey, Convert.ToInt32(isEnabled));
        Log.Write($"Csv headers set in registry: {isEnabled}");
    }
}