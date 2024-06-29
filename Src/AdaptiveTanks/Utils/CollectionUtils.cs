using System.Collections.Generic;

namespace AdaptiveTanks.Utils;

public static class CollectionUtils
{
    public static V GetOrCreateValue<K, V>(this Dictionary<K, V> dict, K key) where V : new()
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = new V();
            dict[key] = value;
        }

        return value;
    }

    public static bool TryPop<T>(this List<T> list, out T item)
    {
        var lastIdx = list.Count - 1;

        if (lastIdx < 0)
        {
            item = default;
            return false;
        }

        item = list[lastIdx];
        list.RemoveAt(lastIdx);
        return true;
    }
}
