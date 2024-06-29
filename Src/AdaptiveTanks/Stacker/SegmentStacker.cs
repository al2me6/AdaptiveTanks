namespace AdaptiveTanks;

#nullable enable

public static class SegmentStacker
{
    public static float MinHeight(
        float diameter, SelectedSegments skinSegments, SelectedSegments coreSegments)
    {
        // TODO: do less work here?
        // But most of the algorithm still needs to run due to negotiation.
        var solution = SolveStack(diameter, 0f, skinSegments, coreSegments, [1f], 0f);
        return solution.Height();
    }

    public static SkinAndCore<SegmentStack> SolveStack(
        float diameter,
        float height,
        SelectedSegments skinSegments,
        SelectedSegments coreSegments,
        float[]? volumeFractions,
        float maxIntertankVolumetricDeviation
    )
    {
        volumeFractions = skinSegments.Intertank != null
                          && coreSegments.Intertank != null
                          && volumeFractions != null
            ? volumeFractions
            : [1f];
        var skinProto = new ProtoSegmentStack(diameter, height, skinSegments, volumeFractions);
        var coreProto = new ProtoSegmentStack(diameter, height, coreSegments, volumeFractions);

        ProtoSegmentStack.NegotiateSegmentAlignment(skinProto, coreProto);

        skinProto.ComputeFlexSegmentAspectRatios();
        coreProto.ComputeFlexSegmentAspectRatios();

        ProtoSegmentStack.NegotiateFlexAspectRatios(
            skinProto, coreProto, maxIntertankVolumetricDeviation);

        skinProto.SolveFlexSegments();
        coreProto.SolveFlexSegments();

        return new SkinAndCore<SegmentStack>(skinProto.Elaborate(), coreProto.Elaborate());
    }
}
