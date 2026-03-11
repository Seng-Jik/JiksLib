using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Control
{
    /// <summary>
    /// 值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
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
            GetSafeEvent<TEvent>().AddListener(listener);

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : struct, TConstraint =>
            GetSafeEvent<TEvent>().RemoveListener(listener);

        /// <summary>
        /// 事件发布器
        /// </summary>
        public readonly struct Publisher
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
                if (eventHandlers.TryGetValue(typeof(TEvent), out var h))
                {
                    var handlers = (SafeEvent<TEvent>)h;
                    SafeEvent<TEvent>.Publisher publisher = new(handlers);
                    publisher.Publish(@event, exceptionsOutput);
                }
            }

            internal Publisher(ValueEventBus<TConstraint> valueEvent)
            {
                eventHandlers = valueEvent.events;
            }

            readonly Dictionary<Type, object> eventHandlers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        SafeEvent<TEvent> GetSafeEvent<TEvent>()
            where TEvent : struct, TConstraint
        {
            if (events.TryGetValue(typeof(TEvent), out var h))
                return (SafeEvent<TEvent>)h;

            SafeEvent<TEvent> e = new(out _);
            events.Add(typeof(TEvent), e);
            return e;
        }

        readonly Dictionary<Type, object> events;
    }
}
