using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Extensions
{
    public static class LinqExtension
    {
        /// <summary>
        /// 序列是否为空
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="e">要判断的序列</param>
        /// <returns>是否为空</returns>
        public static bool IsEmpty<T>(this IEnumerable<T> e)
        {
            foreach (var _ in e) return false;
            return true;
        }

        /// <summary>
        /// 从序列中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">序列</param>
        /// <param name="randomNumber">随机数，范围为[0, 1]</param>
        /// <param name="getWeight">获得元素权重的委托</param>
        /// <returns></returns>
        public static T RandomSelect<T>(
            this IEnumerable<T> ls,
            float randomNumber,
            Func<T, float> getWeight)
        {
            if (ls.IsEmpty())
                throw new InvalidOperationException(
                    "ls cannot be empty.");

            float allWeight = ls.Sum(getWeight);
            float selectedWeight = allWeight * randomNumber;

            T? lastObject = default;

            foreach (var i in ls)
            {
                var p = getWeight(i);
                if (selectedWeight <= p) return i;
                selectedWeight -= p;

                lastObject = i;
            }

            return lastObject!;
        }
    }
}