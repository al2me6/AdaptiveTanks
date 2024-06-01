using System.Collections.Generic;
using System.Linq;

namespace KnapsackProblem;

public static class Divisors
{
    public static float GCD(float a, float b)
    {
        while (b > float.Epsilon)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    public static float GCD(this IEnumerable<float> vals) => vals.Aggregate(GCD);
}
