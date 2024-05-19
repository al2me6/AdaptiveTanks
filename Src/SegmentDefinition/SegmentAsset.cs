using System.Collections.Generic;
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
    public readonly List<SegmentAssetTexture> tex = [];
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public float nativeYMin;
    [Persistent] public Vector2 diameterRange = new(float.NegativeInfinity, float.PositiveInfinity);

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

    public GameObject Prefab { get; private set; }

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        tex.AddRange(node.LoadAllFromNodes<SegmentAssetTexture>());

        Prefab = GameDatabase.Instance.GetModelPrefab(mu);
        if (Prefab == null) Debug.LogError($"asset {mu} was not found!");

        Debug.Log(
            $"ASSET: {mu}, diam. {MinDiameter}-{nativeDiameter}-{MaxDiameter}, yMin {nativeYMin}");
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllToNodes(tex);
    }
}
