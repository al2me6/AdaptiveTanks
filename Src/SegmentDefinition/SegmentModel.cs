using System.Collections.Generic;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public class SegmentModel : IRepeatedConfigNode
{
    public string ConfigNodeName() => "MODEL";

    [Persistent] public float nativeAspectRatio = 1f;
    [Persistent] public float maxDistortion = 1.2f;
    public readonly List<SegmentAsset> assets = [];

    public SegmentAsset GetAssetForDiameter(float diameter)
    {
        // TODO implement
        return assets[0];
    }

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        assets.AddRange(node.LoadAllFromNodes<SegmentAsset>());

        Debug.Log($"MODEL: aspect ratio {nativeAspectRatio}");
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllToNodes(assets);
    }
}
