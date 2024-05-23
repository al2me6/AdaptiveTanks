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
    float Height,
    float Diameter,
    SelectedSegmentDefs SkinSegments,
    SelectedSegmentDefs CoreSegments
);

public readonly record struct BodySolution(SegmentDef segment, List<int> Stack, float Height)
{
    public void WriteToPlacements(
        ref readonly List<SegmentPlacement> placements, List<float> stretches, float baseline)
    {
        for (var i = 0; i < Stack.Count; ++i)
        {
            var placement =
                new SegmentPlacement(SegmentRole.Body, Stack[i], baseline, stretches[i]);
            placements.Add(placement);
            baseline += segment.AspectRatios[placement.ModelIdx] * placement.Stretch;
        }
    }
}

public static class SegmentStacker
{
    private static BodySolution SolveBodyPreliminary(float height, SegmentDef segment)
    {
        List<int> stack = [];
        float runningHeight = 0;
        while (runningHeight < height)
        {
            var remainder = height - runningHeight;
            var bestSegment = -1;
            var bestNewRemainder = float.PositiveInfinity;

            for (var i = 0; i < segment.AspectRatios.Count; ++i)
            {
                var newRemainder = Mathf.Abs(remainder - segment.AspectRatios[i]);
                if (newRemainder < bestNewRemainder)
                {
                    bestSegment = i;
                    bestNewRemainder = newRemainder;
                }
            }

            var addedHeight = segment.AspectRatios[bestSegment];
            // Check if the new segment would overshoot too far. If so, it's better to stop here.
            if (addedHeight < 2f * remainder)
            {
                stack.Add(bestSegment);
                runningHeight += addedHeight;
            }
            else
                break;
        }

        return new BodySolution(segment, stack, runningHeight);
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
        float height, float diameter, SelectedSegmentDefs segments)
    {
        var normalizedHeight = height / diameter;
        var noseHeight = segments.Nose.AspectRatio;
        var mountHeight = segments.Mount.AspectRatio;
        var bodyHeight = normalizedHeight - noseHeight - mountHeight;

        var bodySolution = SolveBodyPreliminary(bodyHeight, segments.Body);
        var bodyStretches = ComputeBodyStretching(bodyHeight, bodySolution);

        List<SegmentPlacement> placements = new(bodySolution.Stack.Count + 2);
        placements.Add(new SegmentPlacement(SegmentRole.Mount, 0, -mountHeight));
        bodySolution.WriteToPlacements(ref placements, bodyStretches, 0f);
        placements.Add(new SegmentPlacement(SegmentRole.Nose, 0, bodyHeight));


        var extent = new Vector2(-mountHeight, bodyHeight + noseHeight);

        return new SegmentStack(diameter, segments, placements, extent);
    }

    public static SkinAndCore<SegmentStack> SolveSkinAndCoreSeparately(StackerParameters parameters)
    {
        return new SkinAndCore<SegmentStack>(
            SolveStack(parameters.Height, parameters.Diameter, parameters.SkinSegments),
            SolveStack(parameters.Height, parameters.Diameter, parameters.CoreSegments));
    }
}
