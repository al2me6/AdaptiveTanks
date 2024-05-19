using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct CoreSolution(List<int> Stack, float Height);

public class SegmentStacker
{
    private SegmentDef _coreSegmentDef;

    public SegmentDef CoreSegmentDef
    {
        get => _coreSegmentDef;
        set
        {
            if (value.kind != SegmentKind.body)
                Debug.LogError($"attempted to set {value.kind} segment {value.name} as core");
            _coreSegmentDef = value;
        }
    }

    protected float CoreSegmentHeights(int i) => CoreSegmentDef.AspectRatios[i];

    public float NormalizedHeight { get; set; }

    protected float NoseHeight { get; private set; } = 0; // TODO calculate
    protected float MountHeight { get; private set; } = 0; //TODO calculate

    public float CapHeight => NoseHeight + MountHeight;
    public float CoreHeight => NormalizedHeight - CapHeight;

    protected CoreSolution BuildCore()
    {
        List<int> stack = [];
        float runningHeight = 0;
        while (runningHeight < CoreHeight)
        {
            var remainder = CoreHeight - runningHeight;
            var bestSegment = -1;
            var bestNewRemainder = float.PositiveInfinity;

            for (var i = 0; i < CoreSegmentDef.AspectRatios.Count; ++i)
            {
                var newRemainder = Mathf.Abs(remainder - CoreSegmentHeights(i));
                if (newRemainder < bestNewRemainder)
                {
                    bestSegment = i;
                    bestNewRemainder = newRemainder;
                }
            }

            var addedHeight = CoreSegmentDef.AspectRatios[bestSegment];
            // Check if the new segment would overshoot too far. If so, it's better to stop here.
            if (addedHeight < 2f * remainder)
            {
                stack.Add(bestSegment);
                runningHeight += addedHeight;
            }
            else
                break;
        }

        return new CoreSolution(stack, runningHeight);
    }

    protected List<float> ComputeCoreStretching(CoreSolution solution)
    {
        var requiredStretch = CoreHeight / solution.Height;
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

    public SegmentStack Build()
    {
        var coreSolution = BuildCore();
        var coreStretches = ComputeCoreStretching(coreSolution);

        List<SegmentPlacement> placements = new(coreSolution.Stack.Count);
        var currentBaseline = 0f;
        Debug.Log("stack solution:");
        for (var i = 0; i < coreSolution.Stack.Count; ++i)
        {
            var placement = new SegmentPlacement(coreSolution.Stack[i], currentBaseline,
                coreStretches[i]);
            Debug.Log(
                $"model {placement.ModelIndex} @ y = {placement.NormalizedBaseline}, stretch {placement.Stretch}");
            placements.Add(placement);
            currentBaseline += CoreSegmentHeights(placement.ModelIndex) * placement.Stretch;
        }

        return new SegmentStack(CoreSegmentDef, placements, new Vector2(0f, CoreHeight));
    }
}
