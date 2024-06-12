using System;
using System.Collections.Generic;
using System.Linq;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public abstract class StyleDef : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name;
    [Persistent] public string displayName;
    [Persistent] protected PersistentListValueType<string> Segments;

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        var positionalRoles = Enum.GetValues(typeof(SegmentRole));
        SegmentsByRole = positionalRoles
            .Cast<SegmentRole>()
            .ToDictionary(role => role, _ => new List<SegmentDef>());
        foreach (var segmentDef in Library<SegmentDef>.GetAll(Segments))
        {
            foreach (var positionalRole in positionalRoles.Cast<SegmentRole>())
            {
                if (segmentDef.Supports(positionalRole))
                    SegmentsByRole[positionalRole].Add(segmentDef);
            }
        }

        Debug.Log($"style {name}");
        Debug.Log($"segments {string.Join(", ", Segments)}");
    }

    public string ItemName() => name;
    public string DisplayName => displayName ?? name;

    public IReadOnlyDictionary<SegmentRole, List<SegmentDef>> SegmentsByRole { get; private set; }
}

[LibraryLoad("AT_SKIN_STYLE", loadOrder: 1)]
public class StyleDefSkin : StyleDef;

[LibraryLoad("AT_CORE_STYLE", loadOrder: 1)]
public class StyleDefCore : StyleDef
{
    public override void Load(ConfigNode node)
    {
        base.Load(node);

        for (var i = 0; i < Segments.Count; ++i)
        {
            var segment = Library<SegmentDef>.Get(Segments[i]);
            if (segment.IsAccessory)
            {
                Debug.LogWarning($"core style {name} may not contain accessories");
                Segments.RemoveAt(i);
            }
        }
    }
}
