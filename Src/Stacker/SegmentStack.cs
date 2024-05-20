using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(int ModelIndex, float Baseline, float Stretch);

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
    SegmentDef CoreSegmentDef,
    List<SegmentPlacement> SegmentPlacements,
    Vector2 NormalizedExtent)
{
    public float ExtentCenter => (NormalizedExtent.x + NormalizedExtent.y) / 2f;

    public IEnumerable<(GameObject prefab, SegmentTransformation transformation)> IterSegments(
        float diameter)
    {
        foreach (var placement in SegmentPlacements)
        {
            var asset = CoreSegmentDef.models[placement.ModelIndex].GetAssetForDiameter(diameter);
            var nativeDiameter = asset.nativeDiameter;
            var effectiveDiameter = diameter / nativeDiameter;
            var scale = new Vector3(1f, placement.Stretch, 1f) * effectiveDiameter;
            var normalizedSegmentBaseline = asset.nativeYMin / nativeDiameter;
            var offset = new Vector3(
                0f,
                (placement.Baseline - normalizedSegmentBaseline * placement.Stretch) * diameter,
                0f);
            yield return (asset.Prefab, new SegmentTransformation(scale, offset));
        }
    }
}
