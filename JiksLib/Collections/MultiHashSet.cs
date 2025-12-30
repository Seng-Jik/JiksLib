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
    {
        /// <summary>
        /// 创建一个空的多重哈希集合
        /// </summary>
        public MultiHashSet()
        {
            dict = new();
            count = 0;
        }

        /// <summary>
        /// 创建一个空的多重哈希集合，使用指定的比较器
        /// </summary>
        /// <param name="comparer">比较器</param>
        public MultiHashSet(IEqualityComparer<T> comparer)
        {
            dict = new(comparer.ThrowIfNull());
            count = 0;
        }

        /// <summary>
        /// 获得元素总数量
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 判断集合是否包含某个元素
        /// </summary>
        public bool Contains(T item) => dict.ContainsKey(item);

        /// <summary>
        /// 判断集合中某个元素的重复次数
        /// </summary>
        /// <param name="item">元素</param>
        /// <returns>重复次数</returns>
        public int GetCountOf(T item)
        {
            if (dict.TryGetValue(item, out var count))
                return count;

            return 0;
        }

        /// <summary>
        /// 清空集合
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            count = 0;
        }

        /// <summary>
        /// 向集合添加一个元素
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>添加后该元素的数量</returns>
        public int Add(T item)
        {
            this.count++;

            if (dict.TryGetValue(item, out var count))
            {
                dict[item] = count + 1;
                return count + 1;
            }
            else
            {
                dict[item] = 1;
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
            if (dict.TryGetValue(item, out var count))
            {
                this.count--;

                if (count > 1)
                {
                    dict[item] = count - 1;
                    return (true, count - 1);
                }
                else
                {
                    dict.Remove(item);
                    return (true, 0);
                }
            }
            else
            {
                return (false, 0);
            }
        }

        /// <summary>
        /// 获得集合的枚举器
        /// </summary>
        /// <returns>集合的枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var i in dict)
                for (int j = 0; j < i.Value; j++)
                    yield return i.Key;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int count;
        readonly Dictionary<T, int> dict;
    }
}