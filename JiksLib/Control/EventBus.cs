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
    /// 提供最强大的功能，但性能比ValueEventBus差
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
        /// 预热某一事件类型的监听器存储
        /// 提前创建该事件类型的内部事件，并预分配指定数量的监听器位置；
        /// 之后为该事件类型注册不超过<paramref name="capacity"/>个监听器时不会触发扩容分配。
        /// </summary>
        /// <typeparam name="TEvent">
        /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
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
        /// 每个事件类型有独立的监听器存储，需要分别预热；预热基类类型不会为派生类型预留容量。
        /// 建议在启动期集中调用；预热只影响监听器注册路径，不改变事件发布的行为。
        /// </remarks>
        public void WarmupListenerSlot<TEvent>(int capacity)
            where TEvent : TBaseEvent
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");

            Type eventType = typeof(TEvent);

            if (innerEvents.ContainsKey(eventType))
                throw new InvalidOperationException(
                    "WarmupListenerSlot can only be called once per event type, before any listener is added.");

            (SafeEvent<TEvent> safeEvent, InnerPublisher innerPublisher) =
                CreateInnerEvent<TEvent>(capacity);
            innerEvents.Add(eventType, (safeEvent, innerPublisher));
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
        public readonly struct Publisher : ISafeEventPublisher<TBaseEvent>
        {
            /// <summary>
            /// 预热某一具体事件类型的发布路径
            /// 提前构建并缓存该类型的类型链，避免第一次发布该类型时在热路径上分配
            /// </summary>
            /// <param name="type">
            /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// 当<paramref name="type"/>为null时抛出
            /// </exception>
            /// <exception cref="ArgumentException">
            /// 当<paramref name="type"/>不能赋值给<typeparamref name="TBaseEvent"/>时抛出
            /// </exception>
            /// <exception cref="InvalidOperationException">
            /// 当该类型已经预热过，或已经发布过该类型的事件时抛出
            /// </exception>
            /// <remarks>
            /// 类型链按事件的运行时类型缓存：预热基类或接口不会覆盖派生类型，需要按实际发布的事件类型分别预热。
            /// 预热只消除第一次发布时的类型链构建，不改变每次发布的查找与派发成本。
            /// </remarks>
            public void WarmupPublish(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (!typeof(TBaseEvent).IsAssignableFrom(type))
                    throw new ArgumentException(
                        $"Type {type} is not assignable to {typeof(TBaseEvent)}.",
                        nameof(type));

                if (typeChains.ContainsKey(type))
                    throw new InvalidOperationException(
                        $"WarmupPublish can only be called once per event type; {type} is already warmed up.");

                GetTypeChain(type);
            }

            /// <summary>
            /// 预热某一具体事件类型的发布路径
            /// </summary>
            /// <typeparam name="TEvent">
            /// 要预热的事件类型，必须是<typeparamref name="TBaseEvent"/>的派生类或实现
            /// </typeparam>
            /// <exception cref="InvalidOperationException">
            /// 当该类型已经预热过，或已经发布过该类型的事件时抛出
            /// </exception>
            /// <remarks>
            /// 与<see cref="WarmupPublish(Type)"/>相同，只是用泛型参数在编译期确定类型。
            /// </remarks>
            public void WarmupPublish<TEvent>()
                where TEvent : TBaseEvent =>
                WarmupPublish(typeof(TEvent));

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

        (SafeEvent<TEvent>, InnerPublisher) GetInnerEvent<TEvent>()
            where TEvent : TBaseEvent
        {
            if (innerEvents.TryGetValue(typeof(TEvent), out var handler))
                return ((SafeEvent<TEvent>)handler.Item1, handler.Item2);

            (SafeEvent<TEvent> safeEvent, InnerPublisher innerPublisher) =
                CreateInnerEvent<TEvent>(0);
            innerEvents.Add(typeof(TEvent), (safeEvent, innerPublisher));
            return (safeEvent, innerPublisher);
        }

        /// <summary>
        /// 创建某一事件类型的内部事件及其发布委托
        /// </summary>
        static (SafeEvent<TEvent>, InnerPublisher) CreateInnerEvent<TEvent>(int capacity)
            where TEvent : TBaseEvent
        {
            SafeEvent<TEvent> safeEvent = new(out var publisher, capacity);

            void innerPublish(TBaseEvent baseEvent, IList<Exception>? exn) =>
                publisher.Publish((TEvent)baseEvent, exn);

            return (safeEvent, innerPublish);
        }

        internal readonly Dictionary<Type, (object, InnerPublisher)> innerEvents = new();
    }
}
