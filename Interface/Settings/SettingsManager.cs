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
    /// This is saved in the registry.
    internal static void SetLanguage(Language newLanguage)
    {
        var newLanguageName = newLanguage.ToCultureName();
        Key.SetValue(ProgramLocaleKey, newLanguageName);
        Log.Write($"Culture set in registry: {newLanguageName}");
    }
}