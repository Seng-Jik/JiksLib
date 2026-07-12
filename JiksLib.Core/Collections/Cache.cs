using System;
using System.Collections.Generic;

namespace JiksLib.Collections
{
    /// <summary>
    /// 异步缓存
    /// 值会通过Callback传回
    /// </summary>
    /// <typeparam name="TKey">被缓存的值的Key</typeparam>
    /// <typeparam name="TValue">被缓存的值的Value</typeparam>
    public interface ICache<TKey, out TValue>
        where TKey : notnull
    {
        /// <summary>
        /// 访问缓存
        /// 如果值已缓存，则会立刻返回该值
        /// 如果值未缓存，则会在淘汰旧值后返回创建的新值
        /// </summary>
        TValue Access(TKey key);

        /// <summary>
        /// 当值被缓存淘汰后调用
        /// </summary>
        public event Action<TKey, TValue>? OnEvicted;

        /// <summary>
        /// 值工厂
        /// 传入Key，当值已就绪时将其传入onReceive
        /// </summary>
        public delegate TValue Factory(TKey key);
    }

    /// <summary>
    /// 异步缓存
    /// 值会通过Callback传回
    /// </summary>
    /// <typeparam name="TKey">被缓存的值的Key</typeparam>
    /// <typeparam name="TValue">被缓存的值的Value</typeparam>
    public interface IAsyncCache<TKey, out TValue>
        where TKey : notnull
    {
        /// <summary>
        /// 访问缓存
        /// 如果值已缓存，则会立刻将已缓存的值传入onReceiveValue
        /// 如果值未缓存，则会在淘汰旧值后使用Factory创建新值并传入onReceiveValue
        /// </summary>
        void Access(
            TKey key,
            Action<TValue> onReceiveValue,
            Action<Exception> onException);

        /// <summary>
        /// 当值被缓存淘汰时调用
        /// </summary>
        public event Action<TKey, TValue>? OnEvicted;

        /// <summary>
        /// 值工厂
        /// 传入Key，当值已就绪时将其传入onReceive
        /// </summary>
        public delegate void Factory(
            TKey key,
            Action<TValue> onReceive,
            Action<Exception> onException);
    }

    public static class Cache
    {
        /// <summary>
        /// 缓存算法
        /// </summary>
        public enum CacheAlgorithm
        {
            FIFO,
            LRU,
            LFU,

            /// <summary>
            /// 这个缓存算法从不淘汰任何值
            /// 忽略 capacity 参数
            /// </summary>
            NeverEvict
        }

        /// <summary>
        /// 创建缓存器
        /// </summary>
        /// <param name="cacheAlgorithm">缓存淘汰算法</param>
        /// <param name="factory">值工厂，当缓存未命中时调用</param>
        /// <param name="capacity">缓存容量，NeverEvict 算法忽略此参数</param>
        /// <exception cref="ArgumentNullException">factory 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// capacity &lt;= 0 且算法不是 NeverEvict 时抛出
        /// </exception>
        public static ICache<TKey, TValue> Make<TKey, TValue>(
            CacheAlgorithm cacheAlgorithm,
            ICache<TKey, TValue>.Factory factory,
            int capacity)
            where TKey : notnull
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (capacity <= 0 && cacheAlgorithm != CacheAlgorithm.NeverEvict)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), "Capacity must be greater than zero.");

            return cacheAlgorithm switch
            {
                CacheAlgorithm.FIFO => new Fifo<TKey, TValue>(factory, capacity),
                CacheAlgorithm.LRU => new Lru<TKey, TValue>(factory, capacity),
                CacheAlgorithm.LFU => new Lfu<TKey, TValue>(factory, capacity),
                CacheAlgorithm.NeverEvict => new NeverEvict<TKey, TValue>(factory),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cacheAlgorithm), $"Unknown cache algorithm: {cacheAlgorithm}."),
            };
        }

        /// <summary>
        /// 创建异步缓存器
        /// </summary>
        /// <param name="cacheAlgorithm">缓存淘汰算法</param>
        /// <param name="factory">异步值工厂，当缓存未命中时调用</param>
        /// <param name="capacity">缓存容量，NeverEvict 算法忽略此参数</param>
        /// <exception cref="ArgumentNullException">factory 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// capacity &lt;= 0 且算法不是 NeverEvict 时抛出
        /// </exception>
        public static IAsyncCache<TKey, TValue> MakeAsync<TKey, TValue>(
            CacheAlgorithm cacheAlgorithm,
            IAsyncCache<TKey, TValue>.Factory factory,
            int capacity)
            where TKey : notnull
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (capacity <= 0 && cacheAlgorithm != CacheAlgorithm.NeverEvict)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), "Capacity must be greater than zero.");

            return cacheAlgorithm switch
            {
                CacheAlgorithm.FIFO => new Fifo<TKey, TValue>(factory, capacity),
                CacheAlgorithm.LRU => new Lru<TKey, TValue>(factory, capacity),
                CacheAlgorithm.LFU => new Lfu<TKey, TValue>(factory, capacity),
                CacheAlgorithm.NeverEvict => new NeverEvict<TKey, TValue>(factory),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cacheAlgorithm), $"Unknown cache algorithm: {cacheAlgorithm}."),
            };
        }

        private abstract class CacheImpl<TKey, TValue> :
            ICache<TKey, TValue>,
            IAsyncCache<TKey, TValue>
            where TKey : notnull
        {
            public CacheImpl(IAsyncCache<TKey, TValue>.Factory asyncFactory)
            {
                this.asyncFactory = asyncFactory;
            }

            public CacheImpl(ICache<TKey, TValue>.Factory factory)
            {
                this.factory = factory;
            }

            public event Action<TKey, TValue>? OnEvicted;

            TValue ICache<TKey, TValue>.Access(TKey key)
            {
                if (TryGetCache(key, out TValue? value))
                    return value!;

                value = factory!(key);
                Put(key, value);
                return value;
            }

            void IAsyncCache<TKey, TValue>.Access(
                TKey key,
                Action<TValue> onReceiveValue,
                Action<Exception> onException)
            {
                if (TryGetCache(key, out TValue? value))
                {
                    onReceiveValue(value!);
                    return;
                }

                asyncFactory!(
                    key,
                    value =>
                    {
                        onReceiveValue(value);
                        Put(key, value);
                    },
                    onException);
            }

            protected void PublishEvictedEvent(TKey key, TValue value)
            {
                OnEvicted?.Invoke(key, value);
            }

            protected abstract bool TryGetCache(TKey key, out TValue? value);
            protected abstract void Put(TKey key, TValue value);
            readonly IAsyncCache<TKey, TValue>.Factory? asyncFactory = null;
            readonly ICache<TKey, TValue>.Factory? factory = null;
        }

        private class NeverEvict<TKey, TValue> : CacheImpl<TKey, TValue>
            where TKey : notnull
        {
            public NeverEvict(IAsyncCache<TKey, TValue>.Factory factory) :
                base(factory)
            {
            }

            public NeverEvict(ICache<TKey, TValue>.Factory factory) :
                base(factory)
            {
            }

            protected override void Put(TKey key, TValue value)
            {
                cache[key] = value;
            }

            protected override bool TryGetCache(TKey key, out TValue? value) =>
                cache.TryGetValue(key, out value);

            readonly Dictionary<TKey, TValue> cache = new();
        }

        /// <summary>
        /// FIFO（先进先出）缓存
        /// 淘汰最早插入的值，访问不影响淘汰顺序
        /// </summary>
        private sealed class Fifo<TKey, TValue> : CacheImpl<TKey, TValue>
            where TKey : notnull
        {
            public Fifo(
                IAsyncCache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            public Fifo(
                ICache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            protected override bool TryGetCache(TKey key, out TValue? value) =>
                cache.TryGetValue(key, out value);

            protected override void Put(TKey key, TValue value)
            {
                if (cache.Count >= capacity)
                {
                    var oldest = order.Dequeue();
                    var evictedValue = cache[oldest];
                    cache.Remove(oldest);
                    PublishEvictedEvent(oldest, evictedValue);
                }

                cache[key] = value;
                order.Enqueue(key);
            }

            readonly Dictionary<TKey, TValue> cache = new();
            readonly Queue<TKey> order = new();
            readonly int capacity;
        }

        /// <summary>
        /// LRU（最近最少使用）缓存
        /// 淘汰最久未被访问的值
        /// </summary>
        private sealed class Lru<TKey, TValue> : CacheImpl<TKey, TValue>
            where TKey : notnull
        {
            public Lru(
                IAsyncCache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            public Lru(
                ICache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            protected override bool TryGetCache(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    // 移到链表尾部（最近使用）
                    accessOrder.Remove(entry.node);
                    accessOrder.AddLast(entry.node);
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override void Put(TKey key, TValue value)
            {
                if (cache.Count >= capacity)
                {
                    var oldest = accessOrder.First!.Value;
                    accessOrder.RemoveFirst();
                    var evictedValue = cache[oldest].value;
                    cache.Remove(oldest);
                    PublishEvictedEvent(oldest, evictedValue);
                }

                var node = accessOrder.AddLast(key);
                cache[key] = (value, node);
            }

            readonly Dictionary<TKey, (TValue value, LinkedListNode<TKey> node)> cache = new();
            readonly LinkedList<TKey> accessOrder = new();
            readonly int capacity;
        }

        /// <summary>
        /// LFU（最不经常使用）缓存
        /// 淘汰访问频率最低的值，频率相同时淘汰最早进入该频率的值
        /// </summary>
        private sealed class Lfu<TKey, TValue> : CacheImpl<TKey, TValue>
            where TKey : notnull
        {
            public Lfu(
                IAsyncCache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            public Lfu(
                ICache<TKey, TValue>.Factory factory,
                int capacity) : base(factory)
            {
                this.capacity = capacity;
            }

            protected override bool TryGetCache(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    // 从旧频率桶中移除
                    var oldFreqList = freqLists[entry.frequency];
                    oldFreqList.Remove(entry.listNode);
                    if (oldFreqList.Count == 0)
                    {
                        freqLists.Remove(entry.frequency);
                        if (entry.frequency == minFrequency)
                            minFrequency++;
                    }

                    // 加入新频率桶
                    entry.frequency++;
                    if (!freqLists.TryGetValue(entry.frequency, out var newFreqList))
                    {
                        newFreqList = new LinkedList<TKey>();
                        freqLists[entry.frequency] = newFreqList;
                    }

                    entry.listNode = newFreqList.AddLast(key);
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override void Put(TKey key, TValue value)
            {
                if (cache.Count >= capacity)
                {
                    var minList = freqLists[minFrequency];
                    var oldest = minList.First!.Value;
                    minList.RemoveFirst();
                    if (minList.Count == 0)
                        freqLists.Remove(minFrequency);

                    var evictedValue = cache[oldest].value;
                    cache.Remove(oldest);
                    PublishEvictedEvent(oldest, evictedValue);
                }

                minFrequency = 1;
                if (!freqLists.TryGetValue(1, out var list))
                {
                    list = new LinkedList<TKey>();
                    freqLists[1] = list;
                }

                var node = list.AddLast(key);
                cache[key] = new LfuEntry(value, 1, node);
            }

            sealed class LfuEntry
            {
                public readonly TValue value;
                public int frequency;
                public LinkedListNode<TKey> listNode;

                public LfuEntry(TValue value, int frequency, LinkedListNode<TKey> listNode)
                {
                    this.value = value;
                    this.frequency = frequency;
                    this.listNode = listNode;
                }
            }

            readonly Dictionary<TKey, LfuEntry> cache = new();
            readonly Dictionary<int, LinkedList<TKey>> freqLists = new();
            readonly int capacity;
            int minFrequency;
        }
    }
}
