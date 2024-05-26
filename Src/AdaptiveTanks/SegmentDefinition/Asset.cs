using System.Linq;
using AdaptiveTanks.Extensions;
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
    [Persistent] public string mu = "AdaptiveTanks/Assets/Empty";
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public float nativeHeight = 1f;
    [Persistent] public float nativeBaseline = -0.5f;
    [Persistent] public bool nativeOrientationIsDown = false;

    [Persistent] public Vector2 diameterRange = new(0f, float.PositiveInfinity);
    [Persistent] public float maxHeightDistortion = 0.25f;

    public Texture[] textures = [];

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        textures = node.LoadAllFromNodes<Texture>().ToArray();
    }

    public override void Save(ConfigNode node)
    {
        base.Save(node);
        node.WriteAllToNodes(textures);
    }

    public float AspectRatio => nativeHeight / nativeDiameter;

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

    public bool SupportsDiameter(float diameter) =>
        true;
    // MinDiameter <= diameter && diameter < MaxDiameter;
    // TODO actually surface this to the UI so this doesn't explode.
    // TODO validate continuity of supported ranges.
}
