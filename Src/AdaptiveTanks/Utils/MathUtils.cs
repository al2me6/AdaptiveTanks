using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class MathUtils
{
    public const float M3toL = 1000f;

    public static bool ApproxEqAbs(float a, float b, float epsilon) =>
        Mathf.Abs(a - b) < epsilon;

    public static bool ApproxEqRel(float a, float b, float ratio) =>
        Mathf.Abs(a - b) < Mathf.Abs(MaxByMagnitude(a, b)) * ratio;

    public static void Clamp(this ref float value, float min, float max) =>
        value = Mathf.Clamp(value, min, max);

    public static float MinByMagnitude(float a, float b) =>
        Mathf.Abs(a) < Mathf.Abs(b) ? a : b;

    public static float MaxByMagnitude(float a, float b) =>
        Mathf.Abs(a) > Mathf.Abs(b) ? a : b;

    public static float RoundUpTo(float value, float increment) =>
        Mathf.Ceil(value / increment) * increment;

    public static float RoundDownTo(float value, float increment) =>
        Mathf.Floor(value / increment) * increment;

    // https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.Private.CoreLib/src/System/MathF.cs#L56C13-L76C57
    public static unsafe float BitDecrement(float x)
    {
        var bits = *(int*)&x;

        if ((bits & 0x7F800000) >= 0x7F800000)
        {
            // NaN returns NaN
            // -Infinity returns -Infinity
            // +Infinity returns float.MaxValue
            return bits == 0x7F800000 ? float.MaxValue : x;
        }

        if (bits == 0x00000000)
        {
            // +0.0 returns -float.Epsilon
            return -float.Epsilon;
        }

        // Negative values need to be incremented
        // Positive values need to be decremented

        bits += bits < 0 ? +1 : -1;
        return *(float*)&bits;
    }

    /// Not the set-theoretic union!
    public static Vector2 BoundsOfIntervals(this IEnumerable<Vector2> intervals)
    {
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        foreach (var interval in intervals)
        {
            min = Mathf.Min(min, interval.x);
            max = Mathf.Max(max, interval.y);
        }

        return new Vector2(min, max);
    }

    public static Vector2 IntersectionOfIntervals(this IEnumerable<Vector2> intervals)
    {
        var min = float.NegativeInfinity;
        var max = float.PositiveInfinity;
        foreach (var interval in intervals)
        {
            min = Mathf.Max(min, interval.x);
            max = Mathf.Min(max, interval.y);
            if (max <= min) return new Vector2(min, min);
        }

        return new Vector2(min, max);
    }

    public static bool IntervalsAreContiguous(IEnumerable<Vector2> intervals)
    {
        var endpoints = intervals
            .SelectMany(i => new (float val, bool enter)[] { (i.x, true), (i.y, false) })
            .OrderBy(pt => pt.val)
            .ThenBy(pt => pt.enter) // Closes before opens.
            .ToList();

        if (endpoints.IsEmpty()) return false;

        var coverCount = 0;
        var lastEndpoint = endpoints[0].val;
        foreach (var (val, open) in endpoints)
        {
            if (coverCount == 0 && val != lastEndpoint) return false;
            coverCount += open ? 1 : -1;
            if (coverCount == 0) lastEndpoint = val;
        }

        return true;
    }

    public static float CylinderVolume(float diameter, float height) =>
        0.25f * Mathf.PI * diameter * diameter * height;

    /// <summary>
    /// Compute the volume of a spheroid.
    /// </summary>
    /// <param name="a">equatorial radius</param>
    /// <param name="c">polar radius</param>
    public static float SpheroidVolume(float a, float c) => 4f / 3f * Mathf.PI * a * a * c;

    public static float CylinderSurfaceArea(float diameter, float height) =>
        0.5f * Mathf.PI * diameter * diameter + Mathf.PI * diameter * height;

    /// <summary>
    /// Compute the surface area of a prolate or oblate spheroid.
    /// </summary>
    /// <param name="a">equatorial radius</param>
    /// <param name="c">polar radius</param>
    public static float SpheroidSurfaceArea(float a, float c)
    {
        var a_sq = a * a;
        var c_sq = c * c;
        if (ApproxEqRel(a, c, 1e-2f)) return 4 * Mathf.PI * a_sq; // nearly spherical
        if (c < a) // oblate
        {
            var e = Mathf.Sqrt(1f - c_sq / a_sq);
            return 2f * Mathf.PI * a_sq + Mathf.PI * c_sq / e * Mathf.Log((1f + e) / (1f - e));
        }
        else // c > a, prolate
        {
            var e = Mathf.Sqrt(1 - a_sq / c_sq);
            return 2 * Mathf.PI * a_sq * (1f + c / (a * e) * Mathf.Asin(e));
        }
    }
}