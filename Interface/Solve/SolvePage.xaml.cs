using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Interface.Settings;
using Microsoft.Win32;
using Scanner;
using Scanner.Result;
using Storage;

namespace Interface.Solve;

public partial class SolvePage
{
    private const int DimX = 1200;
    private const int DimY = 1700;

    private int _resultsTableTaskColumnCount = 0;
    
    private readonly BackgroundWorker _backgroundWorker = new();
    private readonly ObservableCollection<ScanResultView> _csvBuffer = [];
    
    public SolvePage()
    {
        InitializeComponent();
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        ResultsTable.DataContext = _csvBuffer;
    }

    private void ImportPdf_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetLoadPath(Interface.Resources.Lang.Browse_SolveImportTitle);
        if (string.IsNullOrEmpty(path)) 
            return;

        ImportPdfTextbox.Text = path;
    }

    private void CorrectPdf_OnClick(object sender, RoutedEventArgs e)
    {
        var pdfPath = ImportPdfTextbox.Text;
        if (!IsPathValidPdf(pdfPath))
        {
            MessageBox.Show(Interface.Resources.Lang.Solve_NotAPdf, Interface.Resources.Lang.Common_Error,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        BeginFileCorrection(pdfPath);
    }

    private void BeginFileCorrection(string path)
    {
        ProgressBar.Value = 0;
        ExportResultsButton.IsEnabled = false;
        ExportResultsAltButton.IsEnabled = false;
        ImportPdfTextbox.IsEnabled = false;
        BrowseButton.IsEnabled = false;
        CorrectPdf.IsEnabled = false;
        _csvBuffer.Clear();
        _backgroundWorker.WorkerReportsProgress = true;
        ProgressBar.Maximum = GetPageCount(path);
        _backgroundWorker.RunWorkerAsync(argument: path);
    }

    /// Toggle CorrectPdf button based on if path is set to a value.
    private void ImportPdfTextbox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textboxText = ImportPdfTextbox.Text;
        var isPathValid = File.Exists(textboxText);
        var isPathEmpty = string.IsNullOrEmpty(textboxText);
        CorrectPdf.IsEnabled = isPathValid;
        InvalidPathWarningBox.Visibility = (isPathValid || isPathEmpty) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// Checks if the provided file has a valid PDF file signature.
    private static bool IsPathValidPdf(string path)
    {
        try
        {
            var fileSignature = new byte[5];
            var file = File.Open(path, FileMode.Open);
            file.ReadExactly(fileSignature);
            file.Close();
            return fileSignature
                .Zip("%PDF-")
                .Select(s => s.First == s.Second)
                .All(b => b);
        }
        catch (Exception e) when (e is PathTooLongException or UnauthorizedAccessException or NotSupportedException
                                      or EndOfStreamException or IOException)
        {
            return false;
        }
    }

    private void ExportResults_OnClick(object sender, RoutedEventArgs e)
    {
        var exportPath = GetSavePath(Interface.Resources.Lang.Browse_SolveExportTitle);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            var locale = GetLocaleSpecifics();
            var stringLines = _csvBuffer.Select((v, i) => v.ToSheetCsv(i, locale));
            var maxTaskCount = _csvBuffer.Select(v => v.TaskCount).Max();
            var sb = GetStringFromDualStringBuilders(stringLines, GetCsvHeader(locale, maxTaskCount));
            File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(Interface.Resources.Lang.Export_ExportSaved, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Interface.Resources.Lang.Export_ExportFailed + "\n" + ex.Message, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportStats_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: This only works in cases where every test has the exact same tasks, just shuffled.
        var exportPath = GetSavePath(Interface.Resources.Lang.Browse_SolveExportTitle);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            var locale = GetLocaleSpecifics();
            var stringLines = _csvBuffer.Select((v, i) => v.ToTaskCsv(i, locale));
            var maxTaskIndex = _csvBuffer.Select(v =>
                v.ScanResult.Results.Select(q => q.TaskIndex).Where(i => i.Get() is not null).Select(i => (int)i.Get()!)
                    .Max()).Max();
            var sb = GetStringFromDualStringBuilders(stringLines, GetCsvHeader(locale, maxTaskIndex));
            File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(Interface.Resources.Lang.Export_ExportSaved, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Interface.Resources.Lang.Export_ExportFailed + "\n" + ex.Message, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private StringBuilder GetStringFromDualStringBuilders(IEnumerable<DualAutoSeparatedStringBuilder> stringLines, string header)
    {
        var text = new StringBuilder().Append(header);
        foreach (var line in stringLines)
        {
            text.Append(line);
            if (ExtraInfoCheckbox.IsChecked ?? false)
            {
                text.Append(line.ToExtraString());
            }
        }
        return text;
    }

    /// Return the header for the specified number of tasks.
    private static string GetCsvHeader(LocaleSpecifics locale, int taskCount)
    {
        if (!SettingsManager.GetCsvHeaders())
            return string.Empty;
        
        var sb = new StringBuilder();
        var sep = locale.ListSeparator;
        sb.Append(Interface.Resources.Lang.Solve_HeaderPage).Append(sep)
            .Append(Interface.Resources.Lang.Solve_HeaderNeptun).Append(sep)
            .Append(Interface.Resources.Lang.Solve_HeaderTotal).Append(sep)
            .Append(Interface.Resources.Lang.Solve_HeaderSuccess);
        var taskTemplate = Interface.Resources.Lang.Solve_HeaderTask;
        foreach (var taskNum in Enumerable.Range(0, taskCount))
        {
            sb.Append(sep).Append(taskTemplate.Replace("#NUM#", (taskNum + 1).ToString()));
        }
        return sb.Append('\n').ToString();
    }
    
    private static string GetLoadPath(string title)
    {
        OpenFileDialog openDialog = new OpenFileDialog
        {
            Filter = "Portable Document Format|*.pdf|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    private static string GetSavePath(string title)
    {
        SaveFileDialog openDialog = new SaveFileDialog
        {
            Filter = "Comma Separated Values|*.csv|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    private int GetPageCount(string path)
    {
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));
        return reader.GetPageCount();
    }

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        var result = (ScanResultView?)e.UserState ?? throw new ArgumentNullException(nameof(e.UserState));
        _csvBuffer.Add(result);
        UpdateResultsTableColumns(result);
        ProgressBar.Value = e.ProgressPercentage;
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var path = (string)(e.Argument ?? "");
        
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));

        var pageCount = reader.GetPageCount();

        var skipDialog = false;
    
        for (var i = 0; i < pageCount; i++)
        {
            ScanResult scanResult;
            try
            {
                scanResult = ScannerHandler.ScanPdfPage(reader, i);
            }
            catch (Exception ex)
            {
                Log.Write($"Unexpected exception while trying to check exam: {ex.Message}", Log.Severity.Error);
                scanResult = new ScanResult
                {
                    CurrentState = ScanResult.State.UnexpectedException,
                    ExamCode = null,
                    UserCode = null,
                    Results = [],
                };
            }

            // If there is a missing code, open the dialog to fix it.
            if (!skipDialog
                && scanResult.CurrentState is ScanResult.State.MissingExamCode or ScanResult.State.MissingUserCode)
            {
                var pageImg = ScannerHandler.GetSinglePageAsBitmap(reader, i);
                DispatchHelpDialog(pageImg, ref scanResult, out skipDialog);
                
                try 
                {
                    HandleUnfinishedResults(ref scanResult, i, reader);
                }
                catch (Exception ex)
                {
                    Log.Write($"Unexpected exception while trying to check manually corrected exam: {ex.Message}", Log.Severity.Error);
                    scanResult.CurrentState = ScanResult.State.UnexpectedException;
                    scanResult.Results = [];
                }
            }

            var page = i + 1;
            var scanResultView = new ScanResultView(page, scanResult);
            _backgroundWorker.ReportProgress(page, scanResultView);
        }
    }

    private static void DispatchHelpDialog(Bitmap image, ref ScanResult invalidResult, out bool skipFuture)
    {
        bool skipFuturePrompts = false;
        ScanResult invalidResultCopy = invalidResult;
        ScanResult newResult = invalidResult;
        Application.Current.Dispatcher.Invoke(() =>
        {
            MissingCodeTool window = new MissingCodeTool
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FixedResult = invalidResultCopy,
            };
            window.Setup(image, invalidResultCopy);
            window.ShowDialog();
            newResult = window.FixedResult;
            skipFuturePrompts = window.SkipFuturePrompts;
        });
        skipFuture = skipFuturePrompts;
        invalidResult = newResult;
    }

    private static void HandleUnfinishedResults(ref ScanResult result, int i, IDocReader reader)
    {
        switch (result.CurrentState)
        {
            case ScanResult.State.MissingExamCode:
                return; // If still missing code, skip processing
            case ScanResult.State.ManuallyCorrected:
            {
                var pageImg = ScannerHandler.GetSinglePageAsBitmap(reader, i);
                result = ScannerHandler.ScanPageResults(pageImg, 5, result);
                break;
            }
        }
        ScannerHandler.ProcessScanResults(ref result, i);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        ExportResultsButton.IsEnabled = true;
        ExportResultsAltButton.IsEnabled = true;
        ImportPdfTextbox.IsEnabled = true;
        BrowseButton.IsEnabled = true;
        CorrectPdf.IsEnabled = true;
    }

    /// Return LocaleSpecifics based on the CsvLocale setting.
    /// NOTE: Locale settings does *not* modify header & success texts, those rely on the program's language.
    /// Locale exists specifically due to Excel's bullshit, and is not meant to be a separate language setting for exports.
    private static LocaleSpecifics GetLocaleSpecifics()
    {
        return GetLocaleSpecificsFromCulture(SettingsManager.GetCsvLocale() switch
        {
            SettingsManager.CsvLocale.LanguageBased => CultureInfo.CurrentCulture,
            SettingsManager.CsvLocale.LocaleBased => CultureInfo.InstalledUICulture,
            SettingsManager.CsvLocale.English => SettingsManager.Language.En.ToCulture(),
            SettingsManager.CsvLocale.Hungarian => SettingsManager.Language.Hu.ToCulture(),
            _ => throw new ArgumentOutOfRangeException("", @"GetCsvLocale() returned an unexpected value."),
        });
    }

    private static LocaleSpecifics GetLocaleSpecificsFromCulture(CultureInfo culture)
    {
        return new LocaleSpecifics
        {
            Culture = culture,
            DecimalPoint = culture.NumberFormat.NumberDecimalSeparator,
            ListSeparator = culture.TextInfo.ListSeparator,
            SuccessText = Interface.Resources.Lang.Export_ResultSuccess,
            FailText = Interface.Resources.Lang.Export_ResultFailure,
        };
    }

    private void UpdateResultsTableColumns(ScanResultView result)
    {
        var taskCount = result.TaskCount;
        if (_resultsTableTaskColumnCount < taskCount)
        {
            ExtendDataGridToTaskCount(taskCount);
        }
    }

    private void ExtendDataGridToTaskCount(int taskCount)
    {
        const int rightHandColumnCount = 2;
        var firstRightHandColumnIndex = ResultsTable.Columns.Count - rightHandColumnCount;
        var rightHandColumns = ResultsTable.Columns
            .Skip(firstRightHandColumnIndex)
            .Take(rightHandColumnCount)
            .ToArray(); // ToArray() is used to create a copy of the data, otherwise only references are used, which would point to incorrect data.
        for (int i = ResultsTable.Columns.Count - 1; i >= firstRightHandColumnIndex; i--)
            ResultsTable.Columns.RemoveAt(i);

        while (_resultsTableTaskColumnCount < taskCount)
        {
            var column = new DataGridTextColumn();
            column.Header = Interface.Resources.Lang.Solve_HeaderTaskShort.Replace("#NUM#",
                (_resultsTableTaskColumnCount + 1).ToString());
            column.Binding = new Binding($"Points[{_resultsTableTaskColumnCount}]");
            ResultsTable.Columns.Add(column);
            _resultsTableTaskColumnCount++;
        }
        foreach (var rightHandColumn in rightHandColumns)
            ResultsTable.Columns.Add(rightHandColumn);
    }
}