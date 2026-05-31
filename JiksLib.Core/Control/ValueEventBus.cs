using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        public readonly struct SubPublisher<TEvent> : ISafeEventPublisher<TEvent>
        {
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

            SafeEvent<TEvent> e = new(out var publisher);

            events.Add(
                typeof(TEvent),
                (e, (e, o) => publisher.Publish((TEvent)e, o)));

            return e;
        }

        internal delegate void WeakPublisher(
            TConstraint @event,
            IList<Exception>? exceptionsOutput);

        readonly Dictionary<Type, (object, WeakPublisher)> events;
    }
}
