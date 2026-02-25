using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Collections
{
    /// <summary>
    /// 基于数组的链表实现
    /// 非线程安全
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public sealed class ArrayLinkedList<T> :
        ICollection<T>,
        IReadOnlyCollection<T>,
        IEnumerable<T>
    {
        /// <summary>
        /// 元素数量
        /// </summary>
        public int Count { get; private set; } = 0;

        /// <summary>
        /// 第一个元素的槽位索引，如果链表为空则为-1
        /// </summary>
        public int FirstSlot { get; private set; } = -1;

        /// <summary>
        /// 最后一个元素的槽位索引，如果链表为空则为-1
        /// </summary>
        public int LastSlot { get; private set; } = -1;

        /// <summary>
        /// 容量（数组长度）
        /// </summary>
        public int Capacity => array.Length;

        /// <summary>
        /// 初始化ArrayLinkedList
        /// </summary>
        public ArrayLinkedList()
        {
            array = Array.Empty<Node>();
        }

        /// <summary>
        /// 使用指定容量初始化ArrayLinkedList
        /// </summary>
        /// <param name="capacity">初始容量</param>
        public ArrayLinkedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            array = new Node[capacity];
        }

        /// <summary>
        /// 从另一个ArrayLinkedList初始化（浅拷贝）
        /// </summary>
        /// <param name="other">要拷贝的ArrayLinkedList</param>
        public ArrayLinkedList(ArrayLinkedList<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            array = new Node[other.array.Length];
            Array.Copy(other.array, array, other.arrayUsedCount);
            arrayUsedCount = other.arrayUsedCount;
            firstFreeNode = other.firstFreeNode;
            FirstSlot = other.FirstSlot;
            LastSlot = other.LastSlot;
            Count = other.Count;
        }

        /// <summary>
        /// 从可枚举集合初始化ArrayLinkedList
        /// </summary>
        /// <param name="collection">源集合</param>
        public ArrayLinkedList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            // 获取元素数量
            int count = collection switch
            {
                ICollection<T> c => c.Count,
                IReadOnlyCollection<T> roc => roc.Count,
                _ => collection.Count()
            };

            array = new Node[count];
            int index = 0;
            foreach (var item in collection)
            {
                array[index].Used = true;
                array[index].Value = item;
                array[index].Prev = index - 1;
                array[index].Next = index + 1;
                index++;
            }

            if (count > 0)
            {
                array[0].Prev = -1;
                array[count - 1].Next = -1;
                FirstSlot = 0;
                LastSlot = count - 1;
            }
            else
            {
                FirstSlot = -1;
                LastSlot = -1;
            }

            arrayUsedCount = count;
            firstFreeNode = -1;
            Count = count;
        }

        /// <summary>
        /// 获取指定槽位的元素及其前后槽位索引
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="nextSlotIndex">下一个槽位的索引，如果不存在则为-1</param>
        /// <param name="prevSlotIndex">上一个槽位的索引，如果不存在则为-1</param>
        /// <returns>槽位中的元素值</returns>
        /// <exception cref="ArgumentOutOfRangeException">槽位索引无效时抛出</exception>
        public T Get(int slotIndex, out int nextSlotIndex, out int prevSlotIndex)
        {
            EnsureSlotUsed(slotIndex);
            var slot = array[slotIndex];
            nextSlotIndex = slot.Next;
            prevSlotIndex = slot.Prev;
            return slot.Value;
        }

        /// <summary>
        /// 在链表开头添加元素
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>新元素的槽位索引</returns>
        public int AddFirst(T item)
        {
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;
            array[newSlotIndex].Prev = -1;
            array[newSlotIndex].Next = FirstSlot;

            if (FirstSlot != -1)
                array[FirstSlot].Prev = newSlotIndex;

            FirstSlot = newSlotIndex;

            if (LastSlot == -1)
                LastSlot = newSlotIndex;

            Count++;
            return newSlotIndex;
        }

        /// <summary>
        /// 在链表末尾添加元素
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>新元素的槽位索引</returns>
        public int AddLast(T item)
        {
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;
            array[newSlotIndex].Prev = LastSlot;
            array[newSlotIndex].Next = -1;

            if (LastSlot != -1)
                array[LastSlot].Next = newSlotIndex;

            LastSlot = newSlotIndex;

            if (FirstSlot == -1)
                FirstSlot = newSlotIndex;

            Count++;
            return newSlotIndex;
        }

        /// <summary>
        /// 在指定槽位之后添加元素
        /// </summary>
        /// <param name="slotIndex">参考槽位索引</param>
        /// <param name="item">要添加的元素</param>
        /// <returns>新元素的槽位索引</returns>
        /// <exception cref="ArgumentOutOfRangeException">槽位索引无效时抛出</exception>
        public int AddAfter(int slotIndex, T item)
        {
            EnsureSlotUsed(slotIndex);
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;

            int nextSlotIndex = array[slotIndex].Next;
            array[newSlotIndex].Prev = slotIndex;
            array[newSlotIndex].Next = nextSlotIndex;
            array[slotIndex].Next = newSlotIndex;

            if (nextSlotIndex != -1)
                array[nextSlotIndex].Prev = newSlotIndex;

            if (LastSlot == slotIndex)
                LastSlot = newSlotIndex;

            Count++;
            return newSlotIndex;
        }

        /// <summary>
        /// 在指定槽位之前添加元素
        /// </summary>
        /// <param name="slotIndex">参考槽位索引</param>
        /// <param name="item">要添加的元素</param>
        /// <returns>新元素的槽位索引</returns>
        /// <exception cref="ArgumentOutOfRangeException">槽位索引无效时抛出</exception>
        public int AddBefore(int slotIndex, T item)
        {
            EnsureSlotUsed(slotIndex);
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;

            int prevSlotIndex = array[slotIndex].Prev;
            array[newSlotIndex].Next = slotIndex;
            array[newSlotIndex].Prev = prevSlotIndex;
            array[slotIndex].Prev = newSlotIndex;

            if (prevSlotIndex != -1)
                array[prevSlotIndex].Next = newSlotIndex;

            if (FirstSlot == slotIndex)
                FirstSlot = newSlotIndex;

            Count++;
            return newSlotIndex;
        }

        /// <summary>
        /// 移除链表中的所有元素
        /// </summary>
        public void Clear()
        {
            Array.Clear(array, 0, arrayUsedCount);
            FirstSlot = -1;
            LastSlot = -1;
            arrayUsedCount = 0;
            firstFreeNode = -1;
            Count = 0;
        }

        /// <summary>
        /// 确定集合中是否包含特定值
        /// </summary>
        /// <param name="item">要定位的对象</param>
        /// <returns>如果在集合中找到该项，则为 true；否则为 false</returns>
        public bool Contains(T item) => FindSlot(item) != -1;

        /// <summary>
        /// 将集合的元素复制到 ArraySegment 中
        /// </summary>
        /// <param name="segment">目标数组片段</param>
        /// <exception cref="ArgumentException">源集合中的元素数大于目标数组片段的容量</exception>
        public void CopyTo(ArraySegment<T> segment)
        {
            if (segment.Array == null)
                throw new ArgumentNullException(
                    nameof(segment),
                    "Array segment's array cannot be null.");

            if (segment.Count < Count)
                throw new ArgumentException(
                    "Destination array segment is not long enough to copy all the items.");

            int current = FirstSlot;
            int destIndex = segment.Offset;
            while (current != -1)
            {
                segment.Array[destIndex++] = array[current].Value;
                current = array[current].Next;
            }
        }

        /// <summary>
        /// 将集合的元素复制到数组中
        /// </summary>
        public void CopyTo(T[] array) => CopyTo(new ArraySegment<T>(array));

        /// <summary>
        /// 查找指定元素的第一个槽位索引
        /// </summary>
        /// <param name="item">要查找的元素</param>
        /// <returns>元素的槽位索引，如果未找到则返回-1</returns>
        public int FindSlot(T item)
        {
            int current = FirstSlot;
            while (current != -1)
            {
                if (EqualityComparer<T>.Default.Equals(array[current].Value, item))
                    return current;
                current = array[current].Next;
            }

            return -1;
        }

        /// <summary>
        /// 查找指定元素的最后一个槽位索引
        /// </summary>
        /// <param name="item">要查找的元素</param>
        /// <returns>元素的槽位索引，如果未找到则返回-1</returns>
        public int FindLastSlot(T item)
        {
            int current = LastSlot;
            while (current != -1)
            {
                if (EqualityComparer<T>.Default.Equals(array[current].Value, item))
                    return current;
                current = array[current].Prev;
            }

            return -1;
        }

        /// <summary>
        /// 从集合中移除特定对象的第一个匹配项
        /// </summary>
        /// <param name="item">要从集合中移除的对象</param>
        /// <returns>如果成功移除 item，则为 true；否则为 false。如果在原始集合中没有找到 item，该方法也会返回 false。</returns>
        public bool Remove(T item)
        {
            int slotIndex = FindSlot(item);
            if (slotIndex == -1)
                return false;

            RemoveBySlot(slotIndex, out _, out _);
            return true;
        }

        /// <summary>
        /// 通过槽位ID移除元素
        /// </summary>
        /// <param name="slotIndex">要移除的槽位索引</param>
        /// <exception cref="ArgumentOutOfRangeException">槽位索引无效</exception>
        public T RemoveBySlot(int slotIndex, out int prevSlot, out int nextSlot)
        {
            EnsureSlotUsed(slotIndex);

            var node = array[slotIndex];
            prevSlot = node.Prev;
            nextSlot = node.Next;
            var v = node.Value;

            // 更新前一个节点的Next指针
            if (node.Prev != -1)
                array[node.Prev].Next = node.Next;
            else
                FirstSlot = node.Next; // 这是第一个节点

            // 更新后一个节点的Prev指针
            if (node.Next != -1)
                array[node.Next].Prev = node.Prev;
            else
                LastSlot = node.Prev; // 这是最后一个节点

            // 释放槽位
            FreeSlot(slotIndex);
            Count--;

            return v;
        }

        /// <summary>
        /// 尝试移除并返回第一个元素
        /// </summary>
        /// <param name="item">被移除的元素</param>
        /// <returns>如果成功移除元素，则为 true；如果集合为空，则为 false</returns>
        public bool TryRemoveFirst(out T? item)
        {
            if (FirstSlot == -1)
            {
                item = default;
                return false;
            }

            item = array[FirstSlot].Value;
            RemoveBySlot(FirstSlot, out _, out _);
            return true;
        }

        /// <summary>
        /// 尝试移除并返回最后一个元素
        /// </summary>
        /// <param name="item">被移除的元素</param>
        /// <returns>如果成功移除元素，则为 true；如果集合为空，则为 false</returns>
        public bool TryRemoveLast(out T? item)
        {
            if (LastSlot == -1)
            {
                item = default;
                return false;
            }

            item = array[LastSlot].Value;
            RemoveBySlot(LastSlot, out _, out _);
            return true;
        }

        /// <summary>
        /// 确保容量至少为指定值
        /// </summary>
        /// <param name="capacity">最小容量</param>
        /// <exception cref="ArgumentOutOfRangeException">容量为负数时抛出</exception>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            if (array.Length >= capacity) return;

            capacity = (capacity / 4 + 1) * 4;
            Array.Resize(ref array, capacity);
        }

        /// <summary>
        /// 返回当前集合的只读视图
        /// </summary>
        /// <returns>只读集合</returns>
        public IReadOnlyCollection<T> AsReadOnly() => this;

        /// <summary>
        /// 返回枚举器
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// 返回反向枚举器
        /// </summary>
        public ReversedEnumerator GetReversedEnumerator() => new(this);

        /// <summary>
        /// 将集合的元素复制到新数组中
        /// </summary>
        /// <returns>包含集合元素的新数组</returns>
        public T[] ToArray()
        {
            T[] result = new T[Count];
            CopyTo(result);
            return result;
        }

        /// <summary>
        /// 将容量设置为集合中的实际元素数
        /// </summary>
        public void TrimExcess()
        {
            if (Count == 0)
            {
                array = Array.Empty<Node>();
                arrayUsedCount = 0;
                firstFreeNode = -1;
            }
            else if (array.Length > Count)
            {
                // 创建新数组并复制元素
                Node[] newArray = new Node[Count];
                int newIndex = 0;
                int current = FirstSlot;

                while (current != -1)
                {
                    newArray[newIndex].Used = true;
                    newArray[newIndex].Value = array[current].Value;
                    newArray[newIndex].Prev = newIndex - 1;
                    newArray[newIndex].Next = newIndex + 1;
                    current = array[current].Next;
                    newIndex++;
                }

                if (Count > 0)
                {
                    newArray[0].Prev = -1;
                    newArray[Count - 1].Next = -1;
                }

                array = newArray;
                arrayUsedCount = Count;
                firstFreeNode = -1;
                FirstSlot = Count > 0 ? 0 : -1;
                LastSlot = Count > 0 ? Count - 1 : -1;
            }
        }

        void EnsureSlotUsed(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= arrayUsedCount || !array[slotIndex].Used)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        int AllocateSlot()
        {
            if (firstFreeNode != -1)
            {
                int slotIndex = firstFreeNode;
                firstFreeNode = array[slotIndex].Next;
                array[slotIndex].Used = true;
                return slotIndex;
            }
            else
            {
                if (arrayUsedCount == array.Length)
                    EnsureCapacity(Math.Max(4, (arrayUsedCount + 1) * 2));

                int slotIndex = arrayUsedCount++;
                array[slotIndex].Used = true;
                return slotIndex;
            }
        }

        void FreeSlot(int slotIndex)
        {
            EnsureSlotUsed(slotIndex);
            array[slotIndex].Used = false;
            array[slotIndex].Next = firstFreeNode;
            firstFreeNode = slotIndex;
        }

        /// <summary>
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            internal Enumerator(ArrayLinkedList<T> list)
            {
                this.list = list;
                currentSlot = -1;
                started = false;
            }

            public T Current
            {
                get
                {
                    if (currentSlot == -1)
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");
                    return list.array[currentSlot].Value;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    currentSlot = list.FirstSlot;
                    started = true;
                }
                else
                {
                    if (currentSlot != -1)
                        currentSlot = list.array[currentSlot].Next;
                }

                return currentSlot != -1;
            }

            public void Reset()
            {
                currentSlot = -1;
                started = false;
            }

            public void Dispose() { }

            readonly ArrayLinkedList<T> list;
            int currentSlot;
            bool started;

        }

        /// <summary>
        /// 反向枚举器
        /// </summary>
        public struct ReversedEnumerator : IEnumerator<T>
        {
            internal ReversedEnumerator(ArrayLinkedList<T> list)
            {
                this.list = list;
                currentSlot = -1;
                started = false;
            }

            public T Current
            {
                get
                {
                    if (currentSlot == -1)
                        throw new InvalidOperationException(
                            "Enumeration has not started or has already finished.");

                    return list.array[currentSlot].Value;
                }
            }

            T IEnumerator<T>.Current => Current;

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    currentSlot = list.LastSlot;
                    started = true;
                }
                else
                {
                    if (currentSlot != -1)
                        currentSlot = list.array[currentSlot].Prev;
                }

                return currentSlot != -1;
            }

            public void Reset()
            {
                currentSlot = -1;
                started = false;
            }

            public void Dispose() { }

            readonly ArrayLinkedList<T> list;
            int currentSlot;
            bool started;
        }

        private struct Node
        {
            public int Prev;
            public bool Used;
            public T Value;
            public int Next;
        }

        Node[] array = Array.Empty<Node>();
        int arrayUsedCount = 0;
        int firstFreeNode = -1;

        void ICollection<T>.Add(T item) => AddLast(item);
        bool ICollection<T>.IsReadOnly => false;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) =>
            CopyTo(new ArraySegment<T>(
                array,
                arrayIndex,
                array.Length - arrayIndex));
    }
}
