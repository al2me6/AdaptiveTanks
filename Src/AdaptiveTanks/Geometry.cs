using AdaptiveTanks.Extensions;
using UnityEngine;

namespace AdaptiveTanks;

public static class Geometry
{
    public const string RootAnchorName = "__ATRoot";

    public static Transform GetOrCreateRootAnchor(this Part part) =>
        part.transform.Find("model").FindOrCreateChild(RootAnchorName);

    public static Transform GetOrCreateAnchor(this Part part, string name)
    {
        var root = part.GetOrCreateRootAnchor();
        return root.FindOrCreateChild(name);
    }
}

public record SkinAndCore<T>(T Skin, T Core);
