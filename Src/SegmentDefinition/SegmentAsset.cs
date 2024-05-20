using System.Linq;
using AdaptiveTanks.Extensions;
using UnityEngine;

namespace AdaptiveTanks;

public class SegmentAssetTexture : IRepeatedConfigNode
{
    public string ConfigNodeName() => "TEXTURE";
    [Persistent] public string diffuse;
    [Persistent] public string nrm;
    public void Load(ConfigNode node) => ConfigNode.LoadObjectFromConfig(this, node);
    public void Save(ConfigNode node) => ConfigNode.CreateConfigFromObject(this, node);
}

public class SegmentAsset : IRepeatedConfigNode
{
    public string ConfigNodeName() => "ASSET";

    [Persistent] public string mu;
    public SegmentAssetTexture[] tex;
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public float nativeYMin;
    [Persistent] public Vector2 diameterRange = new(0f, float.PositiveInfinity);

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        tex = node.LoadAllFromNodes<SegmentAssetTexture>().ToArray();
        Debug.Log(
            $"ASSET: {mu}, diam. {MinDiameter}-{nativeDiameter}-{MaxDiameter}, yMin {nativeYMin}");
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllToNodes(tex);
    }
}
