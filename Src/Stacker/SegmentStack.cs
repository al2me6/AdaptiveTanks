using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(int ModelIdx, float Baseline, float Stretch);

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
    SegmentDef CoreSegmentDef,
    List<SegmentPlacement> SegmentPlacements,
    Vector2 NormalizedExtent)
{
    public float ExtentCenter => (NormalizedExtent.x + NormalizedExtent.y) / 2f * Diameter;

    public IEnumerable<(string mu, SegmentTransformation transformation)> IterSegments()
    {
        foreach (var placement in SegmentPlacements)
        {
            var asset = CoreSegmentDef.models[placement.ModelIdx].GetAssetForDiameter(Diameter);
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
