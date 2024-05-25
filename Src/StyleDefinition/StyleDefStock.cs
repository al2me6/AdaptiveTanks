using System;
using System.Linq;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public abstract class StyleDefStock : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name;
    [Persistent] public string displayName;
    [Persistent] protected PersistentListValueType<string> Segments;

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        var segmentDefs = Segments.Select(Library<SegmentDef>.Get).ToArray();
        Noses = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Nose)).ToArray();
        Bodies = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Body)).ToArray();
        Mounts = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Mount)).ToArray();

        Debug.Log($"style {name}");
        Debug.Log($"segments {string.Join(", ", Segments)}");
    }

    public string ItemName() => name;

    public string DisplayName => displayName ?? name;
    public SegmentDef[] Noses { get; private set; }
    public SegmentDef[] Bodies { get; private set; }
    public SegmentDef[] Mounts { get; private set; }

    public SegmentDef[] GetAvailableSegments(SegmentRole role)
    {
        return role switch
        {
            SegmentRole.Nose => Noses,
            SegmentRole.Body => Bodies,
            SegmentRole.Mount => Mounts,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }
}

[LibraryLoad("AT_SKIN_STYLE_STOCK", loadOrder: 1)]
public class StyleDefStockSkin : StyleDefStock;

[LibraryLoad("AT_CORE_STYLE_STOCK", loadOrder: 1)]
public class StyleDefStockCore : StyleDefStock;
