using System;

namespace AdaptiveTanks;

public enum SegmentRole : byte
{
    Tank,
    TerminatorTop,
    TerminatorBottom,
    Intertank,
    TankCapInternalTop,
    TankCapInternalBottom
}

public enum CapPosition : byte
{
    Top,
    Bottom
}

public enum SegmentLayer : byte
{
    Skin,
    Core
}

public enum SegmentAlignment : byte
{
    PinBothEnds,
    PinInteriorEnd
}

public static class SegmentClassificationExtensions
{
    public static bool IsTerminator(this SegmentRole role) => role is
        SegmentRole.TerminatorTop or SegmentRole.TerminatorBottom;

    public static CapPosition? TryAsCapPosition(this SegmentRole role) => role switch
    {
        SegmentRole.TerminatorTop or SegmentRole.TankCapInternalTop => CapPosition.Top,
        SegmentRole.TerminatorBottom or SegmentRole.TankCapInternalBottom => CapPosition.Bottom,
        _ => null
    };

    public static SegmentRole AsRoleTerminator(this CapPosition position) => position switch
    {
        CapPosition.Top => SegmentRole.TerminatorTop,
        CapPosition.Bottom => SegmentRole.TerminatorBottom,
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    public static string AsNoseMount(this CapPosition position) => position switch
    {
        CapPosition.Top => "Nose",
        CapPosition.Bottom => "Mount",
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    public static SegmentAlignment Toggle(this SegmentAlignment alignment) => alignment switch
    {
        SegmentAlignment.PinBothEnds => SegmentAlignment.PinInteriorEnd,
        SegmentAlignment.PinInteriorEnd => SegmentAlignment.PinBothEnds,
        _ => throw new ArgumentOutOfRangeException(nameof(alignment))
    };
}
