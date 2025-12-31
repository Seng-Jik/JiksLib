using System;

namespace JiksLib.Collections
{
    /// <summary>
    /// 浮点数范围
    /// </summary>
    public readonly struct FloatRange
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
        /// <exception cref="ArgumentException"></exception>
        public FloatRange(float min, bool includeMin, float max, bool includeMax)
        {
            if (min > max)
                throw new ArgumentException("Min must be less than or equal to Max.");

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
        public readonly bool Contains(int value)
        {
            if (value > Min && value < Max) return true;
            if (IncludeMin && value == Min) return true;
            if (IncludeMax && value == Max) return true;
            return false;
        }
    }
}