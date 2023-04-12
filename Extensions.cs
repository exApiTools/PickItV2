using System;
using System.Collections.Generic;
using System.Linq;

namespace PickIt;

public static class Extensions
{
    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum
    {
        return Enum.GetValues<T>().Where(x => value.HasFlag(x));
    }
}