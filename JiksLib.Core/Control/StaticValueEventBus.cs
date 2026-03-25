using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Control
{
    /// <summary>
    /// 静态值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
    /// 提供最好的性能，只支持值类型的事件，可避免事件发布过程中的装箱和查表操作
    /// </summary>
    /// <typeparam name="TDomain">对事件类型的约束</typeparam>
    public static class StaticValueEventBus<TDomain>
        where TDomain : class
    {
        /// <summary>
        /// 为指定类型的事件添加监听器
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">监听器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddListener<TEvent>(EventHandler<TEvent> handler)
            where TEvent : struct, TDomain
        {
            if (!Publisher.Singleton.weakPublishers.ContainsKey(typeof(TEvent)))
            {
                Publisher.Singleton.weakPublishers.Add(
                    typeof(TEvent),
                    (e, o) => Handler<TEvent>.Publisher.Publish((TEvent)e, o));
            }

            Handler<TEvent>.Event.AddListener(handler);
        }

        /// <summary>
        /// 移除指定类型的指定监听器
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">监听器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveListener<TEvent>(EventHandler<TEvent> handler)
            where TEvent : struct, TDomain =>
            Handler<TEvent>.Event.RemoveListener(handler);

        /// <summary>
        /// 事件发布器
        /// </summary>
        public sealed class Publisher : ISafeEventPublisher<TDomain>
        {
            /// <summary>
            /// 事件发布器实例
            /// 仅用于转换为 ISafeEventPublisher<TDomain>，但转换后会产生装箱操作和查表操作
            /// </summary>
            public static readonly Publisher Singleton = new();

            /// <summary>
            /// 发布事件
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Publish<TEvent>(
                TEvent @event,
                IList<Exception>? exceptionsOutput)
                where TEvent : struct, TDomain =>
                Handler<TEvent>.Publisher.Publish(@event, exceptionsOutput);

            void ISafeEventPublisher<TDomain>.Publish(
                TDomain @event,
                IList<Exception>? exceptionsOutput)
            {
                if (weakPublishers.TryGetValue(@event.GetType(), out var p))
                    p(@event, exceptionsOutput);
            }

            internal Dictionary<Type, ValueEventBus<TDomain>.WeakPublisher>
                weakPublishers = new();

            private Publisher() {}
        }

        private static class Handler<TEvent>
        {
            public static readonly SafeEvent<TEvent> Event = new(out Publisher);
            public static readonly SafeEvent<TEvent>.Publisher Publisher;
        }
    }
}
