using System.Collections.Generic;

namespace AdaptiveTanks.Utils;

public static class ConfigNodeUtils
{
    public static IEnumerable<T> LoadAllFromNodes<T>(this ConfigNode node, string? nodeName = null)
        where T : IConfigNode, new()
    {
        if (string.IsNullOrEmpty(nodeName)) nodeName = typeof(T).Name;
        foreach (ConfigNode child in node.nodes)
        {
            if (child.name != nodeName) continue;
            T item = new();
            item.Load(child);
            yield return item;
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
}
