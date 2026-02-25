using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    /// <summary>
    /// 值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
    /// </summary>
    public sealed class ValueEventBus
    {
        /// <summary>
        /// 构造值类型事件总线，同时构造其事件发布器
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        public ValueEventBus(out Publisher publisher)
        {
            publisher = new(this);
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// </summary>
        public void AddListener<TEvent>(Listener<TEvent> listener)
            where TEvent : struct
        {
            var listenerSet = GetOrCreateListenerSet<TEvent>();

            if (invoking)
                listenerSet.AddListenerDelayed(listener);
            else
                listenerSet.AddListener(listener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<TEvent>(Listener<TEvent> listener)
            where TEvent : struct
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
                return;

            var typeHandler = (EventBusListenerSet<TEvent, ValueType>)handler;

            if (invoking)
                typeHandler.RemoveListenerDelayed(listener);
            else
                typeHandler.RemoveListener(listener);
        }

        /// <summary>
        /// 仅监听一次某个事件，之后自动移除监听器
        /// 该监听器不能使用RemoveListener移除
        /// </summary>
        public void ListenOnce<TEvent>(Listener<TEvent> listener)
            where TEvent : struct
        {
            var listenerSet = GetOrCreateListenerSet<TEvent>();

            void wrappedListener(TEvent e)
            {
                listenerSet.RemoveListenerDelayed(wrappedListener);
                listener(e);
            }

            if (invoking)
                listenerSet.AddListenerDelayed(wrappedListener);
            else
                listenerSet.AddListener(wrappedListener);
        }

        public readonly struct Publisher
        {
            internal Publisher(ValueEventBus eventBus)
            {
                this.eventBus = eventBus;
            }

            /// <summary>
            /// 发布事件
            /// </summary>
            /// <param name="@event">事件对象</param>
            /// <param name="exceptionsOutput">事件监听器产生的异常将会被写入到这个列表中，为null则静默忽略异常</param>
            public void Publish<T>(
                T @event,
                IList<Exception>? exceptionsOutput) where T : struct
            {
                if (eventBus.invoking)
                    throw new InvalidOperationException(
                        "Cannot publish event while another event is being published.");

                try
                {
                    eventBus.invoking = true;

                    if (eventBus.listenerSets.TryGetValue(typeof(T), out var h))
                        ((EventBusListenerSet<T, ValueType>)h).Invoke(
                            @event,
                            exceptionsOutput);
                }
                finally
                {
                    eventBus.invoking = false;
                }
            }

            readonly ValueEventBus eventBus;
        }

        EventBusListenerSet<TEvent, ValueType> GetOrCreateListenerSet<TEvent>()
            where TEvent : struct
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new EventBusListenerSet<TEvent, ValueType>();
                listenerSets.Add(typeof(TEvent), handler);
            }

            return (EventBusListenerSet<TEvent, ValueType>)handler;
        }

        bool invoking = false;
        readonly Dictionary<Type, IEventBusListenerSet<ValueType>> listenerSets = new();
    }
}
