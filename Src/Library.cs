using System.Collections.Generic;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public static class Library
{
    public static readonly Dictionary<string, SegmentDef> Segments = new();
    public static readonly Dictionary<string, StyleDef> Styles = new();

    public static void ModuleManagerPostLoad()
    {
        foreach (var def in GameDatabase.Instance.LoadAllFromNodes<SegmentDef>())
        {
            Segments[def.name] = def;
        }

        foreach (var def in GameDatabase.Instance.LoadAllFromNodes<StyleDef>())
        {
            Styles[def.name] = def;
        }

        Debug.Log($"loaded {Segments.Count} segments", true);
        Debug.Log($"loaded {Styles.Count} styles", true);
    }
}
