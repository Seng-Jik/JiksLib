using System;

namespace JiksLib.Collections
{
    /// <summary>
    /// 浮点数范围
    /// </summary>
    public readonly struct FloatRange : IEquatable<FloatRange>
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public readonly float Min;

        /// <summary>
        /// 是否包含最小值
        /// </summary>
        public readonly bool IncludeMin;

        /// <summary>
        /// 最大值
        /// </summary>
        public readonly float Max;

        /// <summary>
        /// 是否包含最大值
        /// </summary>
        public readonly bool IncludeMax;

        /// <summary>
        /// 构造一个浮点数范围
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="includeMin">是否包含最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="includeMax">是否包含最大值</param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="min"/> 大于 <paramref name="max"/>，或任一值为 NaN 时抛出
        /// </exception>
        public FloatRange(float min, bool includeMin, float max, bool includeMax)
        {
            if (float.IsNaN(min))
                throw new ArgumentException("Min cannot be NaN.", nameof(min));

            if (float.IsNaN(max))
                throw new ArgumentException("Max cannot be NaN.", nameof(max));

            if (min > max)
                throw new ArgumentException(
                    "Min must be less than or equal to Max.", nameof(min));

            Min = min;
            IncludeMin = includeMin;
            Max = max;
            IncludeMax = includeMax;
        }

        /// <summary>
        /// 构造一个浮点数范围，包含最小值但不包含最大
        /// </summary>
        public FloatRange(float minInclude, float maxExclude) :
            this(minInclude, true, maxExclude, false)
        {
        }

        /// <summary>
        /// 判断值是否在浮点数范围内
        /// </summary>
        /// <param name="value">要判断的值</param>
        /// <returns>是否在浮点数范围内</returns>
        public readonly bool Contains(float value)
        {
            if (value > Min && value < Max) return true;
            if (IncludeMin && value == Min) return true;
            if (IncludeMax && value == Max) return true;
            return false;
        }

        /// <summary>
        /// 判断两个浮点数范围是否相等
        /// 比较四个字段；由于构造时拒绝 NaN，这里可以直接使用 float 的相等比较
        /// </summary>
        public bool Equals(FloatRange other) =>
            Min == other.Min
            && IncludeMin == other.IncludeMin
            && Max == other.Max
            && IncludeMax == other.IncludeMax;

        public override bool Equals(object? obj) =>
            obj is FloatRange other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // ±0 相等但 GetHashCode 在部分 TFM 上不同，这里归一化以保证 Equals/GetHashCode 契约
                hash = hash * 23 + (Min == 0f ? 0 : Min.GetHashCode());
                hash = hash * 23 + IncludeMin.GetHashCode();
                hash = hash * 23 + (Max == 0f ? 0 : Max.GetHashCode());
                hash = hash * 23 + IncludeMax.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(FloatRange left, FloatRange right) => left.Equals(right);

        public static bool operator !=(FloatRange left, FloatRange right) => !left.Equals(right);

        public override string ToString()
        {
            char left = IncludeMin ? '[' : '(';
            char right = IncludeMax ? ']' : ')';
            return $"{left}{Min}, {Max}{right}";
        }
    }
}
