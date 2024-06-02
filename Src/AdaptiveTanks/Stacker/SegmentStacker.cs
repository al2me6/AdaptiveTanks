using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveTanks;

public readonly record struct StackerParameters(
    float Diameter,
    float Height,
    SelectedSegments SkinSegments,
    SelectedSegments CoreSegments
);

public readonly record struct BodySolution(List<Asset> Stack, float Height)
{
    public void WriteToPlacements(
        ref readonly List<SegmentPlacement> placements, List<float> stretches, float baseline)
    {
        for (var i = 0; i < Stack.Count; ++i)
        {
            var placement =
                new SegmentPlacement(SegmentRole.Body, Stack[i], baseline, stretches[i]);
            placements.Add(placement);
            baseline += placement.Asset.AspectRatio * placement.Stretch;
        }
    }
}

public static class SegmentStacker
{
    public static SegmentStack SolveStack(
        float diameter, float height, SelectedSegments segments)
    {
        var normalizedHeight = height / diameter;

        // Todo warn if multiple?
        var noseAsset = segments.Nose.GetFirstAssetForDiameter(diameter);
        var mountAsset = segments.Mount.GetFirstAssetForDiameter(diameter);

        var noseHeight = noseAsset.AspectRatio;
        var mountHeight = mountAsset.AspectRatio;
        var bodyHeight = normalizedHeight - noseHeight - mountHeight;

        var bodySolution = BodySolver.SolvePreliminary(
            segments.Body.GetAssetsForDiameter(diameter).ToArray(),
            bodyHeight);
        var bodyStretches = BodySolver.ComputeStretching(bodyHeight, bodySolution);

        List<SegmentPlacement> placements = new(bodySolution.Stack.Count + 2);
        placements.Add(new SegmentPlacement(SegmentRole.Mount, mountAsset, -mountHeight));
        bodySolution.WriteToPlacements(ref placements, bodyStretches, 0f);
        placements.Add(new SegmentPlacement(SegmentRole.Nose, noseAsset, bodyHeight));

        var extent = new Vector2(-mountHeight, bodyHeight + noseHeight);

        return new SegmentStack(diameter, placements, extent);
    }

    public static SkinAndCore<SegmentStack> SolveSkinAndCoreSeparately(StackerParameters parameters)
    {
        return new SkinAndCore<SegmentStack>(
            SolveStack(parameters.Diameter, parameters.Height, parameters.SkinSegments),
            SolveStack(parameters.Diameter, parameters.Height, parameters.CoreSegments));
    }
}
