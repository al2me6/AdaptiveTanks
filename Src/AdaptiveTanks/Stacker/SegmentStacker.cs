namespace AdaptiveTanks;

public static class SegmentStacker
{
    public const float Tolerance = 5e-3f;

    // TODO: do less work here?
    // But most of the algorithm still needs to run due to negotiation.
    public static float MinHeight(float diameter, SelectedSegments skin, SelectedSegments core) =>
        SolveStack(diameter, 0f, skin, core, [1f], 0f).Height;

    public static SegmentStacks SolveStack(
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

        return new SegmentStacks(skinProto.Elaborate(), coreProto.Elaborate());
    }
}
