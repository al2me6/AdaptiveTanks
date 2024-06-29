using System;
using System.Collections;
using System.Collections.Generic;

namespace AdaptiveTanks;

#nullable enable

public record SelectedSegments(
    SegmentDef Tank,
    SegmentDef TerminatorTop,
    SegmentDef TerminatorBottom,
    SegmentDef? Intertank,
    SegmentDef? TankCapInternalTop,
    SegmentDef? TankCapInternalBottom,
    SegmentAlignment AlignTop,
    SegmentAlignment AlignBottom
) : IEnumerable<SegmentDef>
{
    public SelectedSegments(
        string tank,
        string terminatorTop, string terminatorBottom,
        string? intertank,
        string? tankCapInternalTop, string? tankCapInternalBottom,
        SegmentAlignment alignTop, SegmentAlignment alignBottom
    ) : this(
        Library<SegmentDef>.Get(tank),
        Library<SegmentDef>.Get(terminatorTop),
        Library<SegmentDef>.Get(terminatorBottom),
        Library<SegmentDef>.MaybeGet(intertank),
        Library<SegmentDef>.MaybeGet(tankCapInternalTop),
        Library<SegmentDef>.MaybeGet(tankCapInternalBottom),
        alignTop,
        alignBottom)
    {
    }

    public SegmentDef? this[SegmentRole role] => role switch
    {
        SegmentRole.Tank => Tank,
        SegmentRole.TerminatorTop => TerminatorTop,
        SegmentRole.TerminatorBottom => TerminatorBottom,
        SegmentRole.Intertank => Intertank,
        SegmentRole.TankCapInternalTop => TankCapInternalTop,
        SegmentRole.TankCapInternalBottom => TankCapInternalBottom,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public IEnumerator<SegmentDef> GetEnumerator()
    {
        yield return Tank;
        yield return TerminatorTop;
        yield return TerminatorBottom;
        if (Intertank != null) yield return Intertank;
        if (TankCapInternalTop != null) yield return TankCapInternalTop;
        if (TankCapInternalBottom != null) yield return TankCapInternalBottom;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
