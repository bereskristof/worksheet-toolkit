using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Scanner;

namespace Interface.Solve;

public partial class MissingCodeTool : Window
{
    public ScanResult FixedResult = new();
    public bool SkipFuturePrompts = false;
    
    public MissingCodeTool()
    {
        InitializeComponent();
    }

    public void Setup(Bitmap image, ScanResult scanResult)
    {
        PreviewImage.Source = ConvertBitmapToImageSource(image);
        ExamTextBox.Text = (scanResult.ExamCode.ToString() ?? "").Replace("-", "");
        CodeTextBox.Text = scanResult.UserCode ?? "";
    }
    
    private static ImageSource ConvertBitmapToImageSource(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        memoryStream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }

    private void EnterButton_OnClick(object sender, RoutedEventArgs e)
    {
        string examCode = ExamTextBox.Text.Trim();
        if (CodeScanner.IsUuid(examCode)) 
        {
            FixedResult.ExamCode = Guid.Parse(examCode);
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_ExamCode, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        string userCode = CodeTextBox.Text.Trim();
        if (CodeScanner.IsNeptunCode(userCode))
        {
            FixedResult.UserCode = userCode;
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_Neptun, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!CodeScanner.IsValidScanResult(FixedResult))
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_ResultInvalid, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        FixedResult.CurrentState = ScanResult.State.ManuallyCorrected;
        Close();
    }

    private void SkipButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e) => SkipFuturePrompts = true;

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e) => SkipFuturePrompts = false;
}