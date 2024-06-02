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

        return Recurse(dp, maxWeight, items, weights, values);
    }

    private static List<T> Recurse<T>(float[] DPTable, int currentWeight, T[] items, int[] weights, float[] values)
    {
        // BaseCase: The current weight is 0
        if (currentWeight == 0)
            return new List<T>();

        // TODO: Maybe reverse iteration direction
        for (int idx = items.Length-1; idx >= 0; idx--)
        {
            if (weights[idx] > currentWeight)
                continue;
            // Check if removing the weight of the item removes the correct value
            if (DPTable[currentWeight - weights[idx]] + values[idx] == DPTable[currentWeight])
            {
                // Recurse here. That returns a list (or null), append this item to that list if it exists
                List<T> solution = Recurse(DPTable, currentWeight-weights[idx], items, weights, values);
                if (solution == null)
                    continue;
                solution.Add(items[idx]);
                return solution;
            }
        }

        // No item matched the jumps in the DP table, return null
        return null;
    }
}
