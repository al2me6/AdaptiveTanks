namespace AdaptiveTanks;

public readonly struct SegmentPlacement(
    int modelIndex,
    float baseline,
    float stretch)
{
    public readonly int ModelIndex = modelIndex;
    public readonly float NormalizedBaseline = baseline;
    public readonly float Stretch = stretch;
}
