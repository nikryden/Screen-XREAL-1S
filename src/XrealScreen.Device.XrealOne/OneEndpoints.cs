namespace XrealScreen.Device.XrealOne;

/// <summary>
/// Network endpoints exposed by One-series glasses over USB (NCM adapter).
/// [from-source:MIT] Skarian/one-xr, SamiMitwalli/One-Pro-IMU-Retriever-Demo.
/// [hypothesis] XREAL 1S uses the same endpoints — verify in M1 (docs/testing/hardware-test-M1.md).
/// </summary>
public sealed record OneEndpoints
{
    public string Host { get; init; } = "169.254.2.1";

    public int MetadataPort { get; init; } = 52996;

    public int CameraPort { get; init; } = 52997;

    /// <summary>IMU/magnetometer report stream.</summary>
    public int StreamPort { get; init; } = 52998;

    public int ControlPort { get; init; } = 52999;

    public static OneEndpoints Default { get; } = new();

    public IEnumerable<(string Name, int Port)> AllPorts()
    {
        yield return ("metadata", MetadataPort);
        yield return ("camera", CameraPort);
        yield return ("stream", StreamPort);
        yield return ("control", ControlPort);
    }
}
