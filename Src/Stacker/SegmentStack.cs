using System.Collections.Generic;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct SegmentPlacement(
    int ModelIndex,
    float NormalizedBaseline,
    float Stretch);

public record SegmentStack(
    SegmentDef CoreSegmentDef,
    List<SegmentPlacement> SegmentPlacements)
{
    protected void RealizeGeometryFromScratch(Transform anchor, float diameter)
    {
        foreach (var placement in SegmentPlacements)
        {
            // TODO: select asset based on diameter.
            var coreAsset = CoreSegmentDef.models[placement.ModelIndex].assets[0];
            var nativeDiameter = coreAsset.nativeDiameter;
            var effectiveDiameter = diameter / nativeDiameter;

            var coreSegmentGO = Object.Instantiate(coreAsset.Prefab);
            coreSegmentGO.SetActive(true);

            var coreSegmentTransform = coreSegmentGO.transform;
            coreSegmentTransform.NestToParent(anchor);

            coreSegmentTransform.localScale =
                new Vector3(1f, placement.Stretch, 1f) * effectiveDiameter;

            var normalizedBaselinePosition = coreAsset.nativeYMin / nativeDiameter;
            coreSegmentTransform.localPosition = new Vector3(
                0f,
                (placement.NormalizedBaseline - normalizedBaselinePosition * placement.Stretch) *
                diameter,
                0f);
        }
    }

    public void RealizeGeometry(ModuleAdaptiveTank moduleAT, string anchorName,
        SegmentStack previous)
    {
        var anchor = TransformUtils.GetOrCreateAnchorTransform(moduleAT.part, anchorName);

        // TODO: adjust existing stack instead of spawning new stack.
        anchor.ClearChildren();
        RealizeGeometryFromScratch(anchor, moduleAT.diameter);
    }
}
