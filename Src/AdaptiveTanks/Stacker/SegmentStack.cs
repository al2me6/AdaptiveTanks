using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

/// <summary>
/// In diameter-normalized (aspect ratio) units, relative to the root (bottom) of the stack.
/// </summary>
public readonly record struct ProtoSegmentPlacement(
    SegmentRole Role,
    Asset Asset,
    float Baseline,
    float Stretch,
    float Padding
);

public readonly record struct SegmentStackBuilder(float Diameter)
{
    private readonly List<ProtoSegmentPlacement> ProtoPlacements = [];

    public void Add(
        SegmentRole role, Asset asset, float normBaseline, float normStretch, float padding)
    {
        ProtoPlacements.Add(
            new ProtoSegmentPlacement(role, asset, normBaseline, normStretch, padding));
    }

    public void Add(BodySolution bodySolution, float normBaseline)
    {
        foreach (var segment in bodySolution.Stack)
        {
            Add(SegmentRole.Tank, segment.Asset, normBaseline, segment.Stretch, 0f);
            normBaseline += segment.StretchedAspectRatio;
        }
    }

    public SegmentStack Build()
    {
        List<SegmentPlacement> realPlacements = new(ProtoPlacements.Count);

        foreach (var (segmentRole, asset, normBaseline, stretch, normPadding) in ProtoPlacements)
        {
            var yStretch = stretch;
            var nativeDiameter = asset.nativeDiameter;

            var shouldFlip = (segmentRole, asset.nativeOrientationIsDown) is
                (SegmentRole.Tank, true)
                or (SegmentRole.TerminatorTop, true)
                or (SegmentRole.TerminatorBottom, false)
                or (SegmentRole.Intertank, true)
                or (SegmentRole.TankCapInternalTop, true)
                or (SegmentRole.TankCapInternalBottom, false);

            if (shouldFlip) yStretch *= -1;
            var nativeBottom = shouldFlip ? -asset.nativeBaseline.y : asset.nativeBaseline.x;

            var realHeightMin = normBaseline * Diameter;
            var realBottomOffset = nativeBottom / nativeDiameter * stretch * Diameter;
            realPlacements.Add(new SegmentPlacement(
                Role: segmentRole,
                Asset: asset,
                HeightMin: realHeightMin,
                Padding: normPadding * Diameter,
                Scale: new Vector3(1f, yStretch, 1f) * Diameter / nativeDiameter,
                Offset: Vector3.up * (realHeightMin - realBottomOffset)));
        }

        return new SegmentStack(Diameter, realPlacements);
    }
}

/// In real (m) units, relative to the root (bottom) of the stack.
public record SegmentPlacement(
    SegmentRole Role,
    Asset Asset,
    float HeightMin,
    float Padding,
    Vector3 Scale,
    Vector3 Offset)
{
    public float Height => Asset.NativeHeight * Mathf.Abs(Scale.y) + Padding;
    public float HeightMax => HeightMin + Height;

    public Transform? RealizedMesh { get; private set; } = null;

    public void RealizeWith(GameObject go)
    {
        RealizedMesh = go.transform;
        RealizedMesh.localScale = Scale;
        RealizedMesh.localPosition = Offset;
    }
}

public record SegmentStack(float Diameter, IReadOnlyList<SegmentPlacement> Placements)
{
    public float Height { get; } = Placements.Select(pl => pl.Height).Sum();
    public float HalfHeight => Height * 0.5f;

    public float TerminatorBottomHeightMax => GetTerminator(CapPosition.Bottom).HeightMax;
    public float TerminatorTopHeightMin => GetTerminator(CapPosition.Top).HeightMin;

    public (float min, float max) GetRangeOfRegion(CapPosition? region) => region switch
    {
        CapPosition.Bottom => (0f, TerminatorBottomHeightMax),
        null => (TerminatorBottomHeightMax, TerminatorTopHeightMin),
        CapPosition.Top => (TerminatorTopHeightMin, Height),
        _ => throw new ArgumentOutOfRangeException(nameof(region))
    };

    public (CapPosition? region, float min, float max) GetRegionRangeAtHeight(float height)
    {
        CapPosition? region = null;
        if (height < TerminatorBottomHeightMax) region = CapPosition.Bottom;
        else if (TerminatorTopHeightMin <= height) region = CapPosition.Top;
        var (min, max) = GetRangeOfRegion(region);
        return (region, min, max);
    }

    public (CapPosition? region, float normalizedHeight) GetRegionNormalizedHeight(float height)
    {
        var (region, min, max) = GetRegionRangeAtHeight(height);
        return (region, (height - min) / (max - min));
    }

    public float GetRealHeightFromNormalizedRegion(CapPosition? region, float normalizedHeight)
    {
        var (min, max) = GetRangeOfRegion(region);
        return normalizedHeight * (max - min) + min;
    }

    public SegmentPlacement GetTerminator(CapPosition position)
    {
        var terminator = position == CapPosition.Bottom ? Placements[0] : Placements[^1];
        if (!terminator.Role.IsTerminator())
            Debug.LogError("non-terminator segment found in terminating position");
        return terminator;
    }

    public float EvaluateTankVolume()
    {
        var volume = 0f;
        foreach (var placement in Placements)
        {
            var seg = placement.Asset.Segment;
            if (!seg.IsFueled) continue;
            var height = placement.Height;
            var segVolume = seg.geometryModel!.EvaluateVolume(Diameter, height);
            // Debug.Log($"{seg.geometryModel.GetType().Name}(d={Diameter:f}, h={height:f}) = {segVolume:f2} m³");
            volume += segVolume;
        }

        return volume;
    }

    // TODO: handle stretching.
    public float EvaluateStructuralCost() => Placements
        .Select(placement => placement.Asset.Segment.structuralCost)
        .WhereNotNull()
        .Select(cost => cost.Evaluate(Diameter))
        .Sum();

    public float EvaluateStructuralMass() => Placements
        .Select(placement => placement.Asset.Segment.structuralMass)
        .WhereNotNull()
        .Select(mass => mass.Evaluate(Diameter))
        .Sum();

    public float WorstDistortion()
    {
        var worstDelta = 0f;
        foreach (var placement in Placements)
        {
            var delta = Mathf.Abs(placement.Scale.y) / Mathf.Abs(placement.Scale.x) - 1f;
            if (Mathf.Abs(delta) > Mathf.Abs(worstDelta)) worstDelta = delta;
        }

        return worstDelta;
    }
}

public record SegmentStacks
{
    public SegmentStack Skin { get; }
    public SegmentStack Core { get; }

    public SegmentStacks(SegmentStack skin, SegmentStack core)
    {
        Skin = skin;
        Core = core;
        if (!MathUtils.ApproxEqAbs(Skin.Height, Core.Height, SegmentStacker.Tolerance))
            Debug.LogError($"mismatched stack heights {Skin.Height} != {Core.Height}");
        if (!MathUtils.ApproxEqRel(Skin.Diameter, Core.Diameter, 1e-4f))
            Debug.LogError($"mismatched stack diameters {Skin.Diameter} != {Core.Diameter}");
    }

    public float Diameter => Skin.Diameter;
    public float Height => Skin.Height;
    public float HalfHeight => Skin.HalfHeight;

    public void ApplyAnchorPosition(Transform anchor) =>
        anchor.localPosition = Vector3.down * HalfHeight;
}
