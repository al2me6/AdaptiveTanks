using System;

namespace AdaptiveTanks.SegmentDefinition;

[Flags]
public enum SegmentRoleCfg : byte
{
    tank = 1 << 0,
    intertank = 1 << 1,
    tankCapInternal = 1 << 2,
    tankCapTerminal = 1 << 3,
    accessory = 1 << 4
}

[Flags]
public enum CapPositionCfg : byte
{
    top = 1 << 0,
    bottom = 1 << 1,
    either = top | bottom
}

[Flags]
public enum SegmentAlignmentCfg : byte
{
    pinBothEnds = 1 << 0,
    pinInteriorEnd = 1 << 1
}
