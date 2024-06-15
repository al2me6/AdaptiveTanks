using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

#nullable enable
using Role = SegmentRole;
using Alignment = SegmentAlignment;

internal abstract record ProtoSegment;

internal record ProtoSegmentFixed(Role Role, Asset Asset) : ProtoSegment
{
    public float ForceStretch { get; set; } = 1f;
    public float ForcedAspectRatio => ForceStretch * Asset.AspectRatio;
}

internal record ProtoSegmentTerminator(Role Role, Asset Asset, Alignment Align)
    : ProtoSegmentFixed(Role, Asset);

internal record ProtoSegmentFlex(SegmentDef Segment, float FlexFactor) : ProtoSegment
{
    public BodySolution? Solution { get; set; }
}

internal class ProtoSegmentStack(float Diameter, float Height)
{
    private List<ProtoSegment> ProtoSegments { get; } = [];

    public void AddTerminator(
        SelectedSegments segments, CapPosition position, Alignment align)
    {
        var role = position.AsRoleTerminator();
        ProtoSegments.Add(new ProtoSegmentTerminator(
            role,
            segments[role]!.GetFirstAssetForDiameter(Diameter),
            align));
    }

    public void AddFixed(SelectedSegments segments, Role role)
    {
        ProtoSegments.Add(
            new ProtoSegmentFixed(role, segments[role]!.GetFirstAssetForDiameter(Diameter)));
    }

    public void TryAddFixed(SelectedSegments segments, Role role)
    {
        if (segments[role] == null) return;
        AddFixed(segments, role);
    }

    public void AddFlex(SegmentDef segment, float factor)
    {
        ProtoSegments.Add(new ProtoSegmentFlex(segment, factor));
    }

    protected float TotalAspectRatio => Height / Diameter;

    protected float FixedAspectRatio() => ProtoSegments
        .WhereOfType<ProtoSegmentFixed>()
        .Where(seg => seg is not ProtoSegmentTerminator { Align: Alignment.PinInteriorEnd })
        .Select(seg => seg.ForcedAspectRatio)
        .Sum();

    public static void NegotiateStrictAlignment(
        ProtoSegmentStack skin, ProtoSegmentStack core)
    {
        if (skin.ProtoSegments.Count != core.ProtoSegments.Count)
        {
            Debug.LogError("mismatched skin and core proto stacks");
        }

        var maxIdx = Math.Min(skin.ProtoSegments.Count, core.ProtoSegments.Count);
        for (var i = 0; i < maxIdx; ++i)
        {
            if (skin.ProtoSegments[i] is not ProtoSegmentFixed skinSeg ||
                core.ProtoSegments[i] is not ProtoSegmentFixed coreSeg) continue;

            if (!skinSeg.Asset.Segment.useStrictAlignment) continue;

            if (skinSeg is ProtoSegmentTerminator { Align: Alignment.PinInteriorEnd } ||
                coreSeg is ProtoSegmentTerminator { Align: Alignment.PinInteriorEnd })
                continue;

            (skinSeg.ForceStretch, coreSeg.ForceStretch) = Asset.NegotiateAspectRatio(
                skinSeg.Asset, coreSeg.Asset, skinSeg.Asset.Segment.strictAlignmentBias);
        }
    }

    public void SolveFlexSegments()
    {
        var totalFlexFactor = ProtoSegments
            .WhereOfType<ProtoSegmentFlex>()
            .Select(seg => seg.FlexFactor)
            .Sum();

        if (!Mathf.Approximately(totalFlexFactor, 1f))
        {
            Debug.LogError($"non-unity total flex factor {totalFlexFactor}");
        }

        var targetFlexAspect = TotalAspectRatio - FixedAspectRatio();

        foreach (var flexProtoSegment in ProtoSegments.WhereOfType<ProtoSegmentFlex>())
        {
            var contribution = flexProtoSegment.FlexFactor / totalFlexFactor;
            flexProtoSegment.Solution = BodySolver.Solve(
                flexProtoSegment.Segment.GetAssetsForDiameter(Diameter).ToArray(),
                targetFlexAspect * contribution);
        }
    }

    public SegmentStack Elaborate()
    {
        // TODO: respect maxStretch
        // TODO: this needs to be negotiated
        // var solvedFlexAspect = ProtoSegments
        //     .WhereOfType<ProtoSegmentFlex>()
        //     .Select(seg => seg.Solution!.SolutionAspectRatio())
        //     .Sum();
        // var adjustedFixedAspect = TotalAspectRatio - solvedFlexAspect;
        // var fixedStretchFactor = FixedAspectRatio() / adjustedFixedAspect;
        var fixedStretchFactor = 1f;

        var stack = new SegmentStack(Diameter, Height);
        var baseline = 0f;

        for (var i = 0; i < ProtoSegments.Count; i++)
        {
            switch (ProtoSegments[i])
            {
                case ProtoSegmentTerminator(var role, var asset, Alignment.PinInteriorEnd)
                {
                    ForcedAspectRatio: var forcedAspect, ForceStretch: var forceStretch
                }:
                    if (!(i == 0 && role == Role.TerminatorBottom)
                        && !(i == ProtoSegments.Count - 1 && role == Role.TerminatorTop))
                    {
                        Debug.LogError("terminator in unexpected position or with unexpected role");
                    }

                    // A terminator in pinInteriorEnds mode is stacked outside the formal height.
                    // As the top, this makes no difference.
                    // As the bottom, the baseline (i.e. 0) must be shifted below 0.
                    // As it does not count for height, it is also not stretched.
                    // TODO: ^ This seems like a bad idea.
                    if (role == Role.TerminatorBottom) baseline -= forcedAspect;
                    stack.Add(role, asset, baseline, forceStretch);
                    baseline += forcedAspect;
                    break;
                case ProtoSegmentFixed(var role, var asset)
                {
                    ForcedAspectRatio: var forcedAspect, ForceStretch: var forceStretch
                }:
                    stack.Add(role, asset, baseline, forceStretch * fixedStretchFactor);
                    baseline += forcedAspect * fixedStretchFactor;
                    break;
                case ProtoSegmentFlex { Solution: var bodySolution }:
                    stack.Add(bodySolution!, baseline);
                    baseline += bodySolution!.SolutionAspectRatio();
                    break;
            }
        }

        return stack;
    }
}
