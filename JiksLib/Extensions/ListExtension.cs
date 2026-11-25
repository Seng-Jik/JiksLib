using System;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Collections;

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
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">列表为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当 <paramref name="n"/> 不在 [0, Count) 范围内时抛出
        /// </exception>
        public static void RemoveO1<T>(this IList<T> ls, int n)
        {
            ls.ThrowIfNull(nameof(ls));

            if (ls.Count == 0)
                throw new InvalidOperationException("list has no element to remove.");

            if ((uint)n >= (uint)ls.Count)
                throw new ArgumentOutOfRangeException(nameof(n));

            var lastIndex = ls.Count - 1;
            var last = ls[lastIndex];

            // 先删除再回填：定长集合（如数组）的 RemoveAt 会抛异常，
            // 这样异常路径不会留下"已覆盖但未删除"的半成品状态。
            ls.RemoveAt(lastIndex);

            if (n != lastIndex)
                ls[n] = last;
        }

        /// <summary>
        /// 移除最后一个元素并返回该元素
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        public static T PopBack<T>(this IList<T> ls)
        {
            ls.ThrowIfNull(nameof(ls));

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
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="ls"/> 或 <paramref name="predicate"/> 为 null 时抛出
        /// </exception>
        public static int FindIndex<T>(this IReadOnlyList<T> ls, Func<T, bool> predicate)
        {
            ls.ThrowIfNull(nameof(ls));
            predicate.ThrowIfNull(nameof(predicate));

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
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="list"/> 或 <paramref name="rand"/> 为 null 时抛出
        /// </exception>
        public static void Shuffle<T>(this IList<T> list, Random rand)
        {
            list.ThrowIfNull(nameof(list));
            rand.ThrowIfNull(nameof(rand));

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
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        public static IEnumerable<T> GetReversedEnumerable<T>(
            this IReadOnlyList<T> ls)
        {
            ls.ThrowIfNull(nameof(ls));

            for (int i = ls.Count - 1; i >= 0; --i)
                yield return ls[i];
        }

        /// <summary>
        /// 获取只读列表的零分配枚举视图
        /// 返回值复用 <see cref="ReadOnlyListSegment{T}"/> 的结构体枚举器，
        /// 用 foreach 遍历不会产生分配（不会经过 <see cref="IEnumerator{T}"/> 接口）。
        /// 片段覆盖构造时的整个列表区间，不复制元素；区间内元素内容的后续变化会反映到片段上。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">只读列表</param>
        /// <returns>覆盖整个列表的只读片段</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        public static ReadOnlyListSegment<T> AsSegment<T>(this IReadOnlyList<T> ls)
        {
            ls.ThrowIfNull(nameof(ls));
            return new ReadOnlyListSegment<T>(ls);
        }

        /// <summary>
        /// 随机抽取一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">要抽取元素的列表</param>
        /// <param name="randomNumber">随机数，范围为[0,1]，取 1 时返回最后一个元素</param>
        /// <returns>随机抽取的元素</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">列表为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="randomNumber"/> 为 NaN 或不在 [0,1] 内时抛出
        /// </exception>
        public static T RandomSelect<T>(
            this IReadOnlyList<T> ls,
            float randomNumber)
        {
            ls.ThrowIfNull(nameof(ls));

            if (ls.Count == 0)
                throw new InvalidOperationException(
                    "ls cannot be empty.");

            if (float.IsNaN(randomNumber) || randomNumber < 0f || randomNumber > 1f)
                throw new ArgumentOutOfRangeException(
                    nameof(randomNumber),
                    "Random number must be within [0, 1].");

            int index = (int)(randomNumber * ls.Count);
            if (index >= ls.Count) index = ls.Count - 1;

            return ls[index];
        }

        /// <summary>
        /// 按权重随机抽取一个元素的下标
        /// </summary>
        /// <remarks>
        /// 权重函数会被调用两次（一次求和、一次选择），因此必须是纯函数；
        /// 需要只求值一次时，请使用接收预计算权重的重载。
        /// 权重和以 <see cref="double"/> 累加，避免大量 float 权重相加时溢出为 +∞ 导致分布错误。
        /// </remarks>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">要抽取元素的列表</param>
        /// <param name="randomNumber">随机数，必须在 [0, 1] 范围内</param>
        /// <param name="getWeight">获取元素权重的函数，返回值必须是非负有限数</param>
        /// <returns>随机抽取的元素的下标</returns>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 随机数越界 / 为 NaN 或无穷大，或权重为负数 / NaN / 无穷大时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">列表为空或总权重不大于 0 时抛出</exception>
        public static int RandomSelectIndex<T>(
            this IReadOnlyList<T> ls,
            float randomNumber,
            Func<T, float> getWeight)
        {
            ls.ThrowIfNull(nameof(ls));
            getWeight.ThrowIfNull(nameof(getWeight));

            if (ls.Count == 0)
                throw new InvalidOperationException(
                    "Cannot random select on a zero length list.");

            ValidateRandomNumber(randomNumber);

            double allWeight = 0.0;

            for (int i = 0; i < ls.Count; i++)
            {
                float weight = getWeight(ls[i]);
                ValidateWeight(weight, nameof(getWeight));
                allWeight += weight;
            }

            if (!(allWeight > 0.0))
                throw new InvalidOperationException(
                    "Total weight must be greater than zero.");

            double selectedWeight = allWeight * randomNumber;

            for (int i = 0; i < ls.Count; i++)
            {
                float weight = getWeight(ls[i]);
                if (weight > 0f && selectedWeight <= weight) return i;
                selectedWeight -= weight;
            }

            // 浮点误差兜底：返回最后一个正权重项
            for (int i = ls.Count - 1; i >= 0; i--)
            {
                if (getWeight(ls[i]) > 0f) return i;
            }

            throw new InvalidOperationException(
                "Total weight must be greater than zero.");
        }

        /// <summary>
        /// 按预计算权重随机抽取一个元素的下标
        /// 权重只读取一次，适合权重计算有成本或不能重复求值的场景
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="ls">要抽取元素的列表</param>
        /// <param name="randomNumber">随机数，必须在 [0, 1] 范围内</param>
        /// <param name="weights">与列表一一对应的权重，必须是非负有限数</param>
        /// <returns>随机抽取的元素的下标</returns>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        /// <exception cref="ArgumentException">权重数量与列表长度不一致时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 随机数越界 / 为 NaN 或无穷大，或权重为负数 / NaN / 无穷大时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">列表为空或总权重不大于 0 时抛出</exception>
        public static int RandomSelectIndex<T>(
            this IReadOnlyList<T> ls,
            float randomNumber,
            IReadOnlyList<float> weights)
        {
            ls.ThrowIfNull(nameof(ls));
            weights.ThrowIfNull(nameof(weights));

            if (ls.Count == 0)
                throw new InvalidOperationException(
                    "Cannot random select on a zero length list.");

            if (weights.Count != ls.Count)
                throw new ArgumentException(
                    "Weights count must match the list count.",
                    nameof(weights));

            ValidateRandomNumber(randomNumber);

            double allWeight = 0.0;

            for (int i = 0; i < weights.Count; i++)
            {
                ValidateWeight(weights[i], nameof(weights));
                allWeight += weights[i];
            }

            if (!(allWeight > 0.0))
                throw new InvalidOperationException(
                    "Total weight must be greater than zero.");

            double selectedWeight = allWeight * randomNumber;

            for (int i = 0; i < weights.Count; i++)
            {
                float weight = weights[i];
                if (weight > 0f && selectedWeight <= weight) return i;
                selectedWeight -= weight;
            }

            for (int i = weights.Count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f) return i;
            }

            throw new InvalidOperationException(
                "Total weight must be greater than zero.");
        }

        static void ValidateRandomNumber(float randomNumber)
        {
            if (float.IsNaN(randomNumber) || float.IsInfinity(randomNumber) ||
                randomNumber < 0f || randomNumber > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(randomNumber),
                    "Random number must be within [0, 1].");
            }
        }

        static void ValidateWeight(float weight, string paramName)
        {
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    "Weight must be a finite non-negative number.");
            }
        }
    }
}
