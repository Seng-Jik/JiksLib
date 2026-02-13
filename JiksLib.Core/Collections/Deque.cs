using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 双端列表
    /// 非线程安全
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public sealed class Deque<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// 索引器
        /// </summary>
        public T this[int index]
        {
            get => buffer[CalcIndex(index)];
            set => buffer[CalcIndex(index)] = value;
        }

        /// <summary>
        /// 元素数量
        /// </summary>
        public int Count { get; private set; } = 0;

        /// <summary>
        /// 容器容量
        /// </summary>
        public int Capacity => buffer.Length;

        /// <summary>
        /// 向后面添加元素
        /// </summary>
        public void Add(T x)
        {
            if (Count + 1 > buffer.Length)
                Reserve(Math.Max(4, (Count + 1) * 2));

            buffer[rear] = x;
            rear = (rear + 1) % buffer.Length;
            Count++;
        }

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(IReadOnlyList<T> items, int start, int count)
        {
            items.ThrowIfNull();

            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), "Start index cannot be negative.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            if (start + count > items.Count)
                throw new ArgumentOutOfRangeException(nameof(count), "Start index and count exceed list bounds.");

            if (Count + count > buffer.Length)
                Reserve(Math.Max(4, (Count + count) * 2));

            for (int i = 0; i < count; ++i)
                buffer[(rear + i) % buffer.Length] = items[start + i];

            rear = (rear + count) % buffer.Length;
            Count += count;
        }

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(IReadOnlyList<T> items) =>
            AddRange(items, 0, items.ThrowIfNull().Count);

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(ArraySegment<T> items)
        {
            items.Array.ThrowIfNull();

            if (Count + items.Count > buffer.Length)
                Reserve(Math.Max(4, (Count + items.Count) * 2));

            if (rear + items.Count <= buffer.Length)
            {
                Array.Copy(
                    items.Array!,
                    items.Offset,
                    buffer,
                    rear,
                    items.Count);
            }
            else
            {
                int firstPart = buffer.Length - rear;
                Array.Copy(
                    items.Array!,
                    items.Offset,
                    buffer,
                    rear,
                    firstPart);

                Array.Copy(
                    items.Array!,
                    items.Offset + firstPart,
                    buffer,
                    0,
                    items.Count - firstPart);
            }

            rear = (rear + items.Count) % buffer.Length;
            Count += items.Count;
        }

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(T[] items) => AddRange(new ArraySegment<T>(items));

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(IEnumerable<T> items) =>
            AddRange(items, items.ThrowIfNull().Count());

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(ICollection<T> items) =>
            AddRange(items, items.ThrowIfNull().Count);

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(IReadOnlyCollection<T> items) =>
            AddRange(items, items.ThrowIfNull().Count);

        void AddRange(IEnumerable<T> items, int count)
        {
            items.ThrowIfNull();

            if (Count + count > buffer.Length)
                Reserve(Math.Max(4, (Count + count) * 2));

            int i = 0;

            foreach (var item in items)
                buffer[(rear + i++) % buffer.Length] = item;

            rear = (rear + count) % buffer.Length;
            Count += count;
        }

        /// <summary>
        /// 向前面添加元素
        /// </summary>
        public void AddFront(T x)
        {
            if (Count + 1 > buffer.Length)
                Reserve(Math.Max(4, (Count + 1) * 2));

            front--;
            if (front < 0) front += buffer.Length;
            buffer[front] = x;
            Count++;
        }

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(IReadOnlyList<T> items, int start, int count)
        {
            items.ThrowIfNull();

            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), "Start index cannot be negative.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            if (start + count > items.Count)
                throw new ArgumentOutOfRangeException(nameof(count), "Start index and count exceed list bounds.");

            if (Count + count > buffer.Length)
                Reserve(Math.Max(4, (Count + count) * 2));

            // 计算新的front位置（向前移动count个位置）
            front -= count;
            if (front < 0) front += buffer.Length;

            // 复制元素到新位置
            for (int i = 0; i < count; ++i)
                buffer[(front + i) % buffer.Length] = items[start + i];

            Count += count;
        }

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(IReadOnlyList<T> items) =>
            AddRangeFront(items, 0, items.ThrowIfNull().Count);

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(ArraySegment<T> items)
        {
            items.Array!.ThrowIfNull();

            if (Count + items.Count > buffer.Length)
                Reserve(Math.Max(4, (Count + items.Count) * 2));

            // 计算新的front位置（向前移动items.Count个位置）
            front -= items.Count;
            if (front < 0) front += buffer.Length;

            // 复制数据到新位置
            if (front + items.Count <= buffer.Length)
            {
                // 连续区域
                Array.Copy(
                    items.Array!,
                    items.Offset,
                    buffer,
                    front,
                    items.Count);
            }
            else
            {
                // 分两段复制（环形缓冲区）
                int firstPart = buffer.Length - front;
                Array.Copy(
                    items.Array!,
                    items.Offset,
                    buffer,
                    front,
                    firstPart);

                Array.Copy(
                    items.Array!,
                    items.Offset + firstPart,
                    buffer,
                    0,
                    items.Count - firstPart);
            }

            Count += items.Count;
        }

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>

        public void AddRangeFront(T[] items) =>
            AddRangeFront(new ArraySegment<T>(items));

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(IEnumerable<T> items) =>
            AddRangeFront(items, items.ThrowIfNull().Count());

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(ICollection<T> items) =>
            AddRangeFront(items, items.ThrowIfNull().Count);

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(IReadOnlyCollection<T> items) =>
            AddRangeFront(items, items.ThrowIfNull().Count);

        void AddRangeFront(IEnumerable<T> items, int count)
        {
            items.ThrowIfNull();

            if (Count + count > buffer.Length)
                Reserve(Math.Max(4, (Count + count) * 2));

            // 计算新的front位置（向前移动count个位置）
            front -= count;
            if (front < 0) front += buffer.Length;

            // 复制元素到新位置
            int i = 0;
            foreach (var item in items)
                buffer[(front + i++) % buffer.Length] = item;

            Count += count;
        }

        /// <summary>
        /// 删除末尾元素
        /// </summary>
        public bool TryRemoveBack(out T? value)
        {
            if (Count <= 0)
            {
                value = default;
                return false;
            }

            rear--;
            if (rear < 0) rear += buffer.Length;
            var x = buffer[rear];
            buffer[rear] = default!;
            Count--;

            value = x;
            return true;
        }

        /// <summary>
        /// 删除开头元素并返回
        /// </summary>
        public bool TryRemoveFront(out T? value)
        {
            if (Count <= 0)
            {
                value = default;
                return false;
            }

            var x = buffer[front];
            buffer[front] = default!;
            front = (front + 1) % buffer.Length;
            Count--;

            value = x;
            return true;
        }

        /// <summary>
        /// 删除所有元素
        /// </summary>
        public void Clear()
        {
            if (Count == 0) return;

            if (front < rear)
            {
                Array.Clear(buffer, front, rear - front);
            }
            else
            {
                Array.Clear(buffer, front, buffer.Length - front);
                Array.Clear(buffer, 0, rear);
            }

            front = 0;
            rear = 0;
            Count = 0;
        }

        /// <summary>
        /// 保留至少指定数量的元素槽位
        /// 如果请求的容量小于等于当前缓冲区长度，则不进行任何操作
        /// </summary>
        /// <param name="capacity">要保留的最小元素槽数量（必须非负）</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当<paramref name="capacity"/>为负数时抛出
        /// </exception>
        public void Reserve(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            if (buffer.Length >= capacity) return;

            T[] newBuffer = new T[capacity];
            CopyTo(new(newBuffer));

            buffer = newBuffer;
            front = 0;
            rear = Count;
        }

        /// <summary>
        /// 尝试获取对应下标的值
        /// </summary>
        public bool TryIndex(int index, out T? result)
        {
            if (index >= Count || index < 0)
            {
                result = default;
                return false;
            }

            result = buffer[CalcIndex(index)];
            return true;
        }

        /// <summary>
        /// 转换为只读双端列表
        /// </summary>
        public IReadOnlyList<T> AsReadOnly() => this;

        /// <summary>
        /// 将双端列表的内容复制到一个数组片段中
        /// </summary>
        /// <param name="output"></param>
        /// <exception cref="ArgumentException"></exception>
        public void CopyTo(ArraySegment<T> output)
        {
            if (output.Array == null)
                throw new ArgumentException(
                    "Output array cannot be null.", nameof(output));

            if (output.Count < Count)
                throw new ArgumentException(
                    "Output array is too small to hold all elements.", nameof(output));

            if (front < rear)
            {
                Array.Copy(buffer, front, output.Array, output.Offset, Count);
            }
            else
            {
                Array.Copy(buffer, front, output.Array, output.Offset, buffer.Length - front);
                Array.Copy(buffer, 0, output.Array, output.Offset + buffer.Length - front, rear);
            }
        }

        /// <summary>
        /// 将双端列表的内容转换为一个数组
        /// </summary>
        public T[] ToArray()
        {
            T[] result = new T[Count];
            CopyTo(new ArraySegment<T>(result));
            return result;
        }

        /// <summary>
        /// 减少不必要的元素槽位以适应当前元素数量
        /// </summary>
        public void TrimExcess()
        {
            if (Count == 0)
            {
                buffer = Array.Empty<T>();
            }
            else if (Count < buffer.Length)
            {
                buffer = ToArray();
                front = 0;
                rear = Count;
            }
        }

        /// <summary>
        /// 根据Deque下标计算其在缓冲区内的下标
        /// </summary>
        int CalcIndex(int index)
        {
            if (index >= Count || index < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Accessing index {index} on deque with {Count} elements.");

            return (index + front) % buffer.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count != 0)
            {
                if (front < rear)
                {
                    for (int i = front; i < rear; i++)
                        yield return buffer[i];
                }
                else
                {
                    for (int i = front; i < buffer.Length; ++i)
                        yield return buffer[i];

                    for (int i = 0; i < rear; ++i)
                        yield return buffer[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        T[] buffer = Array.Empty<T>();
        int front = 0;
        int rear = 0;
    }
}