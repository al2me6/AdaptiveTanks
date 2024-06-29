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
}
