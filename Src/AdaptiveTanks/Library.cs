using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdaptiveTanks;

/// <summary>
/// Register the annotated type to be loaded by the <see cref="Library"/>.
/// The type must implement <see cref="ILibraryLoad"/>.
/// </summary>
/// <param name="nodeName">The name of the top-level nodes to parse.</param>
/// <param name="loadOrder">Override the loading order, e.g. if Library&lt;U> needs to read
/// Library&lt;T> when loading. The ordering within the same pass number is unspecified.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class LibraryLoadAttribute(string nodeName, int loadOrder = 0) : Attribute
{
    public readonly string NodeName = nodeName;
    public readonly int LoadOrder = loadOrder;
};

public interface ILibraryLoad : IConfigNode
{
    public string ItemName();
}

public static class LibraryLoader
{
    public static void ModuleManagerPostLoad()
    {
        List<(Type t, LibraryLoadAttribute attr)> libraryTypes = new();
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attr = Attribute.GetCustomAttribute(t, typeof(LibraryLoadAttribute));
            if (attr is LibraryLoadAttribute loadAttr) libraryTypes.Add((t, loadAttr));
        }

        foreach (var load in libraryTypes.OrderBy(ta => ta.attr.LoadOrder))
        {
            typeof(Library<>)
                .MakeGenericType([load.t])
                .GetMethod("Load")?
                .Invoke(null, [load.attr.NodeName]);
        }
    }
}

public static class Library<T> where T : ILibraryLoad, new()
{
    public static IReadOnlyDictionary<string, T> Items { get; private set; }
    public static T Get(string name) => Items[name];

    public static void Load(string nodeName)
    {
        var items = new Dictionary<string, T>();

        foreach (var node in GameDatabase.Instance.GetConfigNodes(nodeName))
        {
            var item = new T();
            item.Load(node);
            items[item.ItemName()] = item;
        }

        Items = items;
        Debug.Log($"LIBRARY: loaded {Items.Count} `{typeof(T).Name}`s", true);
    }
}
