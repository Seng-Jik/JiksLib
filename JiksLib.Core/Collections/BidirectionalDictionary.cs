using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JiksLib.Collections
{
    /// <summary>
    /// 双向字典
    /// </summary>
    public sealed class BidirectionalDictionary<TKey, TValue> :
        IReadOnlyBidirectionalDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        public BidirectionalDictionary() :
            this(new Dictionary<TKey, TValue>(), new())
        { }

        public BidirectionalDictionary(
            BidirectionalDictionary<TKey, TValue> copyFrom) :
            this(copyFrom.sequential, copyFrom.reversed)
        { }

        public BidirectionalDictionary(
            BidirectionalDictionary<TKey, TValue> copyFrom,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer) :
            this(
                new Dictionary<TKey, TValue>(copyFrom.sequential, keyComparer),
                new(copyFrom.reversed, valueComparer))
        { }

#if NETCOREAPP

        public BidirectionalDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) :
            this(
                new Dictionary<TKey, TValue>(keyValuePairs),
                new(keyValuePairs.Select(x =>
                    new KeyValuePair<TValue, TKey>(x.Value, x.Key))))
        { }

        public BidirectionalDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer) :
            this(
                new Dictionary<TKey, TValue>(keyValuePairs, keyComparer),
                new(
                    keyValuePairs.Select(x =>
                        new KeyValuePair<TValue, TKey>(x.Value, x.Key)),
                    valueComparer))
        { }

#endif

        public BidirectionalDictionary(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer) :
            this(new Dictionary<TKey, TValue>(keyComparer), new(valueComparer))
        { }

        public BidirectionalDictionary(
            int capacity,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer) :
            this(
                new Dictionary<TKey, TValue>(capacity, keyComparer),
                new(capacity, valueComparer))
        { }

        public BidirectionalDictionary(int capacity) :
            this(new Dictionary<TKey, TValue>(capacity), new(capacity))
        { }

        /// <summary>
        /// 键比较器
        /// </summary>
        public IEqualityComparer<TKey> KeyComparer => sequential.Comparer;

        /// <summary>
        /// 值比较器
        /// </summary>
        public IEqualityComparer<TValue> ValueComparer => reversed.Comparer;

        /// <summary>
        /// 元素总数
        /// </summary>
        public int Count => sequential.Count;

        /// <summary>
        /// 所有的键
        /// </summary>
        public IReadOnlyCollection<TKey> Keys => sequential.Keys;

        /// <summary>
        /// 所有的值
        /// </summary>
        public IReadOnlyCollection<TValue> Values => reversed.Keys;

        /// <summary>
        /// 根据键获得值或设置值
        /// </summary>
        public TValue this[TKey k]
        {
            get => sequential[k];
            set
            {
                // 检查新值是否已映射到不同的键
                if (reversed.TryGetValue(value, out var existingKey) && !KeyComparer.Equals(existingKey, k))
                    throw new ArgumentException("Value is already mapped to a different key.", nameof(value));

                // 移除旧值的反向映射（如果存在）
                if (sequential.TryGetValue(k, out var oldValue))
                    reversed.Remove(oldValue);

                // 设置新的映射
                sequential[k] = value;
                reversed[value] = k;
            }
        }

        /// <summary>
        /// 添加键值对到双向字典中
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            // 检查键是否已存在
            if (sequential.ContainsKey(key))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));

            // 检查值是否已存在
            if (reversed.ContainsKey(value))
                throw new ArgumentException("An item with the same value has already been added.", nameof(value));

            sequential.Add(key, value);
            reversed.Add(value, key);
        }

        /// <summary>
        /// 添加键值对到双向字典中
        /// </summary>
        public void Add(KeyValuePair<TKey, TValue> keyValuePair) =>
            Add(keyValuePair.Key, keyValuePair.Value);

        /// <summary>
        /// 清除所有内容
        /// </summary>
        public void Clear()
        {
            sequential.Clear();
            reversed.Clear();
        }

        /// <summary>
        /// 是否包含键值对
        /// </summary>
        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            sequential.Contains(item);

        /// <summary>
        /// 是否包含键
        /// </summary>
        public bool ContainsKey(TKey key) => sequential.ContainsKey(key);

        /// <summary>
        /// 是否包含值
        /// </summary>
        public bool ContainsValue(TValue value) => reversed.ContainsKey(value);

        /// <summary>
        /// 尝试以键删除键值对
        /// 返回是否删除成功
        /// </summary>
#if NETCOREAPP
        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
#else
        public bool Remove(TKey key, out TValue value)
#endif
        {
            if (sequential.TryGetValue(key, out value))
            {
                sequential.Remove(key);
                reversed.Remove(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试以值删除键值对
        /// 返回是否删除成功
        /// </summary>
#if NETCOREAPP
        public bool RemoveByValue(TValue value, [MaybeNullWhen(false)] out TKey key)
#else
        public bool RemoveByValue(TValue value, out TKey key)
#endif
        {
            if (reversed.TryGetValue(value, out key))
            {
                reversed.Remove(value);
                sequential.Remove(key);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试以键删除键值对
        /// 返回是否删除成功
        /// </summary>
        public bool Remove(TKey key) => Remove(key, out _);

        /// <summary>
        /// 尝试以值删除键值对
        /// 返回是否删除成功
        /// </summary>
        public bool RemoveByValue(TValue value) => RemoveByValue(value, out _);

        /// <summary>
        /// 尝试添加值，若失败则返回 false
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            // 检查键是否已存在
            if (sequential.ContainsKey(key))
                return false;

            // 检查值是否已存在
            if (reversed.ContainsKey(value))
                return false;

            // 添加键值对
            sequential.Add(key, value);
            reversed.Add(value, key);
            return true;
        }

        /// <summary>
        /// 尝试以键查找值
        /// </summary>
#if NETCOREAPP
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
#else
        public bool TryGetValue(TKey key, out TValue value) =>
#endif
            sequential.TryGetValue(key, out value);

        /// <summary>
        /// 尝试以值查找键
        /// </summary>
#if NETCOREAPP
        public bool TryGetKeyByValue(TValue value, [MaybeNullWhen(false)] out TKey key) =>
#else
        public bool TryGetKeyByValue(TValue value, out TKey key) =>
#endif
            reversed.TryGetValue(value, out key);

        /// <summary>
        /// 获取只读引用
        /// </summary>
        public IReadOnlyBidirectionalDictionary<TKey, TValue> AsReadOnly() =>
            this;

        /// <summary>
        /// 获取反向字典只读引用
        /// </summary>
        public IReadOnlyDictionary<TValue, TKey> Reversed => reversed;

        /// <summary>
        /// 创建反向索引的副本
        /// </summary>
        public BidirectionalDictionary<TValue, TKey> CopyAndReverse() =>
            new(reversed, sequential);

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
            sequential.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable)sequential).GetEnumerator();

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() =>
            sequential.GetEnumerator();

        BidirectionalDictionary(
            Dictionary<TKey, TValue> sequential,
            Dictionary<TValue, TKey> reversed)
        {
            this.sequential = sequential;
            this.reversed = reversed;
        }

        readonly Dictionary<TKey, TValue> sequential;
        readonly Dictionary<TValue, TKey> reversed;
    }
}