using System.Collections.Generic;

namespace AdaptiveTanks.Extensions;

public static class ConfigNodeUtils
{
    public static IEnumerable<T> LoadAllFromNodes<T>(this ConfigNode node)
        where T : IConfigNode, new()
    {
        var nodeName = typeof(T).Name;
        for (var i = 0; i < node.CountNodes; ++i)
        {
            if (node.nodes[i].name != nodeName) continue;
            T item = new();
            item.Load(node.nodes[i]);
            yield return item;
        }
    }

    public static void WriteAllToNodes<T>(this ConfigNode node, IEnumerable<T> items)
        where T : IConfigNode
    {
        var nodeName = typeof(T).Name;
        foreach (var item in items)
        {
            var child = new ConfigNode();
            item.Save(child);
            node.AddNode(nodeName, child);
        }
    }

    public static IEnumerable<string> LoadAllNamesFromNodes(this ConfigNode node, string nodeName)
    {
        for (var i = 0; i < node.CountNodes; ++i)
        {
            var child = node.nodes[i];
            if (child.name != nodeName || !child.HasValue("name")) continue;
            yield return child.GetValue("name");
        }
    }

    public static void WriteAllNamesToNodes(this ConfigNode node, string nodeName,
        IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            ConfigNode child = new();
            child.AddValue("name", name);
            node.AddNode(nodeName, child);
        }
    }
}
