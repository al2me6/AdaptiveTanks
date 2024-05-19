using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public class StyleDef : IRepeatedConfigNode
{
    public string ConfigNodeName() => "AT_STYLE_DEF";
    public const string BodyNodeName = "BODY";
    public const string CoreNodeName = "CORE";

    [Persistent] public string name;
    public List<string> bodies;
    public List<string> cores;

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        bodies = node.LoadAllNamesFromNodes(BodyNodeName).ToList();
        cores = node.LoadAllNamesFromNodes(CoreNodeName).ToList();

        Debug.Log($"style {name}");
        Debug.Log($"bodies {string.Join(", ", bodies)}");
        Debug.Log($"cores {string.Join(", ", cores)}");
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllNamesToNodes(BodyNodeName, bodies);
        node.WriteAllNamesToNodes(CoreNodeName, cores);
    }
}
