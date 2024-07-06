using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public abstract class StyleDef : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name;
    [Persistent] public string displayName;

    public IReadOnlyDictionary<SegmentRole, List<SegmentDef>> SegmentsByRole { get; private set; }

    public override void Load(ConfigNode node)
    {
        base.Load(node);
        var segments = LoadSegments(node).ToList();

        var positionalRoles = Enum.GetValues(typeof(SegmentRole));
        SegmentsByRole = positionalRoles
            .Cast<SegmentRole>()
            .ToDictionary(role => role, _ => new List<SegmentDef>());
        foreach (var segmentDef in Library<SegmentDef>.GetAll(segments))
        {
            foreach (var positionalRole in positionalRoles.Cast<SegmentRole>())
            {
                if (segmentDef.Supports(positionalRole))
                    SegmentsByRole[positionalRole].Add(segmentDef);
            }
        }

        Debug.Log($"style {name}");
        Debug.Log($"\tsegments {string.Join(", ", segments)}");
    }

    protected virtual IEnumerable<string> LoadSegments(ConfigNode node) =>
        node.LoadAllNamesFromNodes("Segment");

    public string ItemName() => name;
    public string DisplayName => displayName ?? name;

    public bool SupportsIntertank => SegmentsByRole[SegmentRole.Intertank].Count > 0;
}

[LibraryLoad("AT_SKIN_STYLE", loadOrder: 2)]
public class StyleDefSkin : StyleDef
{
    public string[] allowCoreStyles;
    public string[] blockCoreStyles;

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
