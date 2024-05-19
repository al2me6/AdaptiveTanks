using UnityEngine;

namespace AdaptiveTanks;

public static class TransformUtils
{
    public static Transform FindOrCreateChild(this Transform parent, string name)
    {
        if (parent.Find(name) is Transform child) return child;
        child = new GameObject(name).transform;
        child.NestToParent(parent);
        return child;
    }
}
