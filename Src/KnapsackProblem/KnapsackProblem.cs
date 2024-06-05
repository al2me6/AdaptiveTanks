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
    where T : IKnapsackItem
    {
        var dp = new List<float>(maxWeight + 1);
        var i = 0;
        for (; i <= maxWeight; ++i)
        {
            dp.Add(0);
            for (var j = 0; j < items.Length; ++j)
            {
                if (weights[j] <= i)
                {
                    dp[i] = Math.Max(dp[i], dp[i - weights[j]] + values[j]);
                }
            }
        }

        // Find the items in the first solution
        var firstSolution = Recurse(dp, maxWeight, items, weights, values);

        // Continue taking steps until the solution changes
        var newMaxWeight = maxWeight;
        var oldValue = dp.Last();
        while (oldValue == dp.Last())
        {
            newMaxWeight += 1;
            dp.Add(0);
            for (var j = 0; j < items.Length; ++j)
            {
                if (weights[j] <= newMaxWeight)
                {
                    dp[newMaxWeight] = Math.Max(dp[newMaxWeight], dp[newMaxWeight - weights[j]] + values[j]);
                }
            }
        }
        var secondSolution = Recurse(dp, newMaxWeight, items, weights, values);

        // Check which solution is closest to maxWeight
        // TODO: Check that this math is correct
        var firstFrac = firstSolution.Select(x => x.Weight()).Sum() / maxWeight;
        var secondFrac = maxWeight / secondSolution.Select(x => x.Weight()).Sum();
        return firstFrac > secondFrac ? firstSolution : secondSolution;
    }

    private static List<T> Recurse<T>(List<float> DPTable, int currentWeight, T[] items, int[] weights, float[] values)
    {
        // BaseCase: The current weight is 0
        if (currentWeight == 0)
            return new List<T>();

        // TODO: Maybe reverse iteration direction
        for (int idx = items.Length - 1; idx >= 0; idx--)
        {
            if (weights[idx] > currentWeight)
                continue;
            // Check if removing the weight of the item removes the correct value
            if (DPTable[currentWeight - weights[idx]] + values[idx] == DPTable[currentWeight])
            {
                // Recurse here. That returns a list (or null), append this item to that list if it exists
                List<T> solution = Recurse(DPTable, currentWeight - weights[idx], items, weights, values);
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
