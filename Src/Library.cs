using System;
using System.Collections.Generic;
using System.Reflection;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class LibraryLoadAttribute : Attribute;

public static class LibraryLoader
{
    public static void ModuleManagerPostLoad()
    {
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (Attribute.GetCustomAttribute(t, typeof(LibraryLoadAttribute)) == null) continue;
            typeof(Library<>)
                .MakeGenericType([t])
                .GetMethod("Load")?
                .Invoke(null, null);
        }
    }
}

public static class Library<T> where T : IRepeatedConfigNode, INamedConfigNode, new()
{
    public static IReadOnlyDictionary<string, T> Items { get; private set; }
    public static T Get(string name) => Items[name];
    public static bool HasItem(string name) => Items.ContainsKey(name);

    public static void Load()
    {
        var items = new Dictionary<string, T>();
        foreach (var item in GameDatabase.Instance.LoadAllFromNodes<T>())
        {
            items[item.Name()] = item;
        }

        Items = items;
        Debug.Log($"LIBRARY: loaded {Items.Count} `{typeof(T).Name}`s", true);
    }
}
