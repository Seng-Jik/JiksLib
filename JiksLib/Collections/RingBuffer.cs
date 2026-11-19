using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 定长环形缓存
    /// 容量在构造时固定，元素数量始终位于 0 到 <see cref="Capacity"/> 之间（含两端）。
    /// 最新写入的元素下标为 0，更早写入的元素下标依次递增；
    /// 写满后继续写入会覆盖最旧的元素（即当前末尾元素）。
    /// 非线程安全
    /// </summary>
    /// <remarks>
    /// 枚举期间修改缓冲区（增删、清空等）属于未定义行为：枚举器不检测版本变化，
    /// 可能跳过、重复或读到已移除的元素。需要边遍历边修改时，请先复制一份元素。
    /// </remarks>
    /// <typeparam name="T">元素类型</typeparam>
    public sealed class RingBuffer<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// 初始化定长环形缓存
        /// </summary>
        /// <param name="capacity">固定容量（必须大于 0）</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当 <paramref name="capacity"/> 小于等于 0 时抛出
        /// </exception>
        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity must be greater than zero.");

            buffer = new T[capacity];
        }

        /// <summary>
        /// 索引器
        /// 下标 0 是最新写入的元素，下标越大元素越旧
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当下标超出 <c>[0, Count)</c> 时抛出
        /// </exception>
        public T this[int index]
        {
            get => buffer[CalcIndex(index)];
            set => buffer[CalcIndex(index)] = value;
        }

        /// <summary>
        /// 元素数量（0 到 <see cref="Capacity"/> 之间）
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 固定容量
        /// </summary>
        public int Capacity => buffer.Length;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => count == 0;

        /// <summary>
        /// 是否已写满
        /// </summary>
        public bool IsFull => count == buffer.Length;

        /// <summary>
        /// 写入一个元素
        /// 元素成为下标 0；若缓存已满，最旧的元素（末尾）被覆盖
        /// </summary>
        public void Add(T item)
        {
            head = head == 0 ? buffer.Length - 1 : head - 1;
            buffer[head] = item;

            if (count < buffer.Length)
                count++;
        }

        /// <summary>
        /// 写入一个元素
        /// 元素成为下标 0；若缓存已满，最旧的元素被覆盖并通过 <paramref name="evicted"/> 返回
        /// </summary>
        /// <param name="item">要写入的元素</param>
        /// <param name="evicted">
        /// 被覆盖的最旧元素；未发生覆盖时为 <c>default</c>
        /// </param>
        /// <returns>是否发生了覆盖</returns>
        public bool Add(T item, out T? evicted)
        {
            if (count == buffer.Length)
            {
                evicted = buffer[head == 0 ? buffer.Length - 1 : head - 1];
                Add(item);
                return true;
            }

            evicted = default;
            Add(item);
            return false;
        }

        /// <summary>
        /// 写入一组元素
        /// 元素按给定顺序逐个写入，因此序列中最后一个元素最终位于下标 0；
        /// 写入数量超过剩余空间时会覆盖最旧的元素
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="items"/> 未引用数组时抛出
        /// </exception>
        public void AddRange(ArraySegment<T> items)
        {
            var array = items.Array.ThrowIfNull(nameof(items));

            if (items.Count == 0)
                return;

            if (items.Count < buffer.Length)
            {
                // 从新头部开始按输入顺序的逆序写入，使最后一个元素落在下标 0
                int newHead = head - items.Count;
                if (newHead < 0) newHead += buffer.Length;

                int position = newHead;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    buffer[position] = array[items.Offset + i];
                    position++;
                    if (position == buffer.Length) position = 0;
                }

                head = newHead;

                int remaining = buffer.Length - count;
                count = items.Count >= remaining ? buffer.Length : count + items.Count;
            }
            else
            {
                // 写入数量不少于容量，只有最后 Capacity 个元素会保留
                int newHead = (head - items.Count % buffer.Length) % buffer.Length;
                if (newHead < 0) newHead += buffer.Length;

                int start = items.Offset + items.Count - buffer.Length;
                int position = newHead;
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    buffer[position] = array[start + i];
                    position++;
                    if (position == buffer.Length) position = 0;
                }

                head = newHead;
                count = buffer.Length;
            }
        }

        /// <summary>
        /// 写入一组元素
        /// 元素按给定顺序逐个写入，因此序列中最后一个元素最终位于下标 0
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="items"/> 为 <c>null</c> 时抛出
        /// </exception>
        public void AddRange(T[] items) =>
            AddRange(new ArraySegment<T>(items));

        /// <summary>
        /// 写入一组元素
        /// 元素按给定顺序逐个写入，因此序列中最后一个元素最终位于下标 0
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="items"/> 为 <c>null</c> 时抛出
        /// </exception>
        public void AddRange(IEnumerable<T> items)
        {
            items.ThrowIfNull();

            if (items is T[] array)
            {
                AddRange(new ArraySegment<T>(array));
            }
            else if (items is ArraySegment<T> segment)
            {
                AddRange(segment);
            }
            else
            {
                foreach (var item in items)
                    Add(item);
            }
        }

        /// <summary>
        /// 尝试获取对应下标的值
        /// </summary>
        /// <returns>下标有效时返回 <c>true</c>，否则返回 <c>false</c></returns>
        public bool TryIndex(int index, out T? value)
        {
            if ((uint)index >= (uint)count)
            {
                value = default;
                return false;
            }

            value = buffer[PhysicalIndex(index)];
            return true;
        }

        /// <summary>
        /// 尝试获取最新写入的元素（下标 0）
        /// </summary>
        public bool TryPeekNewest(out T? value)
        {
            if (count == 0)
            {
                value = default;
                return false;
            }

            value = buffer[head];
            return true;
        }

        /// <summary>
        /// 尝试获取最旧的元素（末尾）
        /// </summary>
        public bool TryPeekOldest(out T? value)
        {
            if (count == 0)
            {
                value = default;
                return false;
            }

            int index = head + count - 1;
            if (index >= buffer.Length) index -= buffer.Length;

            value = buffer[index];
            return true;
        }

        /// <summary>
        /// 移除并返回最新写入的元素（下标 0）
        /// </summary>
        /// <returns>缓存非空时返回 <c>true</c>，否则返回 <c>false</c></returns>
        public bool TryRemoveNewest(out T? value)
        {
            if (count == 0)
            {
                value = default;
                return false;
            }

            value = buffer[head];
            buffer[head] = default!;

            head++;
            if (head == buffer.Length) head = 0;
            count--;

            if (count == 0) head = 0;
            return true;
        }

        /// <summary>
        /// 移除并返回最旧的元素（末尾）
        /// </summary>
        /// <returns>缓存非空时返回 <c>true</c>，否则返回 <c>false</c></returns>
        public bool TryRemoveOldest(out T? value)
        {
            if (count == 0)
            {
                value = default;
                return false;
            }

            int index = head + count - 1;
            if (index >= buffer.Length) index -= buffer.Length;

            value = buffer[index];
            buffer[index] = default!;
            count--;

            if (count == 0) head = 0;
            return true;
        }

        /// <summary>
        /// 查找元素，返回其下标（0 是最新写入的元素）
        /// </summary>
        /// <returns>找不到时返回 -1</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < count; i++)
            {
                if (comparer.Equals(buffer[PhysicalIndex(i)], item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 从末尾开始查找元素，返回其下标（0 是最新写入的元素）
        /// </summary>
        /// <returns>找不到时返回 -1</returns>
        public int LastIndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = count - 1; i >= 0; i--)
            {
                if (comparer.Equals(buffer[PhysicalIndex(i)], item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 是否包含指定元素
        /// </summary>
        public bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// 移除第一个匹配的元素（从最新到最旧查找）
        /// </summary>
        /// <returns>找到并移除时返回 <c>true</c>，否则返回 <c>false</c></returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 移除指定下标的元素，后续元素前移以保持顺序
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当下标超出 <c>[0, Count)</c> 时抛出
        /// </exception>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)count)
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Accessing index {index} on ring buffer with {count} elements.");

            for (int i = index; i < count - 1; i++)
                buffer[PhysicalIndex(i)] = buffer[PhysicalIndex(i + 1)];

            buffer[PhysicalIndex(count - 1)] = default!;
            count--;

            if (count == 0) head = 0;
        }

        /// <summary>
        /// 删除所有元素
        /// </summary>
        public void Clear()
        {
            if (count > 0)
            {
                int firstPart = buffer.Length - head;
                if (firstPart > count) firstPart = count;

                Array.Clear(buffer, head, firstPart);

                if (count > firstPart)
                    Array.Clear(buffer, 0, count - firstPart);
            }

            head = 0;
            count = 0;
        }

        /// <summary>
        /// 将缓存内容复制到数组片段中，顺序为从最新到最旧
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="output"/> 未引用数组时抛出
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="output"/> 的可用空间小于 <see cref="Count"/> 时抛出
        /// </exception>
        public void CopyTo(ArraySegment<T> output)
        {
            var outputArray = output.Array.ThrowIfNull(nameof(output));

            if (output.Count < count)
                throw new ArgumentException(
                    "Output array is too small to hold all elements.", nameof(output));

            if (count == 0) return;

            int firstPart = buffer.Length - head;
            if (firstPart > count) firstPart = count;

            Array.Copy(buffer, head, outputArray, output.Offset, firstPart);

            if (count > firstPart)
            {
                Array.Copy(
                    buffer,
                    0,
                    outputArray,
                    output.Offset + firstPart,
                    count - firstPart);
            }
        }

        /// <summary>
        /// 将缓存内容复制到数组的指定位置，顺序为从最新到最旧
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="array"/> 为 <c>null</c> 时抛出
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当 <paramref name="arrayIndex"/> 为负数或大于数组长度时抛出
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 当目标数组的剩余空间小于 <see cref="Count"/> 时抛出
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            array.ThrowIfNull();

            if ((uint)arrayIndex > (uint)array.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(arrayIndex),
                    "Array index is out of range.");

            if (array.Length - arrayIndex < count)
                throw new ArgumentException(
                    "Destination array is too small to hold all elements.", nameof(array));

            CopyTo(new ArraySegment<T>(array, arrayIndex, count));
        }

        /// <summary>
        /// 将缓存内容转换为数组，顺序为从最新到最旧
        /// </summary>
        public T[] ToArray()
        {
            if (count == 0) return Array.Empty<T>();

            T[] result = new T[count];
            CopyTo(new ArraySegment<T>(result));
            return result;
        }

        /// <summary>
        /// 转换为只读列表视图
        /// </summary>
        /// <remarks>
        /// 返回的是当前实例本身，不会创建只读包装器；只读性仅由返回的接口类型保证。
        /// 调用方不应把它强转回 <see cref="RingBuffer{T}"/> 后修改，否则会绕过只读语义。
        /// </remarks>
        public IReadOnlyList<T> AsReadOnly() => this;

        /// <summary>
        /// 枚举器
        /// 枚举顺序为从最新到最旧
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            internal Enumerator(RingBuffer<T> ringBuffer)
            {
                buffer = ringBuffer.buffer;
                start = ringBuffer.head;
                count = ringBuffer.count;
                position = -1;
            }

            public T Current
            {
                get
                {
                    if ((uint)position >= (uint)count)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    int index = start + position;
                    if (index >= buffer.Length) index -= buffer.Length;

                    return buffer[index];
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (position < count)
                    position++;

                return position < count;
            }

            public void Reset() => position = -1;

            public void Dispose() { }

            readonly T[] buffer;
            readonly int start;
            readonly int count;
            int position;
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Details

        /// <summary>
        /// 根据逻辑下标计算缓冲区中的物理下标（不校验范围）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int PhysicalIndex(int index)
        {
            int i = head + index;
            if (i >= buffer.Length) i -= buffer.Length;
            return i;
        }

        /// <summary>
        /// 根据逻辑下标计算缓冲区中的物理下标并校验范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CalcIndex(int index)
        {
            if ((uint)index >= (uint)count)
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Accessing index {index} on ring buffer with {count} elements.");

            return PhysicalIndex(index);
        }

        readonly T[] buffer;
        int head;   // 逻辑下标 0 对应的物理下标
        int count;

        #endregion
    }
}
