using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Interface.Settings;
using Storage;

namespace Interface;

public partial class MainEditor
{
    private ulong _activeWorkerSerial = 0;
    private string[] _missingPackages = [];
    
    public MainEditor()
    {
        InitializeComponent();
        CreateTexCheckWorker();
    }

    /// Creates a new `BackgroundWorker` to test if PdfLaTeX is accessible and set-up correctly. 
    private void CreateTexCheckWorker()
    {
        _activeWorkerSerial++;
        BackgroundWorker backgroundWorker = new();
        backgroundWorker.DoWork += BackgroundLoader_DoWork;
        backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        backgroundWorker.RunWorkerAsync(_activeWorkerSerial);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var latexStatus = TexDoctor.VerifyTexInstallation(out var missingPackages, SettingsManager.GetTexPath() ?? string.Empty);
        _missingPackages = missingPackages;
        ulong mySerial = (ulong)(e.Argument ?? 0);
        e.Result = (latexStatus, mySerial);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var (latexStatus, mySerial) = (ValueTuple<TexDoctor.TexStatus, ulong>)(e.Result ?? (-1, 0));
        if (mySerial != _activeWorkerSerial) // Checking if result became out of date.
        {
            Log.Write("TexCheck background worker was out of date.");
            return;
        }
        TexStatusProgressBar.Visibility = Visibility.Collapsed;
        switch (latexStatus)
        {
            case TexDoctor.TexStatus.Operational:
                break;
            case TexDoctor.TexStatus.MissingPackages:
                WarningBox.Visibility = Visibility.Visible;
                var missingPackagesString = string.Join(", ", _missingPackages);
                var missingPackagesWarning = Interface.Resources.Lang.Tex_MissingPackages
                    .Replace("#PACKAGES#", missingPackagesString);
                WarningLabel.Content = Formatting.FormatPlurality(missingPackagesWarning, _missingPackages);
                break;
            case TexDoctor.TexStatus.NotFound:
                ErrorBox.Visibility = Visibility.Visible;
                break;
            default:
                throw new UnreachableException("VerifyTexInstallation TexStatus outside of enum range.");
        }

    }

    private void SettingsPage_OnTexPathWasUpdated(object? sender, EventArgs e)
    {
        TexStatusProgressBar.Visibility = Visibility.Visible;
        WarningBox.Visibility = Visibility.Collapsed;
        ErrorBox.Visibility = Visibility.Collapsed;
        CreateTexCheckWorker();
    }
}