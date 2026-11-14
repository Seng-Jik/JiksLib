using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Collections;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 事件总线
    /// 可以发布以某一类型为基础的不同类型的事件
    /// TBaseEvent可以是类或接口类型
    /// 仅支持在主线程上使用
    /// 提供最强大的功能，但性能比ValueEventBus差
    /// TypeChain 缓存可以在多个总线实例之间共享，即使 TBaseEvent 不同；
    /// 每条链会为不同 TBaseEvent 缓存独立的过滤视图。
    /// </summary>
    public sealed class EventBus<TBaseEvent>
        where TBaseEvent : class
    {
        /// <summary>
        /// 构造事件总线，同时构造其事件发布器。
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        /// <param name="typeChainCache">
        /// 用于缓存事件运行时类型对应的 EventBusTypeChain 的缓存。
        /// 缓存按运行时事件类型存储完整的类链和接口信息，单个缓存可由任意
        /// <see cref="EventBus{TBaseEvent}"/> 共享；不同 TBaseEvent 的过滤视图
        /// 按需缓存在 EventBusTypeChain 内，互不干扰。
        /// 共享时，缓存容量/淘汰算法、WarmupPublish 的视图预热状态以及
        /// PurgeListenerSlot/PurgeListenerSlots 的清理会作用于所有共享者；
        /// 被清理后其他总线会在下次发布时重建对应视图，不影响正确性。
        /// </param>
        /// <param name="allowAddRemoveListenerDuringPublish">
        /// 是否允许在发布期间添加或移除监听器；允许时操作会延迟到整个最外层发布结束后生效。
        /// 不能与 <paramref name="autoPurgeTypeInfo"/> 同时开启。
        /// </param>
        /// <param name="allowRecursivePublish">
        /// 是否允许在发布期间再次发布事件；允许时嵌套事件进入 FIFO 队列，
        /// 由最外层发布排空。
        /// </param>
        /// <param name="autoPurgeTypeInfo">
        /// 是否在某个事件类型移除最后一个监听器时自动删除其内部存储。
        /// 开启后不能使用 <c>WarmupListenerSlot</c>、<c>PurgeListenerSlot</c>
        /// 或 <c>PurgeListenerSlots</c>。
        /// </param>
        /// <param name="purgeTypeChainCacheWhenAutoPurgeTypeInfo">
        /// 自动清理类型信息时，是否同时从共享 TypeChain 缓存中移除对应的运行时类型。
        /// 默认 true；关闭后仅删除监听器存储，缓存中的类型链保留以供复用。
        /// </param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="allowAddRemoveListenerDuringPublish"/> 与
        /// <paramref name="autoPurgeTypeInfo"/> 同时为 true 时抛出。
        /// </exception>
        public EventBus(
            out Publisher publisher,
            ICache<Type, EventBusTypeChain> typeChainCache,
            bool allowAddRemoveListenerDuringPublish = false,
            bool allowRecursivePublish = false,
            bool autoPurgeTypeInfo = false,
            bool purgeTypeChainCacheWhenAutoPurgeTypeInfo = true)
        {
            if (allowAddRemoveListenerDuringPublish && autoPurgeTypeInfo)
                throw new ArgumentException(
                    "Cannot allow both adding/removing listeners and auto-purging type info.",
                    nameof(autoPurgeTypeInfo));

            publisher = new(this);

            this.allowAddRemoveListenerDuringPublish =
                allowAddRemoveListenerDuringPublish;

            this.purgeTypeChainCacheWhenAutoPurgeTypeInfo =
                purgeTypeChainCacheWhenAutoPurgeTypeInfo;

            this.allowRecursivePublish = allowRecursivePublish;
            this.autoPurgeTypeInfo = autoPurgeTypeInfo;
            this.typeChainCache = typeChainCache;
        }

        /// <summary>
        /// 使用每个实例独立创建的内置 TypeChain 缓存构造事件总线。
        /// 如果多个总线需要共享 TypeChain 缓存，请使用接受 <see cref="ICache{TKey,TValue}"/>
        /// 的构造函数传入同一个缓存实例。
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        /// <param name="allowAddRemoveListenerDuringPublish">
        /// 是否允许在发布期间添加或移除监听器；允许时操作会延迟到整个最外层发布结束后生效。
        /// 不能与 <paramref name="autoPurgeTypeInfo"/> 同时开启。
        /// </param>
        /// <param name="allowRecursivePublish">
        /// 是否允许在发布期间再次发布事件；允许时嵌套事件进入 FIFO 队列，
        /// 由最外层发布排空。
        /// </param>
        /// <param name="autoPurgeTypeInfo">
        /// 是否在某个事件类型移除最后一个监听器时自动删除其内部存储。
        /// 是否同时清理 TypeChain 由
        /// <paramref name="purgeTypeChainCacheWhenAutoPurgeTypeInfo"/> 控制。
        /// </param>
        /// <param name="typeChainCacheAlgorithm">
        /// TypeChain 缓存的淘汰算法，默认 <see cref="Cache.Algorithm.NeverEvict"/>。
        /// </param>
        /// <param name="typeChainCacheCapacity">
        /// TypeChain 缓存容量；<see cref="Cache.Algorithm.NeverEvict"/> 忽略此参数。
        /// </param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="allowAddRemoveListenerDuringPublish"/> 与
        /// <paramref name="autoPurgeTypeInfo"/> 同时为 true 时抛出。
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当容量不合法或 <paramref name="typeChainCacheAlgorithm"/> 未知时抛出。
        /// </exception>
        public EventBus(
            out Publisher publisher,
            bool allowAddRemoveListenerDuringPublish = false,
            bool allowRecursivePublish = false,
            bool autoPurgeTypeInfo = false,
            bool purgeTypeChainCacheWhenAutoPurgeTypeInfo = true,
            Cache.Algorithm typeChainCacheAlgorithm = Cache.Algorithm.NeverEvict,
            int typeChainCacheCapacity = 32) : this(
                out publisher,
                EventBusTypeChain.MakeTypeChainCache(
                    typeChainCacheAlgorithm,
                    typeChainCacheCapacity),
                allowAddRemoveListenerDuringPublish,
                allowRecursivePublish,
                autoPurgeTypeInfo,
                purgeTypeChainCacheWhenAutoPurgeTypeInfo)
        { }


        /// <summary>
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类或子接口
        /// </summary>
        public void AddListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : TBaseEvent
        {
            listener.ThrowIfNull(nameof(listener));

            SafeEvent<TEvent> safeEvent;

            if (innerEvents.TryGetValue(typeof(TEvent), out var h))
                safeEvent = (SafeEvent<TEvent>)h.InnerSafeEvent;
            else
                (safeEvent, _) = CreateAndRegisterInnerEvent<TEvent>(0);

            safeEvent.AddListener(listener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类或子接口
        /// </summary>
        public void RemoveListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : TBaseEvent
        {
            listener.ThrowIfNull(nameof(listener));

            if (innerEvents.TryGetValue(typeof(TEvent), out var h))
            {
                var se = (SafeEvent<TEvent>)h.InnerSafeEvent;
                se.RemoveListener(listener);

                if (autoPurgeTypeInfo && se.ListenerCount <= 0)
                {
                    innerEvents.Remove(typeof(TEvent));

                    if (purgeTypeChainCacheWhenAutoPurgeTypeInfo)
                        typeChainCache.Evict(typeof(TEvent));
                }
            }
        }

        /// <summary>
        /// 获取指定事件类型当前已应用的监听器数量。
        /// 只统计注册在该确切类型上的监听器：不会把基类或接口上的监听器
        /// 计入派生事件类型的数量。未注册过该事件类型时返回 0；
        /// 重复注册的同一监听器按注册次数计数。
        /// 若启用了发布期间增删，返回值是当前已应用的数量：待添加的监听器尚未计入，
        /// 待移除的监听器仍会计入，直到整个最外层发布（包括递归队列排空）结束后才更新。
        /// 该方法不预测本次发布最终会保留多少监听器，也不统计会收到该事件的所有监听器。
        /// </summary>
        /// <typeparam name="TEvent">要查询的事件类型。</typeparam>
        public int GetListenersCount<TEvent>()
            where TEvent : TBaseEvent =>
            GetListenersCount(typeof(TEvent));

        /// <summary>
        /// 获取指定事件类型当前已应用的监听器数量。
        /// 只统计注册在该确切类型上的监听器。
        /// </summary>
        /// <param name="eventType">
        /// 要查询的事件类型；应与注册监听器时使用的类型一致。
        /// </param>
        /// <returns>
        /// 当前已应用的监听器数量；类型未注册或被自动清理时返回 0。
        /// 发布期间延迟增删的可见性同泛型重载。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="eventType"/> 为 null 时抛出。
        /// </exception>
        public int GetListenersCount(Type eventType)
        {
            if (innerEvents.TryGetValue(eventType.ThrowIfNull(nameof(eventType)), out var h))
                return h.GetListenerCount(h.InnerSafeEvent);

            return 0;
        }

        /// <summary>
        /// 预热某一事件类型的监听器存储
        /// 提前创建该事件类型的内部事件，并预分配指定数量的监听器位置；
        /// 之后为该事件类型注册不超过<paramref name="capacity"/>个监听器时不会触发扩容分配。
        /// </summary>
        /// <typeparam name="TEvent">
        /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
        /// </typeparam>
        /// <param name="capacity">
        /// 该事件类型预计注册的监听器数量（必须非负，0 表示只提前创建存储）
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当<paramref name="capacity"/>为负数时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 当该事件类型已经预热过，或已经注册过监听器时抛出
        /// </exception>
        /// <remarks>
        /// 每个事件类型有独立的监听器存储，需要分别预热；预热基类类型不会为派生类型预留容量。
        /// 建议在启动期集中调用；预热只影响监听器注册路径，不改变事件发布的行为。
        /// </remarks>
        public void WarmupListenerSlot<TEvent>(int capacity)
            where TEvent : TBaseEvent
        {
            if (autoPurgeTypeInfo)
                throw new InvalidOperationException(
                    "WarmupListenerSlot cannot be used when autoPurgeTypeInfo is enabled.");

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            Type eventType = typeof(TEvent);

            if (innerEvents.ContainsKey(eventType))
                throw new InvalidOperationException(
                    "WarmupListenerSlot can only be called once per event type, before any listener is added.");

            CreateAndRegisterInnerEvent<TEvent>(capacity);
        }

        /// <summary>
        /// 手动清理指定事件类型的监听器存储。
        /// 只有该类型当前没有任何监听器时才会移除。
        /// </summary>
        /// <param name="typeInfo">要清理的事件类型。</param>
        /// <param name="purgeTypeChainCache">
        /// 是否同时从 TypeChain 缓存中移除该运行时类型，默认 false。
        /// 若缓存与其他总线共享，清理会影响所有共享者；
        /// 其他总线后续发布会重建该类型链，不影响正确性。
        /// </param>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="typeInfo"/> 为 null 时抛出。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeTypeInfo</c> 时抛出。
        /// </exception>
        public bool PurgeListenerSlot(Type typeInfo, bool purgeTypeChainCache = false)
        {
            typeInfo.ThrowIfNull(nameof(typeInfo));

            if (autoPurgeTypeInfo)
                throw new InvalidOperationException(
                    "PurgeListenerSlot cannot be called when autoPurgeTypeInfo is enabled.");

            if (innerEvents.TryGetValue(typeInfo, out var h))
            {
                if (h.GetListenerCount(h.InnerSafeEvent) > 0)
                    return false;

                innerEvents.Remove(typeInfo);
            }

            if (purgeTypeChainCache)
                typeChainCache.Evict(typeInfo);

            return true;
        }

        /// <summary>
        /// 手动清理指定事件类型的监听器存储。
        /// 只有该类型当前没有任何监听器时才会移除。
        /// </summary>
        /// <typeparam name="TEvent">要清理的事件类型。</typeparam>
        /// <param name="purgeTypeChainCache">
        /// 是否同时从 TypeChain 缓存中移除该运行时类型，默认 false。
        /// </param>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        public bool PurgeListenerSlot<TEvent>(bool purgeTypeChainCache = false)
            where TEvent : TBaseEvent =>
            PurgeListenerSlot(typeof(TEvent), purgeTypeChainCache);

        /// <summary>
        /// 清理所有当前没有监听器的事件类型。
        /// </summary>
        /// <param name="typesRemoved">实际被移除的事件类型列表。</param>
        /// <param name="typesRemaining">清理后仍保留的事件类型数量。</param>
        /// <param name="purgeTypeChainCache">
        /// 是否同时从 TypeChain 缓存中移除被清理的运行时类型，默认 false。
        /// 若缓存与其他总线共享，清理会影响所有共享者；
        /// 其他总线后续发布会重建对应类型链，不影响正确性。
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeTypeInfo</c> 时抛出。
        /// </exception>
        public void PurgeListenerSlots(
            out List<Type> typesRemoved,
            out int typesRemaining,
            bool purgeTypeChainCache = false)
        {
            if (autoPurgeTypeInfo)
                throw new InvalidOperationException(
                    "PurgeListenerSlots cannot be called when autoPurgeTypeInfo is enabled.");

            typesRemoved = new();

            foreach (var i in innerEvents)
                if (i.Value.GetListenerCount(i.Value.InnerSafeEvent) == 0)
                    typesRemoved.Add(i.Key);

            foreach (var t in typesRemoved)
            {
                innerEvents.Remove(t);
                if (purgeTypeChainCache) typeChainCache.Evict(t);
            }

            typesRemaining = innerEvents.Count;
        }

        /// <summary>
        /// 事件发布器
        /// </summary>
        public readonly struct Publisher : ISafeEventPublisher<TBaseEvent>
        {
            /// <summary>
            /// 预热某一具体事件类型的发布路径。
            /// 提前构建并缓存该类型的类型链，避免第一次发布该类型时在热路径上分配；
            /// 如果对应视图已经存在，则不做任何事。
            /// </summary>
            /// <param name="type">
            /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// 当<paramref name="type"/>为null时抛出
            /// </exception>
            /// <exception cref="ArgumentException">
            /// 当<paramref name="type"/>不能赋值给<typeparamref name="TBaseEvent"/>时抛出
            /// </exception>
            /// <remarks>
            /// 类型链按事件的运行时类型缓存：预热基类或接口不会覆盖派生类型，需要按实际发布的事件类型分别预热。
            /// 预热只消除第一次发布时的类型链构建，不改变每次发布的查找与派发成本。
            /// 重复调用是幂等的；若 TypeChain 缓存与多个总线共享，同一
            /// (TBaseEvent, 运行时类型) 视图只会构建一次。
            /// </remarks>
            public void WarmupPublish(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (!typeof(TBaseEvent).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type {type} is not assignable to {typeof(TBaseEvent)}.",
                        nameof(type));

                bus
                    .typeChainCache
                    .Access(type)
                    .GetViewWithoutObjectType(typeof(TBaseEvent));
            }

            /// <summary>
            /// 预热某一具体事件类型的发布路径。
            /// 如果对应视图已经存在，则不做任何事。
            /// </summary>
            /// <typeparam name="TEvent">
            /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
            /// </typeparam>
            /// <remarks>
            /// 与<see cref="WarmupPublish(Type)"/>相同，只是用泛型参数在编译期确定类型。
            /// </remarks>
            public void WarmupPublish<TEvent>()
                where TEvent : TBaseEvent =>
                WarmupPublish(typeof(TEvent));

            /// <summary>
            /// 发布事件
            /// </summary>
            /// <param name="@event">事件对象</param>
            /// <param name="exceptionsOutput">事件监听器产生的异常将会被写入到这个列表中，为null则静默忽略异常</param>
            public void Publish(
                TBaseEvent @event,
                ICollection<Exception>? exceptionsOutput)
            {
                var baseEventType = typeof(TBaseEvent);
                var eventType = @event.ThrowIfNull().GetType();

                var typeChain = bus
                    .typeChainCache
                    .Access(eventType);

                PublishOneShot(@event, eventType, exceptionsOutput);

                foreach (var i in typeChain.GetViewWithoutObjectType(baseEventType))
                {
                    PublishOneShot(@event, i, exceptionsOutput);

                    if (!baseEventType.IsInterface && i == baseEventType)
                        break;
                }

                if (baseEventType.IsInterface)
                    PublishOneShot(@event, baseEventType, exceptionsOutput);
                else if (baseEventType == typeof(object) && eventType != typeof(object))
                    PublishOneShot(@event, baseEventType, exceptionsOutput);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void PublishOneShot(
                TBaseEvent @event,
                Type publishAsType,
                ICollection<Exception>? exceptionsOutput)
            {
                if (bus.innerEvents.TryGetValue(publishAsType, out var h))
                    h.Publish(h.InnerSafeEvent, @event, exceptionsOutput);
            }

            readonly EventBus<TBaseEvent> bus;

            internal Publisher(EventBus<TBaseEvent> eventBus)
            {
                bus = eventBus;
            }
        }

        /// <summary>
        /// 创建某一事件类型的内部事件及其发布委托
        /// </summary>
        (SafeEvent<TEvent>, InnerEvent) CreateAndRegisterInnerEvent<TEvent>(
            int capacity)
            where TEvent : TBaseEvent
        {
            SafeEvent<TEvent> safeEvent = new(
                out var publisher,
                allowAddRemoveListenerDuringPublish,
                allowRecursivePublish,
                capacity);

            InnerEvent innerEvent = new(
                safeEvent,
                static (e, eo, ex) =>
                    new SafeEvent<TEvent>.Publisher((SafeEvent<TEvent>)e)
                        .Publish((TEvent)eo, ex),
                static e => ((SafeEvent<TEvent>)e).ListenerCount);

            innerEvents.Add(typeof(TEvent), innerEvent);

            return (safeEvent, innerEvent);
        }

        private readonly struct InnerEvent
        {
            internal delegate void PublishFunction(
                object safeEvent,
                TBaseEvent baseEvent,
                ICollection<Exception>? exceptionsOutput);

            internal delegate int GetListenerCountFunction(object safeEvent);

            internal readonly object InnerSafeEvent;
            internal readonly PublishFunction Publish;
            internal readonly GetListenerCountFunction GetListenerCount;

            internal InnerEvent(
                object innerSafeEvent,
                PublishFunction publish,
                GetListenerCountFunction getListenerCount)
            {
                InnerSafeEvent = innerSafeEvent;
                Publish = publish;
                GetListenerCount = getListenerCount;
            }
        }

        /// <summary>TypeChain 缓存；可由任意 EventBus 共享，并按 TBaseEvent 缓存过滤视图。</summary>
        readonly ICache<Type, EventBusTypeChain> typeChainCache;
        readonly Dictionary<Type, InnerEvent> innerEvents = new();
        readonly bool allowAddRemoveListenerDuringPublish;
        readonly bool allowRecursivePublish;
        readonly bool autoPurgeTypeInfo;
        readonly bool purgeTypeChainCacheWhenAutoPurgeTypeInfo;
    }

    /// <summary>
    /// 事件运行时类型对应的类型链缓存。
    /// 保存接口集合和类继承链（不含运行时类型与 object，二者由 Publisher 单独派发），
    /// 并按接口基础类型缓存过滤视图；基础接口本身不进入视图，而是由 Publisher
    /// 单独派发，避免只实现单个接口的类产生大量单元素数组。
    /// 可由任意 EventBus 实例共享，即使 TBaseEvent 不同。
    /// </summary>
    public sealed class EventBusTypeChain
    {
        /// <summary>
        /// 创建可共享的 EventBusTypeChain 缓存。
        /// </summary>
        /// <param name="algorithm">缓存淘汰算法，默认 <see cref="Cache.Algorithm.NeverEvict"/>。</param>
        /// <param name="capacity">缓存容量；NeverEvict 忽略此参数。</param>
        /// <returns>可由多个 EventBus 共享的 TypeChain 缓存。</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当容量不合法或 <paramref name="algorithm"/> 未知时抛出。
        /// </exception>
        public static ICache<Type, EventBusTypeChain> MakeTypeChainCache(
            Cache.Algorithm algorithm = Cache.Algorithm.NeverEvict,
            int capacity = 32) =>
            Cache.Make<Type, EventBusTypeChain>(
                algorithm,
                static x => new(x),
                capacity);

        internal EventBusTypeChain(Type type)
        {
            interfaces = type.GetInterfaces();
            viewForInterfaceBasedEvent = null;

            List<Type>? baseTypes = null;

            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (current == typeof(object)) break;
                baseTypes ??= new(8);
                baseTypes.Add(current);
            }

            if (baseTypes == null) this.baseTypes = Array.Empty<Type>();
            else this.baseTypes = baseTypes.ToArray();
        }

        /// <summary>
        /// 获取不包含 object 和基础接口本身的派发视图。
        /// 类基础类型返回完整基类链；接口基础类型返回可赋值的基类与派生接口。
        /// 基础接口由 Publisher 单独派发，避免只实现单个接口时为每个 BaseType
        /// 缓存一个单元素小数组。
        /// </summary>
        internal Type[] GetViewWithoutObjectType(Type baseEventType)
        {
            if (!baseEventType.IsInterface) return baseTypes;

            if (viewForInterfaceBasedEvent != null
                && viewForInterfaceBasedEvent.TryGetValue(baseEventType, out var i))
                return i;

            viewForInterfaceBasedEvent ??= new();

            int typesInViewCount = 0;

            foreach (var baseType in baseTypes)
                if (baseEventType.IsAssignableFrom(baseType))
                    typesInViewCount++;

            foreach (var @interface in interfaces)
                if (baseEventType.IsAssignableFrom(@interface)
                    && @interface != baseEventType)
                    typesInViewCount++;

            Type[] filtered = typesInViewCount == 0
                ? Array.Empty<Type>()
                : new Type[typesInViewCount];
            int writeIndex = 0;

            foreach (var baseType in baseTypes)
                if (baseEventType.IsAssignableFrom(baseType))
                    filtered[writeIndex++] = baseType;

            foreach (var @interface in interfaces)
                if (baseEventType.IsAssignableFrom(@interface)
                    && @interface != baseEventType)
                    filtered[writeIndex++] = @interface;

            viewForInterfaceBasedEvent[baseEventType] = filtered;
            return filtered;
        }

        /// <summary>运行时类型的基类链，按派生到基排列，不含运行时类型和 object。</summary>
        readonly Type[] baseTypes;

        /// <summary>运行时类型实现的全部接口。</summary>
        readonly Type[] interfaces;

        /// <summary>按基础接口缓存的过滤视图。</summary>
        Dictionary<Type, Type[]>? viewForInterfaceBasedEvent;
    }
}
