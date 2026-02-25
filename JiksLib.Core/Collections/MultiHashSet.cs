using System;
using System.Collections;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 多重哈希集合
    /// 允许单个元素重复多次
    /// </summary>
    public sealed class MultiHashSet<T> : IReadOnlyMultiSet<T>
        where T : notnull
    {
        /// <summary>
        /// 创建一个空的多重哈希集合
        /// </summary>
        public MultiHashSet()
        {
            dict = new();
            Count = 0;
        }

        /// <summary>
        /// 创建一个空的多重哈希集合，使用指定的比较器
        /// </summary>
        /// <param name="comparer">比较器</param>
        public MultiHashSet(IEqualityComparer<T> comparer)
        {
            dict = new(comparer.ThrowIfNull());
            Count = 0;
        }

        /// <summary>
        /// 获得元素总数量
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 判断集合是否包含某个元素
        /// </summary>
        public bool Contains(T item) => dict.ContainsKey(item.ThrowIfNull());

        /// <summary>
        /// 判断集合中某个元素的重复次数
        /// </summary>
        /// <param name="item">元素</param>
        /// <returns>重复次数</returns>
        public int GetCountOf(T item)
        {
            if (dict.TryGetValue(item.ThrowIfNull(), out var count))
                return count;

            return 0;
        }

        /// <summary>
        /// 清空集合
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            Count = 0;
        }

        /// <summary>
        /// 向集合添加一个元素
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>添加后该元素的数量</returns>
        public int Add(T item)
        {
            item.ThrowIfNull();

            if (dict.TryGetValue(item, out var count))
            {
                dict[item] = count + 1;
                Count++;
                return count + 1;
            }
            else
            {
                dict[item] = 1;
                Count++;
                return 1;
            }
        }

        /// <summary>
        /// 从集合中移除一个元素
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>是否成功移除以及移除后该元素的数量</returns>
        public (bool Success, int Count) Remove(T item)
        {
            item.ThrowIfNull();

            if (dict.TryGetValue(item, out var count))
            {
                if (count > 1)
                {
                    dict[item] = count - 1;
                    Count--;
                    return (true, count - 1);
                }
                else
                {
                    dict.Remove(item);
                    Count--;
                    return (true, 0);
                }
            }
            else
            {
                return (false, 0);
            }
        }

        /// <summary>
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private Dictionary<T, int>.Enumerator dictEnumerator;
            private T currentKey;
            private int totalCount;    // 当前键的总重复次数
            private int returnedCount; // 当前键已返回的次数
            private bool started;

            internal Enumerator(Dictionary<T, int> dict)
            {
                dictEnumerator = dict.GetEnumerator();
                currentKey = default!;
                totalCount = 0;
                returnedCount = 0;
                started = false;
            }

            public T Current
            {
                get
                {
                    if (!started || returnedCount == 0)
                        throw new InvalidOperationException("Enumeration has not started or has already finished.");
                    return currentKey;
                }
            }

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    started = true;
                }

                while (returnedCount >= totalCount)
                {
                    if (!dictEnumerator.MoveNext())
                    {
                        currentKey = default!;
                        totalCount = 0;
                        returnedCount = 0;
                        return false;
                    }

                    var current = dictEnumerator.Current;
                    currentKey = current.Key;
                    totalCount = current.Value;
                    returnedCount = 0;
                }

                returnedCount++;
                return true;
            }

            public void Reset()
            {
                // 重置枚举器到初始状态
                // 由于Dictionary.Enumerator不支持Reset，我们重新创建枚举器
                // 但Reset方法很少使用，我们可以抛出NotSupportedException
                throw new NotSupportedException("Reset is not supported on MultiHashSet enumerator.");
            }

            public void Dispose()
            {
                dictEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 获得集合的枚举器
        /// </summary>
        /// <returns>集合的枚举器</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(dict);
        }

        /// <summary>
        /// 转换为只读集合
        /// </summary>
        public IReadOnlyMultiSet<T> AsReadOnly() => this;

        /// <summary>
        /// 拷贝当前集合
        /// 复制集合结构，但引用类型元素共享对象引用
        /// </summary>
        /// <returns>拷贝后的集合</returns>
        public MultiHashSet<T> Clone() => new(Count, new(dict, dict.Comparer));

        private MultiHashSet(int count, Dictionary<T, int> dict)
        {
            this.dict = dict;
            Count = count;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly Dictionary<T, int> dict;
    }
}