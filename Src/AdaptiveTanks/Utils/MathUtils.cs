using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class MathUtils
{
    public static bool ApproxEqAbsolute(float a, float b, float epsilon) =>
        Mathf.Abs(a - b) < epsilon;

    public static void Clamp(this ref float value, float min, float max) =>
        value = Mathf.Clamp(value, min, max);

    public static float MinByMagnitude(float a, float b) =>
        Mathf.Abs(a) < Mathf.Abs(b) ? a : b;

    public static float RoundUpTo(float value, float increment) =>
        Mathf.Ceil(value / increment) * increment;

    public static float RoundDownTo(float value, float increment) =>
        Mathf.Floor(value / increment) * increment;

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
        List<(float x, bool enter)> endpoints = intervals
            .SelectMany(interval => new[] { (interval.x, true), (interval.y, false) })
            .ToList();

        if (endpoints.Count == 0) return false;

        endpoints.Sort((a, b) => a.x.CompareTo(b.x));

        var coverCount = 0;
        var lastEndpoint = endpoints[0].x;
        foreach (var (x, open) in endpoints)
        {
            if (coverCount == 0 && x != lastEndpoint) return false;
            coverCount += open ? 1 : -1;
            if (coverCount == 0) lastEndpoint = x;
        }

        return true;
    }
}
