using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(
    SegmentRole Role,
    Asset Asset,
    float Baseline,
    float Stretch
);

public readonly record struct SegmentTransformation(
    Vector3 RenormalizedScaling,
    Vector3 RenormalizedOffset)
{
    public void ApplyTo(GameObject go)
    {
        go.transform.localScale = RenormalizedScaling;
        go.transform.localPosition = RenormalizedOffset;
    }
}

public record SegmentStack(float Diameter)
{
    public List<SegmentPlacement> Placements { get; } = [];

    public float AspectRatio { get; internal set; }

    public float Height => AspectRatio * Diameter;
    public float HalfHeight => Height * 0.5f;

    public void Add(SegmentRole role, Asset asset, float baseline, float stretch)
    {
        Placements.Add(new SegmentPlacement(role, asset, baseline, stretch));
    }

    public void Add(BodySolution bodySolution, float baseline)
    {
        foreach (var segment in bodySolution.Stack)
        {
            Add(SegmentRole.Tank, segment.Asset, baseline, segment.Stretch);
            baseline += segment.StretchedAspectRatio;
        }
    }

    public IEnumerable<(string mu, SegmentTransformation transformation)> IterSegments()
    {
        foreach (var (segmentRole, asset, baseline, stretch) in Placements)
        {
            var effectiveDiameter = Diameter / asset.nativeDiameter;
            var nativeHeight = asset.nativeDiameter * asset.AspectRatio;

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

            var flipMultiplier = shouldFlip ? -1 : 1;

            var scale = new Vector3(1f, stretch * flipMultiplier, 1f) * effectiveDiameter;

            var flippedNativeBaseline = shouldFlip ? -nativeTop : nativeBaseline;
            var normalizedNativeBaseline = flippedNativeBaseline / asset.nativeDiameter;
            var offset =
                new Vector3(0f, (baseline - normalizedNativeBaseline * stretch) * Diameter, 0f);

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

public static class SkinAndCoreExtensions
{
    public static float Diameter(this SkinAndCore<SegmentStack> stacks)
    {
        var skinDiam = stacks.Skin.Diameter;
        var coreDiam = stacks.Core.Diameter;
        if (!Mathf.Approximately(skinDiam, coreDiam))
            Debug.LogError("inconsistent skin/core stack diameters");
        return skinDiam;
    }

    public static float Height(this SkinAndCore<SegmentStack> stacks)
    {
        var skinHeight = stacks.Skin.Height;
        var coreHeight = stacks.Core.Height;
        if (!Mathf.Approximately(skinHeight, coreHeight))
            Debug.LogError("inconsistent skin/core stack heights");
        return skinHeight;
    }
}
