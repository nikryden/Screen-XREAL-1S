using System.Numerics;
using XrealScreen.Core.Workspace;

namespace XrealScreen.Render.Scene;

/// <summary>What the renderer needs per frame: view orientation and eye position (neck model).</summary>
/// <param name="Orientation">Orientation the screens are viewed with (possibly constrained to yaw only).</param>
/// <param name="EyeOffset">Eye position relative to the rest position, world FLU metres.</param>
public readonly record struct HeadView(Quaternion Orientation, Vector3 EyeOffset);

/// <summary>Optics of the glasses (per-eye image; the 2D signal is shown to both eyes).</summary>
/// <param name="HorizontalFovDegrees">XREAL 1S: 52° diagonal on 16:10 ≈ 45° × 29° (vendor spec, [hypothesis] exact).</param>
public sealed record GlassesOptics(float HorizontalFovDegrees = 45f, float VerticalFovDegrees = 29f)
{
    public static GlassesOptics Xreal1S { get; } = new();
}

/// <summary>
/// Coordinate conventions. World and head frames are the tracker body frame: X forward, Y left, Z up
/// (see <see cref="Core.Tracking.HeadPose"/>). View space is D3D left-handed: x right, y up, z forward.
/// All matrices use System.Numerics row-vector convention (v * M) and are uploaded as HLSL row_major.
/// </summary>
public static class ViewMath
{
    /// <summary>Maps head-frame FLU (x fwd, y left, z up) to view space (x right, y up, z fwd).</summary>
    public static readonly Matrix4x4 FluToView = new(
        0, 0, 1, 0,   // x (forward) → z
        -1, 0, 0, 0,  // y (left)    → -x
        0, 1, 0, 0,   // z (up)      → y
        0, 0, 0, 1);

    /// <summary>
    /// Typical offset from the neck pivot to the eyes in the head frame (FLU, metres): 8 cm forward, 11 cm up.
    /// The 1S only measures rotation; this neck model turns nodding/leaning rotations into a small eye
    /// translation so the screens come closer when you lean in ([verified-hw] user request 2026-09-26).
    /// </summary>
    public static readonly Vector3 DefaultNeckToEye = new(0.08f, 0f, 0.11f);

    /// <summary>Eye position (world FLU, relative to the eye position when looking straight ahead) for a head orientation.</summary>
    public static Vector3 EyeOffset(Quaternion headRelative, Vector3 neckToEye) =>
        Vector3.Transform(neckToEye, Quaternion.Normalize(headRelative)) - neckToEye;

    /// <summary>World → clip transform for a head orientation (relative to the workspace center).</summary>
    /// <param name="neckToEye">Neck-model offset; <see cref="Vector3.Zero"/> = rotate about the eyes (no parallax).</param>
    public static Matrix4x4 ViewProjection(Quaternion headRelative, GlassesOptics optics, float near = 0.05f, float far = 100f, Vector3 neckToEye = default) =>
        ViewProjection(new HeadView(headRelative, EyeOffset(headRelative, neckToEye)), optics, near, far);

    /// <summary>World → clip transform for a prepared <see cref="HeadView"/>.</summary>
    public static Matrix4x4 ViewProjection(HeadView view, GlassesOptics optics, float near = 0.05f, float far = 100f)
    {
        ArgumentNullException.ThrowIfNull(optics);
        var worldToHead = Matrix4x4.CreateTranslation(-view.EyeOffset) * Matrix4x4.CreateFromQuaternion(Quaternion.Conjugate(Quaternion.Normalize(view.Orientation)));
        float vfov = optics.VerticalFovDegrees * MathF.PI / 180f;
        float aspect = MathF.Tan(optics.HorizontalFovDegrees * MathF.PI / 360f) / MathF.Tan(vfov / 2f);
        var projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(vfov, aspect, near, far);
        return worldToHead * FluToView * projection;
    }

    /// <summary>
    /// Builds a curved (cylinder segment) screen mesh in world FLU coordinates.
    /// Vertex = position (xyz) + texcoord (uv). Triangle list, <paramref name="columns"/> segments.
    /// </summary>
    public static float[] BuildScreenMesh(ScreenPlacement placement, int columns = 32)
    {
        float r = placement.DistanceMeters;
        float halfAngle = placement.WidthMeters / r / 2f;               // arc length → angle
        float centerYaw = placement.YawDegrees * MathF.PI / 180f;
        float centerZ = MathF.Tan(placement.PitchDegrees * MathF.PI / 180f) * r;
        float halfH = placement.HeightMeters / 2f;

        var v = new List<float>(columns * 6 * 5);
        for (int c = 0; c < columns; c++)
        {
            float u0 = (float)c / columns, u1 = (float)(c + 1) / columns;
            // u = 0 is the screen's left edge = larger yaw (positive yaw is to the left).
            float a0 = centerYaw + halfAngle - u0 * 2f * halfAngle;
            float a1 = centerYaw + halfAngle - u1 * 2f * halfAngle;
            var p00 = Point(a0, centerZ + halfH); // top-left
            var p10 = Point(a1, centerZ + halfH); // top-right
            var p01 = Point(a0, centerZ - halfH); // bottom-left
            var p11 = Point(a1, centerZ - halfH); // bottom-right
            Add(p00, u0, 0); Add(p10, u1, 0); Add(p01, u0, 1);
            Add(p01, u0, 1); Add(p10, u1, 0); Add(p11, u1, 1);
        }

        return [.. v];

        Vector3 Point(float yaw, float z) => new(r * MathF.Cos(yaw), r * MathF.Sin(yaw), z);

        void Add(Vector3 p, float u, float t)
        {
            v.Add(p.X); v.Add(p.Y); v.Add(p.Z); v.Add(u); v.Add(t);
        }
    }
}
