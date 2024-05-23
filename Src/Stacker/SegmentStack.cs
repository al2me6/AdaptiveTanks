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
            var asset = segmentDefs[placement.Role].models[placement.ModelIdx]
                .GetAssetForDiameter(Diameter);
            var nativeDiameter = asset.nativeDiameter;
            var effectiveDiameter = Diameter / nativeDiameter;
            var scale = new Vector3(1f, placement.Stretch, 1f) * effectiveDiameter;
            var normalizedSegmentBaseline = asset.nativeYMin / nativeDiameter;
            var offset = new Vector3(
                0f,
                (placement.Baseline - normalizedSegmentBaseline * placement.Stretch) * Diameter,
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
