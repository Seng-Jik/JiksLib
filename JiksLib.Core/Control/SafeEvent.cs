using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Control
{
    /// <summary>
    /// 安全事件处理器委托
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    public delegate void EventHandler<TEvent>(TEvent @event);

    /// <summary>
    /// 安全事件包装器
    /// 提供线程安全的事件发布/订阅机制，支持重入保护和异常安全
    /// 仅支持在主线程上使用
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public sealed class SafeEvent<TEvent>
    {
        /// <summary>
        /// 构造安全事件包装器
        /// </summary>
        /// <param name="publisher">输出的事件发布器</param>
        public SafeEvent(out Publisher publisher)
        {
            publisher = new(this);
        }

        /// <summary>
        /// 添加事件监听器
        /// 在事件处理过程中添加的监听器会延迟到处理完成后生效
        /// </summary>
        /// <param name="listener">事件监听器</param>
        public void AddListener(EventHandler<TEvent> listener)
        {
            if (invoking)
            {
                listenersDelayedOperation ??= new();
                listenersDelayedOperation.Enqueue((true, listener));
            }
            else
            {
                listeners.Add(listener);
                removeIndex = listeners.Count - 1;
            }
        }

        /// <summary>
        /// 移除事件监听器
        /// 在事件处理过程中移除的监听器会延迟到处理完成后生效
        /// </summary>
        /// <param name="listener">要移除的事件监听器</param>
        public void RemoveListener(EventHandler<TEvent> listener)
        {
            if (invoking)
            {
                listenersDelayedOperation ??= new();
                listenersDelayedOperation.Enqueue((false, listener));
            }
            else
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
        }

        /// <summary>
        /// 事件发布器
        /// 用于安全地触发事件
        /// </summary>
        public readonly struct Publisher
        {
            /// <summary>
            /// 安全触发事件
            /// 触发所有已注册的监听器，收集异常并防止递归调用
            /// </summary>
            /// <param name="event">事件对象</param>
            /// <param name="exceptionsOutput">异常输出列表，监听器产生的异常将被收集到此列表中，为null则静默忽略异常</param>
            public readonly void SafeInvoke(
                TEvent @event,
                IList<Exception>? exceptionsOutput)
            {
                if (this.@event.invoking)
                    throw new InvalidOperationException(
                        "SafeEvent cannot be invoked recursively.");

                try
                {
                    this.@event.invoking = true;
                    int rewriteIndex = 0;
                    var listeners = this.@event.listeners;

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
                finally
                {
                    this.@event.invoking = false;
                    this.@event.ApplyDelayedListenerAddOrRemove();
                }
            }

            internal Publisher(SafeEvent<TEvent> @event)
            {
                this.@event = @event;
            }

            readonly SafeEvent<TEvent> @event;
        }

        readonly List<EventHandler<TEvent>?> listeners = new();
        bool invoking = false;
        int removeIndex = 0;

        Queue<(bool isAddOrRemove, EventHandler<TEvent>)>?
            listenersDelayedOperation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyDelayedListenerAddOrRemove()
        {
            if (listenersDelayedOperation == null) return;

            while (listenersDelayedOperation.Count > 0)
            {
                var op = listenersDelayedOperation.Dequeue();

                if (op.isAddOrRemove) AddListener(op.Item2);
                else RemoveListener(op.Item2);
            }
        }
    }
}
