using System;
using System.Collections.Generic;
using JiksLib.Control;

namespace JiksLib.Core.Control
{
    /// <summary>
    /// 事件过滤器
    /// 在事件的发布端监听所有事件，并根据需要过滤事件
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

        public EventFilter(
            TEventPublisher eventPublisher,
            Filter filter)
        {
            this.filter = filter;
            this.eventPublisher = eventPublisher;
        }

        public void Publish(
            TEvent @event,
            IList<Exception>? exceptionsOutput)
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
