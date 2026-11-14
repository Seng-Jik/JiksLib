using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
    /// 提供较好的性能，只支持值类型的事件，可避免事件发布过程中的装箱操作
    /// 支持配置发布期间监听器增删、递归发布以及空类型自动清理。
    /// 注意：创建过任意 SubPublisher 后，总线将永久禁止
    /// PurgeListenerSlot / PurgeListenerSlots；这是为了避免缓存的
    /// SubPublisher 指向已被移除的内部 SafeEvent。该限制是性能与复杂度的权衡：
    /// 精确追踪所有 SubPublisher 副本会带来额外开销和生命周期管理复杂度。
    /// </summary>
    /// <typeparam name="TConstraint">约定事件类型必须实现某一接口</typeparam>
    public sealed class ValueEventBus<TConstraint>
        where TConstraint : class
    {
        /// <summary>
        /// 构造值类型事件总线，同时构造其事件发布器。
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        /// <param name="allowAddRemoveListenerDuringPublish">
        /// 是否允许在发布期间添加或移除监听器；允许时这些操作会延迟到整个最外层发布结束后生效。
        /// 不能与 <paramref name="autoPurgeTypeInfo"/> 同时开启。
        /// </param>
        /// <param name="allowRecursivePublish">
        /// 是否允许在发布期间再次发布事件；允许时嵌套事件进入 FIFO 队列，
        /// 由最外层发布排空。该值会传给内部 SafeEvent 的
        /// <c>allowRecursiveInvoke</c>。
        /// </param>
        /// <param name="autoPurgeTypeInfo">
        /// 是否在某个事件类型移除最后一个监听器时自动删除其内部存储。
        /// 开启后不能使用 <c>WarmupListenerSlot</c>、<c>GetSubPublisher</c>、
        /// <c>PurgeListenerSlot</c> 或 <c>PurgeListenerSlots</c>。
        /// </param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="allowAddRemoveListenerDuringPublish"/> 与
        /// <paramref name="autoPurgeTypeInfo"/> 同时为 true 时抛出。
        /// </exception>
        public ValueEventBus(
            out Publisher publisher,
            bool allowAddRemoveListenerDuringPublish = false,
            bool allowRecursivePublish = false,
            bool autoPurgeTypeInfo = false)
        {
            if (allowAddRemoveListenerDuringPublish && autoPurgeTypeInfo)
            {
                throw new ArgumentException(
                    "allowAddRemoveListenerDuringInvoke and autoPurgeTypeInfo cannot be enabled at the same time.");
            }

            innerEvents = new();
            publisher = new(this);

            this.allowAddRemoveListenerDuringPublish =
                allowAddRemoveListenerDuringPublish;

            this.allowRecursivePublish = allowRecursivePublish;
            this.autoPurgeTypeInfo = autoPurgeTypeInfo;
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// </summary>
        public void AddListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : struct, TConstraint
        {
            listener.ThrowIfNull(nameof(listener));

            SafeEvent<TEvent> safeEvent;

            if (innerEvents.TryGetValue(typeof(TEvent), out var eventHandler))
                safeEvent = (SafeEvent<TEvent>)eventHandler.InnerSafeEvent;
            else
                (safeEvent, _) = CreateAndRegisterInnerEvent<TEvent>(0);

            safeEvent.AddListener(listener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : struct, TConstraint
        {
            listener.ThrowIfNull(nameof(listener));

            if (innerEvents.TryGetValue(typeof(TEvent), out var eventHandler))
            {
                var se = (SafeEvent<TEvent>)eventHandler.InnerSafeEvent;
                se.RemoveListener(listener);

                if (autoPurgeTypeInfo && se.ListenerCount <= 0)
                    innerEvents.Remove(typeof(TEvent));
            }
        }

        /// <summary>
        /// 获取指定值类型事件当前已应用的监听器数量。
        /// 只统计注册在该确切值类型上的监听器；未创建过该事件类型时返回 0；
        /// 重复注册的同一监听器按注册次数计数。
        /// 若启用了发布期间增删，返回值是当前已应用的数量：待添加的监听器尚未计入，
        /// 待移除的监听器仍会计入，直到整个最外层发布（包括递归队列排空）结束后才更新。
        /// 该方法不预测本次发布最终会保留多少监听器。
        /// </summary>
        /// <typeparam name="TEvent">要查询的事件类型。</typeparam>
        public int GetListenersCount<TEvent>()
            where TEvent : struct, TConstraint =>
            GetListenersCount(typeof(TEvent));

        /// <summary>
        /// 获取指定事件类型当前已应用的监听器数量。
        /// 只统计注册在该确切值类型上的监听器。
        /// </summary>
        /// <param name="eventType">
        /// 要查询的事件类型；应与注册监听器时使用的类型一致。
        /// </param>
        /// <returns>
        /// 当前已应用的监听器数量；类型未创建或被自动清理时返回 0。
        /// 发布期间延迟增删的可见性同泛型重载。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="eventType"/> 为 null 时抛出。
        /// </exception>
        public int GetListenersCount(Type eventType)
        {
            if (innerEvents.TryGetValue(eventType.ThrowIfNull(nameof(eventType)), out var h))
                return h.GetListenersCount(h.InnerSafeEvent);

            return 0;
        }

        /// <summary>
        /// 预热某一值类型事件类型的监听器存储
        /// 提前创建该事件类型的内部事件，并预分配指定数量的监听器位置；
        /// 之后为该事件类型注册不超过<paramref name="capacity"/>个监听器时不会触发扩容分配。
        /// </summary>
        /// <typeparam name="TEvent">
        /// 要预热的事件类型，必须是实现<typeparamref name="TConstraint"/>的值类型
        /// </typeparam>
        /// <param name="capacity">
        /// 该事件类型预计注册的监听器数量（必须非负，0 表示只提前创建存储）
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当<paramref name="capacity"/>为负数时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 当该事件类型已经预热过、已经注册过监听器，或启用了
        /// <c>autoPurgeTypeInfo</c> 时抛出。
        /// </exception>
        /// <remarks>
        /// 每个值类型事件有独立的监听器存储，需要分别预热；建议在启动期集中调用。
        /// 预热只影响监听器注册路径，不改变事件发布的行为。
        /// </remarks>
        public void WarmupListenerSlot<TEvent>(int capacity)
            where TEvent : struct, TConstraint
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
        /// 手动清理指定事件类型的内部存储。
        /// 只有该类型当前没有任何监听器时才会移除。
        /// </summary>
        /// <param name="typeInfo">要清理的事件类型。</param>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeTypeInfo</c>，或已经创建过 <c>SubPublisher</c> 时抛出。
        /// 后者的限制是永久性的：即使 SubPublisher 已被丢弃，或只创建过其他事件类型的
        /// SubPublisher，本方法仍会抛出。这是为了避免缓存的 SubPublisher 指向被移除的
        /// 内部 SafeEvent；精确追踪所有副本会带来额外开销与复杂度。
        /// </exception>
        public bool PurgeListenerSlot(Type typeInfo)
        {
            if (autoPurgeTypeInfo)
                throw new InvalidOperationException(
                    "PurgeListenerSlot cannot be called when autoPurgeTypeInfo is enabled.");

            if (subPublisherGot)
                throw new InvalidOperationException(
                    "PurgeListenerSlot cannot be called after a SubPublisher has been created.");

            if (!innerEvents.TryGetValue(typeInfo, out var h))
                return true;

            if (h.GetListenersCount(h.InnerSafeEvent) > 0)
                return false;

            innerEvents.Remove(typeInfo);
            return true;
        }

        /// <summary>
        /// 手动清理指定值类型事件类型的内部存储。
        /// </summary>
        /// <typeparam name="TEvent">要清理的事件类型。</typeparam>
        /// <returns>类型不存在或已成功移除时返回 true；仍有监听器时返回 false。</returns>
        public bool PurgeListenerSlot<TEvent>()
            where TEvent : struct, TConstraint =>
            PurgeListenerSlot(typeof(TEvent));

        /// <summary>
        /// 清理所有当前没有监听器的事件类型。
        /// </summary>
        /// <param name="typesRemoved">实际被移除的事件类型列表。</param>
        /// <param name="typesRemaining">清理后仍保留的事件类型数量。</param>
        /// <exception cref="InvalidOperationException">
        /// 启用 <c>autoPurgeTypeInfo</c>，或已经创建过 <c>SubPublisher</c> 时抛出。
        /// 后者的限制是永久性的：即使 SubPublisher 已被丢弃，或只创建过其他事件类型的
        /// SubPublisher，本方法仍会抛出。这是为了避免缓存的 SubPublisher 指向被移除的
        /// 内部 SafeEvent；精确追踪所有副本会带来额外开销与复杂度。
        /// </exception>
        public void PurgeListenerSlots(
            out List<Type> typesRemoved,
            out int typesRemaining)
        {
            if (autoPurgeTypeInfo)
                throw new InvalidOperationException(
                    "PurgeListenerSlots cannot be called when autoPurgeTypeInfo is enabled.");

            if (subPublisherGot)
                throw new InvalidOperationException(
                    "PurgeListenerSlots cannot be called after a SubPublisher has been created.");

            typesRemoved = new();

            foreach (var i in innerEvents)
                if (i.Value.GetListenersCount(i.Value.InnerSafeEvent) == 0)
                    typesRemoved.Add(i.Key);

            foreach (var t in typesRemoved)
                innerEvents.Remove(t);

            typesRemaining = innerEvents.Count;
        }

        /// <summary>
        /// 事件发布器
        /// 转换为 ISafeEventPublisher<TConstraint> 后若发布事件会导致事件对象装箱
        /// </summary>
        public readonly struct Publisher : ISafeEventPublisher<TConstraint>
        {
            /// <summary>
            /// 发布事件
            /// </summary>
            /// <param name="@event">事件对象</param>
            /// <param name="exceptionsOutput">事件监听器产生的异常将会被写入到这个列表中，为null则静默忽略异常</param>
            public void Publish<TEvent>(
                TEvent @event,
                ICollection<Exception>? exceptionsOutput)
                where TEvent : struct, TConstraint
            {
                if (bus.innerEvents.TryGetValue(typeof(TEvent), out var h))
                {
                    var handlers = (SafeEvent<TEvent>)h.InnerSafeEvent;
                    SafeEvent<TEvent>.Publisher publisher = new(handlers);
                    publisher.Publish(@event, exceptionsOutput);
                }
            }

            /// <summary>
            /// 获取某一类型的事件发布器。
            /// 如果要高频发布事件，则应该使用该方法提前获得子事件发布器，
            /// 提供最好的性能。
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// 当总线启用了 <c>autoPurgeTypeInfo</c> 时抛出：自动清理可能在
            /// SubPublisher 仍被使用时移除对应的内部 SafeEvent。
            /// </exception>
            /// <remarks>
            /// 调用本方法后，当前总线将永久禁止 <c>PurgeListenerSlot</c> 和
            /// <c>PurgeListenerSlots</c>，即使返回的 SubPublisher 已被丢弃，或只创建过
            /// 某一事件类型的 SubPublisher。原因是 SubPublisher 保存了内部
            /// SafeEvent.Publisher，Purge 后它会指向已移除的 SafeEvent 并静默丢事件；
            /// 精确追踪所有结构体副本会带来额外开销与生命周期复杂度，因此采用永久禁用的
            /// 保守策略。这是性能与复杂度的权衡，调用方应在决定使用 SubPublisher 前
            /// 确认不再需要 Purge。
            /// </remarks>
            public SubPublisher<TEvent> GetSubPublisher<TEvent>()
                where TEvent : struct, TConstraint
            {
                if (bus.autoPurgeTypeInfo)
                    throw new InvalidOperationException(
                        "GetSubPublisher cannot be used when autoPurgeTypeInfo is enabled.");

                if (!bus.innerEvents.TryGetValue(typeof(TEvent), out var h))
                    h = bus.CreateAndRegisterInnerEvent<TEvent>(0).Item2;

                bus.subPublisherGot = true;

                return new((SafeEvent<TEvent>)h.InnerSafeEvent);
            }

            void ISafeEventPublisher<TConstraint>.Publish(
                TConstraint @event,
                ICollection<Exception>? exceptionsOutput)
            {
                if (bus.innerEvents.TryGetValue(@event.GetType(), out var h))
                    h.WeakTypedPublish(
                        h.InnerSafeEvent,
                        @event,
                        exceptionsOutput);
            }

            internal Publisher(ValueEventBus<TConstraint> bus)
            {
                this.bus = bus;
            }

            readonly ValueEventBus<TConstraint> bus;
        }

        /// <summary>
        /// 子事件发布器
        /// 可以针对某一类型发布事件，
        /// 使用该发布器发布事件可以在发布事件时避免查找类型和事件装箱的开销
        /// </summary>
        public readonly struct SubPublisher<TEvent> : ISafeEventPublisher<TEvent>
            where TEvent : struct, TConstraint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Publish(
                TEvent @event,
                ICollection<Exception>? exceptionsOutput) =>
                inner.Publish(@event, exceptionsOutput);

            internal SubPublisher(SafeEvent<TEvent> inner)
            {
                this.inner = new(inner);
            }

            readonly SafeEvent<TEvent>.Publisher inner;
        }

        #region Details

        /// <summary>
        /// 创建某一值类型事件类型的内部事件及其发布委托
        /// </summary>
        (SafeEvent<TEvent>, InnerPublisher) CreateAndRegisterInnerEvent<TEvent>(
            int capacity)

            where TEvent : struct, TConstraint
        {
            SafeEvent<TEvent> safeEvent = new(
                out var publisher,
                allowAddRemoveListenerDuringPublish,
                allowRecursivePublish,
                capacity);

            InnerPublisher innerPublisher = new(
                safeEvent,
                static (s, e, ex) => new SafeEvent<TEvent>
                    .Publisher((SafeEvent<TEvent>)s)
                    .Publish((TEvent)e, ex),
                static (s) => ((SafeEvent<TEvent>)s).ListenerCount);

            innerEvents.Add(typeof(TEvent), innerPublisher);

            return (safeEvent, innerPublisher);
        }

        private readonly struct InnerPublisher
        {
            internal delegate void WeakTypedPublishFunction(
                object safeEvent,
                TConstraint eventObject,
                ICollection<Exception>? exceptionsOutput);

            internal delegate int GetListenersCountFunction(object safeEvent);

            internal readonly object InnerSafeEvent;
            internal readonly WeakTypedPublishFunction WeakTypedPublish;
            internal readonly GetListenersCountFunction GetListenersCount;

            internal InnerPublisher(
                object innerSafeEvent,
                WeakTypedPublishFunction weakTypedPublish,
                GetListenersCountFunction getListenersCount)
            {
                InnerSafeEvent = innerSafeEvent;
                WeakTypedPublish = weakTypedPublish;
                GetListenersCount = getListenersCount;
            }
        }

        /// <summary>
        /// 是否创建过任意 SubPublisher；一旦为 true，当前总线将永久禁止
        /// PurgeListenerSlot/PurgeListenerSlots，避免缓存的 SubPublisher
        /// 指向已移除的内部 SafeEvent。
        /// </summary>
        bool subPublisherGot = false;
        readonly Dictionary<Type, InnerPublisher> innerEvents;
        readonly bool allowAddRemoveListenerDuringPublish;
        readonly bool allowRecursivePublish;
        readonly bool autoPurgeTypeInfo;

        #endregion
    }
}
