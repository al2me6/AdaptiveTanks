using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

[Flags]
public enum SegmentRoleSerialize
{
    body = 1,
    nose = 2,
    mount = 4,
    cap = nose | mount,
    intertank = 8
}

public static class SegmentRoleSerializeExtensions
{
    public static bool Is(this SegmentRoleSerialize serialized, SegmentRole role)
    {
        return serialized.HasFlag(role switch
        {
            SegmentRole.Nose => SegmentRoleSerialize.nose,
            SegmentRole.Body => SegmentRoleSerialize.body,
            SegmentRole.Mount => SegmentRoleSerialize.mount,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        });
    }
}

[LibraryLoad("AT_SEGMENT")]
public class SegmentDef : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name;
    [Persistent] protected string displayName;
    [Persistent] public SegmentRoleSerialize role;
    public Asset[] assets;

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        assets = node.LoadAllFromNodes<Asset>().OrderBy(asset => asset.AspectRatio).ToArray();
        if (assets.Length == 0)
        {
            Debug.LogWarning($"segment definition `{name}` contained no assets; adding default");
            assets = [new Asset()];
        }
    }

    public override void Save(ConfigNode node)
    {
        base.Save(node);
        node.WriteAllToNodes(assets);
    }

    public string ItemName() => name;
    public string DisplayName => displayName ?? name;

    public Asset this[int idx] => assets[idx];

    public IEnumerable<Asset> GetAssetsForDiameter(float diameter) =>
        assets.Where(a => a.SupportsDiameter(diameter));

    public Asset GetFirstAssetForDiameter(float diameter) => GetAssetsForDiameter(diameter).First();
}
