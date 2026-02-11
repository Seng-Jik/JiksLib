using System;
using System.Collections;
using System.Collections.Generic;

namespace JiksLib.Collections
{
    /// <summary>
    /// 整数范围
    /// </summary>
    public readonly struct IntRange :
        IEnumerable<int>,
        IReadOnlyCollection<int>,
        IEquatable<IntRange>
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public readonly int Min;

        /// <summary>
        /// 是否包含最小值
        /// </summary>
        public readonly bool IncludeMin;

        /// <summary>
        /// 最大值
        /// </summary>
        public readonly int Max;

        /// <summary>
        /// 是否包含最大值
        /// </summary>
        public readonly bool IncludeMax;

        /// <summary>
        /// 构造一个整数范围
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="includeMin">是否包含最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="includeMax">是否包含最大值</param>
        /// <exception cref="ArgumentException"></exception>
        public IntRange(int min, bool includeMin, int max, bool includeMax)
        {
            if (min > max)
                throw new ArgumentException("Min must be less than or equal to Max.");

            Min = min;
            IncludeMin = includeMin;
            Max = max;
            IncludeMax = includeMax;
        }

        /// <summary>
        /// 构造一个整数范围，包含最小值但不包含最大值
        /// </summary>
        public IntRange(int minInclude, int maxExclude) :
            this(minInclude, true, maxExclude, false)
        {
        }

        /// <summary>
        /// 判断值是否在整数范围内
        /// </summary>
        /// <param name="value">要判断的值</param>
        /// <returns>是否在整数范围内</returns>
        public readonly bool Contains(int value)
        {
            if (value > Min && value < Max) return true;
            if (IncludeMin && value == Min) return true;
            if (IncludeMax && value == Max) return true;
            return false;
        }

        /// <summary>
        /// 该范围内整数的数量
        /// </summary>
        public readonly int Count
        {
            get
            {
                int count = Math.Max(Max - Min - 1, 0);
                if (IncludeMin) count++;
                if (IncludeMax && Max != Min) count++;
                return count;
            }
        }

        /// <summary>
        /// 返回枚举器
        /// </summary>
        public readonly IEnumerator<int> GetEnumerator()
        {
            if (IncludeMin) yield return Min;

            for (int i = Min + 1; i < Max; ++i)
                yield return i;

            if (IncludeMax && Min != Max) yield return Max;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(IntRange other) =>
            Min == other.Min
            && IncludeMin == other.IncludeMin
            && Max == other.Max
            && IncludeMax == other.IncludeMax;
    }
}