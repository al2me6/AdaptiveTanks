using System.Collections.Generic;
using AdaptiveTanks.ConfigNodeExtensions;

namespace AdaptiveTanks;

public static class Library
{
    public static readonly Dictionary<string, SegmentDef> segments = new();
    public static readonly Dictionary<string, StyleDef> styles = new();

    public static void ModuleManagerPostLoad()
    {
        foreach (var def in GameDatabase.Instance.LoadAllFromNodes<SegmentDef>())
        {
            segments[def.name] = def;
        }

        foreach (var def in GameDatabase.Instance.LoadAllFromNodes<StyleDef>())
        {
            styles[def.name] = def;
        }

        Debug.Log($"loaded {segments.Count} segments", true);
        Debug.Log($"loaded {styles.Count} styles", true);
    }
}
