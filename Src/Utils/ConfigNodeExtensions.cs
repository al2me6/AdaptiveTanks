using System.Collections.Generic;

namespace AdaptiveTanks.Extensions;

public interface IRepeatedConfigNode : IConfigNode
{
    public string ConfigNodeName();
}

public static class ConfigNodeUtils
{
    public static IEnumerable<T> LoadAllFromNodes<T>(this GameDatabase db)
        where T : IRepeatedConfigNode, new()
    {
        var nodeName = new T().ConfigNodeName();
        foreach (var node in db.GetConfigNodes(nodeName))
        {
            T item = new();
            item.Load(node);
            yield return item;
        }
    }

    public static IEnumerable<T> LoadAllFromNodes<T>(this ConfigNode node)
        where T : IRepeatedConfigNode, new()
    {
        var nodeName = new T().ConfigNodeName();
        for (var i = 0; i < node.CountNodes; ++i)
        {
            if (node.nodes[i].name != nodeName) continue;
            T item = new();
            item.Load(node.nodes[i]);
            yield return item;
        }
    }

    public static void WriteAllToNodes<T>(this ConfigNode node, IEnumerable<T> items)
        where T : IRepeatedConfigNode
    {
        foreach (var item in items)
            node.AddNode(item.ConfigNodeName(), ConfigNode.CreateConfigFromObject(item));
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