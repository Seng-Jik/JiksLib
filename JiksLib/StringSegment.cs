using System;
using System.Collections;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib
{
    /// <summary>
    /// 字符串片段：对 <see cref="string"/> 的一段只读视图。
    /// 切片、比较、查找与切分都不会复制底层字符串；只有 <see cref="ToString"/> 等
    /// 明确返回 <see cref="string"/> 的成员才会分配。
    /// </summary>
    /// <remarks>
    /// 该类型是值类型，默认值表示空片段。底层字符串为 null 时，所有成员都按空片段处理。
    /// </remarks>
    public readonly struct StringSegment :
        IEquatable<StringSegment>,
        IComparable<StringSegment>,
        IComparable,
        IReadOnlyList<char>
    {
        /// <summary>
        /// 空片段
        /// </summary>
        public static readonly StringSegment Empty = default;

        /// <summary>
        /// 在底层字符串中的起始下标
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// 片段长度
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// 用整个字符串构造片段
        /// </summary>
        /// <param name="source">底层字符串</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public StringSegment(string source)
        {
            this.source = source.ThrowIfNull();
            Offset = 0;
            Length = source.Length;
        }

        /// <summary>
        /// 用字符串的指定区间构造片段
        /// </summary>
        /// <param name="source">底层字符串</param>
        /// <param name="offset">起始下标</param>
        /// <param name="length">长度</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">区间超出字符串范围</exception>
        public StringSegment(string source, int offset, int length)
        {
            this.source = source.ThrowIfNull();

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (offset > source.Length - length)
                throw new ArgumentOutOfRangeException(nameof(length));

            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// 从字符串隐式构造片段
        /// </summary>
        /// <param name="value">字符串</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public static implicit operator StringSegment(string value) => new(value);

        /// <summary>
        /// 底层字符串；默认片段返回空字符串
        /// </summary>
        public string Source => source ?? string.Empty;

        /// <summary>
        /// 是否为空片段
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// 获取片段中指定位置上的字符
        /// </summary>
        /// <param name="index">片段内下标</param>
        /// <exception cref="IndexOutOfRangeException">下标超出片段范围</exception>
        public char this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                    throw new IndexOutOfRangeException(
                        "Index must be within the segment.");

                return source![Offset + index];
            }
        }

        int IReadOnlyCollection<char>.Count => Length;

        /// <summary>
        /// 获取一个切片
        /// </summary>
        /// <param name="startIndex">起始下标</param>
        /// <exception cref="ArgumentOutOfRangeException">下标超出片段范围</exception>
        public StringSegment Slice(int startIndex)
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return Slice(startIndex, Length - startIndex);
        }

        /// <summary>
        /// 获取一个切片
        /// </summary>
        /// <param name="startIndex">起始下标</param>
        /// <param name="length">长度</param>
        /// <exception cref="ArgumentOutOfRangeException">区间超出片段范围</exception>
        public StringSegment Slice(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            if (length < 0 || length > this.Length - startIndex)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (source == null)
                return Empty;

            return new StringSegment(source, Offset + startIndex, length);
        }

        /// <summary>
        /// 将片段复制为一个新字符串
        /// </summary>
        public override string ToString()
        {
            if (Length == 0)
                return string.Empty;

            if (source == null)
                return string.Empty;

            if (Offset == 0 && Length == source.Length)
                return source;

            return source.Substring(Offset, Length);
        }

        /// <summary>
        /// 将片段从指定位置开始的部分复制为字符串
        /// </summary>
        public string ToString(int startIndex) => Slice(startIndex).ToString();

        /// <summary>
        /// 将片段从指定位置开始的部分复制为字符串
        /// </summary>
        public string ToString(int startIndex, int length) =>
            Slice(startIndex, length).ToString();

        /// <summary>
        /// 将片段复制为字符数组
        /// </summary>
        public char[] ToCharArray()
        {
            if (Length == 0)
                return Array.Empty<char>();

            var result = new char[Length];
            source!.CopyTo(Offset, result, 0, Length);
            return result;
        }

        /// <summary>
        /// 将片段复制到字符数组
        /// </summary>
        /// <param name="destination">目标数组</param>
        /// <param name="destinationIndex">目标数组中的起始下标</param>
        /// <exception cref="ArgumentNullException">目标数组为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">目标区间不足</exception>
        public void CopyTo(char[] destination, int destinationIndex = 0)
        {
            destination.ThrowIfNull();

            if (destinationIndex < 0 ||
                destinationIndex > destination.Length - Length)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

            if (Length > 0)
                source!.CopyTo(Offset, destination, destinationIndex, Length);
        }

        /// <summary>
        /// 将片段的一部分复制到字符数组
        /// </summary>
        /// <param name="sourceIndex">片段内起始下标</param>
        /// <param name="destination">目标数组</param>
        /// <param name="destinationIndex">目标数组中的起始下标</param>
        /// <param name="count">要复制的字符数</param>
        /// <exception cref="ArgumentNullException">目标数组为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">区间超出范围</exception>
        public void CopyTo(
            int sourceIndex,
            char[] destination,
            int destinationIndex,
            int count)
        {
            destination.ThrowIfNull();

            if (sourceIndex < 0 || sourceIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            if (count < 0 || count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (destinationIndex < 0 ||
                destinationIndex > destination.Length - count)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

            if (count > 0)
                source!.CopyTo(
                    Offset + sourceIndex,
                    destination,
                    destinationIndex,
                    count);
        }

        /// <summary>
        /// 去除片段首尾的空白字符
        /// </summary>
        public StringSegment Trim()
        {
            int start = 0;
            int end = Length;

            while (start < end && char.IsWhiteSpace(source![Offset + start]))
                start++;

            while (end > start && char.IsWhiteSpace(source![Offset + end - 1]))
                end--;

            return Slice(start, end - start);
        }

        /// <summary>
        /// 去除片段首尾的指定字符
        /// </summary>
        /// <param name="trimChar">要删除的字符</param>
        public StringSegment Trim(char trimChar)
        {
            int start = 0;
            int end = Length;

            while (start < end && source![Offset + start] == trimChar)
                start++;

            while (end > start && source![Offset + end - 1] == trimChar)
                end--;

            return Slice(start, end - start);
        }

        /// <summary>
        /// 去除片段首尾的指定字符集合
        /// </summary>
        /// <param name="trimChars">要删除的字符集合</param>
        /// <exception cref="ArgumentNullException">字符集合为 null</exception>
        public StringSegment Trim(params char[] trimChars)
        {
            trimChars.ThrowIfNull();

            if (trimChars.Length == 0)
                return this;

            int start = 0;
            int end = Length;

            while (start < end &&
                   Array.IndexOf(trimChars, source![Offset + start]) >= 0)
                start++;

            while (end > start &&
                   Array.IndexOf(trimChars, source![Offset + end - 1]) >= 0)
                end--;

            return Slice(start, end - start);
        }

        /// <summary>
        /// 去除片段开头的空白字符
        /// </summary>
        public StringSegment TrimStart()
        {
            int start = 0;

            while (start < Length &&
                   char.IsWhiteSpace(source![Offset + start]))
                start++;

            return Slice(start);
        }

        /// <summary>
        /// 去除片段开头的指定字符
        /// </summary>
        /// <param name="trimChar">要删除的字符</param>
        public StringSegment TrimStart(char trimChar)
        {
            int start = 0;

            while (start < Length && source![Offset + start] == trimChar)
                start++;

            return Slice(start);
        }

        /// <summary>
        /// 去除片段开头的指定字符集合
        /// </summary>
        /// <param name="trimChars">要删除的字符集合</param>
        /// <exception cref="ArgumentNullException">字符集合为 null</exception>
        public StringSegment TrimStart(params char[] trimChars)
        {
            trimChars.ThrowIfNull();

            if (trimChars.Length == 0)
                return this;

            int start = 0;

            while (start < Length &&
                   Array.IndexOf(trimChars, source![Offset + start]) >= 0)
                start++;

            return Slice(start);
        }

        /// <summary>
        /// 去除片段结尾的空白字符
        /// </summary>
        public StringSegment TrimEnd()
        {
            int end = Length;

            while (end > 0 && char.IsWhiteSpace(source![Offset + end - 1]))
                end--;

            return Slice(0, end);
        }

        /// <summary>
        /// 去除片段结尾的指定字符
        /// </summary>
        /// <param name="trimChar">要删除的字符</param>
        public StringSegment TrimEnd(char trimChar)
        {
            int end = Length;

            while (end > 0 && source![Offset + end - 1] == trimChar)
                end--;

            return Slice(0, end);
        }

        /// <summary>
        /// 去除片段结尾的指定字符集合
        /// </summary>
        /// <param name="trimChars">要删除的字符集合</param>
        /// <exception cref="ArgumentNullException">字符集合为 null</exception>
        public StringSegment TrimEnd(params char[] trimChars)
        {
            trimChars.ThrowIfNull();

            if (trimChars.Length == 0)
                return this;

            int end = Length;

            while (end > 0 &&
                   Array.IndexOf(trimChars, source![Offset + end - 1]) >= 0)
                end--;

            return Slice(0, end);
        }

        /// <summary>
        /// 判断片段是否包含指定字符
        /// </summary>
        public bool Contains(char value) => IndexOf(value) >= 0;

        /// <summary>
        /// 判断片段是否包含指定字符串
        /// </summary>
        /// <param name="value">要查找的字符串</param>
        /// <param name="comparisonType">比较方式</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public bool Contains(
            string value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            IndexOf(value, 0, comparisonType) >= 0;

        /// <summary>
        /// 判断片段是否包含另一个片段
        /// </summary>
        /// <param name="value">要查找的片段</param>
        /// <param name="comparisonType">比较方式</param>
        public bool Contains(
            StringSegment value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            IndexOf(value, 0, comparisonType) >= 0;

        /// <summary>
        /// 查找指定字符第一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        public int IndexOf(char value)
        {
            if (Length == 0)
                return -1;

            int index = source!.IndexOf(value, Offset, Length);
            return index < 0 ? -1 : index - Offset;
        }

        /// <summary>
        /// 从指定位置开始查找指定字符第一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符</param>
        /// <param name="startIndex">起始下标</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int IndexOf(char value, int startIndex)
        {
            ValidateStartIndex(startIndex);

            if (startIndex == Length)
                return -1;

            int index = source!.IndexOf(value, Offset + startIndex, Length - startIndex);
            return index < 0 ? -1 : index - Offset;
        }

        /// <summary>
        /// 查找指定字符串第一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符串</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int IndexOf(
            string value,
            int startIndex = 0,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            value.ThrowIfNull();
            ValidateStartIndex(startIndex);

            if (value.Length == 0)
                return startIndex;

            if (value.Length > Length - startIndex)
                return -1;

            int index = Source.IndexOf(
                value,
                Offset + startIndex,
                Length - startIndex,
                comparisonType);

            return index < 0 ? -1 : index - Offset;
        }

        /// <summary>
        /// 查找另一个片段第一次出现的位置
        /// </summary>
        /// <param name="value">要查找的片段</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int IndexOf(
            StringSegment value,
            int startIndex = 0,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            ValidateStartIndex(startIndex);

            if (value.Length == 0)
                return startIndex;

            if (value.Length > Length - startIndex)
                return -1;

            switch (comparisonType)
            {
                case StringComparison.Ordinal:
                    return IndexOfOrdinal(value, startIndex);

                case StringComparison.OrdinalIgnoreCase:
                    return IndexOfOrdinalIgnoreCase(value, startIndex);

                default:
                    {
                        // 区域性比较需要真字符串，只有这个分支会分配。
                        int index = Source.IndexOf(
                            value.ToString(),
                            Offset + startIndex,
                            Length - startIndex,
                            comparisonType);

                        return index < 0 ? -1 : index - Offset;
                    }
            }
        }

        /// <summary>
        /// 查找指定字符最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        public int LastIndexOf(char value)
        {
            for (int i = Length - 1; i >= 0; i--)
            {
                if (source![Offset + i] == value)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 从指定位置向前查找指定字符最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符</param>
        /// <param name="startIndex">起始下标</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int LastIndexOf(char value, int startIndex)
        {
            if (startIndex < 0 || startIndex >= Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            for (int i = startIndex; i >= 0; i--)
            {
                if (source![Offset + i] == value)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 查找指定字符串最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符串</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public int LastIndexOf(
            string value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            LastIndexOf(value, Length, comparisonType);

        /// <summary>
        /// 从指定位置向前查找指定字符串最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的字符串</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int LastIndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            value.ThrowIfNull();
            return LastIndexOf(
                new StringSegment(value),
                startIndex,
                comparisonType);
        }

        /// <summary>
        /// 查找另一个片段最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的片段</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        public int LastIndexOf(
            StringSegment value,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            LastIndexOf(value, Length, comparisonType);

        /// <summary>
        /// 从指定位置向前查找另一个片段最后一次出现的位置
        /// </summary>
        /// <param name="value">要查找的片段</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>片段内下标；找不到时返回 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">起始下标超出片段范围</exception>
        public int LastIndexOf(
            StringSegment value,
            int startIndex,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            ValidateStartIndex(startIndex);

            if (value.Length == 0)
                return Math.Min(startIndex + 1, Length);

            int maxStart = startIndex - value.Length + 1;
            int lastPossible = Length - value.Length;

            if (maxStart > lastPossible)
                maxStart = lastPossible;

            for (int i = maxStart; i >= 0; i--)
            {
                if (MatchesAt(value, i, comparisonType))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 判断片段是否以指定字符开头
        /// </summary>
        public bool StartsWith(char value) =>
            Length > 0 && source![Offset] == value;

        /// <summary>
        /// 判断片段是否以指定字符串开头
        /// </summary>
        /// <param name="value">要匹配的字符串</param>
        /// <param name="comparisonType">比较方式</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public bool StartsWith(
            string value,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            value.ThrowIfNull();
            return StartsWith(new StringSegment(value), comparisonType);
        }

        /// <summary>
        /// 判断片段是否以另一个片段开头
        /// </summary>
        /// <param name="value">要匹配的片段</param>
        /// <param name="comparisonType">比较方式</param>
        public bool StartsWith(
            StringSegment value,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (value.Length > Length)
                return false;

            if (value.Length == 0)
                return true;

            return MatchesAt(value, 0, comparisonType);
        }

        /// <summary>
        /// 判断片段是否以指定字符结尾
        /// </summary>
        public bool EndsWith(char value) =>
            Length > 0 && source![Offset + Length - 1] == value;

        /// <summary>
        /// 判断片段是否以指定字符串结尾
        /// </summary>
        /// <param name="value">要匹配的字符串</param>
        /// <param name="comparisonType">比较方式</param>
        /// <exception cref="ArgumentNullException">字符串为 null</exception>
        public bool EndsWith(
            string value,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            value.ThrowIfNull();
            return EndsWith(new StringSegment(value), comparisonType);
        }

        /// <summary>
        /// 判断片段是否以另一个片段结尾
        /// </summary>
        /// <param name="value">要匹配的片段</param>
        /// <param name="comparisonType">比较方式</param>
        public bool EndsWith(
            StringSegment value,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (value.Length > Length)
                return false;

            if (value.Length == 0)
                return true;

            return MatchesAt(value, Length - value.Length, comparisonType);
        }

        /// <summary>
        /// 判断两个片段是否相等
        /// </summary>
        public bool Equals(StringSegment other) =>
            Equals(other, StringComparison.Ordinal);

        /// <summary>
        /// 按指定比较方式判断两个片段是否相等
        /// </summary>
        /// <param name="other">另一个片段</param>
        /// <param name="comparisonType">比较方式</param>
        public bool Equals(
            StringSegment other,
            StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.Ordinal:
                    return Length == other.Length && CompareOrdinal(this, other) == 0;

                case StringComparison.OrdinalIgnoreCase:
                    return Length == other.Length && CompareOrdinalIgnoreCase(this, other) == 0;

                default:
                    // 文化相关比较可能忽略字符，从而把不同长度的片段视为相等，
                    // 因此不能先用长度快速失败，必须把完整内容交给 BCL 比较。
                    return string.Equals(ToString(), other.ToString(), comparisonType);
            }
        }

        /// <summary>
        /// 按序数比较判断片段是否与字符串相等
        /// </summary>
        public bool Equals(string? other) => Equals(other, StringComparison.Ordinal);

        /// <summary>
        /// 按指定比较方式判断片段是否与字符串相等
        /// </summary>
        /// <param name="other">要比较的字符串</param>
        /// <param name="comparisonType">比较方式</param>
        public bool Equals(
            string? other,
            StringComparison comparisonType)
        {
            if (other == null)
                return false;

            return Equals(new StringSegment(other), comparisonType);
        }

        public override bool Equals(object? obj) => obj is StringSegment other && Equals(other);

        public override int GetHashCode() => GetHashCode(StringComparison.Ordinal);

        /// <summary>
        /// 按指定比较方式计算哈希值
        /// </summary>
        /// <param name="comparisonType">比较方式，只支持 Ordinal 与 OrdinalIgnoreCase</param>
        /// <exception cref="ArgumentException">不支持该比较方式</exception>
        public int GetHashCode(StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.Ordinal:
                    {
                        ulong hash = BKDRHash();
                        return unchecked((int)(hash ^ (hash >> 32)));
                    }

                case StringComparison.OrdinalIgnoreCase:
                    {
                        ulong hash = 0;

                        unchecked
                        {
                            for (int i = 0; i < Length; i++)
                            {
                                hash = hash * 131 +
                                       char.ToUpperInvariant(source![Offset + i]);
                            }
                        }

                        return unchecked((int)(hash ^ (hash >> 32)));
                    }

                default:
                    throw new ArgumentException(
                        "Only Ordinal and OrdinalIgnoreCase are supported.",
                        nameof(comparisonType));
            }
        }

        /// <summary>
        /// 按序数比较判断片段与另一个片段的顺序
        /// </summary>
        public int CompareTo(StringSegment other) =>
            Compare(this, other, StringComparison.Ordinal);

        /// <summary>
        /// 按序数比较判断片段与字符串的顺序
        /// </summary>
        /// <param name="other">要比较的字符串；null 视为排在任何片段之后</param>
        public int CompareTo(string? other)
        {
            if (other == null)
                return 1;

            int minLength = Math.Min(Length, other.Length);

            if (minLength > 0)
            {
                int result = string.CompareOrdinal(
                    Source,
                    Offset,
                    other,
                    0,
                    minLength);

                if (result != 0)
                    return result;
            }

            return Length.CompareTo(other.Length);
        }

        int IComparable.CompareTo(object? obj)
        {
            if (obj == null)
                return 1;

            if (obj is StringSegment other)
                return CompareTo(other);

            throw new ArgumentException(
                $"Object must be of type {nameof(StringSegment)}.",
                nameof(obj));
        }

        /// <summary>
        /// 按指定比较方式比较两个片段
        /// </summary>
        /// <param name="x">第一个片段</param>
        /// <param name="y">第二个片段</param>
        /// <param name="comparisonType">比较方式</param>
        /// <returns>小于零表示 x 在前，大于零表示 y 在前，零表示相等</returns>
        public static int Compare(
            StringSegment x,
            StringSegment y,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            switch (comparisonType)
            {
                case StringComparison.Ordinal:
                    return CompareOrdinal(x, y);

                case StringComparison.OrdinalIgnoreCase:
                    return CompareOrdinalIgnoreCase(x, y);

                default:
                    // 文化相关比较不能用"公共前缀 + 长度"近似：BCL 会忽略某些字符，
                    // 因此必须把完整内容交给 BCL 比较（仅在文化比较路径上分配）。
                    return string.Compare(x.ToString(), y.ToString(), comparisonType);
            }
        }

        /// <summary>
        /// 按序数比较判断两个片段是否相等
        /// </summary>
        public static bool operator ==(StringSegment left, StringSegment right) =>
            left.Equals(right);

        /// <summary>
        /// 按序数比较判断两个片段是否不相等
        /// </summary>
        public static bool operator !=(StringSegment left, StringSegment right) =>
            !left.Equals(right);

        public static bool operator <(StringSegment left, StringSegment right) =>
            left.CompareTo(right) < 0;

        public static bool operator >(StringSegment left, StringSegment right) =>
            left.CompareTo(right) > 0;

        public static bool operator <=(StringSegment left, StringSegment right) =>
            left.CompareTo(right) <= 0;

        public static bool operator >=(StringSegment left, StringSegment right) =>
            left.CompareTo(right) >= 0;

        /// <summary>
        /// 用分隔符切分片段，行为与 <see cref="string.Split(char[])"/> 一致，
        /// 包括保留空项与结尾空项；空片段会得到一个空片段。
        /// </summary>
        /// <param name="separator">分隔符</param>
        public StringSegment[] Split(char separator = ',')
        {
            if (Length == 0)
                return new[] { this };

            int count = 1;

            for (int i = 0; i < Length; i++)
            {
                if (source![Offset + i] == separator)
                    count++;
            }

            var result = new StringSegment[count];
            int resultIndex = 0;
            int start = 0;

            for (int i = 0; i < Length; i++)
            {
                if (source![Offset + i] == separator)
                {
                    result[resultIndex++] =
                        new StringSegment(source!, Offset + start, i - start);
                    start = i + 1;
                }
            }

            result[resultIndex] =
                new StringSegment(source!, Offset + start, Length - start);

            return result;
        }

        /// <summary>
        /// 用分隔符切分片段，空片段返回空数组。
        /// 这是 JiksLib.Extensions.StringExtension.Split0 的片段版本。
        /// </summary>
        /// <param name="separator">分隔符</param>
        public StringSegment[] Split0(char separator = ',') =>
            Length == 0 ? Array.Empty<StringSegment>() : Split(separator);

        /// <summary>
        /// 创建一个零分配的切分器
        /// </summary>
        /// <param name="separator">分隔符</param>
        public Tokenizer Tokenize(char separator = ',') =>
            new(this, separator);

        /// <summary>
        /// 计算片段的 BKDR 哈希值，与
        /// <see cref="StringExtension.BKDRHash(string, ulong)"/> 的结果一致。
        /// </summary>
        /// <param name="inputHash">初始哈希值</param>
        public ulong BKDRHash(ulong inputHash = 0)
        {
            ulong hash = inputHash;

            unchecked
            {
                for (int i = 0; i < Length; i++)
                    hash = hash * 131 + source![Offset + i];
            }

            return hash;
        }

        /// <summary>
        /// 去除片段头部的指定片段
        /// </summary>
        /// <param name="prefix">要去掉的头部</param>
        /// <param name="remain">去掉头部后的剩余部分；失败时为空片段</param>
        /// <returns>是否成功</returns>
        public bool StripPrefix(StringSegment prefix, out StringSegment remain)
        {
            if (!StartsWith(prefix, StringComparison.Ordinal))
            {
                remain = Empty;
                return false;
            }

            remain = Slice(prefix.Length);
            return true;
        }

        /// <summary>
        /// 去除片段尾部的指定片段
        /// </summary>
        /// <param name="suffix">要去掉的尾部</param>
        /// <param name="remain">去掉尾部后的剩余部分；失败时为空片段</param>
        /// <returns>是否成功</returns>
        public bool StripSuffix(StringSegment suffix, out StringSegment remain)
        {
            if (!EndsWith(suffix, StringComparison.Ordinal))
            {
                remain = Empty;
                return false;
            }

            remain = Slice(0, Length - suffix.Length);
            return true;
        }

        /// <summary>
        /// 去除片段头部和尾部的指定片段
        /// </summary>
        /// <param name="prefix">要去掉的头部</param>
        /// <param name="suffix">要去掉的尾部</param>
        /// <param name="remain">去掉头部和尾部后的剩余部分；失败时为空片段</param>
        /// <returns>是否成功</returns>
        public bool StripPrefixSuffix(
            StringSegment prefix,
            StringSegment suffix,
            out StringSegment remain)
        {
            if (!StartsWith(prefix, StringComparison.Ordinal) ||
                !EndsWith(suffix, StringComparison.Ordinal) ||
                prefix.Length + suffix.Length > Length)
            {
                remain = Empty;
                return false;
            }

            remain = Slice(
                prefix.Length,
                Length - prefix.Length - suffix.Length);

            return true;
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ValidateStartIndex(int startIndex)
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        bool MatchesAt(
            StringSegment value,
            int startIndex,
            StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.Ordinal:
                    return string.CompareOrdinal(
                        Source,
                        Offset + startIndex,
                        value.Source,
                        value.Offset,
                        value.Length) == 0;

                case StringComparison.OrdinalIgnoreCase:
                    {
                        string x = Source;
                        string y = value.Source;

                        for (int i = 0; i < value.Length; i++)
                        {
                            if (!CharsEqual(
                                    x[Offset + startIndex + i],
                                    y[value.Offset + i]))
                                return false;
                        }

                        return true;
                    }

                default:
                    return string.Compare(
                        Source,
                        Offset + startIndex,
                        value.Source,
                        value.Offset,
                        value.Length,
                        comparisonType) == 0;
            }
        }

        int IndexOfOrdinal(StringSegment value, int startIndex)
        {
            string x = source!;
            string y = value.source!;
            int valueLength = value.Length;
            int valueOffset = value.Offset;
            char first = y[valueOffset];

            // 先用 string.IndexOf(char) 跳到候选位置，再用 CompareOrdinal 校验，
            // 避免在长片段上逐字符扫描；整个过程仍然零分配。
            int searchStart = Offset + startIndex;
            int searchEnd = Offset + Length - valueLength;

            while (searchStart <= searchEnd)
            {
                int found = x.IndexOf(
                    first,
                    searchStart,
                    searchEnd - searchStart + 1);

                if (found < 0)
                    return -1;

                if (string.CompareOrdinal(
                        x,
                        found,
                        y,
                        valueOffset,
                        valueLength) == 0)
                    return found - Offset;

                searchStart = found + 1;
            }

            return -1;
        }

        int IndexOfOrdinalIgnoreCase(StringSegment value, int startIndex)
        {
            string x = source!;
            string y = value.source!;
            int valueLength = value.Length;
            int valueOffset = value.Offset;
            int lastStart = Length - valueLength;

            for (int i = startIndex; i <= lastStart; i++)
            {
                int j = 0;

                while (j < valueLength &&
                       CharsEqual(
                           x[Offset + i + j],
                           y[valueOffset + j]))
                    j++;

                if (j == valueLength)
                    return i;
            }

            return -1;
        }

        static int CompareOrdinal(StringSegment x, StringSegment y)
        {
            int minLength = Math.Min(x.Length, y.Length);

            if (minLength > 0)
            {
                int result = string.CompareOrdinal(
                    x.Source,
                    x.Offset,
                    y.Source,
                    y.Offset,
                    minLength);

                if (result != 0)
                    return result;
            }

            return x.Length.CompareTo(y.Length);
        }

        static int CompareOrdinalIgnoreCase(StringSegment x, StringSegment y)
        {
            int minLength = Math.Min(x.Length, y.Length);
            string left = x.Source;
            string right = y.Source;

            for (int i = 0; i < minLength; i++)
            {
                char a = char.ToUpperInvariant(left[x.Offset + i]);
                char b = char.ToUpperInvariant(right[y.Offset + i]);

                if (a != b)
                    return a - b;
            }

            return x.Length.CompareTo(y.Length);
        }

        static bool CharsEqual(char x, char y) =>
            x == y || char.ToUpperInvariant(x) == char.ToUpperInvariant(y);

        readonly string? source;

        /// <summary>
        /// 字符枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<char>
        {
            internal Enumerator(StringSegment segment)
            {
                this.segment = segment;
                index = 0;
                current = '\0';
                hasCurrent = false;
            }

            public char Current
            {
                get
                {
                    if (!hasCurrent)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    return current;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index >= segment.Length)
                {
                    hasCurrent = false;
                    current = '\0';
                    return false;
                }

                current = segment.source![segment.Offset + index];
                index++;
                hasCurrent = true;
                return true;
            }

            public void Reset()
            {
                index = 0;
                current = '\0';
                hasCurrent = false;
            }

            public void Dispose() { }

            readonly StringSegment segment;
            int index;
            char current;
            bool hasCurrent;
        }

        /// <summary>
        /// 字符串片段切分器
        /// </summary>
        /// <remarks>
        /// 切分器直接推进底层字符串上的区间，不创建子字符串，也不分配数组。
        /// 枚举行为与 <see cref="string.Split(char[])"/> 一致：保留空项与结尾空项，
        /// 空片段会得到一个空片段。需要跳过空项时请使用
        /// <see cref="Split0(char)"/> 或自行判断 <see cref="StringSegment.IsEmpty"/>。
        /// </remarks>
        public struct Tokenizer : IEnumerator<StringSegment>
        {
            /// <summary>
            /// 构造一个切分器
            /// </summary>
            /// <param name="segment">要切分的片段</param>
            /// <param name="separator">分隔符</param>
            public Tokenizer(StringSegment segment, char separator)
            {
                this.segment = segment;
                this.separator = separator;
                startIndex = 0;
                current = default;
                hasCurrent = false;
            }

            public StringSegment Current
            {
                get
                {
                    if (!hasCurrent)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    return current;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (startIndex > segment.Length)
                {
                    hasCurrent = false;
                    current = default;
                    return false;
                }

                int start = startIndex;
                string? text = segment.source;
                int separatorIndex = -1;

                for (int i = startIndex; i < segment.Length; i++)
                {
                    if (text![segment.Offset + i] == separator)
                    {
                        separatorIndex = i;
                        break;
                    }
                }

                if (separatorIndex >= 0)
                {
                    current = new StringSegment(
                        text!,
                        segment.Offset + start,
                        separatorIndex - start);

                    startIndex = separatorIndex + 1;
                }
                else
                {
                    current = segment.Slice(start);
                    startIndex = segment.Length + 1;
                }

                hasCurrent = true;
                return true;
            }

            public void Reset()
            {
                startIndex = 0;
                current = default;
                hasCurrent = false;
            }

            /// <summary>
            /// 返回自身，使切分器可以直接用于 foreach（结构体按值复制，不会装箱）
            /// </summary>
            public Tokenizer GetEnumerator() => this;

            public void Dispose() { }

            readonly StringSegment segment;
            readonly char separator;
            int startIndex;
            StringSegment current;
            bool hasCurrent;
        }

        /// <summary>
        /// 序数或序数忽略大小写的相等比较器
        /// </summary>
        public sealed class EqualityComparer : IEqualityComparer<StringSegment>
        {
            /// <summary>
            /// 序数比较
            /// </summary>
            public static readonly EqualityComparer Ordinal =
                new(StringComparison.Ordinal);

            /// <summary>
            /// 序数忽略大小写比较
            /// </summary>
            public static readonly EqualityComparer OrdinalIgnoreCase =
                new(StringComparison.OrdinalIgnoreCase);

            /// <summary>
            /// 构造一个相等比较器
            /// </summary>
            /// <param name="comparisonType">比较方式，只支持 Ordinal 与 OrdinalIgnoreCase</param>
            /// <exception cref="ArgumentException">不支持该比较方式</exception>
            public EqualityComparer(StringComparison comparisonType)
            {
                if (comparisonType != StringComparison.Ordinal &&
                    comparisonType != StringComparison.OrdinalIgnoreCase)
                {
                    throw new ArgumentException(
                        "Only Ordinal and OrdinalIgnoreCase are supported.",
                        nameof(comparisonType));
                }

                this.comparisonType = comparisonType;
            }

            public bool Equals(StringSegment x, StringSegment y) =>
                x.Equals(y, comparisonType);

            public int GetHashCode(StringSegment obj) =>
                obj.GetHashCode(comparisonType);

            readonly StringComparison comparisonType;
        }

        /// <summary>
        /// 序数或序数忽略大小写的排序比较器
        /// </summary>
        public sealed class Comparer : IComparer<StringSegment>
        {
            /// <summary>
            /// 序数比较
            /// </summary>
            public static readonly Comparer Ordinal =
                new(StringComparison.Ordinal);

            /// <summary>
            /// 序数忽略大小写比较
            /// </summary>
            public static readonly Comparer OrdinalIgnoreCase =
                new(StringComparison.OrdinalIgnoreCase);

            /// <summary>
            /// 构造一个排序比较器
            /// </summary>
            /// <param name="comparisonType">比较方式，只支持 Ordinal 与 OrdinalIgnoreCase</param>
            /// <exception cref="ArgumentException">不支持该比较方式</exception>
            public Comparer(StringComparison comparisonType)
            {
                if (comparisonType != StringComparison.Ordinal &&
                    comparisonType != StringComparison.OrdinalIgnoreCase)
                {
                    throw new ArgumentException(
                        "Only Ordinal and OrdinalIgnoreCase are supported.",
                        nameof(comparisonType));
                }

                this.comparisonType = comparisonType;
            }

            public int Compare(StringSegment x, StringSegment y) =>
                StringSegment.Compare(x, y, comparisonType);

            readonly StringComparison comparisonType;
        }
    }
}
