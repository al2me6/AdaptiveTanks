using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdaptiveTanks;

#nullable enable

/// <summary>
/// Register the annotated class to be loaded by the <see cref="Library"/>.
/// The class must implement <see cref="ILibraryLoad"/>.
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

        foreach (var load in libraryTypes.OrderBy(load => load.attr.LoadOrder))
        {
            typeof(Library<>)
                .MakeGenericType(load.t)
                .GetMethod("Load")?
                .Invoke(null, [load.attr.NodeName]);
        }
    }
}

public static class Library<T> where T : class, ILibraryLoad, new()
{
    public static IReadOnlyDictionary<string, T> Items { get; private set; } = null!;

    public static T Get(string name)
    {
        if (Items.TryGetValue(name, out var item)) return item;
        throw new KeyNotFoundException($"Library<{typeof(T).Name}>: key `{name}` not found");
    }

    public static T? MaybeGet(string? name) => name != null ? Get(name) : null;

    public static IEnumerable<T> GetAll(IEnumerable<string> names) => names.Select(n => Items[n]);

    public static IEnumerable<string> ItemNames => Items.Keys;

    public static bool Contains(string name) => Items.ContainsKey(name);

    public static void Load(string nodeName)
    {
        var items = new Dictionary<string, T>();

        foreach (var node in GameDatabase.Instance.GetConfigNodes(nodeName))
        {
            var item = new T();
            item.Load(node);
            var name = item.ItemName();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"Library<{typeof(T).Name}>: item must have non-empty name");
                continue;
            }

            items[name] = item;
        }

        Items = items;
        Debug.Log($"Library<{typeof(T).Name}>: loaded {Items.Count} items", true);
    }
}
