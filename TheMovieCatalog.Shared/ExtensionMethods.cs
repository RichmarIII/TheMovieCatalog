using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TheMovieCatalog.Shared.ExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return str == null || str.Length == 0;
        }
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || enumerable.Count() == 0;
        }

        public static void AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, V value) where K : notnull
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        public static byte[] ReadAllBytes(this Stream inStream)
        {
            if (inStream is MemoryStream)
                return ((MemoryStream)inStream).ToArray();

            using (var memoryStream = new MemoryStream())
            {
                inStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static bool IsApproximatelyEqual(this float a, float b, float tolorance = 1E-7f)
        {
            return Math.Abs(a - b) < tolorance;
        }

        public static bool IsApproximatelyEqual(this double a, double b, double tolorance = 1E-15)
        {
            return Math.Abs(a - b) < tolorance;
        }
    }
}
