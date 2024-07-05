using System.Linq;
using AdaptiveTanks.Utils;
using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks;

public class Texture : ConfigNodePersistenceBase
{
    [Persistent] public string diffuse;
    [Persistent] public string nrm;
}

public class Asset : ConfigNodePersistenceBase
{
    [Persistent] public string mu = BuiltinItems.EmptyMuPath;
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public float nativeHeight = 1f;
    [Persistent] public float nativeBaseline = 0f;
    [Persistent] public bool nativeOrientationIsDown = false;

    [Persistent] public Vector2 diameterRange = new(0f, float.PositiveInfinity);

    public Texture[] textures = [];

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        textures = node.LoadAllFromNodes<Texture>().ToArray();
    }

    public SegmentDef Segment { get; internal set; }

    public float AspectRatio => nativeHeight / nativeDiameter;

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

    public bool SupportsDiameter(float diameter) =>
        MinDiameter <= diameter && diameter < MaxDiameter;

    public static (float stretchA, float stretchB) NegotiateAspectRatio(
        Asset a, Asset b, float biasA)
    {
        var targetAspect = a.AspectRatio * biasA + b.AspectRatio * (1f - biasA);
        return (targetAspect / a.AspectRatio, targetAspect / b.AspectRatio);
    }
}
