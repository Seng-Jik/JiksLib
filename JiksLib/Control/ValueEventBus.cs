using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Control
{
    /// <summary>
    /// 值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
    /// 提供较好的性能，只支持值类型的事件，可避免事件发布过程中的装箱操作
    /// </summary>
    /// <typeparam name="TConstraint">约定事件类型必须实现某一接口</typeparam>
    public sealed class ValueEventBus<TConstraint>
        where TConstraint : class
    {
        /// <summary>
        /// 构造值类型事件总线，同时构造其事件发布器
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        public ValueEventBus(out Publisher publisher)
        {
            events = new();
            publisher = new(this);
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
        /// 当该事件类型已经预热过，或已经注册过监听器时抛出
        /// </exception>
        /// <remarks>
        /// 每个值类型事件有独立的监听器存储，需要分别预热；建议在启动期集中调用。
        /// 预热只影响监听器注册路径，不改变事件发布的行为。
        /// </remarks>
        public void WarmupListenerSlot<TEvent>(int capacity)
            where TEvent : struct, TConstraint
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            Type eventType = typeof(TEvent);

            if (events.ContainsKey(eventType))
                throw new InvalidOperationException(
                    "WarmupListenerSlot can only be called once per event type, before any listener is added.");

            (SafeEvent<TEvent> safeEvent, WeakPublisher weakPublisher) =
                CreateInnerEvent<TEvent>(capacity);
            events.Add(eventType, (safeEvent, weakPublisher));
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// </summary>
        public void AddListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : struct, TConstraint =>
            GetInnerEvent<TEvent>().AddListener(listener);

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : struct, TConstraint =>
            GetInnerEvent<TEvent>().RemoveListener(listener);

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
                IList<Exception>? exceptionsOutput)
                where TEvent : struct, TConstraint
            {
                if (bus.events.TryGetValue(typeof(TEvent), out var h))
                {
                    var handlers = (SafeEvent<TEvent>)h.Item1;
                    SafeEvent<TEvent>.Publisher publisher = new(handlers);
                    publisher.Publish(@event, exceptionsOutput);
                }
            }

            /// <summary>
            /// 获取某一类型的事件发布器
            /// 如果要高频发布事件，则应该使用该方法提前获得子事件发布器，
            /// 提供最好的性能。
            /// </summary>
            public SubPublisher<TEvent> GetSubPublisher<TEvent>()
                where TEvent : struct, TConstraint =>
                new(bus.GetInnerEvent<TEvent>());

            void ISafeEventPublisher<TConstraint>.Publish(
                TConstraint @event,
                IList<Exception>? exceptionsOutput)
            {
                if (bus.events.TryGetValue(@event.GetType(), out var h))
                    h.Item2(@event, exceptionsOutput);
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
                IList<Exception>? exceptionsOutput) =>
                inner.Publish(@event, exceptionsOutput);

            internal SubPublisher(SafeEvent<TEvent> inner)
            {
                this.inner = new(inner);
            }

            readonly SafeEvent<TEvent>.Publisher inner;
        }

        SafeEvent<TEvent> GetInnerEvent<TEvent>()
            where TEvent : struct, TConstraint
        {
            if (events.TryGetValue(typeof(TEvent), out var h))
                return (SafeEvent<TEvent>)h.Item1;

            (SafeEvent<TEvent> safeEvent, WeakPublisher weakPublisher) =
                CreateInnerEvent<TEvent>(0);
            events.Add(typeof(TEvent), (safeEvent, weakPublisher));

            return safeEvent;
        }

        /// <summary>
        /// 创建某一值类型事件类型的内部事件及其发布委托
        /// </summary>
        static (SafeEvent<TEvent>, WeakPublisher) CreateInnerEvent<TEvent>(int capacity)
            where TEvent : struct, TConstraint
        {
            SafeEvent<TEvent> safeEvent = new(out var publisher, capacity);

            return (safeEvent, (e, o) => publisher.Publish((TEvent)e, o));
        }

        delegate void WeakPublisher(
            TConstraint @event,
            IList<Exception>? exceptionsOutput);

        readonly Dictionary<Type, (object, WeakPublisher)> events;
    }
}
