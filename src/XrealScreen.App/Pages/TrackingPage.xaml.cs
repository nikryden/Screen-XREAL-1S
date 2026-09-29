using System.ComponentModel;
using System.Globalization;
using Microsoft.UI.Xaml.Controls;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Pages;

public sealed partial class TrackingPage : Page, INotifyPropertyChanged
{
    public TrackingPage()
    {
        InitializeComponent();
        Tracking.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrackingViewModel.IsRunning))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStopped)));
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrackingViewModel Tracking => App.Tracking;

    public bool IsStopped => !Tracking.IsRunning;

    public string FormatDegrees(double value) => value.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "°";
}
