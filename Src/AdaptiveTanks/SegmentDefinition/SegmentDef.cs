using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

[Flags]
public enum SegmentRoleSer : byte
{
    tank = 1 << 0,
    intertank = 1 << 1,
    tankCapInternal = 1 << 2,
    tankCapTerminal = 1 << 3,
    accessory = 1 << 4
}

[Flags]
public enum capPositionSer : byte
{
    top = 1 << 0,
    bottom = 1 << 1,
    either = top | bottom
}

[Flags]
public enum SegmentAlignmentSer : byte
{
    pinBothEnds = 1 << 0,
    pinInteriorEnd = 1 << 1
}

[LibraryLoad("AT_SEGMENT")]
public class SegmentDef : ConfigNodePersistenceBase, ILibraryLoad, ILibraryLoadModify<SegmentDef>
{
    [Persistent] public string name;
    [Persistent] protected string displayName;
    [Persistent] public SegmentRoleSer role = SegmentRoleSer.tank;
    [Persistent] public capPositionSer capPosition = capPositionSer.either;
    [Persistent] public SegmentAlignmentSer align = SegmentAlignmentSer.pinBothEnds;

    public Asset[] assets;

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        if (role.HasFlag(SegmentRoleSer.accessory) && role != SegmentRoleSer.accessory)
        {
            Debug.LogWarning($"accessory segment `{name}` may not declare other roles");
            role = SegmentRoleSer.accessory;
        }

        if (role == SegmentRoleSer.accessory)
        {
            align = SegmentAlignmentSer.pinInteriorEnd;
        }

        if (align.HasFlag(SegmentAlignmentSer.pinInteriorEnd)
            && role != SegmentRoleSer.accessory
            && !role.HasFlag(SegmentRoleSer.tankCapTerminal))
        {
            Debug.LogWarning($"non-terminal segment `{name}` may not be aligned `pinInteriorEnd`");
            align = SegmentAlignmentSer.pinBothEnds;
        }

        assets = node.LoadAllFromNodes<Asset>().OrderBy(asset => asset.AspectRatio).ToArray();
        if (assets.Length == 0)
        {
            Debug.LogWarning($"segment `{name}` contained no assets; adding default");
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

    public bool Supports(SegmentRole targetRole) => targetRole switch
    {
        SegmentRole.Tank =>
            role.HasFlag(SegmentRoleSer.tank),
        SegmentRole.TerminatorTop =>
            (role.HasFlag(SegmentRoleSer.tankCapTerminal)
             || role.HasFlag(SegmentRoleSer.accessory))
            && capPosition.HasFlag(capPositionSer.top),
        SegmentRole.TerminatorBottom =>
            (role.HasFlag(SegmentRoleSer.tankCapTerminal)
             || role.HasFlag(SegmentRoleSer.accessory))
            && capPosition.HasFlag(capPositionSer.bottom),
        SegmentRole.Intertank =>
            role.HasFlag(SegmentRoleSer.intertank),
        SegmentRole.TankCapInternalTop =>
            role.HasFlag(SegmentRoleSer.tankCapInternal)
            && capPosition.HasFlag(capPositionSer.top),
        SegmentRole.TankCapInternalBottom =>
            role.HasFlag(SegmentRoleSer.tankCapInternal)
            && capPosition.HasFlag(capPositionSer.bottom),
        _ => throw new ArgumentOutOfRangeException(nameof(targetRole))
    };

    public bool IsAccessory => role == SegmentRoleSer.accessory;
    public bool IsFueled => !IsAccessory;

    public bool CanToggleAlignment =>
        align == (SegmentAlignmentSer.pinBothEnds | SegmentAlignmentSer.pinInteriorEnd);

    public SegmentAlignment? TryGetOnlyAlignment() => align switch
    {
        SegmentAlignmentSer.pinBothEnds => SegmentAlignment.PinBothEnds,
        SegmentAlignmentSer.pinInteriorEnd => SegmentAlignment.PinInteriorEnd,
        _ => null
    };

    public IEnumerable<Asset> GetAssetsForDiameter(float diameter) =>
        assets.Where(a => a.SupportsDiameter(diameter));

    public Asset GetFirstAssetForDiameter(float diameter) => GetAssetsForDiameter(diameter).First();

    public static readonly SegmentDef CoreAccessorySurrogate = new()
    {
        name = "__ATCoreAccessorySurrogate",
        displayName = "Core Accessory (Placeholder)",
        role = SegmentRoleSer.accessory,
        align = SegmentAlignmentSer.pinInteriorEnd,
        assets = [new Asset { nativeHeight = 0f }]
    };

    public void PostLoadModify(ref Dictionary<string, SegmentDef> items)
    {
        items.Add(CoreAccessorySurrogate.name, CoreAccessorySurrogate);
    }
}
