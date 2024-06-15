namespace AdaptiveTanks;

public static class SegmentStacker
{
    private static ProtoSegmentStack BuildProtoStack(
        float diameter, float height, SelectedSegments segments)
    {
        var protoStack = new ProtoSegmentStack(diameter, height);

        protoStack.AddTerminator(segments, CapPosition.Bottom, segments.AlignBottom);
        protoStack.TryAddFixed(segments, SegmentRole.TankCapInternalBottom);
        // TODO intertank
        protoStack.AddFlex(segments[SegmentRole.Tank]!, 1f);
        protoStack.TryAddFixed(segments, SegmentRole.TankCapInternalTop);
        protoStack.AddTerminator(segments, CapPosition.Top, segments.AlignTop);

        return protoStack;
    }

    public static SkinAndCore<SegmentStack> SolveStack(
        float diameter,
        float height,
        SelectedSegments skinSegments,
        SelectedSegments coreSegments)
    {
        var skinProto = BuildProtoStack(diameter, height, skinSegments);
        var coreProto = BuildProtoStack(diameter, height, coreSegments);

        ProtoSegmentStack.NegotiateStrictAlignment(skinProto, coreProto);

        skinProto.SolveFlexSegments();
        coreProto.SolveFlexSegments();

        return new SkinAndCore<SegmentStack>(skinProto.Elaborate(), coreProto.Elaborate());
    }
}
