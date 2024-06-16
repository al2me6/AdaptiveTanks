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
    public virtual float AdjustedAspectRatio => ForceStretch * Asset.AspectRatio;
}

internal record ProtoSegmentTerminator(Role Role, Asset Asset, Alignment Align)
    : ProtoSegmentFixed(Role, Asset)
{
    public float AspectRatioExtension { get; set; } = 0f;
    public override float AdjustedAspectRatio => base.AdjustedAspectRatio + AspectRatioExtension;
}

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
        .Select(seg => seg.AdjustedAspectRatio)
        .Sum();

    public static void NegotiateSegmentAlignment(
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

            if (skinSeg is ProtoSegmentTerminator { Align: Alignment.PinInteriorEnd } skinSegTerm &&
                coreSeg is ProtoSegmentTerminator { Align: Alignment.PinInteriorEnd } coreSegTerm)
            {
                // PinInteriorEnd is implemented by padding the shorter of the two terminators
                // to have the same 'virtual' aspect ratio as the longer.
                var skinNativeAspect = skinSeg.Asset.AspectRatio;
                var coreNativeAspect = coreSeg.Asset.AspectRatio;
                var padding = Mathf.Abs(skinNativeAspect - coreNativeAspect);
                if (skinNativeAspect < coreNativeAspect)
                    skinSegTerm.AspectRatioExtension = padding;
                else
                    coreSegTerm.AspectRatioExtension = padding;
            }
            else if (skinSeg.Asset.Segment.useStrictAlignment)
            {
                (skinSeg.ForceStretch, coreSeg.ForceStretch) = Asset.NegotiateAspectRatio(
                    skinSeg.Asset, coreSeg.Asset, skinSeg.Asset.Segment.strictAlignmentBias);
            }
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
                case ProtoSegmentTerminator(var role, var asset, _)
                {
                    ForceStretch: var forceStretch, AspectRatioExtension: var aspectExtension
                }:
                    // Note that for a bottom cap, the aspect ratio extension is below the segment.
                    var isBottom = role.TryAsCapPosition() is CapPosition.Bottom;
                    if (isBottom) baseline += aspectExtension * fixedStretchFactor;
                    stack.Add(role, asset, baseline, forceStretch * fixedStretchFactor);
                    if (!isBottom) baseline += aspectExtension * fixedStretchFactor;
                    baseline += asset.AspectRatio * forceStretch * fixedStretchFactor;
                    break;
                case ProtoSegmentFixed(var role, var asset)
                {
                    AdjustedAspectRatio: var adjustedAspect, ForceStretch: var forceStretch
                }:
                    stack.Add(role, asset, baseline, forceStretch * fixedStretchFactor);
                    baseline += adjustedAspect * fixedStretchFactor;
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
