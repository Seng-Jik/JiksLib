using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Extensions
{
    public static class ListExtension
    {
        /// <summary>
        /// 以O(1)时间复杂度删除给定下标的元素
        ///
        /// 删除后列表的最后一个元素将会填补空位
        /// </summary>
        /// <param name="ls">列表</param>
        /// <param name="n">要删除元素的下标</param>
        /// <typeparam name="T">列表元素的类型</typeparam>
        public static void RemoveO1<T>(this IList<T> ls, int n)
        {
            if (ls.Count == 0)
                throw new InvalidOperationException("list has no element to remove.");

            var lastIndex = ls.Count - 1;
            var last = ls[lastIndex];
            ls[n] = last;
            ls.RemoveAt(lastIndex);
        }

        /// <summary>
        /// 移除最后一个元素并返回该元素
        /// </summary>
        public static T PopBack<T>(this IList<T> ls)
        {
            if (ls.Count == 0)
                throw new InvalidOperationException("list has no element to remove.");

            var lastIndex = ls.Count - 1;
            var last = ls[lastIndex];
            ls.RemoveAt(lastIndex);
            return last;
        }

        /// <summary>
        /// 查找第一个符合条件的元素的下标
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">列表</param>
        /// <param name="predicate">条件</param>
        /// <returns>第一个符合条件的元素的下标，如果是-1则查找失败</returns>
        public static int FindIndex<T>(this IReadOnlyList<T> ls, Func<T, bool> predicate)
        {
            for (int i = 0; i < ls.Count; ++i)
                if (predicate(ls[i]))
                    return i;

            return -1;
        }

        /// <summary>
        /// 按照概率对数组进行洗牌
        /// </summary>
        /// <param name="list"></param>
        /// <param name="rand">随机数发生器</param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> list, Random rand)
        {
            //Fisher-Yates洗牌算法
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                //随机抽取[0~i]的索引并与当前索引进行替换
                int j = rand.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// 获取反向的迭代器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ls"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetReversedEnumerable<T>(
            this IReadOnlyList<T> ls)
        {
            for (int i = ls.Count - 1; i >= 0; --i)
                yield return ls[i];
        }

        /// <summary>
        /// 随机抽取一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">要抽取元素的列表</param>
        /// <param name="randomNumber">随机数，范围为[0,1]</param>
        /// <returns>随机抽取的元素</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T RandomSelect<T>(
            this IReadOnlyList<T> ls,
            float randomNumber)
        {
            if (ls.Count == 0)
                throw new InvalidOperationException(
                    "ls cannot be empty.");

            return ls[(int)(randomNumber * (ls.Count - 1))];
        }

        /// <summary>
        /// 随机抽取一个元素的下标
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">要抽取元素的列表</param>
        /// <param name="randomNumber">随机数，范围为[0,1]</param>
        /// <returns>随机抽取的元素的下标</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int RandomSelectIndex<T>(
            this IReadOnlyList<T> ls,
            float randomNumber,
            Func<T, float> getWeight)
        {
            if (ls.Count == 0)
                throw new InvalidOperationException(
                    "Cannot random select on a zero length list.");

            if (ls.Count == 1) return 0;

            float allWeight = ls.Sum(getWeight);
            float selectedWeight = allWeight * randomNumber;

            for (int i = 0; i < ls.Count; ++i)
            {
                var p = getWeight(ls[i]);
                if (selectedWeight <= p) return i;
                selectedWeight -= p;
            }

            return ls.Count - 1;
        }
    }
}