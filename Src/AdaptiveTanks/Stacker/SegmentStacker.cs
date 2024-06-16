namespace AdaptiveTanks;

#nullable enable

public static class SegmentStacker
{
    public static SkinAndCore<SegmentStack> SolveStack(
        float diameter,
        float height,
        SelectedSegments skinSegments,
        SelectedSegments coreSegments,
        float[]? flexFactors)
    {
        flexFactors = skinSegments.Intertank != null
                      && coreSegments.Intertank != null
                      && flexFactors != null
            ? flexFactors
            : [1f];
        var skinProto = new ProtoSegmentStack(diameter, height, skinSegments, flexFactors);
        var coreProto = new ProtoSegmentStack(diameter, height, coreSegments, flexFactors);

        ProtoSegmentStack.NegotiateSegmentAlignment(skinProto, coreProto);

        skinProto.TrySolveFlexSegmentsWithIntertanks();
        coreProto.TrySolveFlexSegmentsWithIntertanks();

        ProtoSegmentStack.NegotiateIntertankAlignment(skinProto, coreProto);

        return new SkinAndCore<SegmentStack>(skinProto.Elaborate(), coreProto.Elaborate());
    }
}
