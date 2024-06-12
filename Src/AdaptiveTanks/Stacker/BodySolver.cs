using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks;

public record StretchedAsset
{
    public Asset Asset { get; init; }
    public float Stretch { get; set; } = 1f;

    public float StretchedAspectRatio => Asset.AspectRatio * Stretch;
}

public record BodySolution(List<StretchedAsset> Stack, float TargetAspectRatio)
{
    public float SolutionAspectRatio() => Stack
        .Select(segment => segment.StretchedAspectRatio)
        .Sum();
}

public static class BodySolver
{
    public static BodySolution Solve(Asset[] availableAssets, float aspectRatio)
    {
        Array.Sort(availableAssets, (a, b) => a.AspectRatio.CompareTo(b.AspectRatio));
        var minimumAspect = availableAssets.Select(asset => asset.AspectRatio).Min();

        List<StretchedAsset> stack = [];
        float runningAspect = 0;

        while (runningAspect < aspectRatio)
        {
            var remainder = aspectRatio - runningAspect;

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
                stack.Add(new StretchedAsset { Asset = bestAsset });
                runningAspect += addedHeight;
            }
            else break;
        }

        var solution = new BodySolution(stack, aspectRatio);
        Stretch(ref solution);

        return solution;
    }

    private static void Stretch(ref BodySolution solution)
    {
        var requiredStretch = solution.TargetAspectRatio / solution.SolutionAspectRatio();
        foreach (var segment in solution.Stack)
        {
            segment.Stretch = requiredStretch;
        }

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
    }
}
