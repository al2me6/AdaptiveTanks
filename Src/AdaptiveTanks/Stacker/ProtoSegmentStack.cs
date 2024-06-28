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
        internal float Padding { get; set; } = 0f;
        internal override float AdjustedAspectRatio => base.AdjustedAspectRatio + Padding;
    }

    /// Assumptions:
    /// - There is at least one `Asset`.
    /// - `Assets` are all from the same `SegmentDef`.
    internal record Flex(Asset[] Assets, float VolumeFraction) : ProtoSegment
    {
        private const float Tolerance = 5e-3f;

        internal Flex? Prev { get; set; } = null;
        internal Flex? Next { get; set; } = null;
        internal float AspectRatio { get; set; } = 0f;
        internal BodySolution? Solution { get; set; } = null;
        internal SegmentDef Segment => Assets[0].Segment;

        internal bool AspectRatioIsEmpty => Mathf.Abs(AspectRatio - 0f) < Tolerance;

        internal bool AspectRatioIsValid =>
            AspectRatioIsEmpty || Segment.minimumTankAspectRatio < AspectRatio + Tolerance;

        private bool ModifyAspect(float shift)
        {
            AspectRatio += shift;
            return AspectRatioIsValid;
        }

        internal bool ShiftAspect(float shift)
        {
            if (!ModifyAspect(shift)) return false;
            switch (Prev, Next)
            {
                case (null, null):
                    return true;
                case (Flex prev, null):
                    return prev.ModifyAspect(-shift);
                case (null, Flex next):
                    return next.ModifyAspect(-shift);
                case (Flex prev, Flex next):
                    var prevValid = prev.ModifyAspect(shift * -0.5f);
                    var nextValid = next.ModifyAspect(shift * -0.5f);
                    return prevValid && nextValid;
            }
        }
    }
}

internal class ProtoSegmentStack
{
    private float Diameter { get; }
    private float Height { get; }
    private List<ProtoSegment> ProtoSegments { get; set; } = [];

    #region ctor

    internal ProtoSegmentStack(
        float diameter, float height, SelectedSegments segments, float[] volumeFractions)
    {
        Diameter = diameter;
        Height = height;
        var flexAssets = segments.Tank.GetAllAssetsFor(diameter).ToArray();

        AddTerminator(segments, CapPosition.Bottom, segments.AlignBottom);
        MaybeAddFixed(segments, Role.TankCapInternalBottom);

        var totalVolumeFraction = volumeFractions.Sum();
        Flex? previousFlex = null;
        for (var i = 0; i < volumeFractions.Length; ++i)
        {
            if (i > 0) AddFixed(segments, Role.Intertank);
            var segFlex = new Flex(flexAssets, volumeFractions[i] / totalVolumeFraction)
            {
                Prev = previousFlex
            };
            if (previousFlex != null) previousFlex.Next = segFlex;
            ProtoSegments.Add(segFlex);
            previousFlex = segFlex;
        }

        MaybeAddFixed(segments, Role.TankCapInternalTop);
        AddTerminator(segments, CapPosition.Top, segments.AlignTop);
    }

    private void AddTerminator(SelectedSegments segments, CapPosition position, Alignment align) =>
        ProtoSegments.Add(new Terminator(position, segments[position.AsRoleTerminator()]!, align));

    private void AddFixed(SelectedSegments segments, Role role) =>
        ProtoSegments.Add(new Fixed(role, segments[role]!));

    private void MaybeAddFixed(SelectedSegments segments, Role role)
    {
        if (segments[role] != null) AddFixed(segments, role);
    }

    #endregion

    #region queries

    private float TotalAspectRatio => Height / Diameter;

    private float CapAndAccessoryAspectRatio() => ProtoSegments
        .WhereIs<Fixed>()
        .Where(seg => seg.Role != Role.Intertank)
        .Select(seg => seg.AdjustedAspectRatio)
        .Sum();

    private float AccessoryAspectRatio() => ProtoSegments
        .WhereIs<Terminator>()
        .Where(seg => seg.Asset!.Segment.IsAccessory)
        .Select(seg => seg.AdjustedAspectRatio)
        .Sum();

    private float FueledAspectRatio() => TotalAspectRatio - AccessoryAspectRatio();

    private Flex? FindFirstFlex() => ProtoSegments.WhereIs<Flex>().FirstOrDefault();

    #endregion

    #region elaboration

    public static void NegotiateSegmentAlignment(ProtoSegmentStack skin, ProtoSegmentStack core)
    {
        if (skin.ProtoSegments.Count != core.ProtoSegments.Count)
            Debug.LogError("mismatched skin and core proto stacks");

        var maxIdx = Math.Min(skin.ProtoSegments.Count, core.ProtoSegments.Count);
        for (var i = 0; i < maxIdx; ++i)
        {
            if (skin.ProtoSegments[i] is not Fixed skinSeg ||
                core.ProtoSegments[i] is not Fixed coreSeg) continue;

            // Select fixed assets. The skin is selected to best match the core.
            // TODO: negotiate the skin and core together?
            coreSeg.Asset = coreSeg.Segment.GetFirstAssetFor(core.Diameter);
            skinSeg.Asset =
                skinSeg.Segment.GetBestAssetFor(skin.Diameter, coreSeg.Asset.AspectRatio);

            if (skinSeg is Terminator { Align: Alignment.PinInteriorEnd } skinSegTerm &&
                coreSeg is Terminator { Align: Alignment.PinInteriorEnd } coreSegTerm)
            {
                // pinInteriorEnd is implemented by padding the shorter of the two terminators
                // to have the same 'virtual' aspect ratio as the longer.
                var skinNativeAspect = skinSeg.Asset.AspectRatio;
                var coreNativeAspect = coreSeg.Asset.AspectRatio;
                var padding = Mathf.Abs(skinNativeAspect - coreNativeAspect);
                if (skinNativeAspect < coreNativeAspect)
                    skinSegTerm.Padding = padding;
                else
                    coreSegTerm.Padding = padding;
            }
            else if (skinSeg.Asset.Segment.useStrictAlignment)
            {
                (skinSeg.ForceStretch, coreSeg.ForceStretch) = Asset.NegotiateAspectRatio(
                    skinSeg.Asset, coreSeg.Asset, skinSeg.Asset.Segment.strictAlignmentBias);
            }
        }
    }

    public void ComputeFlexSegmentAspectRatios()
    {
        for (var i = 1; i < ProtoSegments.Count - 1; ++i)
        {
            if (ProtoSegments[i] is not Flex seg) continue;

            // Contract: tanks always have caps. Thus, the neighboring segments must be fixed
            // and in particular must be either intertanks or tank caps. Never an accessory
            // or another flex segment.
            var segPrev = (Fixed)ProtoSegments[i - 1];
            var segNext = (Fixed)ProtoSegments[i + 1];

            // Only half of the intertank contains this propellant.
            // TODO: compute contribution based on volumes.
            var fixedContribution =
                segPrev.AdjustedAspectRatio * (segPrev.Role == Role.Intertank ? 0.5f : 1f)
                + segNext.AdjustedAspectRatio * (segNext.Role == Role.Intertank ? 0.5f : 1f);
            seg.AspectRatio = FueledAspectRatio() * seg.VolumeFraction - fixedContribution;
        }
    }

    public static void NegotiateFlexAspectRatios(ProtoSegmentStack skin, ProtoSegmentStack core)
    {
        var maxIdx = Math.Min(skin.ProtoSegments.Count, core.ProtoSegments.Count);
        for (var i = 0; i < maxIdx; ++i)
        {
            if (skin.ProtoSegments[i] is not Flex skinSeg ||
                core.ProtoSegments[i] is not Flex coreSeg) continue;

            var hasPrev = skinSeg.Prev != null && coreSeg.Prev != null;
            var hasNext = skinSeg.Next != null && coreSeg.Next != null;

            // If there is no target to shift, there must be no intertank.
            if (!hasPrev && !hasNext)
            {
                ShiftFlexSegment(skinSeg, coreSeg);
                return;
            }

            if (!ShiftFlexSegment(skinSeg, coreSeg))
            {
                Debug.Log("shift failed, excising intertanks");
                skin.ExciseIntertanks();
                core.ExciseIntertanks();
                ShiftFlexSegment(skin.FindFirstFlex()!, core.FindFirstFlex()!);
                return;
            }
        }
    }

    private static bool ShiftFlexSegment(Flex skinSeg, Flex coreSeg)
    {
        var skinMinAspect = skinSeg.Segment.minimumTankAspectRatio;
        var coreMinAspect = coreSeg.Segment.minimumTankAspectRatio;

        // Both segments are sufficiently large. Accept.
        if (skinSeg.AspectRatio > skinMinAspect && coreSeg.AspectRatio > coreMinAspect)
            return true;

        // Else, a shift is required.
        // Case 1: negative aspects expand to 0 aspect.
        // Case 2: too-small positive aspects shrink to 0 aspect.
        // For both, shift is negative of computed aspect. Else 0.
        var skinShift = skinSeg.AspectRatio < skinMinAspect ? -skinSeg.AspectRatio : 0f;
        var coreShift = coreSeg.AspectRatio < coreMinAspect ? -coreSeg.AspectRatio : 0f;
        var shift = (skinShift, coreShift) switch
        {
            // expand, expand
            // Expand maximally. In non-strictly aligned mode the shorter layer will have a
            // force-filled gap.
            (> 0, > 0) => Mathf.Max(skinShift, coreShift),
            // contract, contract
            // Contract minimally. In non-strictly aligned mode the taller layer will have a
            // force-filled gap.
            (< 0, < 0) => Mathf.Max(skinShift, coreShift),
            // expand, contract
            // contract, expand
            // Expand such that both minimum constraints are satisfied.
            (> 0, < 0) or (< 0, > 0) => Mathf.Max(
                skinMinAspect - skinSeg.AspectRatio, coreMinAspect - coreSeg.AspectRatio),
            // One shift is zero. Respect the other one and hope for the best.
            // TODO: do better.
            (_, 0) => skinShift,
            (0, _) => coreShift,
            _ => 0f
        };
        Debug.Log($"shifts: skin {skinShift:f3}, core {coreShift:f3}, unified {shift:f3}");

        if (!skinSeg.ShiftAspect(shift)) return false;
        if (!coreSeg.ShiftAspect(shift)) return false;

        return true;
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
                newProtoSegments.Add(new Flex(segFlex.Assets, 1f) { AspectRatio = newFlexAspect });
            }
            else if (seg is not Fixed(Role.Intertank, _)) newProtoSegments.Add(seg);
        }

        ProtoSegments = newProtoSegments;
    }

    public void SolveFlexSegments()
    {
        foreach (var seg in ProtoSegments)
        {
            if (seg is not Flex segFlex) continue;
            segFlex.Solution = segFlex.AspectRatioIsEmpty
                ? BodySolution.Empty
                : BodySolver.Solve(segFlex.Assets, segFlex.AspectRatio);
        }
    }

    public SegmentStack Elaborate()
    {
        var stack = new SegmentStack(Diameter, Height);
        var baseline = 0f;

        foreach (var seg in ProtoSegments)
        {
            switch (seg)
            {
                case Terminator
                {
                    Position: var position, Role: var role, Asset: var asset,
                    ForceStretch: var forceStretch, Padding: var padding
                }:
                    // Note that for a bottom cap, the padding is below the segment.
                    var isBottom = position == CapPosition.Bottom;
                    if (isBottom) baseline += padding;
                    stack.Add(role, asset, baseline, forceStretch);
                    if (!isBottom) baseline += padding;
                    baseline += asset!.AspectRatio * forceStretch;
                    break;
                case Fixed
                {
                    Role: var role, Asset: var asset,
                    AdjustedAspectRatio: var adjustedAspect, ForceStretch: var forceStretch
                }:
                    stack.Add(role, asset, baseline, forceStretch);
                    baseline += adjustedAspect;
                    break;
                case Flex { Solution: var bodySolution }:
                    stack.Add(bodySolution!, baseline);
                    baseline += bodySolution!.SolutionAspectRatio();
                    break;
            }
        }

        return stack;
    }

    #endregion
}
