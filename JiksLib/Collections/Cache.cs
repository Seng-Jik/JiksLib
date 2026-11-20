using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Collections
{
    /// <summary>
    /// 同步缓存
    /// 值在 <see cref="Access"/> 中同步创建并返回
    /// </summary>
    /// <remarks>
    /// <see cref="Cache.Make{TKey,TValue}"/> 返回的实例<b>只应作为 ICache 使用</b>。
    /// 实现类虽然同时实现了 <see cref="IAsyncCache{TKey,TValue}"/>（为了复用淘汰逻辑），
    /// 但通过异步接口调用会抛 <see cref="InvalidOperationException"/>；
    /// 这是两种创建方式互斥的固有特性，不是缺陷。
    /// <para>
    /// 本缓存<b>不是线程安全的</b>；异步缓存（<see cref="IAsyncCache{TKey,TValue}"/>）的工厂回调
    /// 可能在任意线程上完成，调用方必须自行序列化对缓存及其回调状态的访问。
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">被缓存的值的Key</typeparam>
    /// <typeparam name="TValue">被缓存的值的Value</typeparam>
    public interface ICache<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// 访问缓存
        /// 如果值已缓存，则会立刻返回该值
        /// 如果值未缓存，则会在淘汰旧值后返回创建的新值
        /// </summary>
        TValue Access(TKey key);

        /// <summary>
        /// 手动移除指定键的缓存项。
        /// 如果键存在，会触发 <see cref="OnEvicted"/>；不存在时无操作。
        /// 手动移除不受容量或算法限制，NeverEvict 也允许手动移除。
        /// </summary>
        /// <param name="key">要移除的缓存键。</param>
        void Evict(TKey key);

        /// <summary>
        /// 尝试获取已缓存的值，不会调用值工厂。
        /// </summary>
        /// <param name="key">要查询的缓存键。</param>
        /// <param name="value">命中时返回缓存值；未命中时为 default。</param>
        /// <param name="dontModifyCacheState">
        /// 为 true 时不改变 LRU 的访问顺序或 LFU 的访问频率；
        /// 为 false 时按一次正常缓存访问处理。
        /// </param>
        /// <returns>缓存命中返回 true；否则返回 false。</returns>
        bool GetCached(
            TKey key,
            out TValue value,
            bool dontModifyCacheState = false);

        /// <summary>
        /// 清空全部缓存项，并对每个被移除的项触发一次 <see cref="OnEvicted"/>。
        /// 手动清空不受容量或算法限制，NeverEvict 也允许清空。
        /// 事件在内部缓存清空后逐个触发，因此处理器中可以安全地重新访问缓存。
        /// 不需要通知时请使用 <see cref="Clear"/>。
        /// </summary>
        void EvictAll();

        /// <summary>
        /// 清空全部缓存项，但不会触发 <see cref="OnEvicted"/>。
        /// 与 <see cref="EvictAll"/> 的区别仅在于是否通知淘汰，
        /// 适用于释放缓存或销毁对象时避免事件处理器的额外开销与副作用。
        /// </summary>
        void Clear();

        /// <summary>
        /// 当前缓存项数量。
        /// </summary>
        int Count { get; }

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
    /// <remarks>
    /// <see cref="Cache.MakeAsync{TKey,TValue}"/> 返回的实例<b>只应作为 IAsyncCache 使用</b>。
    /// 实现类虽然同时实现了 <see cref="ICache{TKey,TValue}"/>（为了复用淘汰逻辑），
    /// 但通过同步接口调用会抛 <see cref="InvalidOperationException"/>；
    /// 这是两种创建方式互斥的固有特性，不是缺陷。
    /// <para>
    /// 本缓存<b>不是线程安全的</b>：工厂回调可能在任意线程上完成，
    /// 调用方必须自行序列化对缓存、回调状态与 <see cref="OnEvicted"/> 处理器的访问。
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">被缓存的值的Key</typeparam>
    /// <typeparam name="TValue">被缓存的值的Value</typeparam>
    public interface IAsyncCache<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// 访问缓存
        /// 如果值已缓存，则会立刻将已缓存的值传入onReceiveValue
        /// 如果值未缓存，则会在淘汰旧值后使用Factory创建新值并传入onReceiveValue
        /// 新值在回调之前写入缓存，因此回调内重入同一 key 会直接命中缓存；
        /// 回调抛出的异常会向上传播，但已写入的值会保留。
        /// </summary>
        void Access(
            TKey key,
            Action<TValue> onReceiveValue,
            Action<Exception> onException);

        /// <summary>
        /// 手动移除指定键的缓存项。
        /// 如果键存在，会触发 <see cref="OnEvicted"/>；不存在时无操作。
        /// 手动移除不受容量或算法限制，NeverEvict 也允许手动移除。
        /// </summary>
        /// <param name="key">要移除的缓存键。</param>
        void Evict(TKey key);

        /// <summary>
        /// 清空全部缓存项，并对每个被移除的项触发一次 <see cref="OnEvicted"/>。
        /// 手动清空不受容量或算法限制，NeverEvict 也允许清空。
        /// 事件在内部缓存清空后逐个触发，因此处理器中可以安全地重新访问缓存。
        /// 不需要通知时请使用 <see cref="Clear"/>。
        /// </summary>
        void EvictAll();

        /// <summary>
        /// 清空全部缓存项，但不会触发 <see cref="OnEvicted"/>。
        /// 与 <see cref="EvictAll"/> 的区别仅在于是否通知淘汰，
        /// 适用于释放缓存或销毁对象时避免事件处理器的额外开销与副作用。
        /// </summary>
        void Clear();

        /// <summary>
        /// 尝试获取已缓存的值，不会调用异步值工厂。
        /// </summary>
        /// <param name="key">要查询的缓存键。</param>
        /// <param name="value">命中时返回缓存值；未命中时为 default。</param>
        /// <param name="dontModifyCacheState">
        /// 为 true 时不改变 LRU 的访问顺序或 LFU 的访问频率；
        /// 为 false 时按一次正常缓存访问处理。
        /// </param>
        /// <returns>缓存命中返回 true；否则返回 false。</returns>
        bool GetCached(
            TKey key,
            out TValue value,
            bool dontModifyCacheState = false);


        /// <summary>
        /// 当前缓存项数量。
        /// </summary>
        int Count { get; }

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
        public enum Algorithm
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
            Algorithm cacheAlgorithm,
            ICache<TKey, TValue>.Factory factory,
            int capacity)
            where TKey : notnull
        {
            factory.ThrowIfNull(nameof(factory));
            if (capacity <= 0 && cacheAlgorithm != Algorithm.NeverEvict)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), "Capacity must be greater than zero.");

            return cacheAlgorithm switch
            {
                Algorithm.FIFO => new Fifo<TKey, TValue>(factory, capacity),
                Algorithm.LRU => new Lru<TKey, TValue>(factory, capacity),
                Algorithm.LFU => new Lfu<TKey, TValue>(factory, capacity),
                Algorithm.NeverEvict => new NeverEvict<TKey, TValue>(factory),
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
            Algorithm cacheAlgorithm,
            IAsyncCache<TKey, TValue>.Factory factory,
            int capacity)
            where TKey : notnull
        {
            factory.ThrowIfNull(nameof(factory));
            if (capacity <= 0 && cacheAlgorithm != Algorithm.NeverEvict)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), "Capacity must be greater than zero.");

            return cacheAlgorithm switch
            {
                Algorithm.FIFO => new Fifo<TKey, TValue>(factory, capacity),
                Algorithm.LRU => new Lru<TKey, TValue>(factory, capacity),
                Algorithm.LFU => new Lfu<TKey, TValue>(factory, capacity),
                Algorithm.NeverEvict => new NeverEvict<TKey, TValue>(factory),
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

            public abstract int Count { get; }

            TValue ICache<TKey, TValue>.Access(TKey key)
            {
                if (factory == null)
                    throw new InvalidOperationException(
                        "This cache instance was created by Cache.MakeAsync; use IAsyncCache.Access instead.");

                if (TryGetCache(key, out TValue? value))
                    return value!;

                value = factory(key);
                Put(key, value);
                return value;
            }

            void IAsyncCache<TKey, TValue>.Access(
                TKey key,
                Action<TValue> onReceiveValue,
                Action<Exception> onException)
            {
                if (asyncFactory == null)
                    throw new InvalidOperationException(
                        "This cache instance was created by Cache.Make; use ICache.Access instead.");

                if (TryGetCache(key, out TValue? value))
                {
                    onReceiveValue(value!);
                    return;
                }

                asyncFactory(
                    key,
                    value =>
                    {
                        // 先写入缓存再回调：回调内重入同一 key 会命中缓存，
                        // 不会递归调用工厂；回调抛异常时新值也不会丢失。
                        Put(key, value);
                        onReceiveValue(value);
                    },
                    onException);
            }

            protected void PublishEvictedEvent(TKey key, TValue value)
            {
                OnEvicted?.Invoke(key, value);
            }

            public void Evict(TKey key)
            {
                if (TryRemove(key, out TValue? value))
                    PublishEvictedEvent(key, value!);
            }

            public void EvictAll()
            {
                // 先快照再清空，最后逐个通知：OnEvicted 处理器重入本缓存时，
                // 内部结构已经是空的，不会出现边遍历边修改。
                var evicted = new List<KeyValuePair<TKey, TValue>>();
                RemoveAllEntries(evicted);
                foreach (var pair in evicted)
                    PublishEvictedEvent(pair.Key, pair.Value);
            }

            public void Clear()
            {
                RemoveAllEntries(null);
            }

            public bool GetCached(
                TKey key,
                out TValue value,
                bool dontModifyCacheState = false)
            {
                TValue? cached;
                bool found = dontModifyCacheState
                    ? TryPeek(key, out cached)
                    : TryGetCache(key, out cached);

                if (found)
                {
                    value = cached!;
                    return true;
                }

                value = default!;
                return false;
            }

            protected abstract bool TryGetCache(TKey key, out TValue? value);
            protected abstract bool TryPeek(TKey key, out TValue? value);
            protected abstract bool TryRemove(TKey key, out TValue? value);
            protected abstract void Put(TKey key, TValue value);

            /// <summary>
            /// 移除全部缓存项，并重置算法自身的顺序/频率状态。
            /// </summary>
            /// <param name="evicted">
            /// 非 null 时在其中收集被移除的键值对，供 <see cref="EvictAll"/> 在
            /// 内部状态清空之后再触发事件；为 null 时只清空，不做快照分配。
            /// </param>
            protected abstract void RemoveAllEntries(
                List<KeyValuePair<TKey, TValue>>? evicted);

            readonly IAsyncCache<TKey, TValue>.Factory? asyncFactory;
            readonly ICache<TKey, TValue>.Factory? factory;
        }

        private sealed class NeverEvict<TKey, TValue> : CacheImpl<TKey, TValue>
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

            protected override bool TryPeek(TKey key, out TValue? value) =>
                cache.TryGetValue(key, out value);

            protected override bool TryRemove(TKey key, out TValue? value)
            {
                if (!cache.TryGetValue(key, out value))
                    return false;

                cache.Remove(key);
                return true;
            }

            protected override void RemoveAllEntries(
                List<KeyValuePair<TKey, TValue>>? evicted)
            {
                if (evicted != null)
                {
                    foreach (var pair in cache)
                        evicted.Add(pair);
                }

                cache.Clear();
            }

            public override int Count => cache.Count;

            readonly Dictionary<TKey, TValue> cache = new();
        }

        /// <summary>
        /// FIFO（先进先出）缓存
        /// 淘汰最早插入的值，访问不影响淘汰顺序
        /// </summary>
        /// <remarks>
        /// 顺序表与缓存表共用同一个节点：淘汰或手动移除后节点进入备用槽，
        /// 下一次插入优先复用该节点，因此稳态下插入不再分配 <see cref="LinkedListNode{T}"/>。
        /// </remarks>
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

            protected override bool TryGetCache(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryPeek(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryRemove(TKey key, out TValue? value)
            {
                if (!cache.TryGetValue(key, out var entry))
                {
                    value = default;
                    return false;
                }

                order.Remove(entry.node);
                cache.Remove(key);
                RecycleNode(entry.node);
                value = entry.value;
                return true;
            }

            protected override void Put(TKey key, TValue value)
            {
                if (cache.TryGetValue(key, out var existing))
                {
                    // 已存在：只更新值，不改变插入顺序，也不重复入队
                    cache[key] = (value, existing.node);
                    return;
                }

                bool evicted = false;
                TKey evictedKey = default!;
                TValue evictedValue = default!;
                LinkedListNode<TKey> node;

                if (cache.Count >= capacity && order.First is { } oldestNode)
                {
                    evictedKey = oldestNode.Value;
                    evictedValue = cache[evictedKey].value;
                    order.Remove(oldestNode);
                    cache.Remove(evictedKey);
                    node = oldestNode;
                    node.Value = key;
                    evicted = true;
                }
                else if (spareNode != null)
                {
                    node = spareNode;
                    spareNode = null;
                    node.Value = key;
                }
                else
                {
                    node = new LinkedListNode<TKey>(key);
                }

                order.AddLast(node);
                cache[key] = (value, node);

                // 先完成插入再通知：处理器重入缓存时看到的是完整且不超容量的状态
                if (evicted)
                    PublishEvictedEvent(evictedKey, evictedValue);
            }

            protected override void RemoveAllEntries(
                List<KeyValuePair<TKey, TValue>>? evicted)
            {
                if (evicted != null)
                {
                    foreach (var pair in cache)
                        evicted.Add(new KeyValuePair<TKey, TValue>(
                            pair.Key, pair.Value.value));
                }

                cache.Clear();
                order.Clear();
            }

            void RecycleNode(LinkedListNode<TKey> node)
            {
                // 清空键，避免备用节点持有已移除的 key；节点本身只保留一个。
                node.Value = default!;
                if (spareNode == null)
                    spareNode = node;
            }

            public override int Count => cache.Count;

            readonly Dictionary<TKey, (TValue value, LinkedListNode<TKey> node)> cache = new();
            readonly LinkedList<TKey> order = new();
            readonly int capacity;
            LinkedListNode<TKey>? spareNode;
        }

        /// <summary>
        /// LRU（最近最少使用）缓存
        /// 淘汰最久未被访问的值
        /// </summary>
        /// <remarks>
        /// 访问顺序表与缓存表共用同一个节点：淘汰或手动移除后节点进入备用槽，
        /// 下一次插入优先复用该节点，因此稳态下插入不再分配 <see cref="LinkedListNode{T}"/>。
        /// </remarks>
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
                    // 移到链表尾部（最近使用）；已经在尾部时跳过无意义的摘链/挂链
                    if (accessOrder.Last != entry.node)
                    {
                        accessOrder.Remove(entry.node);
                        accessOrder.AddLast(entry.node);
                    }

                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryPeek(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryRemove(TKey key, out TValue? value)
            {
                if (!cache.TryGetValue(key, out var entry))
                {
                    value = default;
                    return false;
                }

                accessOrder.Remove(entry.node);
                cache.Remove(key);
                RecycleNode(entry.node);
                value = entry.value;
                return true;
            }

            protected override void Put(TKey key, TValue value)
            {
                if (cache.TryGetValue(key, out var existing))
                {
                    // 已存在：更新值并刷新为最近使用，不重复插入节点
                    if (accessOrder.Last != existing.node)
                    {
                        accessOrder.Remove(existing.node);
                        accessOrder.AddLast(existing.node);
                    }

                    cache[key] = (value, existing.node);
                    return;
                }

                bool evicted = false;
                TKey evictedKey = default!;
                TValue evictedValue = default!;
                LinkedListNode<TKey> node;

                if (cache.Count >= capacity && accessOrder.First is { } oldestNode)
                {
                    evictedKey = oldestNode.Value;
                    evictedValue = cache[evictedKey].value;
                    accessOrder.Remove(oldestNode);
                    cache.Remove(evictedKey);
                    node = oldestNode;
                    node.Value = key;
                    evicted = true;
                }
                else if (spareNode != null)
                {
                    node = spareNode;
                    spareNode = null;
                    node.Value = key;
                }
                else
                {
                    node = new LinkedListNode<TKey>(key);
                }

                accessOrder.AddLast(node);
                cache[key] = (value, node);

                // 先完成插入再通知：处理器重入缓存时看到的是完整且不超容量的状态
                if (evicted)
                    PublishEvictedEvent(evictedKey, evictedValue);
            }

            protected override void RemoveAllEntries(
                List<KeyValuePair<TKey, TValue>>? evicted)
            {
                if (evicted != null)
                {
                    foreach (var pair in cache)
                        evicted.Add(new KeyValuePair<TKey, TValue>(
                            pair.Key, pair.Value.value));
                }

                cache.Clear();
                accessOrder.Clear();
            }

            void RecycleNode(LinkedListNode<TKey> node)
            {
                // 清空键，避免备用节点持有已移除的 key；节点本身只保留一个。
                node.Value = default!;
                if (spareNode == null)
                    spareNode = node;
            }

            public override int Count => cache.Count;

            readonly Dictionary<TKey, (TValue value, LinkedListNode<TKey> node)> cache = new();
            readonly LinkedList<TKey> accessOrder = new();
            readonly int capacity;
            LinkedListNode<TKey>? spareNode;
        }

        /// <summary>
        /// LFU（最不经常使用）缓存
        /// 淘汰访问频率最低的值，频率相同时淘汰最早进入该频率的值
        /// </summary>
        /// <remarks>
        /// 频率桶、桶内节点与缓存条目都带备用槽：命中升频、淘汰与手动移除产生的对象
        /// 会被后续操作复用，因此稳态下命中与插入不再分配。
        /// </remarks>
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
                        RecycleBucket(oldFreqList);
                        if (entry.frequency == minFrequency)
                            minFrequency++;
                    }

                    // 加入新频率桶；节点本身从旧桶摘除后可原地复用，不再新建
                    entry.frequency++;
                    if (!freqLists.TryGetValue(entry.frequency, out var newFreqList))
                    {
                        newFreqList = TakeBucket();
                        freqLists[entry.frequency] = newFreqList;
                    }

                    newFreqList.AddLast(entry.listNode);
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryPeek(TKey key, out TValue? value)
            {
                if (cache.TryGetValue(key, out var entry))
                {
                    value = entry.value;
                    return true;
                }

                value = default;
                return false;
            }

            protected override bool TryRemove(TKey key, out TValue? value)
            {
                if (!cache.TryGetValue(key, out var entry))
                {
                    value = default;
                    return false;
                }

                var list = freqLists[entry.frequency];
                list.Remove(entry.listNode);
                if (list.Count == 0)
                {
                    freqLists.Remove(entry.frequency);
                    RecycleBucket(list);
                    if (entry.frequency == minFrequency)
                        RecalculateMinFrequency();
                }

                cache.Remove(key);
                value = entry.value;

                // 条目与节点进入备用槽，下一次插入优先复用
                entry.value = default!;
                entry.frequency = 0;
                entry.listNode.Value = default!;
                spareEntry = entry;
                return true;
            }

            protected override void Put(TKey key, TValue value)
            {
                if (cache.TryGetValue(key, out var existing))
                {
                    // 已存在：只更新值，不改变访问频率与桶位置
                    existing.value = value;
                    return;
                }

                bool evicted = false;
                TKey evictedKey = default!;
                TValue evictedValue = default!;
                LfuEntry entry;

                if (cache.Count >= capacity &&
                    freqLists.TryGetValue(minFrequency, out var minList))
                {
                    var oldestNode = minList.First!;
                    evictedKey = oldestNode.Value;
                    var evictedEntry = cache[evictedKey];
                    evictedValue = evictedEntry.value;
                    minList.Remove(oldestNode);
                    if (minList.Count == 0)
                    {
                        freqLists.Remove(minFrequency);
                        RecycleBucket(minList);
                    }

                    cache.Remove(evictedKey);

                    // 淘汰项的对象与节点原地复用给新键
                    entry = evictedEntry;
                    entry.value = value;
                    entry.frequency = 1;
                    entry.listNode = oldestNode;
                    oldestNode.Value = key;
                    evicted = true;
                }
                else if (spareEntry != null)
                {
                    entry = spareEntry;
                    spareEntry = null;
                    entry.value = value;
                    entry.frequency = 1;
                    entry.listNode.Value = key;
                }
                else
                {
                    entry = new LfuEntry(value, 1, new LinkedListNode<TKey>(key));
                }

                minFrequency = 1;
                if (!freqLists.TryGetValue(1, out var list))
                {
                    list = TakeBucket();
                    freqLists[1] = list;
                }

                list.AddLast(entry.listNode);
                cache[key] = entry;

                // 先完成插入再通知：处理器重入缓存时看到的是完整且不超容量的状态
                if (evicted)
                    PublishEvictedEvent(evictedKey, evictedValue);
            }

            protected override void RemoveAllEntries(
                List<KeyValuePair<TKey, TValue>>? evicted)
            {
                if (evicted != null)
                {
                    foreach (var pair in cache)
                        evicted.Add(new KeyValuePair<TKey, TValue>(
                            pair.Key, pair.Value.value));
                }

                cache.Clear();
                freqLists.Clear();
                minFrequency = 0;

                if (spareEntry != null)
                {
                    spareEntry.value = default!;
                    spareEntry.frequency = 0;
                    spareEntry.listNode.Value = default!;
                }

                spareBucket?.Clear();
            }

            /// <summary>
            /// 取出可复用的空频率桶；没有备用桶时才新建。
            /// </summary>
            LinkedList<TKey> TakeBucket()
            {
                var bucket = spareBucket;
                if (bucket == null)
                    return new LinkedList<TKey>();

                spareBucket = null;
                return bucket;
            }

            /// <summary>
            /// 回收已清空的频率桶。只保留一个备用桶，避免长期持有大量空对象。
            /// </summary>
            void RecycleBucket(LinkedList<TKey> bucket)
            {
                bucket.Clear();
                if (spareBucket == null)
                    spareBucket = bucket;
            }

            /// <summary>
            /// 移除最小频率桶后重新确定最小频率。
            /// 直接在现存桶的键上取最小值，避免频率跨度很大时逐个整数探测。
            /// </summary>
            void RecalculateMinFrequency()
            {
                if (freqLists.Count == 0)
                {
                    minFrequency = 0;
                    return;
                }

                int min = int.MaxValue;
                foreach (var frequency in freqLists.Keys)
                {
                    if (frequency < min)
                        min = frequency;
                }

                minFrequency = min;
            }

            sealed class LfuEntry
            {
                public TValue value;
                public int frequency;
                public LinkedListNode<TKey> listNode;

                public LfuEntry(TValue value, int frequency, LinkedListNode<TKey> listNode)
                {
                    this.value = value;
                    this.frequency = frequency;
                    this.listNode = listNode;
                }
            }

            public override int Count => cache.Count;

            readonly Dictionary<TKey, LfuEntry> cache = new();
            readonly Dictionary<int, LinkedList<TKey>> freqLists = new();
            readonly int capacity;
            int minFrequency;
            LinkedList<TKey>? spareBucket;
            LfuEntry? spareEntry;
        }
    }
}
