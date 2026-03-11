using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// 注册某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void AddListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : TBaseEvent =>
            GetInnerEvent<TEvent>().Item1.AddListener(listener);

        /// <summary>
        /// 移除某一类型的事件监听器
        /// TEvent应该是TBaseEvent的子类，或者是被事件实现的interface
        /// </summary>
        public void RemoveListener<TEvent>(EventHandler<TEvent> listener)
            where TEvent : TBaseEvent =>
            GetInnerEvent<TEvent>().Item1.RemoveListener(listener);

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
                var typeChain = GetTypeChain(@event.ThrowIfNull().GetType())
                    .Types;

                foreach (var i in typeChain)
                {
                    if (eventBus.innerEvents.TryGetValue(i, out var h))
                        h.Item2(@event, exceptionsOutput);
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
                internal readonly List<Type> Types;

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

        internal delegate void InnerPublisher(
            TBaseEvent baseEvent,
            IList<Exception>? exceptionsOutput);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        (SafeEvent<TEvent>, InnerPublisher) GetInnerEvent<TEvent>()
            where TEvent : TBaseEvent
        {
            if (innerEvents.TryGetValue(typeof(TEvent), out var handler))
                return ((SafeEvent<TEvent>)handler.Item1, handler.Item2);

            SafeEvent<TEvent> safeEvent = new(out var publisher);

            void innerPublish(TBaseEvent baseEvent, IList<Exception>? exn) =>
                publisher.Publish((TEvent)baseEvent, exn);

            innerEvents.Add(typeof(TEvent), (safeEvent, innerPublish));
            return (safeEvent, innerPublish);
        }

        internal readonly Dictionary<Type, (object, InnerPublisher)> innerEvents = new();
    }
}
