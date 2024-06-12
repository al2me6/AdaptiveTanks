namespace AdaptiveTanks;

public static class SegmentStacker
{
    private static SegmentStack SolveStack(float diameter, float height, SelectedSegments segments)
    {
        var protoStack = new ProtoSegmentStack(diameter, height);

        protoStack.AddTerminator(segments, CapPosition.Bottom, segments.AlignBottom);
        protoStack.TryAddFixed(segments, SegmentRole.TankCapInternalBottom);
        // TODO intertank
        protoStack.AddFlex(segments[SegmentRole.Tank]!, 1f);
        protoStack.TryAddFixed(segments, SegmentRole.TankCapInternalTop);
        protoStack.AddTerminator(segments, CapPosition.Top, segments.AlignTop);

        return protoStack.Elaborate();
    }

    public static SkinAndCore<SegmentStack> SolveStack(
        float diameter,
        float height,
        SelectedSegments skinSegments,
        SelectedSegments coreSegments)
    {
        return new SkinAndCore<SegmentStack>(
            Skin: SolveStack(diameter, height, skinSegments),
            Core: SolveStack(diameter, height, coreSegments));
    }
}