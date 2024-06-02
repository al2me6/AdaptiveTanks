using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks;

public static class BodySolver
{
    public static BodySolution SolvePreliminary(Asset[] availableAssets, float height)
    {
        Array.Sort(availableAssets, (a, b) => a.AspectRatio.CompareTo(b.AspectRatio));
        var minimumAspect = availableAssets.Select(asset => asset.AspectRatio).Min();

        List<Asset> stack = [];
        float runningHeight = 0;

        while (runningHeight < height)
        {
            var remainder = height - runningHeight;

            var bestAsset = availableAssets[0];
            var bestNewRemainder = float.PositiveInfinity;

            for (var i = 0; i < availableAssets.Length; ++i)
            {
                var candidate = availableAssets[i];
                var newRemainder = remainder - candidate.AspectRatio;
                var absNewRemainder = Mathf.Abs(newRemainder);

                // Worse than current best candidate. Skip.
                if (absNewRemainder > bestNewRemainder) continue;

                // Worse than not doing anything at all. Skip.
                if (absNewRemainder > remainder) continue;

                // Still under, but the remainder is too small to fit another segment
                // without overshooting. If this is not the smallest one, don't use it.
                if (0 < newRemainder && newRemainder < minimumAspect && i > 0) continue;

                // Went over with a larger-than-minimum candidate.
                if (newRemainder < 0 && i > 0)
                {
                    // Construct a guess from an alternative stack with an asset one smaller.
                    // If that gives a better outcome, take that.
                    var previousAspect = availableAssets[i - 1].AspectRatio;
                    var possibleStackWithPrevious = remainder - previousAspect - minimumAspect;
                    if (Mathf.Abs(possibleStackWithPrevious) < absNewRemainder) continue;
                }

                // Accept candidate.
                bestAsset = candidate;
                bestNewRemainder = absNewRemainder;
            }

            if (bestNewRemainder < float.PositiveInfinity)
            {
                var addedHeight = bestAsset.AspectRatio;
                stack.Add(bestAsset);
                runningHeight += addedHeight;
            }
            else break;
        }

        return new BodySolution(stack, runningHeight);
    }

    public static List<float> ComputeStretching(float height, BodySolution solution)
    {
        var requiredStretch = height / solution.Height;
        var stretches = Enumerable.Repeat(requiredStretch, solution.Stack.Count).ToList();

        // var isClamped = Enumerable.Repeat(false, solution.Stack.Count).ToList();
        //
        // for (var i = 0; i < solution.Stack.Count; ++i)
        // {
        //     // Note: distortion is the difference between the actual stretching factor and 1.
        //     var tolerableDistortion = CoreSegmentSet.models[i].maxDistortion;
        //     var requiredDistortion = stretches[i] - 1f;
        //
        //     if (Mathf.Abs(requiredDistortion) < tolerableDistortion) continue;
        //
        //     isClamped[i] = true;
        //
        //     var originalContribution = CoreSegmentHeights(i) * stretches[i];
        //     stretches[i] = 1 + tolerableDistortion * Mathf.Sign(requiredDistortion);
        //     var clampedContribution = CoreSegmentHeights(i) * stretches[i];
        //     var totalAdjustment = clampedContribution - originalContribution;
        //
        //     var totalAdjustableHeight = 0f;
        //     for (var j = 0; j < solution.Stack.Count; ++j)
        //     {
        //         if (isClamped[j]) continue;
        //         totalAdjustableHeight += CoreSegmentHeights(j) * stretches[j];
        //     }
        //
        //     for (var j = 0; j < solution.Stack.Count; ++j)
        //     {
        //         var otherContribution = CoreSegmentHeights(j) * stretches[j];
        //         var otherContributionFraction = otherContribution / totalAdjustableHeight;
        //         var adjustmentContribution = totalAdjustment * otherContributionFraction;
        //         var adjustedContribution = otherContribution + adjustmentContribution;
        //         stretches[j] = adjustedContribution / CoreSegmentHeights(j);
        //     }
        // }

        // TODO: verify not every segment got clamped. If so, what do?

        return stretches;
    }
}
