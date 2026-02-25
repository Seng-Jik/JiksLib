using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 事件监听器
    /// </summary>
    public delegate void Listener<TEvent>(TEvent @event);

    /// <summary>
    /// 事件总线
    /// 可以发布以某一类型为基础的不同类型的事件
    /// TBaseEvent可以是类或接口类型
    /// 仅支持在主线程上使用
    /// </summary>
    public sealed class EventBus<TBaseEvent>
        where TBaseEvent : class
    {
        /// <summary>
        /// 构造事件总线，同时构造其事件发布器
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        public EventBus(out Publisher publisher)
        {
            publisher = new(this);
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void AddListener<TEvent>(Listener<TEvent> listener)
            where TEvent : TBaseEvent
        {
            var listenerSet = GetOrCreateListenerSet<TEvent>();

            if (invoking)
                listenerSet.AddListenerDelayed(listener);
            else
                listenerSet.AddListener(listener);
        }

        /// <summary>
        /// 仅监听一次某个事件，之后自动移除监听器
        /// 该监听器不能使用RemoveListener移除
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void ListenOnce<TEvent>(Listener<TEvent> listener)
            where TEvent : TBaseEvent
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

        /// <summary>
        /// 移除某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void RemoveListener<TEvent>(Listener<TEvent> listener)
            where TEvent : TBaseEvent
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
                return;

            var typeHandler = (EventBusListenerSet<TEvent, TBaseEvent>)handler;

            if (invoking)
                typeHandler.RemoveListenerDelayed(listener);
            else
                typeHandler.RemoveListener(listener);
        }

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
            public void Publish(
                TBaseEvent @event,
                IList<Exception>? exceptionsOutput)
            {
                if (eventBus.invoking)
                    throw new InvalidOperationException(
                        "Cannot publish event while another event is being published.");

                try
                {
                    eventBus.invoking = true;

                    var typeChain = eventBus
                        .GetTypeChain(@event.ThrowIfNull().GetType())
                        .Types;

                    foreach (var i in typeChain)
                    {
                        if (eventBus.listenerSets.TryGetValue(i, out var h))
                            h.Invoke(@event, exceptionsOutput);
                    }
                }
                finally
                {
                    eventBus.invoking = false;
                }
            }

            readonly EventBus<TBaseEvent> eventBus;

            internal Publisher(EventBus<TBaseEvent> eventBus)
            {
                this.eventBus = eventBus;
            }
        }

        EventBusListenerSet<TEvent, TBaseEvent> GetOrCreateListenerSet<TEvent>()
            where TEvent : TBaseEvent
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new EventBusListenerSet<TEvent, TBaseEvent>();
                listenerSets.Add(typeof(TEvent), handler);
            }

            return (EventBusListenerSet<TEvent, TBaseEvent>)handler;
        }

        TypeChain GetTypeChain(Type type)
        {
            if (!typeChains.TryGetValue(type, out var chain))
            {
                chain = new(type);
                typeChains.Add(type, chain);
            }

            return chain;
        }

        bool invoking = false;
        readonly Dictionary<Type, TypeChain> typeChains = new();
        readonly Dictionary<Type, IEventBusListenerSet<TBaseEvent>> listenerSets = new();

        private sealed class TypeChain
        {
            internal readonly IReadOnlyList<Type> Types;

            internal TypeChain(Type type)
            {
                List<Type> listenerTypes;

                var t = type;

                if (typeof(TBaseEvent).IsInterface)
                {
                    var interfaces = type.GetInterfaces();
                    listenerTypes = new(8 + interfaces.Length);

                    for (int i = 0; i < interfaces.Length; ++i)
                        if (typeof(TBaseEvent).IsAssignableFrom(interfaces[i]))
                            listenerTypes.Add(interfaces[i]);

                    while (t != null)
                    {
                        if (typeof(TBaseEvent).IsAssignableFrom(t))
                            listenerTypes.Add(t);
                        else
                            break;

                        t = t.BaseType;
                    }
                }
                else
                {
                    listenerTypes = new(8);

                    while (t != null)
                    {
                        listenerTypes.Add(t);
                        if (t == typeof(TBaseEvent)) break;
                        t = t.BaseType;
                    }
                }

                listenerTypes.TrimExcess();
                Types = listenerTypes;
            }
        }
    }
}
