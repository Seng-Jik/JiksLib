using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 事件总线监听器
    /// </summary>
    public delegate void EventBusListener<TEvent>(TEvent @event);

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
        public void AddListener<TEvent>(EventBusListener<TEvent> listener)
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
        public void ListenOnce<TEvent>(EventBusListener<TEvent> listener)
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
        public void RemoveListener<TEvent>(EventBusListener<TEvent> listener)
            where TEvent : TBaseEvent
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
                return;

            var listenerSet = (EventBusListenerSet<TEvent>)handler;

            if (invoking)
                listenerSet.RemoveListenerDelayed(listener);
            else
                listenerSet.RemoveListener(listener);
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

                    var typeChain = GetTypeChain(@event.ThrowIfNull().GetType())
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
                typeChains = new();
            }

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

            TypeChain GetTypeChain(Type type)
            {
                if (!typeChains.TryGetValue(type, out var chain))
                {
                    chain = new(type);
                    typeChains.Add(type, chain);
                }

                return chain;
            }

            readonly Dictionary<Type, TypeChain> typeChains;
        }

        EventBusListenerSet<TEvent> GetOrCreateListenerSet<TEvent>()
            where TEvent : TBaseEvent
        {
            if (!listenerSets.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new EventBusListenerSet<TEvent>();
                listenerSets.Add(typeof(TEvent), handler);
            }

            return (EventBusListenerSet<TEvent>)handler;
        }

        internal EventBus() { }
        internal bool invoking = false;
        internal readonly Dictionary<Type, IEventBusListenerSet> listenerSets = new();

        internal interface IEventBusListenerSet
        {
            internal abstract void Invoke(
                TBaseEvent baseEvent,
                IList<Exception>? exceptionsOutput);
        }

        internal sealed class EventBusListenerSet<T> :
            IEventBusListenerSet
            where T : notnull, TBaseEvent
        {
            readonly List<EventBusListener<T>?> listeners = new();
            int removeIndex = 0;

            readonly Queue<(bool isAddOrRemove, EventBusListener<T>)>
                listenersDelayedOperation = new();

            internal void AddListener(EventBusListener<T> listener)
            {
                listeners.Add(listener);
                removeIndex = listeners.Count - 1;
            }

            internal void AddListenerDelayed(EventBusListener<T> listener)
            {
                listenersDelayedOperation.Enqueue((true, listener));
            }

            internal void RemoveListener(EventBusListener<T> listener)
            {
                if (removeIndex >= listeners.Count || removeIndex < 0)
                    removeIndex = listeners.Count - 1;

                for (int i = removeIndex; i >= 0; --i)
                {
                    if (listeners[i] == listener)
                    {
                        listeners[i] = null;
                        removeIndex = i - 1;
                        return;
                    }
                }

                for (int i = removeIndex + 1; i < listeners.Count; ++i)
                {
                    if (listeners[i] == listener)
                    {
                        listeners[i] = null;
                        removeIndex = i - 1;
                        return;
                    }
                }
            }

            internal void RemoveListenerDelayed(EventBusListener<T> listener)
            {
                listenersDelayedOperation.Enqueue((false, listener));
            }

            void ApplyDelayedListenerAddOrRemove()
            {
                while (listenersDelayedOperation.Count > 0)
                {
                    var op = listenersDelayedOperation.Dequeue();

                    if (op.isAddOrRemove) AddListener(op.Item2);
                    else RemoveListener(op.Item2);
                }
            }

            internal void Invoke(
                T @event,
                IList<Exception>? exceptionsOutput)
            {
                int rewriteIndex = 0;

                ApplyDelayedListenerAddOrRemove();

                for (int i = 0; i < listeners.Count; ++i)
                {
                    var listener = listeners[i];

                    if (listener == null)
                        continue;

                    listeners[i] = null;
                    listeners[rewriteIndex++] = listener;

                    try
                    {
                        listener(@event);
                    }
                    catch (Exception ex)
                    {
                        exceptionsOutput?.Add(ex);
                    }
                }

                if (rewriteIndex < listeners.Count)
                {
                    listeners.RemoveRange(
                        rewriteIndex,
                        listeners.Count - rewriteIndex);
                }
            }

            void IEventBusListenerSet.Invoke(
                TBaseEvent baseEvent,
                IList<Exception>? exceptionsOutput)
            {
                Invoke((T)baseEvent, exceptionsOutput);
            }
        }
    }
}
