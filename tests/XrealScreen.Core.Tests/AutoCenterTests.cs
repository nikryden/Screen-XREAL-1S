using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Tests;

public class AutoCenterTests
{
    private static float Rad(float deg) => deg * MathF.PI / 180f;
    private static float Deg(float rad) => rad * 180f / MathF.PI;

    [Fact]
    public void Manual_NeverMovesReference()
    {
        var ac = new AutoCenter { Settings = new AutoCenterSettings { Policy = RecenterPolicy.Manual } };
        for (int i = 0; i < 1000; i++)
        {
            ac.Update(Rad(60), 0.01f);
        }

        Assert.Equal(0f, ac.ReferenceYaw);
    }

    [Fact]
    public void Follow_InsideDeadZone_DoesNotMove()
    {
        var ac = new AutoCenter { Settings = new AutoCenterSettings { Policy = RecenterPolicy.Follow, DeadZoneDegrees = 20 } };
        for (int i = 0; i < 1000; i++)
        {
            ac.Update(Rad(15), 0.01f);
        }

        Assert.Equal(0f, ac.ReferenceYaw);
    }

    [Fact]
    public void Follow_OutsideDeadZone_SettlesAtDeadZoneEdge()
    {
        var ac = new AutoCenter { Settings = new AutoCenterSettings { Policy = RecenterPolicy.Follow, DeadZoneDegrees = 20, SmoothingSeconds = 0.3f } };
        float relative = 0;
        for (int i = 0; i < 1000; i++)
        {
            relative = ac.Update(Rad(70), 0.01f);
        }

        Assert.InRange(Deg(relative), 19.9f, 20.1f);
        Assert.InRange(Deg(ac.ReferenceYaw), 49.9f, 50.1f);
    }

    [Fact]
    public void Follow_RespectsMaxSpeed()
    {
        var ac = new AutoCenter { Settings = new AutoCenterSettings { Policy = RecenterPolicy.Follow, DeadZoneDegrees = 0, SmoothingSeconds = 0, MaxFollowSpeedDegrees = 30 } };
        ac.Update(Rad(90), 0.1f);
        Assert.InRange(Deg(ac.ReferenceYaw), 2.99f, 3.01f);
    }

    [Fact]
    public void Follow_WrapsAcrossPlusMinus180()
    {
        var ac = new AutoCenter { Settings = new AutoCenterSettings { Policy = RecenterPolicy.Follow, DeadZoneDegrees = 10, SmoothingSeconds = 0.1f } };
        ac.Recenter(Rad(175));
        float relative = 0;
        for (int i = 0; i < 500; i++)
        {
            relative = ac.Update(Rad(-155), 0.01f); // 30° to the left across the seam
        }

        Assert.InRange(Deg(relative), 9.9f, 10.1f);
    }
}
