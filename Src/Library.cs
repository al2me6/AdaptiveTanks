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
    private static readonly Dictionary<string, T> items = new();
    public static T Get(string name) => items[name];

    public static void Load()
    {
        foreach (var item in GameDatabase.Instance.LoadAllFromNodes<T>())
        {
            items[item.Name()] = item;
        }

        Debug.Log($"LIBRARY: loaded {items.Count} `{typeof(T).Name}`s", true);
    }
}
