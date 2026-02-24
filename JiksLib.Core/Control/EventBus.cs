using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
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
        /// 事件监听器
        /// </summary>
        public delegate void Listener<TEvent>(TEvent @event) where TEvent : TBaseEvent;

        /// <summary>
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void AddListener<TEvent>(Listener<TEvent> listener) where TEvent : TBaseEvent
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new TypeHandler<TEvent>();
                typeHandlers.Add(typeof(TEvent), handler);
            }

            var typeHandler = (TypeHandler<TEvent>)handler;

            if (invoking)
                typeHandler.AddListenerDelayed(listener);
            else
                typeHandler.AddListener(listener);
        }

        /// <summary>
        /// 仅监听一次某个事件，之后自动移除监听器
        /// 该监听器不能使用RemoveListener移除
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void ListenOnce<TEvent>(Listener<TEvent> listener) where TEvent : TBaseEvent
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new TypeHandler<TEvent>();
                typeHandlers.Add(typeof(TEvent), handler);
            }

            var typeHandler = (TypeHandler<TEvent>)handler;

            void wrappedListener(TEvent e)
            {
                typeHandler.RemoveListenerDelayed(wrappedListener);
                listener(e);
            }

            if (invoking)
                typeHandler.AddListenerDelayed(wrappedListener);
            else
                typeHandler.AddListener(wrappedListener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void RemoveListener<TEvent>(Listener<TEvent> listener) where TEvent : TBaseEvent
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
                return;

            var typeHandler = (TypeHandler<TEvent>)handler;

            if (invoking)
                typeHandler.RemoveListenerDelayed(listener);
            else
                typeHandler.RemoveListener(listener);
        }

        /// <summary>
        /// 事件发布器
        /// </summary>
        public sealed class Publisher
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
                        if (eventBus.typeHandlers.TryGetValue(i, out var h))
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
        readonly Dictionary<Type, TypeHandler> typeHandlers = new();

        private sealed class TypeChain
        {
            internal readonly IReadOnlyList<Type> Types;

            internal TypeChain(Type type)
            {
                List<Type> listenerTypes = new();

                var t = type;

                if (typeof(TBaseEvent).IsInterface)
                {
                    while (t != typeof(object) && t != null)
                    {
                        listenerTypes.Add(t);
                        t = t.BaseType;
                        // 如果不实现 TBaseEvent 则不添加
                    }

                    listenerTypes.AddRange(type.GetInterfaces()); // 改为只添加 TBaseEvent 的子接口
                }
                else
                {
                    while (t != null)
                    {
                        listenerTypes.Add(t);
                        if (t == typeof(TBaseEvent)) break;
                        t = t.BaseType;
                    }
                }

                Types = listenerTypes;
            }
        }

        private abstract class TypeHandler
        {
            internal abstract void Invoke(
                TBaseEvent baseEvent,
                IList<Exception>? exceptionsOutput);
        }

        private sealed class TypeHandler<TEvent> : TypeHandler
            where TEvent : TBaseEvent
        {
            readonly List<Listener<TEvent>?> listeners = new();
            int removeIndex = 0;

            readonly Queue<(bool isAddOrRemove, Listener<TEvent>)>
                listenersDelayedOperation = new();

            internal void AddListener(Listener<TEvent> listener)
            {
                listeners.Add(listener);
                removeIndex = listeners.Count - 1;
            }

            internal void AddListenerDelayed(Listener<TEvent> listener)
            {
                listenersDelayedOperation.Enqueue((true, listener));
            }

            internal void RemoveListener(Listener<TEvent> listener)
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

            internal void RemoveListenerDelayed(Listener<TEvent> listener)
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

            internal override void Invoke(
                TBaseEvent baseEvent,
                IList<Exception>? exceptionsOutput)
            {
                TEvent @event = (TEvent)baseEvent;
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
                    catch (Exception e)
                    {
                        exceptionsOutput?.Add(e);
                    }
                }

                if (rewriteIndex < listeners.Count)
                {
                    listeners.RemoveRange(
                        rewriteIndex,
                        listeners.Count - rewriteIndex);
                }
            }
        }
    }
}

