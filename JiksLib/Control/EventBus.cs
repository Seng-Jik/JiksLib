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
    /// TypeChain 缓存按 TBaseEvent 独立：相同基类型的多个总线可以共享缓存，
    /// 不同基类型的缓存内容不同，不能共用同一个缓存。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 发布期间增删监听器的"延迟"是<b>按事件类型独立</b>生效的：每个事件类型拥有独立的
    /// <see cref="SafeEvent{TEvent}"/> 会话。因此监听器在处理运行时类型 E 的事件时去增删
    /// 基类型 B 的监听器，由于 B 的会话尚未开始，改动会立即生效，并影响同一次发布中随后
    /// 对 B 的派发。这是"每种事件类型独立维护重入策略"的固有特性，不是缺陷；
    /// 若需要跨类型的一致性，请在发布前完成监听器增删。
    /// </para>
    /// <para>
    /// 事件派发只覆盖运行时类型的实际类型链：运行时类型本身、它实现的接口、它的基类，
    /// 以及 <typeparamref name="TBaseEvent"/>。不会为泛型变体（协变 / 逆变）额外合成派发目标：
    /// 例如 <c>IHandler&lt;Base&gt;</c> 与 <c>IHandler&lt;Derived&gt;</c> 之间的变体转换，
    /// 不会让注册在其中一个上的监听器收到另一个的事件。需要跨变体处理时，请把监听器注册到
    /// 共同的 <typeparamref name="TBaseEvent"/>，或按运行时类型分别注册。
    /// </para>
    /// </remarks>
    public sealed class EventBus<TBaseEvent>
        where TBaseEvent : class
    {
        /// <summary>
        /// 构造事件总线，同时构造其事件发布器。
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        /// <param name="typeChainCache">
        /// 用于缓存事件运行时类型对应的 TypeChain 的缓存。
        /// 缓存内容依赖当前 TBaseEvent，只能由相同 TBaseEvent 的多个总线共享；
        /// 不同 TBaseEvent 的 TypeChain 不兼容。
        /// 共享时，缓存容量/淘汰算法、WarmupPublish 的预热状态以及
        /// ClearTypeChainCache 的清理会作用于所有共享者；
        /// 被清理后其他总线会在下次发布时重建对应类型链，不影响正确性。
        /// </param>
        /// <param name="allowAddRemoveListenerDuringPublish">
        /// 是否允许在发布期间添加或移除监听器；允许时操作会延迟到该事件类型的最外层发布
        /// （包括其递归队列排空）结束后生效。注意延迟是<b>按事件类型独立</b>的，
        /// 对其它事件类型的增删会立即生效，详见类型注释。
        /// 不能与 <paramref name="autoPurgeListenerSlot"/> 同时开启。
        /// </param>
        /// <param name="allowRecursivePublish">
        /// 是否允许在发布期间再次发布事件；允许时嵌套事件进入 FIFO 队列，
        /// 由最外层发布排空。
        /// </param>
        /// <param name="autoPurgeListenerSlot">
        /// 是否在某个事件类型移除最后一个监听器时自动删除其内部存储。
        /// 开启后不能使用 <c>WarmupListenerSlot</c>、<c>PurgeListenerSlot</c>
        /// 或 <c>PurgeListenerSlots</c>。
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="typeChainCache"/> 为 null 时抛出。
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="allowAddRemoveListenerDuringPublish"/> 与
        /// <paramref name="autoPurgeListenerSlot"/> 同时为 true 时抛出。
        /// </exception>
        public EventBus(
            out Publisher publisher,
            ICache<Type, TypeChain> typeChainCache,
            bool allowAddRemoveListenerDuringPublish = false,
            bool allowRecursivePublish = false,
            bool autoPurgeListenerSlot = false)
        {
            if (allowAddRemoveListenerDuringPublish && autoPurgeListenerSlot)
                throw new ArgumentException(
                    "Cannot allow both adding/removing listeners and auto-purging listener slots.",
                    nameof(autoPurgeListenerSlot));

            this.typeChainCache = typeChainCache.ThrowIfNull(nameof(typeChainCache));

            publisher = new(this);

            this.allowAddRemoveListenerDuringPublish =
                allowAddRemoveListenerDuringPublish;

            this.allowRecursivePublish = allowRecursivePublish;
            this.autoPurgeListenerSlot = autoPurgeListenerSlot;
        }

        /// <summary>
        /// 使用每个实例独立创建的内置 TypeChain 缓存构造事件总线。
        /// 如果多个相同 TBaseEvent 的总线需要共享 TypeChain 缓存，请使用接受
        /// <see cref="ICache{TKey,TValue}"/> 的构造函数传入同一个缓存实例。
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        /// <param name="allowAddRemoveListenerDuringPublish">
        /// 是否允许在发布期间添加或移除监听器；允许时操作会延迟到该事件类型的最外层发布
        /// （包括其递归队列排空）结束后生效。注意延迟是<b>按事件类型独立</b>的，
        /// 对其它事件类型的增删会立即生效，详见类型注释。
        /// 不能与 <paramref name="autoPurgeListenerSlot"/> 同时开启。
        /// </param>
        /// <param name="allowRecursivePublish">
        /// 是否允许在发布期间再次发布事件；允许时嵌套事件进入 FIFO 队列，
        /// 由最外层发布排空。
        /// </param>
        /// <param name="autoPurgeListenerSlot">
        /// 是否在某个事件类型移除最后一个监听器时自动删除其内部监听器存储。
        /// 该选项不会清理 TypeChain 缓存。
        /// </param>
        /// <param name="typeChainCacheAlgorithm">
        /// TypeChain 缓存的淘汰算法，默认 <see cref="Cache.Algorithm.NeverEvict"/>。
        /// </param>
        /// <param name="typeChainCacheCapacity">
        /// TypeChain 缓存容量；<see cref="Cache.Algorithm.NeverEvict"/> 忽略此参数。
        /// </param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="allowAddRemoveListenerDuringPublish"/> 与
        /// <paramref name="autoPurgeListenerSlot"/> 同时为 true 时抛出。
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当容量不合法或 <paramref name="typeChainCacheAlgorithm"/> 未知时抛出。
        /// </exception>
        public EventBus(
            out Publisher publisher,
            bool allowAddRemoveListenerDuringPublish = false,
            bool allowRecursivePublish = false,
            bool autoPurgeListenerSlot = false,
            Cache.Algorithm typeChainCacheAlgorithm = Cache.Algorithm.NeverEvict,
            int typeChainCacheCapacity = 32) : this(
                out publisher,
                TypeChain.MakeTypeChainCache(
                    typeChainCacheAlgorithm,
                    typeChainCacheCapacity),
                allowAddRemoveListenerDuringPublish,
                allowRecursivePublish,
                autoPurgeListenerSlot)
        { }


        /// <summary>
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类或子接口
        /// </summary>
        public void AddListener<TEvent>(SafeEventHandler<TEvent> listener)
            where TEvent : TBaseEvent
        {
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
        public void RemoveListener<TEvent>(SafeEventHandler<TEvent> listener)
            where TEvent : TBaseEvent
        {
            if (innerEvents.TryGetValue(typeof(TEvent), out var h))
            {
                var se = (SafeEvent<TEvent>)h.InnerSafeEvent;
                se.RemoveListener(listener);

                if (autoPurgeListenerSlot && se.ListenerCount <= 0)
                    innerEvents.Remove(typeof(TEvent));
            }
            else
            {
                listener.ThrowIfNull(nameof(listener));
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
            if (autoPurgeListenerSlot)
                throw new InvalidOperationException(
                    "WarmupListenerSlot cannot be used when autoPurgeListenerSlot is enabled.");

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
        /// <param name="eventType">要清理的事件类型。</param>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="eventType"/> 为 null 时抛出。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeListenerSlot</c> 时抛出。
        /// </exception>
        public bool PurgeListenerSlot(Type eventType)
        {
            eventType.ThrowIfNull(nameof(eventType));

            if (!typeof(TBaseEvent).IsAssignableFrom(eventType))
            {
                throw new ArgumentException(
                    $"Event type {eventType} is not assignable to {typeof(TBaseEvent)}.",
                    nameof(eventType));
            }

            if (autoPurgeListenerSlot)
                throw new InvalidOperationException(
                    "PurgeListenerSlot cannot be called when autoPurgeListenerSlot is enabled.");

            if (innerEvents.TryGetValue(eventType, out var h))
            {
                if (h.GetListenerCount(h.InnerSafeEvent) > 0)
                    return false;

                innerEvents.Remove(eventType);
            }

            return true;
        }

        /// <summary>
        /// 手动清理指定事件类型的监听器存储。
        /// 只有该类型当前没有任何监听器时才会移除。
        /// </summary>
        /// <typeparam name="TEvent">要清理的事件类型。</typeparam>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeListenerSlot</c> 时抛出。
        /// </exception>
        public bool PurgeListenerSlot<TEvent>()
            where TEvent : TBaseEvent =>
            PurgeListenerSlot(typeof(TEvent));

        /// <summary>
        /// 清理所有当前没有监听器的事件类型。
        /// </summary>
        /// <param name="typesRemoved">实际被移除的事件类型列表。</param>
        /// <param name="typesRemaining">清理后仍保留的事件类型数量。</param>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeListenerSlot</c> 时抛出。
        /// </exception>
        public void PurgeListenerSlots(
            out List<Type> typesRemoved,
            out int typesRemaining)
        {
            if (autoPurgeListenerSlot)
                throw new InvalidOperationException(
                    "PurgeListenerSlots cannot be called when autoPurgeListenerSlot is enabled.");

            typesRemoved = new();

            foreach (var i in innerEvents)
                if (i.Value.GetListenerCount(i.Value.InnerSafeEvent) == 0)
                    typesRemoved.Add(i.Key);

            foreach (var t in typesRemoved)
                innerEvents.Remove(t);

            typesRemaining = innerEvents.Count;
        }

        /// <summary>
        /// 事件发布器
        /// </summary>
        public readonly struct Publisher : ISafeEventPublisher<TBaseEvent>
        {
            /// <summary>
            /// 发布事件
            /// </summary>
            /// <param name="event">事件对象</param>
            /// <param name="exceptionsOutput">事件监听器产生的异常将会被写入到这个列表中，为null则静默忽略异常</param>
            public void Publish(
                TBaseEvent @event,
                ICollection<Exception>? exceptionsOutput)
            {
                var baseEventType = typeof(TBaseEvent);
                var eventType = @event.ThrowIfNull(nameof(@event)).GetType();

                var typeChain = bus
                    .typeChainCache
                    .Access(eventType);

                PublishOneShot(@event, eventType, exceptionsOutput);

                foreach (var i in typeChain.TypesToPublish)
                {
                    PublishOneShot(@event, i, exceptionsOutput);
                }

                if (baseEventType != eventType)
                    PublishOneShot(@event, baseEventType, exceptionsOutput);
            }

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
            /// 重复调用是幂等的；若 TypeChain 缓存与相同 TBaseEvent 的其他总线共享，
            /// 同一运行时类型链只会构建一次。
            /// </remarks>
            public void WarmupPublish(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (!typeof(TBaseEvent).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type {type} is not assignable to {typeof(TBaseEvent)}.",
                        nameof(type));

                bus.typeChainCache.Access(type);
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
            /// 清空当前 TBaseEvent 的 TypeChain 缓存。
            /// 会移除所有运行时类型链，包括已经预热但没有监听器的类型；
            /// 不会清理监听器存储，下一次发布对应类型时会重新构建类型链。
            /// 若缓存与相同 TBaseEvent 的其他总线共享，会同时清空它们的缓存；
            /// 预热状态也会丢失。底层使用 <see cref="ICache{TKey,TValue}.Clear"/>，
            /// 不触发 OnEvicted。
            /// </summary>
            public void ClearTypeChainCache() =>
                bus.typeChainCache.Clear();

            /// <summary>
            /// 当前 TypeChain 缓存中的条目数量。
            /// 包含已经预热但没有监听器的运行时类型；若缓存与相同 TBaseEvent 的
            /// 其他总线共享，则返回共享缓存的总条目数。
            /// </summary>
            public int CachedTypeChains => bus.typeChainCache.Count;

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
        /// 某个 EventBus 基类型专用的类型链缓存。
        /// 每个 TBaseEvent 拥有独立的 TypeChain 与缓存内容，不能跨不同基类型共享。
        /// 运行时事件类型由 Publisher 直接派发，因此列表只保存需要额外派发的接口或基类类型。
        /// </summary>
        public readonly struct TypeChain
        {
            /// <summary>
            /// 创建当前 TBaseEvent 专用的 TypeChain 缓存。
            /// </summary>
            /// <param name="algorithm">缓存淘汰算法，默认 <see cref="Cache.Algorithm.NeverEvict"/>。</param>
            /// <param name="capacity">缓存容量；NeverEvict 忽略此参数。</param>
            /// <returns>可由相同 TBaseEvent 的多个 EventBus 共享的 TypeChain 缓存。</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// 当容量不合法或 <paramref name="algorithm"/> 未知时抛出。
            /// </exception>
            public static ICache<Type, TypeChain> MakeTypeChainCache(
                Cache.Algorithm algorithm = Cache.Algorithm.NeverEvict,
                int capacity = 32) =>
                Cache.Make<Type, TypeChain>(
                    algorithm,
                    static x => new(x),
                    capacity);

            internal TypeChain(Type type)
            {
                int typeChainLength = 0;

                for (
                    var i = type.BaseType;
                    i != null && i != typeof(object) && i != typeof(TBaseEvent);
                    i = i.BaseType)
                {
                    if (typeof(TBaseEvent).IsAssignableFrom(i))
                        typeChainLength++;
                }

                Type[]? interfaces = null;

                if (typeof(TBaseEvent).IsInterface)
                {
                    interfaces = type.GetInterfaces();

                    foreach (var i in interfaces)
                        if (typeof(TBaseEvent).IsAssignableFrom(i)
                            && i != typeof(TBaseEvent))
                            typeChainLength++;
                }

                if (typeChainLength == 0)
                    TypesToPublish = Array.Empty<Type>();
                else
                {
                    TypesToPublish = new Type[typeChainLength];
                    int writeIndex = 0;

                    if (interfaces != null)
                    {
                        foreach (var i in interfaces)
                            if (typeof(TBaseEvent).IsAssignableFrom(i)
                                && i != typeof(TBaseEvent))
                                TypesToPublish[writeIndex++] = i;
                    }

                    for (
                        var i = type.BaseType;
                        i != null && i != typeof(object) && i != typeof(TBaseEvent);
                        i = i.BaseType)
                    {
                        if (typeof(TBaseEvent).IsAssignableFrom(i))
                            TypesToPublish[writeIndex++] = i;
                    }
                }
            }

            /// <summary>
            /// 运行时类型之外需要额外派发的类型；接口基础类型先写接口再写基类，
            /// 类基础类型按派生到基排列。
            /// </summary>
            internal readonly Type[] TypesToPublish;
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

        readonly ICache<Type, TypeChain> typeChainCache;
        readonly Dictionary<Type, InnerEvent> innerEvents = new();
        readonly bool allowAddRemoveListenerDuringPublish;
        readonly bool allowRecursivePublish;
        readonly bool autoPurgeListenerSlot;
    }
}
