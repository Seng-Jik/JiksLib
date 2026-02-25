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
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<int>
        {
            private readonly IntRange range;
            private int current;
            private int nextValue;
            private bool started;
            private bool valid;

            internal Enumerator(IntRange range)
            {
                this.range = range;
                current = -1;
                started = false;
                valid = false;
                // 计算第一个要返回的值
                if (range.Min == range.Max)
                {
                    // 处理相等情况
                    if (range.IncludeMin || range.IncludeMax)
                        nextValue = range.Min;
                    else
                        nextValue = range.Min + 1; // 确保无元素
                }
                else
                {
                    if (range.IncludeMin)
                        nextValue = range.Min;
                    else
                        nextValue = range.Min + 1;
                }
            }

            public int Current
            {
                get
                {
                    if (!valid)
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");
                    return current;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    started = true;
                }

                valid = false;

                // 处理min == max的特殊情况
                if (range.Min == range.Max
                    && nextValue == range.Min
                    && (range.IncludeMin || range.IncludeMax))
                {
                    current = nextValue;
                    nextValue++;
                    valid = true;
                    return true;
                }

                if (nextValue < range.Max)
                {
                    current = nextValue;
                    nextValue++;
                    valid = true;
                    return true;
                }

                if (nextValue == range.Max
                    && range.IncludeMax
                    && range.Min != range.Max)
                {
                    current = nextValue;
                    nextValue++;
                    valid = true;
                    return true;
                }

                current = -1;
                valid = false;
                return false;
            }

            public void Reset()
            {
                current = -1;
                started = false;
                valid = false;
                // 重新计算nextValue
                if (range.Min == range.Max)
                {
                    if (range.IncludeMin || range.IncludeMax)
                        nextValue = range.Min;
                    else
                        nextValue = range.Min + 1;
                }
                else
                {
                    if (range.IncludeMin)
                        nextValue = range.Min;
                    else
                        nextValue = range.Min + 1;
                }
            }

            public void Dispose() { }
        }

        /// <summary>
        /// 返回枚举器
        /// </summary>
        public readonly Enumerator GetEnumerator() => new(this);

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
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

        public override string ToString()
        {
            char left = IncludeMin ? '[' : '(';
            char right = IncludeMax ? ']' : ')';
            return $"{left}{Min}, {Max}{right}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is IntRange i)
                return Equals(i);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Min.GetHashCode();
                hash = hash * 23 + IncludeMin.GetHashCode();
                hash = hash * 23 + Max.GetHashCode();
                hash = hash * 23 + IncludeMax.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(IntRange left, IntRange right) => left.Equals(right);
        public static bool operator !=(IntRange left, IntRange right) => !left.Equals(right);
    }
}