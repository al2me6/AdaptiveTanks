using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;
using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks;

[Flags]
public enum SegmentRoleCfg : byte
{
    tank = 1 << 0,
    intertank = 1 << 1,
    tankCapInternal = 1 << 2,
    tankCapTerminal = 1 << 3,
    accessory = 1 << 4
}

[Flags]
public enum CapPositionCfg : byte
{
    top = 1 << 0,
    bottom = 1 << 1,
    either = top | bottom
}

[Flags]
public enum SegmentAlignmentCfg : byte
{
    pinBothEnds = 1 << 0,
    pinInteriorEnd = 1 << 1
}

[LibraryLoad("AT_SEGMENT")]
public class SegmentDef : ConfigNodePersistenceBase, ILibraryLoad
{
    #region fields

    [Persistent] public string name;
    [Persistent] protected string displayName;
    [Persistent] public SegmentRoleCfg role = SegmentRoleCfg.tank;
    [Persistent] public CapPositionCfg capPosition = CapPositionCfg.either;
    [Persistent] public SegmentAlignmentCfg align = SegmentAlignmentCfg.pinBothEnds;
    [Persistent] public bool useStrictAlignment = false;
    [Persistent] public float strictAlignmentBias = 0.5f;

    public Asset[] assets;

    #endregion

    #region constants

    // The following segments must be defined in configs:

    public const string EmptyAccessoryName = "_AT_empty_accessory";

    #endregion

    #region deserialization

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        if (role.HasFlag(SegmentRoleCfg.accessory) && role != SegmentRoleCfg.accessory)
        {
            Debug.LogWarning($"accessory segment `{name}` may not declare other roles");
            role = SegmentRoleCfg.accessory;
        }

        if (role == SegmentRoleCfg.accessory)
        {
            align = SegmentAlignmentCfg.pinInteriorEnd;
        }

        if (align.HasFlag(SegmentAlignmentCfg.pinInteriorEnd)
            && role != SegmentRoleCfg.accessory
            && !role.HasFlag(SegmentRoleCfg.tankCapTerminal))
        {
            Debug.LogWarning($"non-terminal segment `{name}` may not be aligned `pinInteriorEnd`");
            align = SegmentAlignmentCfg.pinBothEnds;
        }

        if (strictAlignmentBias is < 0f or > 1f)
        {
            Debug.LogWarning($"segment `{name}`: invalid alignment bias {strictAlignmentBias}");
            strictAlignmentBias = Mathf.Clamp01(strictAlignmentBias);
        }

        assets = node.LoadAllFromNodes<Asset>().OrderBy(asset => asset.AspectRatio).ToArray();
        if (assets.Length == 0)
        {
            Debug.LogWarning($"segment `{name}` contained no assets; adding default");
            assets = [new Asset()];
        }

        foreach (var asset in assets) asset.Segment = this;
    }

    public string ItemName() => name;

    #endregion

    #region queries

    public string DisplayName => displayName ?? name;

    public bool IsAccessory => role == SegmentRoleCfg.accessory;
    public bool IsFueled => !IsAccessory;

    public bool Supports(SegmentRole targetRole) => targetRole switch
    {
        SegmentRole.Tank =>
            role.HasFlag(SegmentRoleCfg.tank),
        SegmentRole.TerminatorTop =>
            (role.HasFlag(SegmentRoleCfg.tankCapTerminal)
             || role.HasFlag(SegmentRoleCfg.accessory))
            && capPosition.HasFlag(CapPositionCfg.top),
        SegmentRole.TerminatorBottom =>
            (role.HasFlag(SegmentRoleCfg.tankCapTerminal)
             || role.HasFlag(SegmentRoleCfg.accessory))
            && capPosition.HasFlag(CapPositionCfg.bottom),
        SegmentRole.Intertank =>
            role.HasFlag(SegmentRoleCfg.intertank),
        SegmentRole.TankCapInternalTop =>
            role.HasFlag(SegmentRoleCfg.tankCapInternal)
            && capPosition.HasFlag(CapPositionCfg.top),
        SegmentRole.TankCapInternalBottom =>
            role.HasFlag(SegmentRoleCfg.tankCapInternal)
            && capPosition.HasFlag(CapPositionCfg.bottom),
        _ => throw new ArgumentOutOfRangeException(nameof(targetRole))
    };

    public bool CanToggleAlignment =>
        align == (SegmentAlignmentCfg.pinBothEnds | SegmentAlignmentCfg.pinInteriorEnd);

    #endregion

    #region getters

    public SegmentAlignment? TryGetOnlyAlignment() => align switch
    {
        SegmentAlignmentCfg.pinBothEnds => SegmentAlignment.PinBothEnds,
        SegmentAlignmentCfg.pinInteriorEnd => SegmentAlignment.PinInteriorEnd,
        _ => null
    };

    public IEnumerable<Asset> GetAllAssetsFor(float diameter) =>
        assets.Where(a => a.SupportsDiameter(diameter));

    public Asset GetFirstAssetFor(float diameter) => GetAllAssetsFor(diameter).First();

    public Asset GetBestAssetFor(float diameter, float targetAspect)
    {
        Asset best = null;
        var bestDeviation = float.PositiveInfinity;
        foreach (var candidate in GetAllAssetsFor(diameter))
        {
            var candidateDeviation = Mathf.Abs(candidate.AspectRatio - targetAspect);
            if (candidateDeviation > bestDeviation) continue;
            best = candidate;
            bestDeviation = candidateDeviation;
        }

        return best;
    }

    #endregion
}
