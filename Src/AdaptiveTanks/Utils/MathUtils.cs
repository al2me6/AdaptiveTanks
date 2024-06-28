using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class MathUtils
{
    public static bool ApproxEqAbsolute(float a, float b, float epsilon) =>
        Mathf.Abs(a - b) < epsilon;

    public static float MinByMagnitude(float a, float b) => Mathf.Abs(a) < Mathf.Abs(b) ? a : b;
}
