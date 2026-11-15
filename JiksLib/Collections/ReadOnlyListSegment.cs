using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 列表片段：对 <see cref="IReadOnlyList{T}"/> 的一段只读视图。
    /// 切片不会复制底层列表；只有 <see cref="ToArray"/> 等明确返回新数组的成员才会分配。
    /// </summary>
    /// <remarks>
    /// 该类型是值类型，默认值表示空片段。构造时不允许底层列表为 null；
    /// 默认片段的 <see cref="Source"/> 返回 <see cref="Array.Empty{T}"/>，所有成员都按空片段处理。
    /// 片段是底层列表的实时视图，底层列表内容的变化会反映到片段上。
    /// 相等性按“底层列表是同一实例且区间相同”判断，不比较元素内容。
    /// </remarks>
    /// <typeparam name="T">元素类型</typeparam>
    public readonly struct ReadOnlyListSegment<T> :
        IEquatable<ReadOnlyListSegment<T>>,
        IReadOnlyList<T>
    {
        /// <summary>
        /// 空片段
        /// </summary>
        public static readonly ReadOnlyListSegment<T> Empty = default;

        /// <summary>
        /// 在底层列表中的起始下标
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// 片段长度
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// 用整个列表构造片段
        /// </summary>
        /// <param name="source">底层列表</param>
        /// <exception cref="ArgumentNullException">列表为 null</exception>
        public ReadOnlyListSegment(IReadOnlyList<T> source)
        {
            this.source = source.ThrowIfNull(nameof(source));
            Offset = 0;
            Count = source.Count;
        }

        /// <summary>
        /// 用列表的指定区间构造片段
        /// </summary>
        /// <param name="source">底层列表</param>
        /// <param name="offset">起始下标</param>
        /// <param name="count">长度</param>
        /// <exception cref="ArgumentNullException">列表为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">区间超出列表范围</exception>
        public ReadOnlyListSegment(IReadOnlyList<T> source, int offset, int count)
        {
            this.source = source.ThrowIfNull(nameof(source));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset > source.Count - count)
                throw new ArgumentOutOfRangeException(nameof(count));

            Offset = offset;
            Count = count;
        }

        /// <summary>
        /// 从数组隐式构造片段
        /// </summary>
        /// <param name="source">数组</param>
        /// <exception cref="ArgumentNullException">数组为 null</exception>
        public static implicit operator ReadOnlyListSegment<T>(T[] source) =>
            new(source);

        /// <summary>
        /// 底层列表；默认片段返回空数组
        /// </summary>
        public IReadOnlyList<T> Source => source ?? Array.Empty<T>();

        /// <summary>
        /// 是否为空片段
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// 获取片段中指定位置上的元素
        /// </summary>
        /// <param name="index">片段内下标</param>
        /// <exception cref="IndexOutOfRangeException">下标超出片段范围</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException(
                        "Index must be within the segment.");

                return source[Offset + index];
            }
        }

        int IReadOnlyCollection<T>.Count => Count;

        /// <summary>
        /// 获取一个片段
        /// </summary>
        /// <param name="index">起始下标</param>
        /// <exception cref="ArgumentOutOfRangeException">下标超出片段范围</exception>
        public ReadOnlyListSegment<T> Slice(int index)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return SliceCore(index, Count - index);
        }

        /// <summary>
        /// 获取一个片段
        /// </summary>
        /// <param name="index">起始下标</param>
        /// <param name="count">长度</param>
        /// <exception cref="ArgumentOutOfRangeException">区间超出片段范围</exception>
        public ReadOnlyListSegment<T> Slice(int index, int count)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0 || count > Count - index)
                throw new ArgumentOutOfRangeException(nameof(count));

            return SliceCore(index, count);
        }

        /// <summary>
        /// 将片段复制到数组
        /// </summary>
        /// <param name="destination">目标数组</param>
        /// <exception cref="ArgumentNullException">目标数组为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">目标数组容量不足</exception>
        public void CopyTo(T[] destination) => CopyTo(destination, 0);

        /// <summary>
        /// 将片段复制到数组
        /// </summary>
        /// <param name="destination">目标数组</param>
        /// <param name="destinationIndex">目标数组中的起始下标</param>
        /// <exception cref="ArgumentNullException">目标数组为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">目标区间不足</exception>
        public void CopyTo(T[] destination, int destinationIndex)
        {
            destination.ThrowIfNull();

            if (destinationIndex < 0 ||
                destinationIndex > destination.Length - Count)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

            CopyToCore(destination, destinationIndex);
        }

        /// <summary>
        /// 将片段复制为一个新数组
        /// </summary>
        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();

            var result = new T[Count];
            CopyToCore(result, 0);
            return result;
        }

        /// <summary>
        /// 判断两个片段是否相等：底层列表为同一实例且区间相同
        /// </summary>
        public bool Equals(ReadOnlyListSegment<T> other) =>
            ReferenceEquals(Source, other.Source) &&
            Offset == other.Offset &&
            Count == other.Count;

        public override bool Equals(object? obj) =>
            obj is ReadOnlyListSegment<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RuntimeHelpers.GetHashCode(Source);
                hash = (hash * 397) ^ Offset;
                hash = (hash * 397) ^ Count;
                return hash;
            }
        }

        /// <summary>
        /// 判断两个片段是否相等
        /// </summary>
        public static bool operator ==(
            ReadOnlyListSegment<T> left,
            ReadOnlyListSegment<T> right) =>
            left.Equals(right);

        /// <summary>
        /// 判断两个片段是否不相等
        /// </summary>
        public static bool operator !=(
            ReadOnlyListSegment<T> left,
            ReadOnlyListSegment<T> right) =>
            !left.Equals(right);

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ReadOnlyListSegment<T> SliceCore(int index, int count)
        {
            if (source == null)
                return Empty;

            return new ReadOnlyListSegment<T>(
                source,
                Offset + index,
                count);
        }

        void CopyToCore(T[] destination, int destinationIndex)
        {
            if (Count == 0)
                return;

            switch (source)
            {
                case T[] array:
                    Array.Copy(
                        array,
                        Offset,
                        destination,
                        destinationIndex,
                        Count);
                    break;

                case List<T> list:
                    list.CopyTo(Offset, destination, destinationIndex, Count);
                    break;

                default:
                    for (int i = 0; i < Count; i++)
                        destination[destinationIndex + i] = source[Offset + i];
                    break;
            }
        }

        readonly IReadOnlyList<T> source;

        /// <summary>
        /// 元素枚举器
        /// </summary>
        /// <remarks>
        /// 枚举器需要推进内部游标，因此是可变结构体而非 readonly struct；
        /// 这样在 foreach 中不会装箱，也不会产生分配。
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal Enumerator(ReadOnlyListSegment<T> segment)
            {
                source = segment.Source;
                offset = segment.Offset;
                count = segment.Count;
                index = -1;
                current = default!;
            }

            public T Current
            {
                get
                {
                    if ((uint)index >= (uint)count)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    return current;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                int nextIndex = index + 1;

                if ((uint)nextIndex >= (uint)count)
                {
                    index = count;
                    current = default!;
                    return false;
                }

                index = nextIndex;
                current = source[offset + nextIndex];
                return true;
            }

            public void Reset()
            {
                index = -1;
                current = default!;
            }

            public void Dispose() { }

            readonly IReadOnlyList<T> source;
            readonly int offset;
            readonly int count;
            int index;
            T current;
        }
    }
}
