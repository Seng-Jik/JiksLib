using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Extensions
{
    public static class RandomSelectExtension
    {
        /// <summary>
        /// 从序列中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">序列</param>
        /// <param name="randomNumber">随机数，范围为[0, 1]</param>
        /// <param name="getWeight">获得元素权重的委托</param>
        /// <returns></returns>
        public static T RandomSelect<T>(
            this IReadOnlyList<T> ls,
            float randomNumber,
            Func<T, float> getWeight)
        {
            if (ls.Count <= 0)
                throw new InvalidOperationException(
                    "ls cannot be empty.");

            float allWeight = ls.Sum(getWeight);
            float selectedWeight = allWeight * randomNumber;

            for (int i = 0; i < ls.Count; ++i)
            {
                var p = getWeight(ls[i]);
                if (selectedWeight <= p) return ls[i];
                selectedWeight -= p;
            }

            return ls[ls.Count - 1];
        }
    }
}