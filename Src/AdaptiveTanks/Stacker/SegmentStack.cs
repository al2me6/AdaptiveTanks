using System.Collections.Generic;
using System.Linq;
using System.Text;
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
)
{
    public float RealHeight(float diameter) => Asset.AspectRatio * Stretch * diameter;
}

public readonly record struct SegmentTransformation(Vector3 RealScale, Vector3 RealOffset)
{
    public void ApplyTo(GameObject go)
    {
        go.transform.localScale = RealScale;
        go.transform.localPosition = RealOffset;
    }
}

public class SegmentStack
{
    public List<SegmentPlacement> Placements { get; } = [];

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

    public IEnumerable<(Asset asset, SegmentTransformation transformation)> IterSegments(
        float diameter)
    {
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
                new Vector3(1f, yStretch, 1f) * diameter / nativeDiameter;
            var realOffset =
                Vector3.up * (normBaseline - nativeBottom / nativeDiameter * stretch) * diameter;

            yield return (asset, new SegmentTransformation(realScale, realOffset));
        }
    }

    public float EvaluateTankVolume(float diameter)
    {
        var volume = 0f;
        foreach (var placement in Placements)
        {
            var seg = placement.Asset.Segment;
            if (!seg.IsFueled) continue;
            var realHeight = placement.RealHeight(diameter);
            var segVolume = seg.geometryModel!.EvaluateVolume(diameter, realHeight);
            // Debug.Log($"{seg.geometryModel.GetType().Name}(d={diameter:f}, h={realHeight:f}) = {segVolume:f2} m³");
            volume += segVolume;
        }

        return volume;
    }

    // TODO: handle stretching.
    public float EvaluateStructuralCost(float diameter) => Placements
        .Select(placement => placement.Asset.Segment.structuralCost)
        .WhereNotNull()
        .Select(cost => cost.Evaluate(diameter))
        .Sum();

    public float EvaluateStructuralMass(float diameter) => Placements
        .Select(placement => placement.Asset.Segment.structuralMass)
        .WhereNotNull()
        .Select(mass => mass.Evaluate(diameter))
        .Sum();

    public float WorstDistortion()
    {
        var worstDelta = 0f;
        foreach (var placement in Placements)
        {
            var delta = placement.Stretch - 1f;
            if (Mathf.Abs(delta) > Mathf.Abs(worstDelta)) worstDelta = delta;
        }

        return worstDelta;
    }

    public string DebugPrint()
    {
        var sb = new StringBuilder();
        foreach (var (segmentRole, asset, baseline, _) in Placements)
        {
            sb.AppendFormat("{0:f} [{1}]: {2}\n",
                baseline, segmentRole, asset.mu.Split('/')[^1]);
        }

        return sb.ToString();
    }
}

public record SegmentStacks(
    float Diameter,
    float AspectRatio,
    SegmentStack Skin,
    SegmentStack Core
)
{
    public float Height => Diameter * AspectRatio;
    public float HalfHeight => Height * 0.5f;
}
