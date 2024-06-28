namespace AdaptiveTanks;

#nullable enable

public static class SegmentStacker
{
    public static SkinAndCore<SegmentStack> SolveStack(
        float diameter,
        float height,
        SelectedSegments skinSegments,
        SelectedSegments coreSegments,
        float[]? volumeFractions)
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

        ProtoSegmentStack.NegotiateFlexAspectRatios(skinProto, coreProto);

        skinProto.SolveFlexSegments();
        coreProto.SolveFlexSegments();

        return new SkinAndCore<SegmentStack>(skinProto.Elaborate(), coreProto.Elaborate());
    }
}
