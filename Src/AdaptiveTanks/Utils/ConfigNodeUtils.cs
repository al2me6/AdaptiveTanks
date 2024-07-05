using System.Collections.Generic;

namespace AdaptiveTanks.Utils;

public static class ConfigNodeUtils
{
    public static IEnumerable<T> LoadAllFromNodes<T>(this ConfigNode node)
        where T : IConfigNode, new()
    {
        var nodeName = typeof(T).Name;
        foreach (ConfigNode child in node.nodes)
        {
            if (child.name != nodeName) continue;
            T item = new();
            item.Load(child);
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
        foreach (ConfigNode child in node.nodes)
        {
            if (child.name != nodeName) continue;

            var name = child.GetValue("name");
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"`{nodeName}` declaration must have non-empty name");
                continue;
            }

            yield return name;
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
