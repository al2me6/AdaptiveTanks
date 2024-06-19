using System.Collections;
using System.Collections.Generic;

namespace AdaptiveTanks.Utils;

public static class Itertools
{
    public static IEnumerable<T> WhereIs<T>(this IEnumerable iter)
    {
        foreach (var item in iter)
        {
            if (item is T downcast) yield return downcast;
        }
    }
}