using ROUtils;
using UnityEngine;

namespace AdaptiveTanks;

public class TransformUtils
{
    public static Transform GetOrCreateAnchorTransform(Part part, string name)
    {
        if (part.transform.FindDeepChild(name) is Transform existingAnchor) return existingAnchor;
        var newAnchor = new GameObject(name).transform;
        newAnchor.NestToParent(part.transform.FindDeepChild("model"));
        return newAnchor;
    }
}