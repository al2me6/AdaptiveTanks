using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

#nullable enable
using Role = SegmentRole;
using Alignment = SegmentAlignment;
using static ProtoSegment;

// A poor man's tagged union.
internal abstract record ProtoSegment
{
    internal record Fixed(Role Role, SegmentDef Segment) : ProtoSegment
    {
        public Asset? Asset { get; set; } = null;
        internal float ForceStretch { get; set; } = 1f;
        internal virtual float AdjustedAspectRatio => ForceStretch * Asset!.AspectRatio;
    }

    internal record Terminator(CapPosition Position, SegmentDef Segment, Alignment Align)
        : Fixed(Position.AsRoleTerminator(), Segment)
    {
        internal float AspectRatioExtension { get; set; } = 0f;

        internal override float AdjustedAspectRatio =>
            base.AdjustedAspectRatio + AspectRatioExtension;
    }

    internal record Flex(Asset[] Assets, float FlexFactor) : ProtoSegment
    {
        internal BodySolution? Solution { get; set; } = null;
    }
}

internal class ProtoSegmentStack
{
    private float Diameter { get; }
    private float Height { get; }
    private List<ProtoSegment> ProtoSegments { get; set; } = [];

    public ProtoSegmentStack(
        float diameter, float height, SelectedSegments segments, float[] flexFactors)
    {
        Diameter = diameter;
        Height = height;
        var flexAssets = segments.Tank.GetAssetsForDiameter(diameter).ToArray();

        AddTerminator(segments, CapPosition.Bottom, segments.AlignBottom);
        TryAddFixed(segments, Role.TankCapInternalBottom);

        var totalFlexFactor = flexFactors.Sum();
        for (var i = 0; i < flexFactors.Length; ++i)
        {
            if (i > 0) ProtoSegments.Add(new Fixed(Role.Intertank, segments.Intertank!));
            ProtoSegments.Add(new Flex(flexAssets, flexFactors[i] / totalFlexFactor));
        }

        TryAddFixed(segments, Role.TankCapInternalTop);
        AddTerminator(segments, CapPosition.Top, segments.AlignTop);
    }

    private void AddTerminator(SelectedSegments segments, CapPosition position, Alignment align) =>
        ProtoSegments.Add(new Terminator(position, segments[position.AsRoleTerminator()]!, align));

    private void AddFixed(SelectedSegments segments, Role role) =>
        ProtoSegments.Add(new Fixed(role, segments[role]!));

    private void TryAddFixed(SelectedSegments segments, Role role)
    {
        if (segments[role] != null) AddFixed(segments, role);
    }

    private float TotalAspectRatio => Height / Diameter;

    private float CapAndAccessoryAspectRatio() => ProtoSegments
        .WhereOfType<Fixed>()
        .Where(seg => seg.Role != Role.Intertank)
        .Select(seg => seg.AdjustedAspectRatio)
        .Sum();

    private float FueledAspectRatio() => TotalAspectRatio - ProtoSegments
        .WhereOfType<Terminator>()
        .Where(seg => seg.Asset!.Segment.IsAccessory)
        .Select(seg => seg.AdjustedAspectRatio)
        .Sum();

    public static void NegotiateSegmentAlignment(ProtoSegmentStack skin, ProtoSegmentStack core)
    {
        if (skin.ProtoSegments.Count != core.ProtoSegments.Count)
            Debug.LogError("mismatched skin and core proto stacks");

        var maxIdx = Math.Min(skin.ProtoSegments.Count, core.ProtoSegments.Count);
        for (var i = 0; i < maxIdx; ++i)
        {
            if (skin.ProtoSegments[i] is not Fixed skinSeg ||
                core.ProtoSegments[i] is not Fixed coreSeg) continue;

            // Select fixed assets. The skin is negotiated based on the core asset.
            coreSeg.Asset = coreSeg.Segment.GetFirstAssetForDiameter(core.Diameter);
            skinSeg.Asset =
                skinSeg.Segment.GetAssetOfNearestRatio(skin.Diameter, coreSeg.Asset.AspectRatio);

            if (skinSeg is Terminator { Align: Alignment.PinInteriorEnd } skinSegTerm &&
                coreSeg is Terminator { Align: Alignment.PinInteriorEnd } coreSegTerm)
            {
                // pinInteriorEnd is implemented by padding the shorter of the two terminators
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

    public void TrySolveFlexSegmentsWithIntertanks()
    {
        for (var i = 1; i < ProtoSegments.Count - 1; ++i)
        {
            if (ProtoSegments[i] is not Flex segFlex) continue;

            // Contract: tanks always have caps. Thus, the neighboring segments must be fixed
            // and in particular must be either intertanks or tank caps. Never an accessory
            // or another flex segment.
            var segPrev = (Fixed)ProtoSegments[i - 1];
            var segNext = (Fixed)ProtoSegments[i + 1];

            // An intertank contains only half of this propellant.
            var fixedContribution =
                segPrev.AdjustedAspectRatio * (segPrev.Role == Role.Intertank ? 0.5f : 1f)
                + segNext.AdjustedAspectRatio * (segNext.Role == Role.Intertank ? 0.5f : 1f);
            var flexContribution = FueledAspectRatio() * segFlex.FlexFactor - fixedContribution;
            if (flexContribution <= 0) break;

            // TODO: deal with zero-height solutions more properly.
            // This is a reasonable use-case. (e.g. Shuttle ET.)
            var flexSolution = BodySolver.Solve(segFlex.Assets, flexContribution);
            if (flexSolution.SolutionAspectRatio() == 0f) break;

            segFlex.Solution = flexSolution;
        }
    }

    private void ExciseIntertanks()
    {
        List<ProtoSegment> newProtoSegments = new(5);

        var foundFirstFlex = false;
        foreach (var seg in ProtoSegments)
        {
            if (seg is Flex segFlex)
            {
                if (foundFirstFlex) continue;
                foundFirstFlex = true;
                var newFlexAspect = TotalAspectRatio - CapAndAccessoryAspectRatio();
                var replacementFlex = new Flex(segFlex.Assets, 1f)
                {
                    Solution = BodySolver.Solve(segFlex.Assets, newFlexAspect)
                };
                newProtoSegments.Add(replacementFlex);
            }
            else if (seg is not Fixed(Role.Intertank, _)) newProtoSegments.Add(seg);
        }

        ProtoSegments = newProtoSegments;
    }

    public static void NegotiateIntertankAlignment(
        ProtoSegmentStack skin, ProtoSegmentStack core)
    {
        var skinSolved = skin.ProtoSegments.WhereOfType<Flex>().All(seg => seg.Solution != null);
        var coreSolved = core.ProtoSegments.WhereOfType<Flex>().All(seg => seg.Solution != null);

        // TODO: relax if possible. Move stretching out of BodySolver.Solve here.

        if (!skinSolved || !coreSolved)
        {
            skin.ExciseIntertanks();
            core.ExciseIntertanks();
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

        foreach (var seg in ProtoSegments)
        {
            switch (seg)
            {
                case Terminator
                {
                    Position: var position, Role: var role, Asset: var asset,
                    ForceStretch: var forceStretch, AspectRatioExtension: var aspectExtension
                }:
                    // Note that for a bottom cap, the aspect ratio extension is below the segment.
                    var isBottom = position == CapPosition.Bottom;
                    if (isBottom) baseline += aspectExtension * fixedStretchFactor;
                    stack.Add(role, asset, baseline, forceStretch * fixedStretchFactor);
                    if (!isBottom) baseline += aspectExtension * fixedStretchFactor;
                    baseline += asset!.AspectRatio * forceStretch * fixedStretchFactor;
                    break;
                case Fixed
                {
                    Role: var role, Asset: var asset,
                    AdjustedAspectRatio: var adjustedAspect, ForceStretch: var forceStretch
                }:
                    stack.Add(role, asset, baseline, forceStretch * fixedStretchFactor);
                    baseline += adjustedAspect * fixedStretchFactor;
                    break;
                case Flex { Solution: var bodySolution }:
                    stack.Add(bodySolution!, baseline);
                    baseline += bodySolution!.SolutionAspectRatio();
                    break;
            }
        }

        return stack;
    }
}
