using System;
using System.Collections;
using System.Collections.Generic;

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
        IEnumerable<T>,
        System.Collections.ICollection
    {
        /// <summary>
        /// 元素数量
        /// </summary>
        public int Count { get; private set; } = 0;

        /// <summary>
        /// 第一个元素的槽位索引，如果链表为空则为-1
        /// </summary>
        public int FirstSlotIndex { get; private set; } = -1;

        /// <summary>
        /// 最后一个元素的槽位索引，如果链表为空则为-1
        /// </summary>
        public int LastSlotIndex { get; private set; } = -1;

        /// <summary>
        /// 容量（数组长度）
        /// </summary>
        public int Capacity => array.Length;

        /// <summary>
        /// 获取或设置指定索引处的元素
        /// </summary>
        /// <param name="index">从零开始的索引</param>
        /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int current = FirstSlotIndex;
                for (int i = 0; i < index; i++)
                {
                    current = array[current].Next;
                }
                return array[current].Value!;
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int current = FirstSlotIndex;
                for (int i = 0; i < index; i++)
                {
                    current = array[current].Next;
                }
                array[current].Value = value;
            }
        }

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
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

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
            FirstSlotIndex = other.FirstSlotIndex;
            LastSlotIndex = other.LastSlotIndex;
            Count = other.Count;
        }

        /// <summary>
        /// 从集合初始化ArrayLinkedList
        /// </summary>
        /// <param name="collection">源集合</param>
        public ArrayLinkedList(ICollection<T> collection) : this((IEnumerable<T>)collection)
        {
        }

        /// <summary>
        /// 从只读集合初始化ArrayLinkedList
        /// </summary>
        /// <param name="collection">源集合</param>
        public ArrayLinkedList(IReadOnlyCollection<T> collection) : this((IEnumerable<T>)collection)
        {
        }

        /// <summary>
        /// 从可枚举集合初始化ArrayLinkedList
        /// </summary>
        /// <param name="collection">源集合</param>
        public ArrayLinkedList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            // 如果是ICollection<T>或IReadOnlyCollection<T>，使用更高效的方法
            if (collection is ICollection<T> coll)
            {
                array = new Node[coll.Count];
                int index = 0;
                foreach (var item in coll)
                {
                    array[index].Used = true;
                    array[index].Value = item;
                    array[index].Prev = index - 1;
                    array[index].Next = index + 1;
                    index++;
                }

                if (coll.Count > 0)
                {
                    array[0].Prev = -1;
                    array[coll.Count - 1].Next = -1;
                    FirstSlotIndex = 0;
                    LastSlotIndex = coll.Count - 1;
                }
                else
                {
                    FirstSlotIndex = -1;
                    LastSlotIndex = -1;
                }

                arrayUsedCount = coll.Count;
                firstFreeNode = -1;
                Count = coll.Count;
            }
            else if (collection is IReadOnlyCollection<T> roc)
            {
                array = new Node[roc.Count];
                int index = 0;
                foreach (var item in roc)
                {
                    array[index].Used = true;
                    array[index].Value = item;
                    array[index].Prev = index - 1;
                    array[index].Next = index + 1;
                    index++;
                }

                if (roc.Count > 0)
                {
                    array[0].Prev = -1;
                    array[roc.Count - 1].Next = -1;
                    FirstSlotIndex = 0;
                    LastSlotIndex = roc.Count - 1;
                }
                else
                {
                    FirstSlotIndex = -1;
                    LastSlotIndex = -1;
                }

                arrayUsedCount = roc.Count;
                firstFreeNode = -1;
                Count = roc.Count;
            }
            else
            {
                // 对于普通IEnumerable，需要先收集到列表中
                var list = new List<T>();
                foreach (var item in collection)
                {
                    list.Add(item);
                }

                array = new Node[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    array[i].Used = true;
                    array[i].Value = list[i];
                    array[i].Prev = i - 1;
                    array[i].Next = i + 1;
                }

                if (list.Count > 0)
                {
                    array[0].Prev = -1;
                    array[list.Count - 1].Next = -1;
                    FirstSlotIndex = 0;
                    LastSlotIndex = list.Count - 1;
                }
                else
                {
                    FirstSlotIndex = -1;
                    LastSlotIndex = -1;
                }

                arrayUsedCount = list.Count;
                firstFreeNode = -1;
                Count = list.Count;
            }
        }

        public T Get(int slotIndex, out int nextSlotIndex, out int prevSlotIndex)
        {
            EnsureSlotUsed(slotIndex);
            var slot = array[slotIndex];
            nextSlotIndex = slot.Next;
            prevSlotIndex = slot.Prev;
            return slot.Value!;
        }

        public void AddFirst(T item)
        {
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;
            array[newSlotIndex].Prev = -1;
            array[newSlotIndex].Next = FirstSlotIndex;

            if (FirstSlotIndex != -1)
                array[FirstSlotIndex].Prev = newSlotIndex;

            FirstSlotIndex = newSlotIndex;

            if (LastSlotIndex == -1)
                LastSlotIndex = newSlotIndex;

            Count++;
        }

        public void AddLast(T item)
        {
            int newSlotIndex = AllocateSlot();
            array[newSlotIndex].Value = item;
            array[newSlotIndex].Prev = LastSlotIndex;
            array[newSlotIndex].Next = -1;

            if (LastSlotIndex != -1)
                array[LastSlotIndex].Next = newSlotIndex;

            LastSlotIndex = newSlotIndex;

            if (FirstSlotIndex == -1)
                FirstSlotIndex = newSlotIndex;

            Count++;
        }

        /// <summary>
        /// 将项添加到集合中（添加到末尾）
        /// </summary>
        /// <param name="item">要添加的项</param>
        public void Add(T item) => AddLast(item);

        /// <summary>
        /// 获取一个值，该值指示集合是否为只读
        /// </summary>
        public bool IsReadOnly => false;

        public void AddAfter(int slotIndex, T item)
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

            if (LastSlotIndex == slotIndex)
                LastSlotIndex = newSlotIndex;

            Count++;
        }

        public void AddBefore(int slotIndex, T item)
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

            if (FirstSlotIndex == slotIndex)
                FirstSlotIndex = newSlotIndex;

            Count++;
        }

        public void Clear()
        {
            Array.Clear(array, 0, arrayUsedCount);
            FirstSlotIndex = -1;
            LastSlotIndex = -1;
            arrayUsedCount = 0;
            firstFreeNode = -1;
            Count = 0;
        }

        /// <summary>
        /// 确定集合中是否包含特定值
        /// </summary>
        /// <param name="item">要定位的对象</param>
        /// <returns>如果在集合中找到该项，则为 true；否则为 false</returns>
        public bool Contains(T item) => Find(item) != -1;

        /// <summary>
        /// 从特定的数组索引开始，将集合的元素复制到一个数组中
        /// </summary>
        /// <param name="array">作为从集合复制的元素的目标的一维数组</param>
        /// <param name="arrayIndex">array 中从零开始的索引，从此处开始复制</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex 小于 0</exception>
        /// <exception cref="ArgumentException">源集合中的元素数大于从 arrayIndex 到目标 array 末尾的可用空间</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index cannot be negative.");
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not long enough to copy all the items.");

            int current = FirstSlotIndex;
            int destIndex = arrayIndex;
            while (current != -1)
            {
                array[destIndex++] = this.array[current].Value!;
                current = this.array[current].Next;
            }
        }

        /// <summary>
        /// 将集合的元素复制到 ArraySegment 中
        /// </summary>
        /// <param name="segment">目标数组片段</param>
        /// <exception cref="ArgumentException">源集合中的元素数大于目标数组片段的容量</exception>
        public void CopyTo(ArraySegment<T> segment)
        {
            if (segment.Array == null)
                throw new ArgumentNullException(nameof(segment), "Array segment's array cannot be null.");
            if (segment.Count < Count)
                throw new ArgumentException("Destination array segment is not long enough to copy all the items.");

            int current = FirstSlotIndex;
            int destIndex = segment.Offset;
            while (current != -1)
            {
                segment.Array[destIndex++] = array[current].Value!;
                current = array[current].Next;
            }
        }

        /// <summary>
        /// 搜索指定的对象，并返回整个集合中第一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在集合中定位的对象</param>
        /// <returns>如果在整个集合中找到 item 的第一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        public int Find(T item)
        {
            int current = FirstSlotIndex;
            int index = 0;
            while (current != -1)
            {
                if (EqualityComparer<T>.Default.Equals(array[current].Value!, item))
                    return index;
                current = array[current].Next;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// 搜索指定的对象，并返回整个集合中最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在集合中定位的对象</param>
        /// <returns>如果在整个集合中找到 item 的最后一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        public int FindLast(T item)
        {
            int current = LastSlotIndex;
            int index = Count - 1;
            while (current != -1)
            {
                if (EqualityComparer<T>.Default.Equals(array[current].Value!, item))
                    return index;
                current = array[current].Prev;
                index--;
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

            RemoveBySlotID(slotIndex);
            return true;
        }

        /// <summary>
        /// 通过槽位ID移除元素
        /// </summary>
        /// <param name="slotIndex">要移除的槽位索引</param>
        /// <exception cref="ArgumentOutOfRangeException">槽位索引无效</exception>
        public void RemoveBySlotID(int slotIndex)
        {
            EnsureSlotUsed(slotIndex);

            var node = array[slotIndex];

            // 更新前一个节点的Next指针
            if (node.Prev != -1)
                array[node.Prev].Next = node.Next;
            else
                FirstSlotIndex = node.Next; // 这是第一个节点

            // 更新后一个节点的Prev指针
            if (node.Next != -1)
                array[node.Next].Prev = node.Prev;
            else
                LastSlotIndex = node.Prev; // 这是最后一个节点

            // 释放槽位
            FreeSlot(slotIndex);
            Count--;
        }

        /// <summary>
        /// 尝试移除并返回第一个元素
        /// </summary>
        /// <param name="item">被移除的元素</param>
        /// <returns>如果成功移除元素，则为 true；如果集合为空，则为 false</returns>
        public bool TryRemoveFirst(out T? item)
        {
            if (FirstSlotIndex == -1)
            {
                item = default;
                return false;
            }

            item = array[FirstSlotIndex].Value!;
            RemoveBySlotID(FirstSlotIndex);
            return true;
        }

        /// <summary>
        /// 尝试移除并返回最后一个元素
        /// </summary>
        /// <param name="item">被移除的元素</param>
        /// <returns>如果成功移除元素，则为 true；如果集合为空，则为 false</returns>
        public bool TryRemoveLast(out T? item)
        {
            if (LastSlotIndex == -1)
            {
                item = default;
                return false;
            }

            item = array[LastSlotIndex].Value!;
            RemoveBySlotID(LastSlotIndex);
            return true;
        }

        // 辅助方法：查找元素的槽位索引
        private int FindSlot(T item)
        {
            int current = FirstSlotIndex;
            while (current != -1)
            {
                if (EqualityComparer<T>.Default.Equals(array[current].Value!, item))
                    return current;
                current = array[current].Next;
            }
            return -1;
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

        public IReadOnlyCollection<T> AsReadOnly() => this;

        // IEnumerable<T> 和 IEnumerable 接口实现
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayLinkedList<T> list;
            private int currentSlot;
            private bool started;

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
                    return list.array[currentSlot].Value!;
                }
            }

            object? System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    currentSlot = list.FirstSlotIndex;
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
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// 反向枚举器
        /// </summary>
        public struct ReversedEnumerator : IEnumerator<T>
        {
            private readonly ArrayLinkedList<T> list;
            private int currentSlot;
            private bool started;

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
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");
                    return list.array[currentSlot].Value!;
                }
            }

            object? System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    currentSlot = list.LastSlotIndex;
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
        }

        public ReversedEnumerator GetReversedEnumerator() => new ReversedEnumerator(this);

        /// <summary>
        /// 将集合的元素复制到新数组中
        /// </summary>
        /// <returns>包含集合元素的新数组</returns>
        public T[] ToArray()
        {
            T[] result = new T[Count];
            CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// 将容量设置为集合中的实际元素数（如果该数小于某个阈值）
        /// </summary>
        public void TrimExcess()
        {
            if (Count == 0)
            {
                array = Array.Empty<Node>();
                arrayUsedCount = 0;
                firstFreeNode = -1;
            }
            else if (array.Length > Count * 2) // 如果容量远大于元素数量，进行修剪
            {
                // 创建新数组并复制元素
                Node[] newArray = new Node[Count];
                int newIndex = 0;
                int current = FirstSlotIndex;

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
                FirstSlotIndex = Count > 0 ? 0 : -1;
                LastSlotIndex = Count > 0 ? Count - 1 : -1;
            }
        }

        void EnsureSlotUsed(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= arrayUsedCount || !array[slotIndex].Used)
                throw new ArgumentOutOfRangeException(nameof(slotIndex)); // 修复这里的异常信息
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

        private struct Node
        {
            public int Prev;
            public bool Used;
            public T? Value;
            public int Next;
        }

        Node[] array;
        int arrayUsedCount = 0;
        int firstFreeNode = -1;

        // ICollection 接口显式实现
        bool System.Collections.ICollection.IsSynchronized => false;

        object System.Collections.ICollection.SyncRoot => this;

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("Only single dimensional arrays are supported.", nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("The array must have zero lower bound.", nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            if (array.Length - index < Count)
                throw new ArgumentException("Destination array is not long enough to copy all the items.");

            try
            {
                int current = FirstSlotIndex;
                int destIndex = index;
                while (current != -1)
                {
                    array.SetValue(this.array[current].Value, destIndex++);
                    current = this.array[current].Next;
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Invalid array type.", nameof(array));
            }
        }
    }
}