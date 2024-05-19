using UnityEngine;

namespace AdaptiveTanks;

public class Geometry
{
    public const string RootAnchorName = "__ATRoot";

    public static Transform GetOrCreateRootAnchor(Part part) =>
        part.transform.Find("model").FindOrCreateChild(RootAnchorName);

    public static Transform GetOrCreateAnchor(Part part, string name)
    {
        var root = GetOrCreateRootAnchor(part);
        return root.FindOrCreateChild(name);
    }
}
