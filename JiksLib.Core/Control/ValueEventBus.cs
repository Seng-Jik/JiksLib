using System;
using System.Collections.Generic;

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
            eventBus = new();
            publisher = new(eventBus);
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// </summary>
        public void AddListener<TEvent>(EventBusListener<TEvent> listener)
            where TEvent : struct, TConstraint => eventBus.AddListener(listener);

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<TEvent>(EventBusListener<TEvent> listener)
            where TEvent : struct, TConstraint => eventBus.RemoveListener(listener);

        /// <summary>
        /// 仅监听一次某个事件，之后自动移除监听器
        /// 该监听器不能使用RemoveListener移除
        /// </summary>
        public void ListenOnce<TEvent>(EventBusListener<TEvent> listener)
            where TEvent : struct, TConstraint => eventBus.ListenOnce(listener);

        /// <summary>
        /// 事件发布器
        /// </summary>
        public readonly struct Publisher
        {
            internal Publisher(EventBus<TConstraint> eventBus)
            {
                this.eventBus = eventBus;
            }

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
                if (eventBus.invoking)
                    throw new InvalidOperationException(
                        "Cannot publish event while another event is being published.");

                try
                {
                    eventBus.invoking = true;

                    if (eventBus.listenerSets.TryGetValue(typeof(TEvent), out var h))
                        ((EventBus<TConstraint>.EventBusListenerSet<TEvent>)h).Invoke(
                            @event,
                            exceptionsOutput);
                }
                finally
                {
                    eventBus.invoking = false;
                }
            }

            readonly EventBus<TConstraint> eventBus;
        }

        readonly EventBus<TConstraint> eventBus;
    }
}
