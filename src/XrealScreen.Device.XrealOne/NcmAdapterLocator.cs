using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XrealScreen.Device.XrealOne;

/// <summary>A local network adapter that looks like the glasses' USB network link.</summary>
public sealed record NcmAdapter(string Name, string Description, IPAddress LocalAddress, OperationalStatus Status);

/// <summary>
/// Finds the USB network adapter created by the glasses. The PC side is expected on
/// 169.254.2.x (observed 169.254.2.10 on One Pro) [from-source:research-only]; 1S [hypothesis].
/// </summary>
public static class NcmAdapterLocator
{
    public static IReadOnlyList<NcmAdapter> FindCandidates(string glassesHost = "169.254.2.1")
    {
        var target = IPAddress.Parse(glassesHost).GetAddressBytes();
        var result = new List<NcmAdapter>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                var local = unicast.Address.GetAddressBytes();
                // Same /24 as the glasses (169.254.2.x).
                if (local[0] == target[0] && local[1] == target[1] && local[2] == target[2])
                {
                    result.Add(new NcmAdapter(nic.Name, nic.Description, unicast.Address, nic.OperationalStatus));
                }
            }
        }

        return result;
    }

    /// <summary>Tries a TCP connect; returns the elapsed time or null when unreachable.</summary>
    public static async Task<TimeSpan?> TryConnectAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            return System.Diagnostics.Stopwatch.GetElapsedTime(start);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
