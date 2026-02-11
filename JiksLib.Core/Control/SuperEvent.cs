using System;
using System.Collections.Generic;
using JiksLib.Collections;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 超级事件
    /// 可以发布以某一类型为基础的不同类型的事件
    /// TBaseEvent不能是接口类型
    /// 仅支持在主线程上使用
    /// </summary>
    public sealed class SuperEvent<TBaseEvent>
        where TBaseEvent : class
    {
        /// <summary>
        /// 构造超级事件，同时构造其事件发布器
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        public SuperEvent(out Publisher publisher)
        {
            if (typeof(TBaseEvent).IsInterface)
                throw new InvalidOperationException(
                    "Interface types are not supported by super event.");

            publisher = new(this);
        }

        /// <summary>
        /// 事件监听器
        /// </summary>
        public delegate void Listener<TEvent>(TEvent @event);

        /// <summary>
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void AddListener<TEvent>(Listener<TEvent> listener)
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new TypeHandler<TEvent>();
                typeHandlers.Add(typeof(TEvent), handler);
            }

            var typeHandler = (TypeHandler<TEvent>)handler;
            typeHandler.AddListener(listener);
        }

        /// <summary>
        /// 注册某一类型的事件监听器，但是仅监听一次
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void AddOnceListener<TEvent>(Listener<TEvent> listener)
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
            {
                handler = new TypeHandler<TEvent>();
                typeHandlers.Add(typeof(TEvent), handler);
            }

            var typeHandler = (TypeHandler<TEvent>)handler;

            void wrappedListener(TEvent e)
            {
                try
                {
                    listener(e);
                }
                finally
                {
                    typeHandler.RemoveListener(wrappedListener);
                }
            }

            typeHandler.AddListener(wrappedListener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void RemoveListener<TEvent>(Listener<TEvent> listener)
        {
            if (!typeHandlers.TryGetValue(typeof(TEvent), out var handler))
                return;

            var typedHandler = (TypeHandler<TEvent>)handler;
            typedHandler.RemoveListener(listener);
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
                var typeChain = superEvent
                    .GetTypeChain(@event.ThrowIfNull().GetType())
                    .Types;

                foreach (var i in typeChain)
                {
                    if (superEvent.typeHandlers.TryGetValue(i, out var h))
                    {
                        if (h.Invoke(@event, exceptionsOutput))
                            superEvent.typeHandlers.Remove(i);
                    }
                }
            }

            readonly SuperEvent<TBaseEvent> superEvent;

            internal Publisher(SuperEvent<TBaseEvent> superEvent)
            {
                this.superEvent = superEvent;
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

        readonly Dictionary<Type, TypeChain> typeChains = new();
        readonly Dictionary<Type, TypeHandler> typeHandlers = new();

        private sealed class TypeChain
        {
            public readonly IReadOnlyList<Type> Types;

            public TypeChain(Type type)
            {
                List<Type> listenerTypes = new();

                var t = type;

                while (t != null)
                {
                    listenerTypes.Add(t);
                    t = t.BaseType;
                }

                listenerTypes.AddRange(type.GetInterfaces());
                Types = listenerTypes;
            }
        }

        private abstract class TypeHandler
        {
            public abstract bool Invoke(
                object baseEvent,
                IList<Exception>? exceptionsOutput);
        }

        private sealed class TypeHandler<TEvent> : TypeHandler
        {
            readonly MultiHashSet<Listener<TEvent>> listeners = new();
            readonly List<Listener<TEvent>> listenersDelayedAdd = new();
            readonly List<Listener<TEvent>> listenersDelayedRemove = new();

            public void AddListener(Listener<TEvent> listener)
            {
                listenersDelayedAdd.Add(listener);
            }

            public void RemoveListener(Listener<TEvent> listener)
            {
                listenersDelayedRemove.Add(listener);
            }

            void ApplyDelayedListenerAddOrRemove()
            {
                for (int i = 0; i < listenersDelayedAdd.Count; ++i)
                    listeners.Add(listenersDelayedAdd[i]);

                listenersDelayedAdd.Clear();

                for (int i = 0; i < listenersDelayedRemove.Count; ++i)
                    listeners.Remove(listenersDelayedRemove[i]);

                listenersDelayedRemove.Clear();
            }

            public override bool Invoke(
                object baseEvent,
                IList<Exception>? exceptionsOutput)
            {
                ApplyDelayedListenerAddOrRemove();

                TEvent @event = (TEvent)baseEvent;

                foreach (var listener in listeners)
                {
                    try
                    {
                        listener(@event);
                    }
                    catch (Exception e)
                    {
                        exceptionsOutput?.Add(e);
                    }
                }

                ApplyDelayedListenerAddOrRemove();

                return listeners.Count <= 0;
            }
        }
    }
}
