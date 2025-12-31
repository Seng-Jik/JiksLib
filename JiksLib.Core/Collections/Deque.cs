using System;
using System.Collections;
using System.Collections.Generic;

namespace JiksLib.Collections
{
    /// <summary>
    /// 双端列表
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
                Reserve((Count + 1) * 2);

            buffer[rear] = x;
            rear = (rear + 1) % buffer.Length;
            Count++;
        }

        /// <summary>
        /// 向前面添加元素
        /// </summary>
        public void AddFront(T x)
        {
            if (Count + 1 > buffer.Length)
                Reserve((Count + 1) * 2);

            front--;
            if (front < 0) front += buffer.Length;
            buffer[front] = x;
            Count++;
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
            for (int i = 0; i < buffer.Length; ++i)
                buffer[i] = default!;

            front = 0;
            rear = 0;
            Count = 0;
        }

        /// <summary>
        /// 保留内存
        /// </summary>
        /// <param name="capacity">要保留的元素槽数量</param>
        public void Reserve(int capacity)
        {
            if (buffer.Length >= capacity) return;

            int newCapacity = Math.Max(buffer.Length * 2, capacity);
            T[] newBuffer = new T[newCapacity];

            for (int i = 0; i < Count; ++i)
                newBuffer[i] = this[i];

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
        /// 根据Deque下标计算其在缓冲区内的下标
        /// </summary>
        int CalcIndex(int index)
        {
            if (index >= Count || index < 0)
                throw new ArgumentOutOfRangeException(
                    "Accessing index " + index + " on deque with " +
                    Count + " elements.");

            return (index + front) % buffer.Length;
        }
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        T[] buffer = new T[4];
        int front = 0;
        int rear = 0;
    }
}