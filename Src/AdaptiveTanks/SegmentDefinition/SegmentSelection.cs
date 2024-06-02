using System;

namespace AdaptiveTanks;

public readonly record struct SelectedSegments(
    SegmentDef Nose,
    SegmentDef Body,
    SegmentDef Mount
)
{
    public SelectedSegments(string nose, string body, string mount) : this(
        Library<SegmentDef>.Get(nose),
        Library<SegmentDef>.Get(body),
        Library<SegmentDef>.Get(mount))
    {
    }

    public SegmentDef this[SegmentRole role] => role switch
    {
        SegmentRole.Nose => Nose,
        SegmentRole.Body => Body,
        SegmentRole.Mount => Mount,
        _ => throw new IndexOutOfRangeException()
    };
};
