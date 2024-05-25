using System.Linq;
using AdaptiveTanks.Extensions;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public class Model : ConfigNodePersistenceBase
{
    [Persistent] public float nativeAspectRatio = 1f;
    [Persistent] public float maxDistortion = 0f;
    [Persistent] public bool nativelyUpsideDown = false;
    public Asset[] assets;

    public Asset GetAssetForDiameter(float diameter)
    {
        // TODO implement
        return assets[0];
    }

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        assets = node.LoadAllFromNodes<Asset>().ToArray();
    }

    public override void Save(ConfigNode node)
    {
        base.Save(node);
        node.WriteAllToNodes(assets);
    }
}
