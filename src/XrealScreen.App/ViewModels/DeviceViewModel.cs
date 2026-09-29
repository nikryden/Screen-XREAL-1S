using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using XrealScreen.Device.XrealOne;

namespace XrealScreen.App.ViewModels;

/// <summary>Device page: checks the glasses' USB network link and ports (same as `xrs probe`).</summary>
public sealed partial class DeviceViewModel : ObservableObject
{
    public ObservableCollection<string> Results { get; } = [];

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = "Not checked";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Plug in the glasses and select Check connection.";

    [ObservableProperty]
    public partial InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;

    [RelayCommand]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        Results.Clear();
        StatusTitle = "Checking…";
        StatusMessage = string.Empty;
        Severity = InfoBarSeverity.Informational;

        var endpoints = OneEndpoints.Default;
        var adapters = await Task.Run(() => NcmAdapterLocator.FindCandidates(endpoints.Host), cancellationToken).ConfigureAwait(true);
        if (adapters.Count == 0)
        {
            Results.Add("No USB network adapter on 169.254.2.x found.");
        }

        foreach (var a in adapters)
        {
            Results.Add($"Adapter: {a.Description} ({a.LocalAddress}, {a.Status})");
        }

        bool streamOpen = false;
        foreach (var (name, port) in endpoints.AllPorts())
        {
            var elapsed = await NcmAdapterLocator.TryConnectAsync(endpoints.Host, port, TimeSpan.FromSeconds(1.5), cancellationToken).ConfigureAwait(true);
            Results.Add($"{endpoints.Host}:{port} ({name}): {(elapsed is null ? "not reachable" : $"open, {elapsed.Value.TotalMilliseconds:F0} ms")}");
            streamOpen |= name == "stream" && elapsed is not null;
        }

        if (streamOpen)
        {
            StatusTitle = "Glasses reachable";
            StatusMessage = "The IMU stream port is open. Start head tracking on the Tracking page.";
            Severity = InfoBarSeverity.Success;
        }
        else
        {
            StatusTitle = "Glasses not reachable";
            StatusMessage = "No IMU stream found. See docs/testing/hardware-test-M1.md.";
            Severity = InfoBarSeverity.Warning;
        }
    }
}
