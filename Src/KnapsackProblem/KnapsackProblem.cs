using System;
using System.Collections.Generic;
using System.Linq;

namespace KnapsackProblem;

public interface IKnapsackItem
{
    public float Weight();
}

public static class UnboundedKnapsack
{
    public static List<T> SolveFloatApproximateAsDP<T>(T[] items, float maxWeight,
        Func<T, float> appraiser, float tolerance)
        where T : IKnapsackItem
    {
        var values = items.Select(appraiser).ToArray();

        var itemWeightGCD = items.Select(item => item.Weight()).GCD();
        var discretizationFactor = Math.Max(tolerance, itemWeightGCD);

        var rescaledWeights = items
            .Select(item => item.Weight())
            .Select(weight => (int)Math.Round(weight / discretizationFactor))
            .ToArray();
        var rescaledMaxWeight = (int)Math.Round(maxWeight / discretizationFactor);

        return SolveDP(items, rescaledWeights, values, rescaledMaxWeight);
    }

    public static List<T> SolveDP<T>(T[] items, int[] weights, float[] values, int maxWeight)
    {
        var dp = new float[maxWeight + 1];

        for (var i = 0; i <= maxWeight; ++i)
        {
            for (var j = 0; j < items.Length; ++j)
            {
                if (weights[j] <= i)
                {
                    dp[i] = Math.Max(dp[i], dp[i - weights[j]] + values[j]);
                }
            }
        }

        return new List<T>();
    }
}
