using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(
    SegmentRole Role,
    Asset Asset,
    float Baseline,
    float Stretch = 1f
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

public record SegmentStack(
    float Diameter,
    List<SegmentPlacement> SegmentPlacements,
    Vector2 NormalizedExtent)
{
    public float ExtentCenter => (NormalizedExtent.x + NormalizedExtent.y) / 2f * Diameter;
    public float HalfHeight => (NormalizedExtent.y - NormalizedExtent.x) / 2f * Diameter;

    public IEnumerable<(string mu, SegmentTransformation transformation)> IterSegments()
    {
        foreach (var (segmentRole, asset, baseline, stretch) in SegmentPlacements)
        {
            var effectiveDiameter = Diameter / asset.nativeDiameter;
            var nativeHeight = asset.nativeDiameter * asset.AspectRatio;

            var nativeBaseline = asset.nativeBaseline;
            (nativeBaseline, var nativeTop) = asset.nativeOrientationIsDown
                ? (nativeBaseline - nativeHeight, nativeBaseline)
                : (nativeBaseline, nativeBaseline + nativeHeight);

            var shouldFlip =
                (segmentRole == SegmentRole.Nose && asset.nativeOrientationIsDown) ||
                (segmentRole == SegmentRole.Body && asset.nativeOrientationIsDown) ||
                (segmentRole == SegmentRole.Mount && !asset.nativeOrientationIsDown);
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
        foreach (var placement in SegmentPlacements)
        {
            var delta = placement.Stretch - 1f;
            if (Mathf.Abs(delta) > Mathf.Abs(worstDelta)) worstDelta = delta;
        }

        return worstDelta;
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

    public static float HalfHeight(this SkinAndCore<SegmentStack> stacks)
    {
        var skinHalfHeight = stacks.Skin.HalfHeight;
        var coreHalfHeight = stacks.Core.HalfHeight;
        if (!Mathf.Approximately(skinHalfHeight, coreHalfHeight))
            Debug.LogError("inconsistent skin/core stack heights");
        return skinHalfHeight;
    }
}
