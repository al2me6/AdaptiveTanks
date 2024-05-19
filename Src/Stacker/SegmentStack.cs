using System.Collections.Generic;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

public class SegmentStack(SegmentDef coreDef, List<SegmentPlacement> segments)
{
    public SegmentDef coreSegmentDef = coreDef;
    public List<SegmentPlacement> segmentPlacements = segments;

    protected void RealizeGeometryFromScratch(Transform anchor, float diameter)
    {
        foreach (var placement in segmentPlacements)
        {
            // TODO: select asset based on diameter.
            var coreAsset = coreSegmentDef.models[placement.ModelIndex].assets[0];
            var coreSegmentGO = Object.Instantiate(coreAsset.prefab);
            coreSegmentGO.SetActive(true);
            var coreSegmentTransform = coreSegmentGO.transform;
            coreSegmentTransform.NestToParent(anchor);
            coreSegmentTransform.localScale = new Vector3(1f, placement.Stretch, 1f) * diameter;
            coreSegmentTransform.localPosition = Vector3.up *
                                                 (placement.NormalizedBaseline +
                                                  coreAsset.nativeYMin) * diameter;
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
