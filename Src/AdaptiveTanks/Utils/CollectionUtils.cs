using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace AdaptiveTanks.Utils;

public interface IItemName
{
    public string ItemName();
}

public class NamedCollection<T> : KeyedCollection<string, T> where T : IItemName
{
    protected override string GetKeyForItem(T item) => item.ItemName();
}

public static class CollectionUtils
{
    public static bool IsEmpty<T>(this IReadOnlyCollection<T> coll) => coll.Count == 0;

    public static V GetOrCreateValue<K, V>(this Dictionary<K, V> dict, K key) where V : new()
    {
        if (dict.TryGetValue(key, out var value)) return value;
        value = new V();
        dict[key] = value;
        return value;
    }

    public static bool TryPop<T>(this List<T> list, [NotNullWhen(true)] out T? item)
    {
        var lastIdx = list.Count - 1;

        if (lastIdx < 0)
        {
            item = default;
            return false;
        }

        item = list[lastIdx]!;
        list.RemoveAt(lastIdx);
        return true;
    }
}
