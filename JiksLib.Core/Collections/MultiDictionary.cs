using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 多重字典
    /// 一个键可以对应多个值
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    public sealed class MultiDictionary<TKey, TValue> :
        IReadOnlyMultiDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        /// <summary>
        /// 创建一个空的多重字典
        /// </summary>
        public MultiDictionary()
        {
            dict = new();
            Count = 0;
            valueComparer = null;
        }

        /// <summary>
        /// 创建一个空的多重字典
        /// 使用指定的键比较器
        /// </summary>
        public MultiDictionary(IEqualityComparer<TKey> keyComparer)
        {
            dict = new(keyComparer.ThrowIfNull());
            Count = 0;
            valueComparer = null;
        }

        /// <summary>
        /// 创建一个空的多重字典，使用指定的键和值的比较器
        /// </summary>
        /// <param name="keyComparer">键的比较器</param>
        /// <param name="valueComparer">值的比较器</param>
        public MultiDictionary(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
        {
            dict = new(keyComparer.ThrowIfNull());
            Count = 0;
            this.valueComparer = valueComparer.ThrowIfNull();
        }

        /// <summary>
        /// 清空所有内容
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            Count = 0;
        }

        /// <summary>
        /// 添加一个键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Add(TKey key, TValue value)
        {
            if (!dict.TryGetValue(key.ThrowIfNull(), out var set))
            {
                set = valueComparer != null ? new(valueComparer) : new();
                dict[key] = set;
            }

            set.Add(value);
            Count++;
        }

        /// <summary>
        /// 添加一个键值对
        /// </summary>
        /// <param name="item">键值对</param>
        public void Add(KeyValuePair<TKey, TValue> item) =>
            Add(item.Key, item.Value);

        /// <summary>
        /// 移除一个键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(TKey key, TValue value)
        {
            if (!dict.TryGetValue(key.ThrowIfNull(), out var set))
                return false;

            bool success = set.Remove(value).Success;

            if (success)
            {
                Count--;

                if (set.Count == 0)
                    dict.Remove(key);
            }

            return success;
        }

        /// <summary>
        /// 移除一个键值对
        /// </summary>
        /// <param name="item">键值对</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item) =>
            Remove(item.Key, item.Value);

        /// <summary>
        /// 移除某个键下所有的键值对
        /// </summary>
        /// <returns>移除的元素数量</returns>
        public int Remove(TKey key)
        {
            if (!dict.TryGetValue(key.ThrowIfNull(), out var set))
                return 0;

            dict.Remove(key);
            Count -= set.Count;
            return set.Count;
        }

        /// <summary>
        /// 获取指定键所有值的只读容器
        /// </summary>
        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                if (dict.TryGetValue(key.ThrowIfNull(), out var set))
                    return set;
                else
                    return Array.Empty<TValue>();
            }
        }

        /// <summary>
        /// 所有的键
        /// </summary>
        public IReadOnlyCollection<TKey> Keys => dict.Keys;

        /// <summary>
        /// 所有的值
        /// </summary>
        public IEnumerable<TValue> Values =>
            dict.Values.SelectMany(static set => set);

        /// <summary>
        /// 元素数量
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 多重字典中是否包含对应的键
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>是否包含该键</returns>
        public bool ContainsKey(TKey key) => dict.ContainsKey(key.ThrowIfNull());

        /// <summary>
        /// 判断字典中是否包含某个值
        /// O(n) 操作
        /// </summary>
        /// <param name="value">要判断的值</param>
        /// <returns>是否包含</returns>
        public bool ContainsValue(TValue value)
        {
            value.ThrowIfNull();

            foreach (var set in dict.Values)
                if (set.Contains(value))
                    return true;

            return false;
        }

        /// <summary>
        /// 多重字典中是否包含对应的键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>是否包含该键值对</returns>
        public bool Contains(TKey key, TValue value)
        {
            if (dict.TryGetValue(key.ThrowIfNull(), out var set))
                return set.Contains(value.ThrowIfNull());

            return false;
        }

        /// <summary>
        /// 多重字典中是否包含对应的键值对
        /// </summary>
        /// <param name="item">键值对</param>
        /// <returns>是否包含该键值对</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            Contains(item.Key, item.Value);

        /// <summary>
        /// 获得指定键中值的数量
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值的数量</returns>
        public int GetValueCountOfKey(TKey key)
        {
            if (dict.TryGetValue(key.ThrowIfNull(), out var set))
                return set.Count;

            return 0;
        }

        /// <summary>
        /// 获得指定键值对的数量
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>指定键值对的数量</returns>
        public int GetCountOfKeyValuePair(TKey key, TValue value)
        {
            if (dict.TryGetValue(key.ThrowIfNull(), out var set))
                return set.GetCountOf(value.ThrowIfNull());

            return 0;
        }

        /// <summary>
        /// 获得指定键值对的数量
        /// </summary>
        /// <param name="item">键值对</param>
        /// <returns>指定键值对的数量</returns>
        public int GetCountOfKeyValuePair(KeyValuePair<TKey, TValue> item) =>
            GetCountOfKeyValuePair(item.Key, item.Value);

        /// <summary>
        /// 拷贝当前多重字典，但引用类型元素共享相同对象引用
        /// </summary>
        /// <returns>拷贝后的多重字典</returns>
        public MultiDictionary<TKey, TValue> Clone()
        {
            var clone = valueComparer != null ?
                new MultiDictionary<TKey, TValue>(dict.Comparer, valueComparer) :
                new MultiDictionary<TKey, TValue>(dict.Comparer);

            clone.Count = Count;

            foreach (var kv in dict)
                clone.dict.Add(kv.Key, kv.Value.Clone());

            return clone;
        }

        /// <summary>
        /// 转换为只读字典
        /// </summary>
        public IReadOnlyMultiDictionary<TKey, TValue> AsReadOnly() => this;

        /// <summary>
        /// 获得所有键值对的枚举器
        /// </summary>
        /// <returns>所有键值对的枚举器</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var i in dict)
                foreach (var j in i.Value)
                    yield return new KeyValuePair<TKey, TValue>(i.Key, j);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool ILookup<TKey, TValue>.Contains(TKey key) =>
            ContainsKey(key);

        IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key] =>
            this[key];

        IEnumerator<IGrouping<TKey, TValue>> IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()
        {
            foreach (var k in dict)
                yield return new Grouping(k.Key, this[k.Key]);
        }

        private sealed class Grouping : IGrouping<TKey, TValue>
        {
            public TKey Key { get; private set; }

            public Grouping(
                TKey key,
                IEnumerable<TValue> values)
            {
                Key = key;
                this.values = values;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            readonly IEnumerable<TValue> values;
        }

        readonly Dictionary<TKey, MultiHashSet<TValue>> dict;
        readonly IEqualityComparer<TValue>? valueComparer;
    }
}