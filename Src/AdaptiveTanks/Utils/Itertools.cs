using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> iter) where T : class =>
        iter.Where(item => item != null);
}
