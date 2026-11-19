using System;
using System.Collections;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif
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
        /// 与 <see cref="HashSet{T}.Contains"/> 保持一致：元素为 null 时返回 false，而不是抛异常
        /// </summary>
        public bool Contains(T item) => item != null && dict.ContainsKey(item);

        /// <summary>
        /// 判断集合中某个元素的重复次数
        /// 元素为 null 时返回 0
        /// </summary>
        /// <param name="item">元素</param>
        /// <returns>重复次数</returns>
        public int GetCountOf(T item) =>
            item != null && dict.TryGetValue(item, out var count) ? count : 0;

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

#if NET6_0_OR_GREATER
            // 单次哈希：取引用后原地更新（netstandard2.1 / net48 没有 CollectionsMarshal）
            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(
                dict,
                item,
                out bool exists);

            if (exists)
            {
                count++;
                Count++;
                return count;
            }

            count = 1;
            Count++;
            return 1;
#else
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
#endif
        }

        /// <summary>
        /// 从集合中移除一个元素
        /// 与 <see cref="HashSet{T}.Remove"/> 保持一致：元素为 null 时返回 (false, 0)
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>是否成功移除以及移除后该元素的数量</returns>
        public (bool Success, int Count) Remove(T item)
        {
            if (item == null)
                return (false, 0);

#if NET6_0_OR_GREATER
            // 单次哈希：取引用后原地更新或删除
            ref int count = ref CollectionsMarshal.GetValueRefOrNullRef(dict, item);

            if (Unsafe.IsNullRef(ref count))
                return (false, 0);

            if (count > 1)
            {
                count--;
                Count--;
                return (true, count);
            }

            dict.Remove(item);
            Count--;
            return (true, 0);
#else
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
#endif
        }

        /// <summary>
        /// 获得集合的枚举器
        /// </summary>
        /// <returns>集合的枚举器</returns>
        public Enumerator GetEnumerator() => new(dict);

        /// <summary>
        /// 转换为只读集合
        /// 返回的是当前实例的只读视图，禁止强转回 <see cref="MultiHashSet{T}"/> 修改；强转属于未定义行为
        /// </summary>
        public IReadOnlyMultiSet<T> AsReadOnly() => this;

        /// <summary>
        /// 拷贝当前集合
        /// 复制集合结构，但引用类型元素共享对象引用
        /// </summary>
        /// <returns>拷贝后的集合</returns>
        public MultiHashSet<T> Clone() => new(Count, new(dict, dict.Comparer));

        MultiHashSet(int count, Dictionary<T, int> dict)
        {
            this.dict = dict;
            Count = count;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        readonly Dictionary<T, int> dict;

        /// <summary>
        /// 枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            Dictionary<T, int>.Enumerator dictEnumerator;
            T currentKey;
            int totalCount;    // 当前键的总重复次数
            int returnedCount; // 当前键已返回的次数
            bool started;

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
    }
}
