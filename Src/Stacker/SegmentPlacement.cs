#nullable enable

namespace AdaptiveTanks;

public readonly struct SegmentPlacement(
    string? body,
    string? skin,
    float baseline,
    float distortion = 1)
{
    public string? Body { get; } = body;
    public string? Skin { get; } = skin;
    public float NormalizedBaseline { get; } = baseline;
    public float Distortion { get; } = distortion;
}
