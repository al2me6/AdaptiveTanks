using System.Collections.Generic;
using AdaptiveTanks.ConfigNodeExtensions;

namespace AdaptiveTanks;

public class SegmentModel : IRepeatedConfigNode
{
    public string ConfigNodeName() => "MODEL";

    [Persistent] public float nativeAspectRatio = 1f;
    public readonly List<SegmentAsset> assets = [];

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
