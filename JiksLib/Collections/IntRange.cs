using System;
using System.Collections;
using System.Collections.Generic;

namespace JiksLib.Collections
{
    /// <summary>
    /// 整数范围
    /// </summary>
    /// <remarks>
    /// 当 <see cref="Min"/> 与 <see cref="Max"/> 相等时，只要 <see cref="IncludeMin"/> 或
    /// <see cref="IncludeMax"/> 任一为 true，该单点就属于范围（<c>[x, x]</c>、<c>[x, x)</c>
    /// 与 <c>(x, x]</c> 表示同一个单点区间）；两端都不包含时为空区间。
    /// <see cref="Count"/>、<see cref="Contains"/> 与枚举结果始终一致。
    /// </remarks>
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
        /// <exception cref="ArgumentException">当 <paramref name="min"/> 大于 <paramref name="max"/> 时抛出</exception>
        public IntRange(int min, bool includeMin, int max, bool includeMax)
        {
            if (min > max)
                throw new ArgumentException(
                    "Min must be less than or equal to Max.", nameof(min));

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
        /// 当数量超过 <see cref="int.MaxValue"/> 时抛出 <see cref="OverflowException"/>
        /// </summary>
        /// <exception cref="OverflowException">
        /// 范围包含的整数数量超过 <see cref="int.MaxValue"/> 时抛出
        /// </exception>
        public readonly int Count
        {
            get
            {
                if (Max == Min)
                    return IncludeMin || IncludeMax ? 1 : 0;

                // 用 long 计算：Max - Min - 1 在 [int.MinValue, int.MaxValue] 上会溢出
                long count = (long)Max - Min - 1;

                if (IncludeMin) count++;
                if (IncludeMax) count++;

                if (count > int.MaxValue)
                    throw new OverflowException(
                        "The number of integers in this range exceeds Int32.MaxValue.");

                return (int)count;
            }
        }

        /// <summary>
        /// 返回枚举器
        /// </summary>
        public readonly Enumerator GetEnumerator() => new(this);

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

        /// <summary>
        /// 枚举器
        /// 内部使用 long 游标，避免 Max 为 <see cref="int.MaxValue"/> 时自增回绕导致
        /// 从 <see cref="int.MinValue"/> 重新枚举整个整数空间。
        /// </summary>
        public struct Enumerator : IEnumerator<int>
        {
            internal Enumerator(IntRange range)
            {
                this.range = range;
                current = -1;
                valid = false;
                nextValue = ComputeStart(range);
            }

            /// <summary>
            /// 计算第一个候选值；空区间返回的值会大于上限，从而第一次 MoveNext 即结束。
            /// </summary>
            static long ComputeStart(IntRange range)
            {
                if (range.Max == range.Min)
                {
                    // 单点区间：只要任意一端包含，该点就属于范围
                    return range.IncludeMin || range.IncludeMax
                        ? range.Min
                        : (long)range.Max + 1;
                }

                return range.IncludeMin ? range.Min : (long)range.Min + 1;
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
                // Min == Max 时，只要任一端点被包含，该点就属于范围；上界必须取 Max，
                // 否则 [x, x) 会枚举为空，而 Count / Contains 认为有 1 个元素。
                long max;

                if (range.Max == range.Min)
                {
                    max = range.IncludeMin || range.IncludeMax
                        ? range.Max
                        : (long)range.Max - 1;
                }
                else
                {
                    max = range.IncludeMax ? range.Max : (long)range.Max - 1;
                }

                if (nextValue > max)
                {
                    valid = false;
                    current = -1;
                    return false;
                }

                current = (int)nextValue;
                nextValue++;
                valid = true;
                return true;
            }

            public void Reset()
            {
                current = -1;
                valid = false;
                nextValue = ComputeStart(range);
            }

            public void Dispose() { }

            readonly IntRange range;
            int current;
            long nextValue;
            bool valid;
        }
    }
}
