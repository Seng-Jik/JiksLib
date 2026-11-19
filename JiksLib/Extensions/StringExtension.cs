using System;

namespace JiksLib.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// Split，但是为空时返回0个元素的数组
        /// </summary>
        /// <param name="s"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string[] Split0(this string s, char sep = ',')
        {
            s.ThrowIfNull();
            if (s == "") return Array.Empty<string>();
            else return s.Split(sep);
        }

        /// <summary>
        /// 计算字符串的 BKDR 哈希值
        /// 乘子使用 16777619（FNV-1a 素数）：旧实现的 131 小于 char 值域，
        /// 会产生结构性碰撞（例如 "ab" 与 "\u3205"），因此不再使用。
        /// </summary>
        /// <param name="s">要计算哈希的字符串</param>
        /// <param name="inputHash">初始哈希值，便于增量拼接</param>
        public static ulong BKDRHash(
            this string s,
            ulong inputHash = 0)
        {
            const ulong seed = 16777619;
            ulong hash = inputHash;

            unchecked
            {
                foreach (var i in s)
                    hash = hash * seed + i;
            }

            return hash;
        }

        /// <summary>
        /// 去除指定的字符串头部
        /// </summary>
        /// <param name="x">字符串</param>
        /// <param name="prefix">要去掉的头部</param>
        /// <param name="remain">
        /// 去掉头部后的剩余部分；与 <see cref="StringSegment.StripPrefix"/> 一致，失败时为空字符串
        /// （是否成功请以返回值为准）
        /// </param>
        /// <returns>是否成功</returns>
        public static bool StripPrefix(
            this string x,
            string prefix,
            out string remain)
        {
            x.ThrowIfNull();
            prefix.ThrowIfNull(nameof(prefix));

            // 必须与字符串版其它 API（以及 StringSegment 版）保持一致：按序数比较，
            // 否则文化敏感匹配成功但按码元长度裁剪会得到错误结果。
            if (!x.StartsWith(prefix, StringComparison.Ordinal))
            {
                remain = string.Empty;
                return false;
            }

            remain = x.Substring(prefix.Length);
            return true;
        }

        /// <summary>
        /// 去掉指定字符串尾部
        /// </summary>
        /// <param name="x">字符串</param>
        /// <param name="suffix">要去掉的尾部</param>
        /// <param name="remain">
        /// 去掉尾部后的剩余部分；与 <see cref="StringSegment.StripSuffix"/> 一致，失败时为空字符串
        /// （是否成功请以返回值为准）
        /// </param>
        /// <returns>是否成功</returns>
        public static bool StripSuffix(
            this string x,
            string suffix,
            out string remain)
        {
            x.ThrowIfNull();
            suffix.ThrowIfNull(nameof(suffix));

            if (!x.EndsWith(suffix, StringComparison.Ordinal))
            {
                remain = string.Empty;
                return false;
            }

            remain = x.Substring(0, x.Length - suffix.Length);
            return true;
        }

        /// <summary>
        /// 去掉指定字符串头部和尾部
        /// </summary>
        /// <param name="x">字符串</param>
        /// <param name="prefix">要去掉的头部</param>
        /// <param name="suffix">要去掉的尾部</param>
        /// <param name="remain">
        /// 去掉头尾后的剩余部分；与 <see cref="StringSegment.StripPrefixSuffix"/> 一致，
        /// 失败时为空字符串（是否成功请以返回值为准）
        /// </param>
        /// <returns>是否成功</returns>
        public static bool StripPrefixSuffix(
            this string x,
            string prefix,
            string suffix,
            out string remain)
        {
            x.ThrowIfNull();
            prefix.ThrowIfNull(nameof(prefix));
            suffix.ThrowIfNull(nameof(suffix));

            // 前后缀在字符串上重叠时无法同时去掉，直接视为不匹配，
            // 避免 Substring 传入负长度（与 StringSegment 版行为一致）。
            if (prefix.Length + suffix.Length > x.Length)
            {
                remain = string.Empty;
                return false;
            }

            if (x.StartsWith(prefix, StringComparison.Ordinal)
                && x.EndsWith(suffix, StringComparison.Ordinal))
            {
                remain = x.Substring(
                    prefix.Length,
                    x.Length - prefix.Length - suffix.Length);

                return true;
            }

            remain = string.Empty;
            return false;
        }
    }
}
