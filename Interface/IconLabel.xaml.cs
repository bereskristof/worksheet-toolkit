using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Interface;

public partial class IconLabel : INotifyPropertyChanged
{
    private static event EventHandler? ModeChanged;
    private static bool _areIconsEnabled = true;

    public static void EnableIcons(bool enable)
    {
        _areIconsEnabled = enable;
        ModeChanged?.Invoke(null, EventArgs.Empty);
    }


    private const int IconVerticalPadding = 2;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public IconLabel()
    {
        InitializeComponent();
        ModeChanged += (_, _) =>
        {
            UpdateVisibilityIcons();
            OnPropertyChanged(nameof(Text));
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(IsIconOnly));
        };
    }
    
    public required string Icon
    {
        get;
        set
        {
            field = value;
            UpdateVisibilityIcons();
            OnPropertyChanged();
        }
    } = string.Empty;

    public required string Text
    {
        get;
        set
        {
            field = value;
            UpdateVisibilityIcons();
            OnPropertyChanged();
        }
    } = string.Empty;

    public bool IsIconOnly
    {
        get;
        set
        {
            field = value;
            UpdateVisibilityIcons();
            OnPropertyChanged();
        }
    } = false;

    private void UpdateVisibilityIcons()
    {
        switch (_areIconsEnabled)
        {
            case true:
                UpdateVisibilityIconsOn();
                break;
            case false:
                UpdateVisibilityIconsOff();
                break;
        }
    }

    private void UpdateVisibilityIconsOff()
    {
        TextString.Visibility = Visibility.Visible;
        IconString.Visibility = Visibility.Collapsed;
        UpdatePadding();
    }

    private void UpdateVisibilityIconsOn()
    {
        var isIconSet = !string.IsNullOrEmpty(Icon);
        IconString.Visibility = Visibility.Visible;
        TextString.Visibility = IsIconOnly switch
        {
            true => isIconSet ? Visibility.Collapsed : Visibility.Visible,
            false => Visibility.Visible,
        };
        UpdatePadding();
    }

    private void UpdatePadding()
    {
        var isIconVisible = !string.IsNullOrEmpty(Icon) && IconString.Visibility == Visibility.Visible;
        var isTextVisible = !string.IsNullOrEmpty(Text) && TextString.Visibility == Visibility.Visible;
        IconString.Padding = (isIconVisible && isTextVisible) switch
        {
            true => new Thickness(0, IconVerticalPadding, 3, IconVerticalPadding),
            false => new Thickness(0, IconVerticalPadding, 0, IconVerticalPadding),
        };
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}