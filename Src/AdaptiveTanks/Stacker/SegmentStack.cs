using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

/// <summary>
/// In diameter-normalized (i.e. aspect ratio) units.
/// </summary>
public readonly record struct SegmentPlacement(
    SegmentRole Role,
    Asset Asset,
    float Baseline,
    float Stretch
);

public readonly record struct SegmentStackBuilder(float Diameter)
{
    private readonly List<SegmentPlacement> Placements = [];

    public void Add(SegmentRole role, Asset asset, float normBaseline, float normStretch)
    {
        Placements.Add(new SegmentPlacement(role, asset, normBaseline, normStretch));
    }

    public void Add(BodySolution bodySolution, float normBaseline)
    {
        foreach (var segment in bodySolution.Stack)
        {
            Add(SegmentRole.Tank, segment.Asset, normBaseline, segment.Stretch);
            normBaseline += segment.StretchedAspectRatio;
        }
    }

    public SegmentStack Build(float aspectRatio)
    {
        List<SegmentRealization> realizations = new(Placements.Count);

        foreach (var (segmentRole, asset, normBaseline, stretch) in Placements)
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

            var realScale =
                new Vector3(1f, yStretch, 1f) * Diameter / nativeDiameter;
            var realOffset =
                Vector3.up * (normBaseline - nativeBottom / nativeDiameter * stretch) * Diameter;

            realizations.Add(new SegmentRealization(segmentRole, asset, realScale, realOffset));
        }

        return new SegmentStack(Diameter, aspectRatio, realizations);
    }
}

public record SegmentRealization(
    SegmentRole Role,
    Asset Asset,
    Vector3 Scale,
    Vector3 Offset)
{
    public float Height => Asset.NativeHeight * Mathf.Abs(Scale.y);

    public void ApplyTo(GameObject go)
    {
        go.transform.localScale = Scale;
        go.transform.localPosition = Offset;
    }
}

public record SegmentStack(
    float Diameter,
    float AspectRatio,
    List<SegmentRealization> Realizations)
{
    public float EvaluateTankVolume()
    {
        var volume = 0f;
        foreach (var realization in Realizations)
        {
            var seg = realization.Asset.Segment;
            if (!seg.IsFueled) continue;
            var height = realization.Height;
            var segVolume = seg.geometryModel!.EvaluateVolume(Diameter, height);
            // Debug.Log($"{seg.geometryModel.GetType().Name}(d={Diameter:f}, h={height:f}) = {segVolume:f2} m³");
            volume += segVolume;
        }

        return volume;
    }

    // TODO: handle stretching.
    public float EvaluateStructuralCost() => Realizations
        .Select(realization => realization.Asset.Segment.structuralCost)
        .WhereNotNull()
        .Select(cost => cost.Evaluate(Diameter))
        .Sum();

    public float EvaluateStructuralMass() => Realizations
        .Select(realization => realization.Asset.Segment.structuralMass)
        .WhereNotNull()
        .Select(mass => mass.Evaluate(Diameter))
        .Sum();

    public float WorstDistortion()
    {
        var worstDelta = 0f;
        foreach (var (_, _, scale, _) in Realizations)
        {
            var delta = Mathf.Abs(scale.y) / Mathf.Abs(scale.x) - 1f;
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
        if (!MathUtils.ApproxEqRelative(Skin.AspectRatio, Core.AspectRatio, 1e-2f))
            Debug.LogError($"mismatched solution aspects {Skin.AspectRatio}, {Core.AspectRatio}");
        if (!MathUtils.ApproxEqRelative(Skin.Diameter, Core.Diameter, 1e-4f))
            Debug.LogError($"mismatched solution aspects {Skin.Diameter}, {Core.Diameter}");
    }

    public float Diameter => Skin.Diameter;
    public float Height => Skin.Diameter * Skin.AspectRatio;
    public float HalfHeight => Height * 0.5f;
}
