using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SelectedSegmentDefs(
    SegmentDef Nose,
    SegmentDef Body,
    SegmentDef Mount
)
{
    public SegmentDef this[SegmentRole role] => role switch
    {
        SegmentRole.Nose => Nose,
        SegmentRole.Body => Body,
        SegmentRole.Mount => Mount,
        _ => throw new IndexOutOfRangeException()
    };
};

public readonly record struct StackerParameters(
    float Diameter,
    float Height,
    SelectedSegmentDefs SkinSegments,
    SelectedSegmentDefs CoreSegments
);

public readonly record struct BodySolution(List<Asset> Stack, float Height)
{
    public void WriteToPlacements(
        ref readonly List<SegmentPlacement> placements, List<float> stretches, float baseline)
    {
        for (var i = 0; i < Stack.Count; ++i)
        {
            var placement =
                new SegmentPlacement(SegmentRole.Body, Stack[i], baseline, stretches[i]);
            placements.Add(placement);
            baseline += placement.Asset.AspectRatio * placement.Stretch;
        }
    }
}

public static class SegmentStacker
{
    private static BodySolution SolveBodyPreliminary(Asset[] availableAssets, float height)
    {
        List<Asset> stack = [];
        float runningHeight = 0;
        while (runningHeight < height)
        {
            var remainder = height - runningHeight;
            var bestAsset = availableAssets[0];
            var bestNewRemainder = float.PositiveInfinity;

            foreach (var asset in availableAssets)
            {
                var newRemainder = Mathf.Abs(remainder - asset.AspectRatio);
                if (newRemainder < bestNewRemainder)
                {
                    bestAsset = asset;
                    bestNewRemainder = newRemainder;
                }
            }

            var addedHeight = bestAsset.AspectRatio;
            // Check if the new segment would overshoot too far. If so, it's better to stop here.
            if (addedHeight < 2f * remainder)
            {
                stack.Add(bestAsset);
                runningHeight += addedHeight;
            }
            else
                break;
        }

        return new BodySolution(stack, runningHeight);
    }

    private static List<float> ComputeBodyStretching(float height, BodySolution solution)
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

    public static SegmentStack SolveStack(
        float diameter, float height, SelectedSegmentDefs segments)
    {
        var normalizedHeight = height / diameter;

        // Todo warn if multiple?
        var noseAsset = segments.Nose.GetFirstAssetForDiameter(diameter);
        var mountAsset = segments.Mount.GetFirstAssetForDiameter(diameter);

        var noseHeight = noseAsset.AspectRatio;
        var mountHeight = mountAsset.AspectRatio;
        var bodyHeight = normalizedHeight - noseHeight - mountHeight;

        var bodySolution = SolveBodyPreliminary(
            segments.Body.GetAssetsForDiameter(diameter).ToArray(),
            bodyHeight);
        var bodyStretches = ComputeBodyStretching(bodyHeight, bodySolution);

        List<SegmentPlacement> placements = new(bodySolution.Stack.Count + 2);
        placements.Add(new SegmentPlacement(SegmentRole.Mount, mountAsset, -mountHeight));
        bodySolution.WriteToPlacements(ref placements, bodyStretches, 0f);
        placements.Add(new SegmentPlacement(SegmentRole.Nose, noseAsset, bodyHeight));

        var extent = new Vector2(-mountHeight, bodyHeight + noseHeight);

        return new SegmentStack(diameter, placements, extent);
    }

    public static SkinAndCore<SegmentStack> SolveSkinAndCoreSeparately(StackerParameters parameters)
    {
        return new SkinAndCore<SegmentStack>(
            SolveStack(parameters.Diameter, parameters.Height, parameters.SkinSegments),
            SolveStack(parameters.Diameter, parameters.Height, parameters.CoreSegments));
    }
}
