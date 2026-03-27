using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 优先级队列
    /// 使用二叉堆实现，提供基于优先级的元素出队顺序
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <typeparam name="TPriority">优先级类型</typeparam>
    public sealed class PriorityQueue<TElement, TPriority>
    {
        /// <summary>
        /// 使用默认优先级比较器创建空的优先级队列
        /// </summary>
        public PriorityQueue() : this(Comparer<TPriority>.Default) { }

        /// <summary>
        /// 使用指定的优先级比较器创建空的优先级队列
        /// </summary>
        /// <param name="comparer">优先级比较器</param>
        public PriorityQueue(IComparer<TPriority> comparer)
        {
            heap = new();
            Comparer = comparer;
        }

        /// <summary>
        /// 使用默认优先级比较器创建优先级队列，并初始化指定的元素集合
        /// </summary>
        /// <param name="collection">初始元素集合</param>
        public PriorityQueue(IEnumerable<(TElement, TPriority)> collection) :
            this(collection, Comparer<TPriority>.Default)
        { }

        /// <summary>
        /// 使用指定的优先级比较器创建优先级队列，并初始化指定的元素集合
        /// </summary>
        /// <param name="collection">初始元素集合</param>
        /// <param name="comparer">优先级比较器</param>
        public PriorityQueue(
            IEnumerable<(TElement, TPriority)> collection,
            IComparer<TPriority> comparer)
        {
            heap = collection.ToList();
            Comparer = comparer;
            Heapify();
        }

        /// <summary>
        /// 使用指定的初始容量和优先级比较器创建空的优先级队列
        /// </summary>
        /// <param name="capacity">初始容量</param>
        /// <param name="comparer">优先级比较器</param>
        public PriorityQueue(
            int capacity,
            IComparer<TPriority> comparer)
        {
            heap = new(capacity: capacity);
            Comparer = comparer;
        }

        /// <summary>
        /// 使用指定的初始容量和默认优先级比较器创建空的优先级队列
        /// </summary>
        /// <param name="capacity">初始容量</param>
        public PriorityQueue(int capacity) :
            this(capacity, Comparer<TPriority>.Default)
        {}

        /// <summary>
        /// 获取优先级队列的容量
        /// </summary>
        public int Capacity => heap.Capacity;

        /// <summary>
        /// 获取优先级比较器
        /// </summary>
        public IComparer<TPriority> Comparer { get; private set; }

        /// <summary>
        /// 获取优先级队列中的元素数量
        /// </summary>
        public int Count => heap.Count;

        /// <summary>
        /// 清空优先级队列中的所有元素
        /// </summary>
        public void Clear() => heap.Clear();

        /// <summary>
        /// 移除并返回优先级最高的元素
        /// </summary>
        /// <returns>优先级最高的元素及其优先级</returns>
        /// <exception cref="InvalidOperationException">优先级队列为空时抛出</exception>
        public (TElement, TPriority) Dequeue()
        {
            if (!TryDequeue(out var element, out var priority))
                throw new InvalidOperationException("Priority queue is empty.");

            return (element, priority);
        }

        /// <summary>
        /// 移除优先级最高的元素，然后添加新元素
        /// 此操作比先调用<see cref="Dequeue"/>再调用<see cref="Enqueue"/>更高效
        /// </summary>
        /// <param name="element">要添加的新元素</param>
        /// <param name="priority">新元素的优先级</param>
        /// <returns>被移除的优先级最高的元素及其优先级</returns>
        /// <exception cref="InvalidOperationException">优先级队列为空时抛出</exception>
        public (TElement, TPriority) DequeueEnqueue(
            TElement element, TPriority priority)
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty.");

            var result = heap[0];
            heap[0] = (element, priority);
            SiftDown(0);
            return result;
        }

        /// <summary>
        /// 添加元素到优先级队列
        /// </summary>
        /// <param name="element">要添加的元素</param>
        /// <param name="priority">元素的优先级</param>
        public void Enqueue(TElement element, TPriority priority)
        {
            heap.Add((element, priority));
            SiftUp(heap.Count - 1);
        }

        /// <summary>
        /// 添加多个元素到优先级队列，所有元素使用相同的优先级
        /// </summary>
        /// <param name="elements">要添加的元素集合</param>
        /// <param name="priority">所有元素的优先级</param>
        public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
        {
            elements.ThrowIfNull();

            int originalCount = heap.Count;

            // 在NETCOREAPP下预分配内存以减少扩容次数
#if NETCOREAPP
            if (elements is IReadOnlyCollection<TElement> roc)
            {
                heap.EnsureCapacity(heap.Count + roc.Count);
            }
            else if (elements is ICollection<TElement> c)
            {
                heap.EnsureCapacity(heap.Count + c.Count);
            }
            else if (elements is ICollection c2)
            {
                heap.EnsureCapacity(heap.Count + c2.Count);
            }
#endif

            foreach (var element in elements)
            {
                heap.Add((element, priority));
            }

            // 如果添加了元素，进行堆化
            if (heap.Count > originalCount)
                PartialHeapify(originalCount);  // 只堆化新增的部分
        }

        /// <summary>
        /// 添加多个元素到优先级队列，每个元素带有各自的优先级
        /// </summary>
        /// <param name="elements">要添加的元素及其优先级集合</param>
        public void EnqueueRange(IEnumerable<(TElement, TPriority)> elements)
        {
            elements.ThrowIfNull();

            int originalCount = heap.Count;

            // 在NETCOREAPP下预分配内存以减少扩容次数
#if NETCOREAPP
            if (elements is IReadOnlyCollection<(TElement, TPriority)> roc)
            {
                heap.EnsureCapacity(heap.Count + roc.Count);
            }
            else if (elements is ICollection<(TElement, TPriority)> c)
            {
                heap.EnsureCapacity(heap.Count + c.Count);
            }
            else if (elements is ICollection c2)
            {
                heap.EnsureCapacity(heap.Count + c2.Count);
            }
#endif

            heap.AddRange(elements);

            // 如果添加了元素，进行堆化
            if (heap.Count > originalCount)
                PartialHeapify(originalCount);  // 只堆化新增的部分
        }

#if NETCOREAPP
        /// <summary>
        /// 确保优先级队列至少具有指定的容量
        /// </summary>
        /// <param name="capacity">要确保的最小容量</param>
        public void EnsureCapacity(int capacity) => heap.EnsureCapacity(capacity);
#endif

        /// <summary>
        /// 返回优先级最高的元素但不移除它
        /// </summary>
        /// <returns>优先级最高的元素及其优先级</returns>
        /// <exception cref="InvalidOperationException">优先级队列为空时抛出</exception>
        public (TElement, TPriority) Peek()
        {
            if (!TryPeek(out var element, out var priority))
                throw new InvalidOperationException("Priority queue is empty.");

            return (element, priority);
        }

        /// <summary>
        /// 将容量减少到实际元素数量，以最小化内存占用
        /// </summary>
        public void TrimExcess() => heap.TrimExcess();

        /// <summary>
        /// 尝试移除并返回优先级最高的元素
        /// </summary>
        /// <param name="element">如果成功，则为优先级最高的元素；否则为默认值</param>
        /// <param name="priority">如果成功，则为优先级最高的元素的优先级；否则为默认值</param>
        /// <returns>如果成功移除元素则为true；如果优先级队列为空则为false</returns>
        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (heap.Count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            var result = heap[0];
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            if (heap.Count > 0)
                SiftDown(0);

            element = result.Item1;
            priority = result.Item2;
            return true;
        }

        /// <summary>
        /// 尝试返回优先级最高的元素但不移除它
        /// </summary>
        /// <param name="element">如果成功，则为优先级最高的元素；否则为默认值</param>
        /// <param name="priority">如果成功，则为优先级最高的元素的优先级；否则为默认值</param>
        /// <returns>如果成功获取元素则为true；如果优先级队列为空则为false</returns>
        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (heap.Count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            var (elem, pri) = heap[0];
            element = elem;
            priority = pri;
            return true;
        }

        /// <summary>
        /// 从优先级队列中移除指定的元素
        /// </summary>
        /// <param name="element">要移除的元素</param>
        /// <param name="removedElement">如果成功，则为被移除的元素；否则为默认值</param>
        /// <param name="removePriority">如果成功，则为被移除元素的优先级；否则为默认值</param>
        /// <param name="comparer">用于比较元素的相等比较器，如果为null则使用默认相等比较器</param>
        /// <returns>如果成功移除元素则为true；如果未找到元素则为false</returns>
        public bool Remove(
            TElement element,
            out TElement removedElement,
            out TPriority removePriority,
            IEqualityComparer<TElement>? comparer = null)
        {
            int index = FindIndex(element, comparer);
            if (index < 0)
            {
                removedElement = default!;
                removePriority = default!;
                return false;
            }

            var (elem, pri) = heap[index];
            removedElement = elem;
            removePriority = pri;

            // 用最后一个元素替换
            heap[index] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            if (index < heap.Count) // 如果删除的不是最后一个元素
            {
                // 可能需要上浮或下沉
                SiftUp(index);
                SiftDown(index);
            }

            return true;
        }

        /// <summary>
        /// 创建优先级队列的浅表副本
        /// </summary>
        /// <returns>优先级队列的浅表副本</returns>
        public PriorityQueue<TElement, TPriority> Clone() =>
            new(Comparer, heap.ToList());

        /// <summary>
        /// 获取优先级队列中所有元素的集合，无序
        /// </summary>
        public UnorderedItemsView UnorderedItems => new(heap);

        private PriorityQueue(
            IComparer<TPriority> comparer,
            List<(TElement, TPriority)> heap)
        {
            Comparer = comparer;
            this.heap = heap;
        }

        // 堆维护方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetParentIndex(int index) => (index - 1) / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetLeftChildIndex(int index) => index * 2 + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetRightChildIndex(int index) => index * 2 + 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Swap(int i, int j)
        {
            (heap[i], heap[j]) = (heap[j], heap[i]);
        }

        void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = GetParentIndex(index);
                if (Comparer.Compare(heap[index].Item2, heap[parent].Item2) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        void SiftDown(int index)
        {
            int count = heap.Count;
            while (true)
            {
                int left = GetLeftChildIndex(index);
                int right = GetRightChildIndex(index);
                int smallest = index;

                if (left < count && Comparer.Compare(heap[left].Item2, heap[smallest].Item2) < 0)
                    smallest = left;

                if (right < count && Comparer.Compare(heap[right].Item2, heap[smallest].Item2) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        void Heapify()
        {
            // 从最后一个非叶子节点开始下沉
            for (int i = heap.Count / 2 - 1; i >= 0; i--)
                SiftDown(i);
        }

        void PartialHeapify(int originalCount)
        {
            // 只对新添加的元素进行上浮操作
            // 因为原始堆已经是有效的，只需要确保新元素正确放置
            for (int i = originalCount; i < heap.Count; i++)
            {
                SiftUp(i);
            }
        }

        int FindIndex(TElement element, IEqualityComparer<TElement>? comparer)
        {
            comparer ??= EqualityComparer<TElement>.Default;
            for (int i = 0; i < heap.Count; i++)
            {
                if (comparer.Equals(heap[i].Item1, element))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 无序元素的枚举器
        /// </summary>
        public struct UnorderedItemsViewEnumerator : IEnumerator<(TElement Element, TPriority Priority)>
        {
            private readonly List<(TElement, TPriority)> _heap;
            private int _index;

            internal UnorderedItemsViewEnumerator(List<(TElement, TPriority)> heap)
            {
                _heap = heap;
                _index = -1;
            }

            public (TElement Element, TPriority Priority) Current
            {
                get
                {
                    if (_index < 0 || _index >= _heap.Count)
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");

                    return _heap[_index];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _index++;
                return _index < _heap.Count;
            }

            public void Reset() => _index = -1;

            public void Dispose() { }
        }

        /// <summary>
        /// 无序元素的只读视图（结构体，避免分配）
        /// </summary>
        public readonly struct UnorderedItemsView : IReadOnlyCollection<(TElement Element, TPriority Priority)>
        {
            private readonly List<(TElement, TPriority)> _heap;

            internal UnorderedItemsView(List<(TElement, TPriority)> heap)
            {
                _heap = heap;
            }

            public int Count => _heap.Count;

            public UnorderedItemsViewEnumerator GetEnumerator() => new UnorderedItemsViewEnumerator(_heap);

            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        readonly List<(TElement, TPriority)> heap;
    }
}
