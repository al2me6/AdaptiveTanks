using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(
    SegmentRole Role,
    int ModelIdx,
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
    SelectedSegmentDefs segmentDefs,
    List<SegmentPlacement> SegmentPlacements,
    Vector2 NormalizedExtent)
{
    public float ExtentCenter => (NormalizedExtent.x + NormalizedExtent.y) / 2f * Diameter;
    public float HalfHeight => (NormalizedExtent.y - NormalizedExtent.x) / 2f * Diameter;

    public IEnumerable<(string mu, SegmentTransformation transformation)> IterSegments()
    {
        foreach (var placement in SegmentPlacements)
        {
            var model = segmentDefs[placement.Role].models[placement.ModelIdx];
            var asset = model.GetAssetForDiameter(Diameter);

            var nativeDiameter = asset.nativeDiameter;
            var effectiveDiameter = Diameter / nativeDiameter;
            var nativeHeight = nativeDiameter * model.nativeAspectRatio;

            var nativelyUpsideDown = model.nativelyUpsideDown;

            var nativeBaseline = asset.nativeBaseline;
            (nativeBaseline, var nativeTop) = nativelyUpsideDown
                ? (nativeBaseline - nativeHeight, nativeBaseline)
                : (nativeBaseline, nativeBaseline + nativeHeight);

            var shouldFlip = (placement.Role == SegmentRole.Nose && nativelyUpsideDown) ||
                             (placement.Role == SegmentRole.Body && nativelyUpsideDown) ||
                             (placement.Role == SegmentRole.Mount && !nativelyUpsideDown);
            var flipMultiplier = shouldFlip ? -1 : 1;

            var scale = new Vector3(1f, placement.Stretch * flipMultiplier, 1f) * effectiveDiameter;

            var flippedNativeBaseline = shouldFlip ? -nativeTop : nativeBaseline;
            var normalizedBaseline = flippedNativeBaseline / nativeDiameter;
            var offset = new Vector3(
                0f,
                (placement.Baseline - normalizedBaseline * placement.Stretch) * Diameter,
                0f);

            yield return (asset.mu, new SegmentTransformation(scale, offset));
        }
    }
}

public static class SkinAndCoreExtensions
{
    public static float Diameter(this SkinAndCore<SegmentStack> stacks)
    {
        var skinDiam = stacks.Skin.Diameter;
        var coreDiam = stacks.Core.Diameter;
        if (skinDiam != coreDiam)
            throw new InvalidOperationException("inconsistent skin/core stack diameters");
        return skinDiam;
    }

    public static float HalfHeight(this SkinAndCore<SegmentStack> stacks)
    {
        var skinHalfHeight = stacks.Skin.HalfHeight;
        var coreHalfHeight = stacks.Core.HalfHeight;
        if (skinHalfHeight != coreHalfHeight)
            throw new InvalidOperationException("inconsistent skin/core stack heights");
        return skinHalfHeight;
    }
}
