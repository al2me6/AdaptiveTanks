using System.Collections.Generic;
using System.Text;
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

    public IEnumerable<(string muPath, SegmentTransformation transformation)> IterSegments(
        float diameter)
    {
        foreach (var (segmentRole, asset, normBaseline, stretch) in Placements)
        {
            var yStretch = stretch;
            var nativeHeight = asset.nativeHeight;
            var nativeDiameter = asset.nativeDiameter;

            var nativeBaseline = asset.nativeBaseline;
            (nativeBaseline, var nativeTop) = asset.nativeOrientationIsDown
                ? (nativeBaseline - nativeHeight, nativeBaseline)
                : (nativeBaseline, nativeBaseline + nativeHeight);

            var shouldFlip = (segmentRole, asset.nativeOrientationIsDown) is
                (SegmentRole.Tank, true)
                or (SegmentRole.TerminatorTop, true)
                or (SegmentRole.TerminatorBottom, false)
                or (SegmentRole.Intertank, true)
                or (SegmentRole.TankCapInternalTop, true)
                or (SegmentRole.TankCapInternalBottom, false);
            if (shouldFlip) yStretch *= -1;
            if (shouldFlip) nativeBaseline = -nativeTop;

            var realScale =
                new Vector3(1f, yStretch, 1f) * diameter / nativeDiameter;
            var realOffset =
                Vector3.up * (normBaseline - nativeBaseline / nativeDiameter * stretch) * diameter;

            yield return (asset.mu, new SegmentTransformation(realScale, realOffset));
        }
    }

            var flippedNativeBaseline = shouldFlip ? -nativeTop : nativeBaseline;
            var normalizedNativeBaseline = flippedNativeBaseline / asset.nativeDiameter;
            var offset = Vector3.up * (baseline - normalizedNativeBaseline * stretch) * diameter;

            yield return (asset.mu, new SegmentTransformation(scale, offset));
        }
    }

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
