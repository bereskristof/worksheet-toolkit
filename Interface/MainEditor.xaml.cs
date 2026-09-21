using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Storage;

namespace Interface;

public partial class MainEditor
{
    private readonly BackgroundWorker _backgroundWorker = new();
    private string[] _missingPackages = [];
    
    public MainEditor()
    {
        InitializeComponent();
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        _backgroundWorker.RunWorkerAsync();
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var latexStatus = TexDoctor.VerifyTexInstallation(out var missingPackages);
        _missingPackages = missingPackages;
        e.Result = latexStatus;
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var latexStatus = (TexDoctor.TexStatus)(e.Result ?? -1);
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
}