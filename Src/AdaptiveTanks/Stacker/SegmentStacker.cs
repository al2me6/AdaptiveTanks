namespace AdaptiveTanks;

public readonly record struct StackerParameters(
    float Diameter,
    float Height,
    SelectedSegments SkinSegments,
    SelectedSegments CoreSegments
);

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

    public static SkinAndCore<SegmentStack> SolveStack(in StackerParameters parameters)
    {
        return new SkinAndCore<SegmentStack>(
            Skin: SolveStack(parameters.Diameter, parameters.Height, parameters.SkinSegments),
            Core: SolveStack(parameters.Diameter, parameters.Height, parameters.CoreSegments));
    }
}
