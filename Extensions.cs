using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace Random_Features.Libs
{
    public static class Extensions
    {
        public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum
        {
            return Enum.GetValues<T>().Where(x => value.HasFlag(x));
        }

        public static Vector2 XY(this Vector3 v3) => new Vector2(v3.X, v3.Y);

        public static IEnumerator GetEnumerator(this IEnumerator enumerator) => enumerator;

        public static IEnumerator Drill(this IEnumerator enumerator)
        {
            foreach (var item in enumerator)
            {
                if (item is IEnumerator innerEnumerator)
                {
                    foreach (var innerItem in Drill(innerEnumerator))
                    {
                        yield return innerItem;
                    }
                }
                else
                {
                    yield return item;
                }
            }
        }
    }
}
