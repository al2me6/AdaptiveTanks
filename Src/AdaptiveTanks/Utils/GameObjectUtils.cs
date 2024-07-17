using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class GameObjectUtils
{
    public static void SetLayerExceptTriggers(this GameObject go, int layer) =>
        go.SetLayerRecursive(layer, true, 1 << (int)UnityLayer.PartTriggers);

    public static void SetLayerExceptTriggers(this GameObject go, UnityLayer layer) =>
        SetLayerExceptTriggers(go, (int)layer);
}