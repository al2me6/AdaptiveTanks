using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public abstract class StyleDefStock : INamedConfigNode
{
    public const string SegmentNodeName = "SEGMENT";

    [Persistent] public string name;
    protected string[] segments;

    public string Name() => name;
    public SegmentDef[] Noses { get; private set; }
    public SegmentDef[] Bodies { get; private set; }
    public SegmentDef[] Mounts { get; private set; }

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        segments = node.LoadAllNamesFromNodes("SEGMENT").ToArray();

        var segmentDefs = segments.Select(Library<SegmentDef>.Get).ToArray();
        Noses = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Nose)).ToArray();
        Bodies = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Body)).ToArray();
        Mounts = segmentDefs.Where(seg => seg.role.Is(SegmentRole.Mount)).ToArray();

        Debug.Log($"style {name}");
        Debug.Log($"segments {string.Join(", ", segments)}");
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllNamesToNodes(SegmentNodeName, segments);
    }
}

[LibraryLoad]
public class StyleDefStockSkin : StyleDefStock, IRepeatedConfigNode
{
    public string ConfigNodeName() => "AT_SKIN_STYLE_STOCK";
}

[LibraryLoad]
public class StyleDefStockCore : StyleDefStock, IRepeatedConfigNode
{
    public string ConfigNodeName() => "AT_CORE_STYLE_STOCK";
}
