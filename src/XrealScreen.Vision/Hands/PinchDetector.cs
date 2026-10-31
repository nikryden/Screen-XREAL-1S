namespace XrealScreen.Vision.Hands;

public enum PinchEvent
{
    None,
    Pressed,
    Released,
}

/// <summary>
/// Turns the pinch ratio (thumb–index distance / hand size) into press and release events with hysteresis:
/// press below <see cref="CloseRatio"/> held for <see cref="MinHold"/>, release above <see cref="OpenRatio"/>.
/// A lost hand releases after <see cref="LostRelease"/> so a drag never sticks.
/// </summary>
public sealed class PinchDetector
{
    private bool _closingSeen;
    private double _closingSince;
    private double _lastHandSeconds;

    public float CloseRatio { get; init; } = 0.35f;

    public float OpenRatio { get; init; } = 0.6f;

    /// <summary>How long the ratio must stay closed before a press. At 15 fps one frame is 67 ms, so 0 = the first closed frame.</summary>
    public TimeSpan MinHold { get; init; } = TimeSpan.Zero;

    public TimeSpan LostRelease { get; init; } = TimeSpan.FromMilliseconds(300);

    public bool IsPressed { get; private set; }

    /// <summary>Feeds one frame. Pass <c>float.NaN</c> when no hand was found.</summary>
    public PinchEvent Update(float ratio, double timeSeconds)
    {
        if (float.IsNaN(ratio))
        {
            _closingSeen = false;
            if (IsPressed && timeSeconds - _lastHandSeconds >= LostRelease.TotalSeconds)
            {
                IsPressed = false;
                return PinchEvent.Released;
            }

            return PinchEvent.None;
        }

        _lastHandSeconds = timeSeconds;
        if (IsPressed)
        {
            if (ratio > OpenRatio)
            {
                IsPressed = false;
                return PinchEvent.Released;
            }

            return PinchEvent.None;
        }

        if (ratio < CloseRatio)
        {
            if (!_closingSeen)
            {
                _closingSeen = true;
                _closingSince = timeSeconds;
            }

            if (timeSeconds - _closingSince >= MinHold.TotalSeconds)
            {
                IsPressed = true;
                _closingSeen = false;
                return PinchEvent.Pressed;
            }
        }
        else
        {
            _closingSeen = false;
        }

        return PinchEvent.None;
    }

    public void Reset()
    {
        IsPressed = false;
        _closingSeen = false;
    }
}
