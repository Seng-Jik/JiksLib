using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 双端列表
    /// 非线程安全
    /// </summary>
    /// <remarks>
    /// 枚举期间修改队列（增删、扩容、清空等）属于未定义行为：枚举器不检测版本变化，
    /// 可能跳过、重复或读到已移除的元素。需要边遍历边修改时，请先复制一份元素。
    /// </remarks>
    /// <typeparam name="T">元素类型</typeparam>
    public sealed class Deque<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// 初始化双端列表
        /// </summary>
        public Deque() { }

        /// <summary>
        /// 使用指定容量初始化双端列表
        /// 预分配后，元素数量不超过<paramref name="capacity"/>时添加元素不会触发扩容
        /// </summary>
        /// <param name="capacity">初始容量（必须非负）</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当<paramref name="capacity"/>为负数时抛出
        /// </exception>
        public Deque(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            buffer = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        }

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
        public int Count { get; private set; }

        /// <summary>
        /// 容器容量
        /// </summary>
        public int Capacity => buffer.Length;

        /// <summary>
        /// 向后面添加元素
        /// </summary>
        public void Add(T x)
        {
            if (Count == buffer.Length)
                EnsureCapacity(Growth((long)Count + 1));

            buffer[rear] = x;
            rear++;
            if (rear == buffer.Length) rear = 0;
            Count++;
        }

        /// <summary>
        /// 添加一组元素到末尾
        /// </summary>
        public void AddRange(ArraySegment<T> items)
        {
            items.Array.ThrowIfNull(nameof(items));

            if (items.Count == 0)
                return;

            if ((long)Count + items.Count > buffer.Length)
                EnsureCapacity(Growth((long)Count + items.Count));

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
        /// 只枚举一次源序列：即使序列只能枚举一次也不会写坏环形缓冲
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            items.ThrowIfNull(nameof(items));

            if (items is T[] array)
            {
                AddRange(new ArraySegment<T>(array));
                return;
            }

            if (items is ArraySegment<T> segment)
            {
                AddRange(segment);
                return;
            }

            // 已知数量时一次性预留，未知时在循环中按需扩容。
            int knownCount = items switch
            {
                IReadOnlyCollection<T> roc => roc.Count,
                ICollection<T> c => c.Count,
                ICollection c => c.Count,
                _ => -1,
            };

            if (knownCount > 0 && (long)Count + knownCount > buffer.Length)
                EnsureCapacity(Growth((long)Count + knownCount));

            foreach (var item in items)
            {
                if (Count == buffer.Length)
                    EnsureCapacity(Growth((long)Count + 1));

                buffer[rear] = item;
                rear++;
                if (rear == buffer.Length) rear = 0;
                Count++;
            }
        }

        /// <summary>
        /// 向前面添加元素
        /// </summary>
        public void AddFront(T x)
        {
            if (Count == buffer.Length)
                EnsureCapacity(Growth((long)Count + 1));

            front--;
            if (front < 0) front += buffer.Length;
            buffer[front] = x;
            Count++;
        }

        /// <summary>
        /// 添加一组元素到开头
        /// </summary>
        public void AddRangeFront(ArraySegment<T> items)
        {
            items.Array!.ThrowIfNull(nameof(items));

            if (items.Count == 0)
                return;

            if ((long)Count + items.Count > buffer.Length)
                EnsureCapacity(Growth((long)Count + items.Count));

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
        /// 只枚举一次源序列
        /// </summary>
        public void AddRangeFront(IEnumerable<T> items)
        {
            items.ThrowIfNull(nameof(items));

            if (items is T[] array)
            {
                AddRangeFront(new ArraySegment<T>(array));
                return;
            }

            if (items is ArraySegment<T> segment)
            {
                AddRangeFront(segment);
                return;
            }

            // 前插需要先知道元素数量才能确定新的 front，因此物化一次。
            AddRangeFrontCore(new List<T>(items));
        }

        /// <summary>
        /// 从已知长度的列表前插；只用于 <see cref="AddRangeFront(IEnumerable{T})"/> 的慢路径。
        /// </summary>
        void AddRangeFrontCore(List<T> items)
        {
            if (items.Count == 0)
                return;

            if ((long)Count + items.Count > buffer.Length)
                EnsureCapacity(Growth((long)Count + items.Count));

            front -= items.Count;
            if (front < 0) front += buffer.Length;

            for (int i = 0; i < items.Count; ++i)
                buffer[(front + i) % buffer.Length] = items[i];

            Count += items.Count;
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
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            if (buffer.Length >= capacity) return;

            capacity = RoundUpCapacity(capacity);

            T[] newBuffer = new T[capacity];
            CopyTo(new(newBuffer));

            buffer = newBuffer;
            front = 0;
            rear = Count;
        }

        /// <summary>
        /// 计算扩容后的目标容量（至少容纳 <paramref name="required"/> 个元素）。
        /// 使用 long 计算避免整数溢出，并在超过 int.MaxValue 时截断。
        /// </summary>
        static int Growth(long required)
        {
            long target = Math.Max(4L, required * 2);
            return target > int.MaxValue ? int.MaxValue : (int)target;
        }

        /// <summary>
        /// 把容量向上取整到 4 的倍数（保持原有扩容粒度），并防止溢出。
        /// </summary>
        static int RoundUpCapacity(int capacity)
        {
            long rounded = ((long)capacity / 4 + 1) * 4;
            return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
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
        /// 转换为只读双端列表视图
        /// </summary>
        /// <remarks>
        /// 返回的是当前实例本身，不会创建只读包装器；只读性仅由返回的接口类型保证。
        /// 调用方不应把它强转回 <see cref="Deque{T}"/> 后修改，否则会绕过只读语义。
        /// </remarks>
        public IReadOnlyList<T> AsReadOnly() => this;

        /// <summary>
        /// 将双端列表的内容复制到一个数组片段中
        /// </summary>
        /// <param name="output"></param>
        /// <exception cref="ArgumentException"></exception>
        public void CopyTo(ArraySegment<T> output)
        {
            var outputArray = output.Array.ThrowIfNull(nameof(output));

            if (output.Count < Count)
                throw new ArgumentException(
                    "Output array is too small to hold all elements.", nameof(output));

            // 空双端队列的 front/rear 可能不相等（例如 Clear 后再次前插再移除），
            // 下面的环形分段复制假设"有元素"，因此必须先返回。
            if (Count == 0)
                return;

            if (front < rear)
            {
                Array.Copy(buffer, front, outputArray, output.Offset, Count);
            }
            else
            {
                Array.Copy(buffer, front, outputArray, output.Offset, buffer.Length - front);
                Array.Copy(buffer, 0, outputArray, output.Offset + buffer.Length - front, rear);
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
                front = 0;
                rear = 0;
            }
            else if (Count < buffer.Length)
            {
                buffer = ToArray();
                front = 0;
                rear = Count;
            }
        }

        #region Details

        /// <summary>
        /// 根据Deque下标计算其在缓冲区内的下标
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CalcIndex(int index)
        {
            if (index >= Count || index < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Accessing index {index} on deque with {Count} elements.");

            return (index + front) % buffer.Length;
        }

        /// <summary>
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            internal Enumerator(Deque<T> deque)
            {
                count = deque.Count;
                front = deque.front;
                buffer = deque.buffer;
                position = -1;
                started = false;
            }

            public T Current
            {
                get
                {
                    if (position < 0 || position >= count)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    int index = (front + position) % buffer.Length;
                    return buffer[index];
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    started = true;
                    position = 0;
                    return position < count;
                }
                else
                {
                    if (position >= count) return false;
                    position++;
                    return position < count;
                }
            }

            public void Reset()
            {
                position = -1;
                started = false;
            }

            public void Dispose() { }

            readonly int count;
            readonly int front;
            readonly T[] buffer;
            int position; // 0到count，position == count表示结束
            bool started;

        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        T[] buffer = Array.Empty<T>();
        int front;
        int rear;

        #endregion
    }
}
