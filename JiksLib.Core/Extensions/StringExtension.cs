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
            if (s == "") return Array.Empty<string>();
            else return s.Split(sep);
        }

        /// <summary>
        /// 计算字符串的BKDR哈希值
        /// </summary>
        public static ulong BKDRHash(
            this string s,
            ulong inputHash = 0)
        {
            ulong seed = 131;
            ulong hash = inputHash;

            foreach (var i in s)
                hash = hash * seed + i;

            return hash;
        }

        /// <summary>
        /// 去除指定的字符串头部
        /// </summary>
        /// <param name="x">字符串</param>
        /// <param name="prefix">要去掉的头部</param>
        /// <param name="remain">去掉头部后的剩余部分</param>
        /// <returns>是否成功</returns>
        public static bool StripPrefix(
            this string x,
            string prefix,
            out string? remain)
        {
            if (!x.StartsWith(prefix))
            {
                remain = null;
                return false;
            }

            remain = x.Substring(prefix.Length);
            return true;
        }

        /// <summary>
        /// 去掉指定字符串尾部
        /// </summary>
        public static bool StripSuffix(
            this string x,
            string suffix,
            out string? remain)
        {
            if (!x.EndsWith(suffix))
            {
                remain = null;
                return false;
            }

            remain = x.Substring(0, x.Length - suffix.Length);
            return true;
        }

        /// <summary>
        /// 去掉指定字符串头部和尾部
        /// </summary>
        public static bool StripPrefixSuffix(
            this string x,
            string prefix,
            string suffix,
            out string? remain)
        {
            if (x.StartsWith(prefix) && x.EndsWith(suffix))
            {
                remain = x.Substring(
                    prefix.Length,
                    x.Length - prefix.Length - suffix.Length);

                return true;
            }

            remain = null;
            return false;
        }
    }
}
