using System.Globalization;
using System.Windows;
using Storage;

namespace Interface.Settings;

public partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();
        InitializeLanguageDropdown();
    }

    /// Initialize the language dropdown, setting its value to the programs current language,
    /// then connecting the required callbacks.
    private void InitializeLanguageDropdown()
    {
        LanguageDropdown.SelectedIndex = (int)SettingsManager.GetLanguage();
        LanguageDropdownEn.Selected += SetLanguageToEnglish;
        LanguageDropdownHu.Selected += SetLanguageToHungarian;
    }
    
    private void SetLanguageToEnglish(object sender, RoutedEventArgs e) 
        => SetLanguage(SettingsManager.Language.En);

    private void SetLanguageToHungarian(object sender, RoutedEventArgs e) 
        => SetLanguage(SettingsManager.Language.Hu);

    /// Common language changing method that asks the user to restart the application.
    private void SetLanguage(SettingsManager.Language targetLanguage)
    {
        SettingsManager.SetLanguage(targetLanguage);
        // Skip asking for restart if the new language is the current one
        if (targetLanguage.ToCultureName() == CultureInfo.CurrentUICulture.Name)
            return;
        var newCulture = targetLanguage.ToCulture();
        var restartMessage = Interface.Resources.Lang.ResourceManager.GetString("Settings_LanguageChangeRestart", newCulture);
        var titleMessage = Interface.Resources.Lang.ResourceManager.GetString("Settings_RestartRequired", newCulture);
        
        var restart = MessageBox.Show(restartMessage, titleMessage, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (restart == MessageBoxResult.Yes)
            RestartApplication();
    }

    /// Restart the application.
    private static void RestartApplication()
    {
        Log.Write("The program is restarting itself...");
        System.Diagnostics.Process.Start(Environment.ProcessPath ?? throw new InvalidOperationException());
        Application.Current.Shutdown();
    }
}