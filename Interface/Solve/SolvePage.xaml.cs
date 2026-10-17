using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Interface.Settings;
using Interface.Solve.ResultView;
using Microsoft.Win32;
using Scanner.Bubbles;
using Scanner.Codes;
using Scanner.Collect;
using Scanner.Managing;

namespace Interface.Solve;

public partial class SolvePage
{
    private const int DimX = 1200;
    private const int DimY = 1700;

    private int _resultsTableTaskColumnCount = 0;
    
    private readonly ObservableCollection<ScanResultView> _csvBuffer = [];
    private readonly DataGridColumn[] _resultsTableInitialColumns;

    private static bool _skipExamSessionPrompts = false;
    private static bool _skipUserSessionPrompts = false;
    
    public SolvePage()
    {
        InitializeComponent();
        _resultsTableInitialColumns = [.. ResultsTable.Columns];
        ResultsTable.DataContext = _csvBuffer;
        ResultsTable.Visibility = Visibility.Visible;
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
        ResetTaskCount();
        ProgressBar.Maximum = GetPageCount(path);
        SolvePagesAsync(path);
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
                v.Backing.Ok?.Results.Select(q => q.TaskIndex).Max()).Max() ?? 0;
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

    private void UpdateProgress(ScanResultView result)
    {
        // Insert ordered
        // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.binarysearch?view=net-10.0#returns
        var index = _csvBuffer.ToList().BinarySearch(result);
        if (index < 0)
            index = ~index;
        _csvBuffer.Insert(index, result);
        
        UpdateResultsTableColumns(result);
        ProgressBar.Value += 1;
    }

    private async void SolvePagesAsync(string path)
    {
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));
        var pageCount = reader.GetPageCount();
        var mutex = new Mutex();
        
        var range = Enumerable.Range(0, pageCount);
        // All tasks are awaited later:
        // ReSharper disable once AccessToDisposedClosure
        var tasks = range.Select(i => System.Threading.Tasks.Task.Run(() => SolveSinglePage(reader, i, mutex))).ToList();
        
        while (tasks.Count > 0)
        {
            var completed = await System.Threading.Tasks.Task.WhenAny(tasks);
            tasks.Remove(completed);
            
            var (pageIndex, results) = await completed;
            var page = pageIndex + 1;
            var scanResultView = new ScanResultView(page, results);
            UpdateProgress(scanResultView);
        }

        FinishSolvingPages();
    }

    private static Tuple<int, ScanResults> SolveSinglePage(IDocReader reader, int pageIndex, Mutex mutex)
    {
        var pageStep = Scanner.Scanner.Begin();
        var codeStep = pageStep.GetPage(reader, pageIndex);
        var codeResult = codeStep.GetCodes();
        var bubblesStep = codeResult switch
        {
            IStepCodesBubbles bubbles => bubbles,
            MissingData missing => HandleMissingExamDataEntry(codeStep, missing, mutex),
            _ => throw new UnreachableException(),
        };
        var checkStep = bubblesStep.GetSelectedAnswers();
        var collectStep = checkStep.CheckAnswers();
        var collectResult = collectStep.CollectData();
        var result = collectResult switch
        {
            ScanResults results => results,
            MissingData missing => HandleMissingUserDataEntry(collectStep, missing, mutex),
            _ => throw new UnreachableException(),
        };
        return new Tuple<int, ScanResults>(pageIndex, result);
    }

    private static IStepCodesBubbles HandleMissingExamDataEntry(IStepCodes codeStep, MissingData missing, Mutex mutex)
    {
        mutex.WaitOne();
        try
        {
            if (_skipExamSessionPrompts)
                return codeStep.GetCodes(missing);
            var data = DispatchHelpDialog(missing);
            // This should be temporary while Rider updated its union support
            #pragma warning disable CS8655
            return (data) switch
            #pragma warning restore CS8655
            {
                MissingData skip => codeStep.GetCodes(skip),
                FilledData set => codeStep.GetCodes(set),
            };
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static ScanResults HandleMissingUserDataEntry(IStepCollect collectStep, MissingData missing, Mutex mutex)
    {
        mutex.WaitOne();
        try
        {
            if (_skipExamSessionPrompts)
                return collectStep.CollectData(missing);
            var data = DispatchHelpDialog(missing);
            return collectStep.CollectData(data);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
    
    private static AnyData DispatchHelpDialog(MissingData missing)
    {
        AnyData? data = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new MissingCodeTool
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MissingData = missing,
            };
            window.Setup();
            window.ShowDialog();
            data = window.ReturnAnyData;
            _skipExamSessionPrompts = window.SkipFutureExamPrompts;
            _skipUserSessionPrompts = window.SkipFutureUserPrompts;
        });
        
        return data ?? throw new InvalidDataException();
    }

    private void FinishSolvingPages()
    {
        ExportResultsButton.IsEnabled = true;
        ExportResultsAltButton.IsEnabled = true;
        ImportPdfTextbox.IsEnabled = true;
        BrowseButton.IsEnabled = true;
        CorrectPdf.IsEnabled = true;
        _skipExamSessionPrompts = false;
        _skipUserSessionPrompts = false;
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

    private void ResetTaskCount()
    {
        _resultsTableTaskColumnCount = 0;
        ResultsTable.Columns.Clear();
        foreach (var column in _resultsTableInitialColumns)
            ResultsTable.Columns.Add(column);
    }

    private void ResetSelected_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        if (button.DataContext is not ScanResultView view)
            return;
        view.Reset();
    }
}