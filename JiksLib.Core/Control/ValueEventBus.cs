using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    /// <summary>
    /// 值类型事件总线
    /// 可以发布值类型的事件
    /// 仅支持在主线程上使用
    /// </summary>
    public sealed class ValueEventBus
    {
        /// <summary>
        /// 事件监听器
        /// </summary>
        public delegate void Listener<T>(T @event) where T : struct;

        /// <summary>
        /// 构造值类型事件总线，同时构造其事件发布器
        /// </summary>
        /// <param name="publisher">构造出的事件发布器</param>
        public ValueEventBus(out Publisher publisher)
        {
            publisher = new(this);
        }

        /// <summary>
        /// 注册某一类型的事件监听器
        /// </summary>
        public void AddListener<T>(Listener<T> listener) where T : struct
        {
            if (!typeHandlers.TryGetValue(typeof(T), out var handler))
            {
                handler = new TypeHandler<T>();
                typeHandlers.Add(typeof(T), handler);
            }

            var typeHandler = (TypeHandler<T>)handler;

            if (invoking)
                typeHandler.AddListenerDelayed(listener);
            else
                typeHandler.AddListener(listener);
        }

        /// <summary>
        /// 移除某一类型的事件监听器
        /// </summary>
        public void RemoveListener<T>(Listener<T> listener) where T : struct
        {
            if (!typeHandlers.TryGetValue(typeof(T), out var handler))
                return;

            var typeHandler = (TypeHandler<T>)handler;

            if (invoking)
                typeHandler.RemoveListenerDelayed(listener);
            else
                typeHandler.RemoveListener(listener);
        }

        /// <summary>
        /// 仅监听一次某个事件，之后自动移除监听器
        /// 该监听器不能使用RemoveListener移除
        /// </summary>
        public void ListenOnce<T>(Listener<T> listener) where T : struct
        {
            if (!typeHandlers.TryGetValue(typeof(T), out var handler))
            {
                handler = new TypeHandler<T>();
                typeHandlers.Add(typeof(T), handler);
            }

            var typeHandler = (TypeHandler<T>)handler;

            void wrappedListener(T e)
            {
                typeHandler.RemoveListenerDelayed(wrappedListener);
                listener(e);
            }

            if (invoking)
                typeHandler.AddListenerDelayed(wrappedListener);
            else
                typeHandler.AddListener(wrappedListener);
        }

        public sealed class Publisher
        {
            internal Publisher(ValueEventBus eventBus)
            {
                this.eventBus = eventBus;
            }

            /// <summary>
            /// 发布事件
            /// </summary>
            /// <param name="@event">事件对象</param>
            /// <param name="exceptionsOutput">事件监听器产生的异常将会被写入到这个列表中，为null则静默忽略异常</param>
            public void Publish<T>(
                T @event,
                IList<Exception>? exceptionsOutput) where T : struct
            {
                if (eventBus.invoking)
                    throw new InvalidOperationException(
                        "Cannot publish event while another event is being published.");

                try
                {
                    eventBus.invoking = true;

                    if (eventBus.typeHandlers.TryGetValue(typeof(T), out var h))
                        ((TypeHandler<T>)h).Invoke(@event, exceptionsOutput);
                }
                finally
                {
                    eventBus.invoking = false;
                }
            }

            readonly ValueEventBus eventBus;
        }

        private sealed class TypeHandler<T> where T : struct
        {
            readonly List<Listener<T>?> listeners = new();
            int removeIndex = 0;

            readonly Queue<(bool isAddOrRemove, Listener<T>)>
                listenersDelayedOperation = new();

            internal void AddListener(Listener<T> listener)
            {
                listeners.Add(listener);
                removeIndex = listeners.Count - 1;
            }

            internal void AddListenerDelayed(Listener<T> listener)
            {
                listenersDelayedOperation.Enqueue((true, listener));
            }

            internal void RemoveListener(Listener<T> listener)
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

            internal void RemoveListenerDelayed(Listener<T> listener)
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
        }

        bool invoking = false;
        readonly Dictionary<Type, object> typeHandlers = new();
    }
}
