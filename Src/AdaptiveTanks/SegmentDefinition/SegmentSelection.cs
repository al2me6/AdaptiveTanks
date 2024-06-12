﻿using System;

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
)
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
        intertank != null ? Library<SegmentDef>.Get(intertank) : null,
        tankCapInternalTop != null ? Library<SegmentDef>.Get(tankCapInternalTop) : null,
        tankCapInternalBottom != null ? Library<SegmentDef>.Get(tankCapInternalBottom) : null,
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
}
