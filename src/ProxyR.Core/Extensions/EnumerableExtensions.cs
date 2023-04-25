using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ProxyR.Core.Extensions
{
    /// <summary>
    /// This static class provides extension methods for the IEnumerable interface.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Splits an IEnumerable into multiple IEnumerables of a given size.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source IEnumerable.</typeparam>
        /// <param name="source">The source IEnumerable.</param>
        /// <param name="size">The size of the resulting IEnumerables.</param>
        /// <returns>An array of IEnumerables of the given size.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int size)
                 => source.Select((x, i) => new { Index = i, Value = x })
                          .GroupBy(x => x.Index / size)
                          .Select(x => x.Select(v => v.Value).ToArray())
                          .ToArray();
    }

}
