﻿using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public abstract class StyleDef : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name = null!;
    [Persistent] protected string? displayName = null;

    public Dictionary<SegmentRole, List<SegmentDef>> Segments { get; private set; } = null!;
    public List<(string Id, string DisplayName)> LinkedMaterials { get; } = [];

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        var segments = Library<SegmentDef>.GetAll(LoadSegments(node)).ToList();

        var roles = Enum.GetValues(typeof(SegmentRole));
        Segments = roles
            .Cast<SegmentRole>()
            .ToDictionary(role => role, _ => new List<SegmentDef>());
        foreach (var segment in segments)
        {
            foreach (var role in roles.Cast<SegmentRole>())
            {
                if (segment.Supports(role)) Segments[role].Add(segment);
            }
        }

        Debug.Log($"style {name}");
        Debug.Log($"\tsegments {string.Join(", ", segments)}");

        HashSet<string>? commonLinkIds = null;
        foreach (var segment in segments)
        {
            foreach (var asset in segment.assets)
            {
                var assetMaterials = asset.materials.Select(mat => mat.LinkId);
                if (commonLinkIds == null) commonLinkIds = [..assetMaterials];
                else commonLinkIds.IntersectWith(assetMaterials);
            }
        }

        foreach (var material in segments[0].assets[0].materials)
        {
            if (!commonLinkIds!.Contains(material.LinkId)) continue;
            LinkedMaterials.Add((material.LinkId, material.Def.DisplayName));
        }
    }

    protected virtual IEnumerable<string> LoadSegments(ConfigNode node) =>
        node.LoadAllNamesFromNodes("Segment");

    public string ItemName() => name;
    public string DisplayName => displayName ?? name;

    public bool SupportsIntertank => Segments[SegmentRole.Intertank].Count > 0;
}

[LibraryLoad("AT_SKIN_STYLE", loadOrder: 2)]
public class StyleDefSkin : StyleDef
{
    public string[] allowCoreStyles = null!;
    public string[] blockCoreStyles = null!;

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        allowCoreStyles = node.LoadAllNamesFromNodes("AllowCore").ToArray();

        var block = node.LoadAllNamesFromNodes("BlockCore").ToArray();
        if (block.Length > 0 && allowCoreStyles.Length > 0)
        {
            Debug.LogWarning($"skin style `{name}` cannot declare both allow and blocklists");
            block = [];
        }

        blockCoreStyles = block;

        ValidateFilterList(allowCoreStyles, "allow");
        ValidateFilterList(blockCoreStyles, "block");
    }

    private void ValidateFilterList(string[] cores, string desc)
    {
        foreach (var core in cores)
        {
            if (!Library<StyleDefCore>.Contains(core))
            {
                Debug.LogWarning($"skin style `{name}`: {desc}list entry `{core}` does not exist");
            }
        }
    }

    public string[] GetAllowedCores(IEnumerable<string> styles)
    {
        if (allowCoreStyles.Length > 0)
            return styles.Where(s => allowCoreStyles.Contains(s)).ToArray();
        if (blockCoreStyles.Length > 0)
            return styles.Where(s => !blockCoreStyles.Contains(s)).ToArray();
        return styles.ToArray();
    }
}

[LibraryLoad("AT_CORE_STYLE", loadOrder: 1)]
public class StyleDefCore : StyleDef
{
    protected override IEnumerable<string> LoadSegments(ConfigNode node)
    {
        foreach (var segmentName in base.LoadSegments(node))
        {
            var segment = Library<SegmentDef>.Get(segmentName);
            if (segment.IsAccessory)
            {
                Debug.LogWarning($"core style `{name}` may not contain accessories");
                continue;
            }

            yield return segmentName;
        }
    }
}
