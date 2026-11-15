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
            collection.ThrowIfNull(nameof(collection));
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
        /// 已分配过的最大稀疏下标。
        /// 尚未分配任何下标（新建或 <see cref="Clear"/> 之后）时为 -1；
        /// 通过 <see cref="RemoveAt"/> / <see cref="Remove"/> 删除元素不会回收该值，
        /// 因此"空集合"也可能返回一个大于 -1 的历史最大值。
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
            Array.Clear(InnerBuffer, 0, Count);
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
                EnsureAdditionalCapacity(1);

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
        /// 批量添加一组元素，并返回它们在稀疏集中的下标
        /// </summary>
        /// <remarks>
        /// 新元素的下标是 <paramref name="outputIndexList"/> 中
        /// [<paramref name="outputIndexListBegin"/>,
        /// <paramref name="outputIndexListBegin"/> + <paramref name="outputIndexCount"/>)
        /// 区间，顺序与 <paramref name="items"/> 的枚举顺序一致。
        /// 为避免分配，这里直接返回内部的反向索引表，不要写入其中；
        /// 调用 Remove/RemoveAt/Clear 后该表会被改写，返回的区间随之失效。
        /// 本方法只枚举 <paramref name="items"/> 一次，因此可以接受只能枚举一次的序列。
        /// </remarks>
        /// <param name="items">要添加的元素，不能为 null</param>
        /// <param name="outputIndexList">新元素下标所在的内部列表</param>
        /// <param name="outputIndexListBegin">新元素下标区间的起始位置</param>
        /// <param name="outputIndexCount">新元素下标区间的长度</param>
        public void AddRange(
            IEnumerable<T> items,
            out IReadOnlyList<int> outputIndexList,
            out int outputIndexListBegin,
            out int outputIndexCount)
        {
            items.ThrowIfNull(nameof(items));

            outputIndexList = reversedIndexes;
            outputIndexListBegin = Count;

            // 已知数量时一次性预留；未知数量时边写边扩容。
            // 不能先 Count 再枚举，否则对只能枚举一次的序列会得到错误的长度。
            int knownCount = items switch
            {
                IReadOnlyCollection<T> roc => roc.Count,
                ICollection<T> c => c.Count,
                ICollection c => c.Count,
                _ => -1,
            };

            if (knownCount > 0)
                EnsureAdditionalCapacity(knownCount);

            foreach (var item in items)
            {
                EnsureAdditionalCapacity(1);

                int newIndex = Count++;
                InnerBuffer[newIndex] = item;

                if (newIndex > MaxIndex)
                {
                    reversedIndexes.Add(newIndex);
                    indexes.Add(newIndex);
                    MaxIndex = newIndex;
                }
            }

            outputIndexCount = Count - outputIndexListBegin;
        }

        /// <summary>
        /// 批量添加数组中的所有元素，并返回它们在稀疏集中的下标
        /// </summary>
        /// <remarks>
        /// 等价于接收 <see cref="ArraySegment{T}"/> 的重载；
        /// 下标区间的含义与失效条件见该重载。
        /// </remarks>
        /// <param name="items">要添加的数组，不能为 null</param>
        /// <param name="outputIndexList">新元素下标所在的内部列表</param>
        /// <param name="outputIndexListBegin">新元素下标区间的起始位置</param>
        /// <param name="outputIndexCount">新元素下标区间的长度</param>
        public void AddRange(
            T[] items,
            out IReadOnlyList<int> outputIndexList,
            out int outputIndexListBegin,
            out int outputIndexCount) =>
            AddRange(
                new ArraySegment<T>(items),
                out outputIndexList,
                out outputIndexListBegin,
                out outputIndexCount);

        /// <summary>
        /// 批量添加数组片段中的所有元素，并返回它们在稀疏集中的下标
        /// </summary>
        /// <remarks>
        /// 新元素的下标是 <paramref name="outputIndexList"/> 中
        /// [<paramref name="outputIndexListBegin"/>,
        /// <paramref name="outputIndexListBegin"/> + <paramref name="outputIndexCount"/>)
        /// 区间，顺序与片段中的元素顺序一致。
        /// 为避免分配，这里直接返回内部的反向索引表，不要写入其中；
        /// 调用 Remove/RemoveAt/Clear 后该表会被改写，返回的区间随之失效。
        /// </remarks>
        /// <param name="items">要添加的数组片段</param>
        /// <param name="outputIndexList">新元素下标所在的内部列表</param>
        /// <param name="outputIndexListBegin">新元素下标区间的起始位置</param>
        /// <param name="outputIndexCount">新元素下标区间的长度</param>
        public void AddRange(
            ArraySegment<T> items,
            out IReadOnlyList<int> outputIndexList,
            out int outputIndexListBegin,
            out int outputIndexCount)
        {
            outputIndexList = reversedIndexes;
            outputIndexListBegin = Count;
            outputIndexCount = items.Count;

            if (items.Count == 0) return;

            EnsureAdditionalCapacity(items.Count);

            Array.Copy(
                items.Array.ThrowIfNull(nameof(items)),
                items.Offset,
                InnerBuffer,
                Count,
                items.Count);

            for (int i = 0; i < items.Count; ++i)
            {
                int newIndex = Count++;

                if (newIndex > MaxIndex)
                {
                    reversedIndexes.Add(newIndex);
                    indexes.Add(newIndex);
                    MaxIndex = newIndex;
                }
            }
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

            capacity = RoundUpCapacity(capacity);

            T[] newBuffer = new T[capacity];
            Array.Copy(InnerBuffer, newBuffer, Count);
            InnerBuffer = newBuffer;

#if NETCOREAPP
            indexes.EnsureCapacity(capacity);
            reversedIndexes.EnsureCapacity(capacity);
#endif
        }

        /// <summary>
        /// 确保还能再容纳 <paramref name="additional"/> 个元素。
        /// </summary>
        void EnsureAdditionalCapacity(int additional)
        {
            long required = (long)Count + additional;

            if (required <= InnerBuffer.Length)
                return;

            EnsureCapacity(Growth(required));
        }

        /// <summary>
        /// 计算扩容后的目标容量（至少容纳 <paramref name="required"/> 个元素）。
        /// 使用 long 计算避免整数溢出。
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
        /// 尝试删除指定位置的元素
        /// O(1)
        /// </summary>
        public bool RemoveAt(int index)
        {
            if (index > MaxIndex || index < 0) return false;
            int innerIndex = indexes[index];
            if (innerIndex >= Count) return false;
            int lastInnerIndex = Count - 1;

            if (innerIndex != lastInnerIndex)
            {
                (InnerBuffer[innerIndex], InnerBuffer[lastInnerIndex]) =
                    (InnerBuffer[lastInnerIndex], InnerBuffer[innerIndex]);

                (reversedIndexes[innerIndex], reversedIndexes[lastInnerIndex]) =
                    (reversedIndexes[lastInnerIndex], reversedIndexes[innerIndex]);

                indexes[index] = lastInnerIndex;
                indexes[reversedIndexes[innerIndex]] = innerIndex;
            }

            InnerBuffer[--Count] = default!;
            return true;
        }

        /// <summary>
        /// 查找给定值，如果有多个，则返回其中任何一个，不一定是最前面的
        /// O(n)
        /// </summary>
        public int IndexOf(T item)
        {
            var buffer = InnerBuffer;
            var cnt = Count;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < cnt; ++i)
            {
                if (comparer.Equals(item, buffer[i]))
                    return reversedIndexes[i];
            }

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
            var array = target.Array.ThrowIfNull();

            int writeIndex = 0;
            var buffer = InnerBuffer;
            var cnt = Count;
            var maxIdx = MaxIndex;
            var segCount = target.Count;
            var segOffset = target.Offset;

            for (int i = 0; i <= maxIdx && writeIndex < segCount; ++i)
            {
                int innerIdx = indexes[i];
                if (innerIdx < cnt)
                    array[segOffset + writeIndex++] = buffer[innerIdx];
            }
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
            var buffer = InnerBuffer;
            var cnt = Count;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < cnt; ++i)
            {
                if (comparer.Equals(item, buffer[i]))
                    return true;
            }

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
            public readonly void Dispose() { }

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
                var set = sparseSet;
                var cnt = set.Count;
                var max = set.MaxIndex;
                while (++currentIndex <= max)
                {
                    if (set.indexes[currentIndex] < cnt)
                        return true;
                }

                return false;
            }

            public void Reset() => currentIndex = -1;
            public readonly void Dispose() { }

            readonly SparseSet<T> sparseSet;
            int currentIndex;
        }

        readonly List<int> indexes;
        readonly List<int> reversedIndexes;

        bool ICollection<T>.IsReadOnly => false;
        void ICollection<T>.Add(T item) => Add(item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            array.ThrowIfNull(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(
                    "Destination array is too small to hold all elements.",
                    nameof(array));

            CopyToOrdered(new ArraySegment<T>(array, arrayIndex, Count));
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
