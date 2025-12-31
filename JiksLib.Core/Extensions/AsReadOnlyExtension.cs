#nullable enable

using System.Collections.Generic;

namespace JiksLib.Extensions
{
    /// <summary>
    /// 转换各种集合为只读的
    /// </summary>
    public static class AsReadOnlyExtension
    {
        public static IReadOnlyDictionary<T, U> AsReadOnly<T, U>(
            this Dictionary<T, U> d) where T : notnull => d;

        public static IReadOnlyList<T> AsReadOnly<T>(this List<T> ls) => ls;
        public static IReadOnlyList<T> AsReadOnly<T>(this T[] ls) => ls;
        public static IReadOnlyCollection<T> AsReadOnly<T>(this HashSet<T> s) => s;
        public static IReadOnlyCollection<T> AsReadOnly<T>(this LinkedList<T> s) => s;
    }
}
