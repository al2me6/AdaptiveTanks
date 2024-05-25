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
    [Persistent] public string mu;
    public Texture[] textures;
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public float nativeBaseline;
    [Persistent] public Vector2 diameterRange = new(0f, float.PositiveInfinity);

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

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
}
