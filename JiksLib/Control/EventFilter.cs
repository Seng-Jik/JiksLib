using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    /// <summary>
    /// 事件过滤器
    /// 在事件的发布端监听所有事件，并根据需要过滤事件。
    /// 过滤函数抛出的异常会被写入 exceptionsOutput 并丢弃该事件，不会向外抛出。
    /// </summary>
    /// <typeparam name="TEventPublisher">事件发布器类型</typeparam>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public readonly struct EventFilter<TEventPublisher, TEvent>
        : ISafeEventPublisher<TEvent>
        where TEventPublisher : ISafeEventPublisher<TEvent>
    {
        /// <summary>
        /// 事件过滤函数
        /// </summary>
        /// <param name="event">事件，允许对事件进行修改</param>
        /// <returns>返回true则继续传递事件，否则丢弃事件</returns>
        public delegate bool Filter(ref TEvent @event);

        /// <summary>
        /// 构造事件过滤器
        /// </summary>
        /// <param name="eventPublisher">被装饰的事件发布器</param>
        /// <param name="filter">过滤函数，返回 false 时丢弃事件</param>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="eventPublisher"/> 或 <paramref name="filter"/> 为 null 时抛出
        /// </exception>
        public EventFilter(
            TEventPublisher eventPublisher,
            Filter filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));

            if (eventPublisher == null)
                throw new ArgumentNullException(nameof(eventPublisher));

            this.eventPublisher = eventPublisher;
        }

        public void Publish(
            TEvent @event,
            ICollection<Exception>? exceptionsOutput)
        {
            bool success = false;

            try
            {
                success = filter(ref @event);
            }
            catch (Exception e)
            {
                exceptionsOutput?.Add(e);
            }

            if (success)
            {
                eventPublisher.Publish(
                    @event,
                    exceptionsOutput);
            }
        }

        readonly TEventPublisher eventPublisher;
        readonly Filter filter;
    }
}
