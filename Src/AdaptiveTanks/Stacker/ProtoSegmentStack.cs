using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

#nullable enable

internal abstract record ProtoSegment;

internal record ProtoSegmentFixed(SegmentRole Role, Asset Asset) : ProtoSegment;

internal record ProtoSegmentTerminator(SegmentRole Role, Asset Asset, SegmentAlignment Align)
    : ProtoSegmentFixed(Role, Asset);

internal record ProtoSegmentFlex(SegmentDef Segment, float FlexFactor) : ProtoSegment
{
    public BodySolution? Solution { get; set; }
}

internal record ProtoSegmentStack(float Diameter, float Height)
{
    private List<ProtoSegment> ProtoSegments { get; } = [];

    public void AddTerminator(
        SelectedSegments segments, CapPosition position, SegmentAlignment align)
    {
        var role = position.AsRoleTerminator();
        ProtoSegments.Add(new ProtoSegmentTerminator(
            role,
            segments[role]!.GetFirstAssetForDiameter(Diameter),
            align));
    }

    public void AddFixed(SelectedSegments segments, SegmentRole role)
    {
        ProtoSegments.Add(
            new ProtoSegmentFixed(role, segments[role]!.GetFirstAssetForDiameter(Diameter)));
    }

    public void TryAddFixed(SelectedSegments segments, SegmentRole role)
    {
        if (segments[role] == null) return;
        AddFixed(segments, role);
    }

    public void AddFlex(SegmentDef segment, float factor)
    {
        ProtoSegments.Add(new ProtoSegmentFlex(segment, factor));
    }

    public SegmentStack Elaborate()
    {
        var aspectRatio = Height / Diameter;

        var fixedAspect = ProtoSegments
            .WhereOfType<ProtoSegmentFixed>()
            .Where(seg => seg is not ProtoSegmentTerminator(_, _, SegmentAlignment.PinInteriorEnd))
            .Select(seg => seg.Asset.AspectRatio)
            .Sum();

        var totalFlexFactor = ProtoSegments
            .WhereOfType<ProtoSegmentFlex>()
            .Select(seg => seg.FlexFactor)
            .Sum();

        if (!Mathf.Approximately(totalFlexFactor, 1f))
        {
            Debug.LogError($"non-unity total flex factor {totalFlexFactor}");
        }

        var targetFlexAspect = aspectRatio - fixedAspect;

        foreach (var flexProtoSegment in ProtoSegments.WhereOfType<ProtoSegmentFlex>())
        {
            var contribution = flexProtoSegment.FlexFactor / totalFlexFactor;
            flexProtoSegment.Solution = BodySolver.Solve(
                flexProtoSegment.Segment.GetAssetsForDiameter(Diameter).ToArray(),
                targetFlexAspect * contribution);
        }

        var solvedFlexAspect = ProtoSegments
            .WhereOfType<ProtoSegmentFlex>()
            .Select(seg => seg.Solution!.SolutionAspectRatio())
            .Sum();
        var adjustedFixedAspect = aspectRatio - solvedFlexAspect;
        var fixedStretchFactor = fixedAspect / adjustedFixedAspect; // TODO respect maxStretch

        var stack = new SegmentStack(Diameter, Height);
        var baseline = 0f;

        for (var i = 0; i < ProtoSegments.Count; i++)
        {
            switch (ProtoSegments[i])
            {
                case ProtoSegmentTerminator(var role, var asset, SegmentAlignment.PinInteriorEnd):
                {
                    if (!(i == 0 && role == SegmentRole.TerminatorBottom)
                        && !(i == ProtoSegments.Count - 1 && role == SegmentRole.TerminatorTop))
                    {
                        Debug.LogError("terminator in unexpected position or with unexpected role");
                    }

                    // A terminator in pinInteriorEnds mode is stacked outside the formal height.
                    // As the top, this makes no difference.
                    // As the bottom, the baseline (i.e. 0) must be shifted below 0.
                    // As it does not count for height, it is also not stretched.
                    // TODO: ^ This seems like a bad idea.
                    if (role == SegmentRole.TerminatorBottom) baseline -= asset.AspectRatio;
                    stack.Add(role, asset, baseline, 1f);
                    baseline += asset.AspectRatio;
                    break;
                }
                case ProtoSegmentFixed(var role, var asset):
                {
                    stack.Add(role, asset, baseline, fixedStretchFactor);
                    baseline += asset.AspectRatio * fixedStretchFactor;
                    break;
                }
                case ProtoSegmentFlex { Solution: var bodySolution }:
                {
                    stack.Add(bodySolution!, baseline);
                    baseline += bodySolution!.SolutionAspectRatio();
                    break;
                }
            }
        }

        return stack;
    }
}
