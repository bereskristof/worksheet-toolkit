using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Storage;

namespace Interface.Settings;

public partial class SettingsPage
{
    /// Sent when a new 
    public event EventHandler? TexPathWasUpdated;
    
    public SettingsPage()
    {
        InitializeComponent();
        InitializeLanguageDropdown();
        InitializeTexPathBox();
    }

    /// Initialize the language dropdown, setting its value to the programs current language,
    /// then connecting the required callbacks.
    private void InitializeLanguageDropdown()
    {
        LanguageDropdown.SelectedIndex = (int)SettingsManager.GetLanguage();
        LanguageDropdownEn.Selected += SetLanguageToEnglish;
        LanguageDropdownHu.Selected += SetLanguageToHungarian;
    }

    private void InitializeTexPathBox()
    {
        TexPath.Text = SettingsManager.GetTexPath() ?? string.Empty;
    }
    
    private static void SetLanguageToEnglish(object sender, RoutedEventArgs e) 
        => SetLanguage(SettingsManager.Language.En);

    private static void SetLanguageToHungarian(object sender, RoutedEventArgs e) 
        => SetLanguage(SettingsManager.Language.Hu);

    /// Common language changing method that asks the user to restart the application.
    private static void SetLanguage(SettingsManager.Language targetLanguage)
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

    /// Open a browse menu to set the latex `bin/` path. 
    private void TexBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var texDialog = new OpenFileDialog
        {
            Filter = "PdfLaTeX Executable|pdflatex.exe|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = Interface.Resources.Lang.Browse_FindTexTitle,
        };
        
        var maybeTexPath = texDialog.ShowDialog() == true ? Path.GetFullPath(texDialog.FileName) : null;
        if (maybeTexPath == null)
            return;
        
        TexPath.Text = maybeTexPath;
    }

    /// Tex path field was updated.
    private void TexPath_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var newPath = TexPath.Text;
        SettingsManager.SetTexPath(newPath);
        TexPathWasUpdated?.Invoke(this, EventArgs.Empty);
    }
}