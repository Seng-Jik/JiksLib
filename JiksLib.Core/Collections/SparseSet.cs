using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 稀疏集
    /// </summary>
    public sealed class SparseSet<T> :
        ICollection<T>,
        IReadOnlyCollection<T>,
        IEnumerable<T>
    {
        public SparseSet()
        {
            InnerBuffer = Array.Empty<T>();
            indexes = new();
            reversedIndexes = new();
            MaxIndex = -1;
            Count = 0;
        }

        public SparseSet(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            InnerBuffer = new T[capacity];
            indexes = new(capacity: capacity);
            reversedIndexes = new(capacity: capacity);
            MaxIndex = -1;
            Count = 0;
        }

        public SparseSet(IEnumerable<T> collection)
        {
            InnerBuffer = collection.ToArray();
            indexes = new(InnerBuffer.Length);

            for (int i = 0; i < InnerBuffer.Length; ++i)
                indexes.Add(i);

            reversedIndexes = new(indexes);
            MaxIndex = InnerBuffer.Length - 1;
            Count = InnerBuffer.Length;
        }

        /// <summary>
        /// 获得内部缓存
        /// 以高效地更新其中的所有元素
        /// 需要即用即弃，对SparseSet调用Add或EnsureCapacity后失效
        /// 有效范围为 0 ~ (Count - 1)
        /// </summary>
        public T[] InnerBuffer { get; private set; }

        /// <summary>
        /// 从内部缓存下标转换到稀疏集的下标
        /// </summary>
        public int InnerBufferIndexToSparseSetIndex(int innerBufferIndex)
        {
            if (innerBufferIndex < 0 || innerBufferIndex >= Count)
                throw new IndexOutOfRangeException();

            return reversedIndexes[innerBufferIndex];
        }

        /// <summary>
        /// 从稀疏集下标转换到内部缓存下标
        /// </summary>
        public bool SparseSetIndexToInnerBufferIndex(
            int index, out int innerBufferIndex)
        {
            if (index < 0 || index > MaxIndex)
            {
                innerBufferIndex = 0;
                return false;
            }

            innerBufferIndex = indexes[index];

            if (innerBufferIndex >= Count)
            {
                innerBufferIndex = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取或设置指定稀疏下标位置的元素
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (TryIndex(index, out var item))
                    return item;

                throw new IndexOutOfRangeException();
            }

            set
            {
                if (SparseSetIndexToInnerBufferIndex(index, out var i))
                    InnerBuffer[i] = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }

        /// <summary>
        /// 元素数量
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 最大下标
        /// 空集合为-1
        /// </summary>
        public int MaxIndex { get; private set; }

        /// <summary>
        /// 内部缓冲区的容量。大于等于 <see cref="Count"/>
        /// </summary>
        public int Capacity => InnerBuffer.Length;

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < Count; ++i)
                InnerBuffer[i] = default!;

            Count = 0;
            MaxIndex = -1;
            indexes.Clear();
            reversedIndexes.Clear();
        }

        /// <summary>
        /// 插入并返回它的下标
        /// 若不触发扩容则O(1)
        /// </summary>
        public int Add(T item)
        {
            if (Count >= InnerBuffer.Length)
            {
                int capacity;

                try
                {
                    capacity = checked((Count + 1) * 2);
                }
                catch (OverflowException)
                {
                    capacity = int.MaxValue;
                }

                EnsureCapacity(Math.Max(4, capacity));
            }

            int newIndex = Count++;
            InnerBuffer[newIndex] = item;

            if (newIndex <= MaxIndex)
                return reversedIndexes[newIndex];

            reversedIndexes.Add(newIndex);
            indexes.Add(newIndex);
            MaxIndex = newIndex;
            return newIndex;
        }

        /// <summary>
        /// 保留空间
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            if (InnerBuffer.Length >= capacity) return;

            try
            {
                capacity = (capacity / 4 + 1) * 4;
            }
            catch (OverflowException)
            {
                capacity = int.MaxValue;
            }

            T[] newBuffer = new T[capacity];
            Array.Copy(InnerBuffer, newBuffer, Count);
            InnerBuffer = newBuffer;

#if NETCOREAPP
            indexes.EnsureCapacity(capacity);
            reversedIndexes.EnsureCapacity(capacity);
#endif
        }

        /// <summary>
        /// 尝试删除指定位置的元素
        /// O(1)
        /// </summary>
        public bool RemoveAt(int index)
        {
            if (index > MaxIndex || index < 0) return false;
            int innerIndex = indexes[index];
            if (innerIndex >= Count) return false;
            int lastInnerIndex = Count - 1;

            (InnerBuffer[innerIndex], InnerBuffer[lastInnerIndex]) =
                (InnerBuffer[lastInnerIndex], InnerBuffer[innerIndex]);

            (reversedIndexes[innerIndex], reversedIndexes[lastInnerIndex]) =
                (reversedIndexes[lastInnerIndex], reversedIndexes[innerIndex]);

            indexes[index] = lastInnerIndex;
            indexes[reversedIndexes[innerIndex]] = innerIndex;

            InnerBuffer[--Count] = default!;
            return true;
        }

        /// <summary>
        /// 查找给定值，如果有多个，则返回其中任何一个，不一定是最前面的
        /// O(n)
        /// </summary>
        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; ++i)
                if (EqualityComparer<T>.Default.Equals(item, InnerBuffer[i]))
                    return reversedIndexes[i];

            return -1;
        }

        /// <summary>
        /// 移除一个给定值，如果有多个，则可能移除其中的任何一个，不一定是最前面的
        /// </summary>
        public bool Remove(T item)
        {
            var i = IndexOf(item);
            if (i == -1) return false;
            return RemoveAt(i);
        }

        /// <summary>
        /// 尝试索引给定的下标
        /// </summary>
        public bool TryIndex(int index, out T item)
        {
            if (SparseSetIndexToInnerBufferIndex(index, out var i))
            {
                item = InnerBuffer[i];
                return true;
            }
            else
            {
                item = default!;
                return false;
            }
        }

        /// <summary>
        /// 有序地复制到给定数组片段，中间的空位将会被忽略
        /// 如果数组提前结束，则后面的内容将不会被复制
        /// </summary>
        public void CopyToOrdered(ArraySegment<T> target)
        {
            target.Array.ThrowIfNull();

            int writeIndex = 0;
            for (int i = 0; i <= MaxIndex && writeIndex < target.Count; ++i)
                if (TryIndex(i, out var item))
                    target.Array![target.Offset + writeIndex++] = item;
        }

        /// <summary>
        /// 无序地复制到给定的数组片段。按内部缓冲区顺序复制，性能优于 <see cref="CopyToOrdered"/>
        /// </summary>
        public void CopyToUnordered(ArraySegment<T> target) =>
            Array.Copy(
                sourceArray: InnerBuffer,
                sourceIndex: 0,
                destinationArray: target.Array.ThrowIfNull(),
                destinationIndex: target.Offset,
                length: Math.Min(Count, target.Count));

        /// <summary>
        /// 是否包含指定元素
        /// </summary>
        public bool Contains(T item)
        {
            for (int i = 0; i < Count; ++i)
                if (EqualityComparer<T>.Default.Equals(item, InnerBuffer[i]))
                    return true;

            return false;
        }

        /// <summary>
        /// 获取无序的缓存友好迭代器。按内部缓冲区顺序遍历，跳过已删除元素的空隙
        /// </summary>
        public UnorderedEnumerator GetEnumerator() => new(this);

        /// <summary>
        /// 获得有序的迭代器。按稀疏下标从小到大的顺序遍历，跳过已删除元素的空隙
        /// </summary>
        public OrderedEnumerator GetOrderedEnumerator => new(this);

        public struct UnorderedEnumerator :
            IEnumerator<T>
        {
            public UnorderedEnumerator(SparseSet<T> sparseSet)
            {
                this.sparseSet = sparseSet;
                currentIndex = -1;
            }

            public readonly T Current
            {
                get
                {
                    if (currentIndex < 0 || currentIndex >= sparseSet.Count)
                        throw new InvalidOperationException();

                    return sparseSet.InnerBuffer[currentIndex];
                }
            }

            readonly object? IEnumerator.Current => Current;
            public bool MoveNext() => ++currentIndex < sparseSet.Count;
            public void Reset() => currentIndex = -1;
            public readonly void Dispose() {}

            readonly SparseSet<T> sparseSet;
            int currentIndex;
        }

        public struct OrderedEnumerator :
            IEnumerator<T>
        {
            public OrderedEnumerator(SparseSet<T> sparseSet)
            {
                currentIndex = -1;
                this.sparseSet = sparseSet;
            }

            public readonly T Current
            {
                get
                {
                    if (sparseSet.TryIndex(currentIndex, out var x))
                        return x;
                    else
                        throw new InvalidOperationException();
                }
            }

            readonly object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                while (++currentIndex <= sparseSet.MaxIndex)
                    if (sparseSet.TryIndex(currentIndex, out _))
                        return true;

                return false;
            }

            public void Reset() => currentIndex = -1;
            public readonly void Dispose() {}

            readonly SparseSet<T> sparseSet;
            int currentIndex;
        }

        readonly List<int> indexes;
        readonly List<int> reversedIndexes;

        bool ICollection<T>.IsReadOnly => false;
        void ICollection<T>.Add(T item) => Add(item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            CopyToOrdered(new(array, arrayIndex, array.Length - arrayIndex));
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
