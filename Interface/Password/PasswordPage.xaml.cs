using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Interface.Password;

public partial class PasswordPage
{
    public event EventHandler? PasswordUnlocked;
    private readonly BackgroundWorker _backgroundLoader = new();
    private bool _lastUnlockResult;
    
    public PasswordPage()
    {
        InitializeComponent();
        _backgroundLoader.DoWork += BackgroundLoader_DoWork;
        _backgroundLoader.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        Loaded += Page_Loaded;
    }
    
    // Main password page methods

    private void Page_Loaded(object sender, RoutedEventArgs e) 
        => MainPasswordBox.Focus();

    private void MainPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e) 
        => IncorrectLabel.Visibility = Visibility.Hidden;

    private void ButtonLoadDb_OnClick(object sender, RoutedEventArgs e)
    {
        string password = MainPasswordBox.Password;
        LoadingBar.Visibility = Visibility.Visible;
        _backgroundLoader.RunWorkerAsync(password);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        string password = e.Argument?.ToString() ?? string.Empty;
        _lastUnlockResult = Storage.Encryption.TryPassword(password);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        LoadingBar.Visibility = Visibility.Hidden;
        if (_lastUnlockResult)
            PasswordUnlocked?.Invoke(this, EventArgs.Empty);
        else
            IncorrectLabel.Visibility = Visibility.Visible;
    }
    
    // Page mode switching methods

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PasswordMode.IsSelected) SwapToPasswordMode();
        else if (ImportMode.IsSelected) SwapToImportMode();
        else if (CreateMode.IsSelected) SwapToNewMode();
    }
    
    public void SwapToImportMode(bool force = false)
    {
        if (force) ImportMode.IsSelected = true;
        AddFileBox.Text = GetDefaultDbPath();
        BackButtonUpdateVisibility();
    }
    
    private void SwapToPasswordMode(bool force = false)
    {
        if (force) PasswordMode.IsSelected = true;
        BackButtonUpdateVisibility();
    }
    
    public void SwapToNewMode(bool force = false)
    {
        if (force) CreateMode.IsSelected = true;
        BackButtonUpdateVisibility();
    }
    
    private void BackButtonUpdateVisibility()
    {
        PasswordMode.Visibility = GetDefaultDbPath() == string.Empty ? Visibility.Collapsed : Visibility.Visible;
    }
    
    // Creation page methods

    private void CreateBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetSavePath(Interface.Resources.Lang.Setup_Title);
        if (!string.IsNullOrEmpty(path))
            NewFileBox.Text = path;
    }

    private void CreateConfirm_OnClick(object sender, RoutedEventArgs e) 
        => CreateNewDatabase(NewFileBox.Text.Trim(), NewPasswordBox.Password);

    private void NewPasswordBox_GotFocus(object sender, RoutedEventArgs e) 
        => UpdatePasswordHint(NewPasswordBox.Password.Length);

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) 
        => UpdatePasswordHint(NewPasswordBox.Password.Length);

    private void NewPasswordBox_OnLostFocus(object sender, RoutedEventArgs e)
        => PasswordHintLabel.Visibility = Visibility.Hidden;

    private void UpdatePasswordHint(long passwordLength) 
        => PasswordHintLabel.Visibility = passwordLength >= 8 ? Visibility.Hidden : Visibility.Visible;

    private static string GetSavePath(string title)
    {
        SaveFileDialog saveDialog = new SaveFileDialog
        {
            Filter = "Database|*.db|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return saveDialog.ShowDialog() == true ? Path.GetFullPath(saveDialog.FileName) : string.Empty;
    }

    private void CreateNewDatabase(string filepath, string password)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_InvalidPath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Storage.Manager.CreateDatabase(filepath, password);
        MessageBox.Show(Interface.Resources.Lang.PasswordResult_CreatedSuccess, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        SaveDatabasePath(filepath);
        SwapToPasswordMode(true);
    }
    
    // Import page methods
    
    private void ImportBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetLoadPath(Interface.Resources.Lang.Import_Title);
        if (!string.IsNullOrEmpty(path))
            AddFileBox.Text = path;
    }
    
    private void ImportConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        string filepath = AddFileBox.Text.Trim();
        if (string.IsNullOrEmpty(filepath))
        {
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_InvalidPath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (Storage.Manager.OpenDatabase(filepath))
        {
            SaveDatabasePath(filepath);
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_ImportedSuccess, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            SwapToPasswordMode(true);
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_ImportFailed, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private static string GetLoadPath(string title)
    {
        OpenFileDialog openDialog = new OpenFileDialog
        {
            Filter = "Database|*.db|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    // Common methods
    
    private static void SaveDatabasePath(string filepath)
    {
        var key = Registry.CurrentUser.CreateSubKey(Interface.Resources.RegistryNames.KeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
        key.SetValue(Interface.Resources.RegistryNames.ValueDbPath, filepath);
    }
    
    private static string GetDefaultDbPath()
    {
        RegistryKey? path = Registry.CurrentUser.OpenSubKey(Interface.Resources.RegistryNames.KeyPath);
        string defaultPath = path?.GetValue(Interface.Resources.RegistryNames.ValueDbPath) as string ?? string.Empty;
        return defaultPath;
    }

    private void LanguageEnglish_Selected(object sender, RoutedEventArgs e) 
        => ChangeLanguage("en-US");

    private void LanguageHungarian_Selected(object sender, RoutedEventArgs e) 
        => ChangeLanguage("hu-HU");

    private static void ChangeLanguage(string newCulture)
    {
        var key = Registry.CurrentUser.CreateSubKey(Interface.Resources.RegistryNames.KeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
        key.SetValue(Interface.Resources.RegistryNames.ValueLocale, newCulture);
        
        System.Diagnostics.Process.Start(Environment.ProcessPath ?? throw new InvalidOperationException());
        Application.Current.Shutdown();
    }

    /// Toggle settings sidebar visibility
    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var targetVisibility = SettingsBar.Visibility switch
        {
            Visibility.Collapsed => Visibility.Visible,
            _ => Visibility.Collapsed,
        };
        SettingsBar.Visibility = targetVisibility;
    }
}