using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Control
{
    public delegate void EventHandler<TEvent>(TEvent @event);

    public sealed class SafeEvent<TEvent>
    {
        public SafeEvent(out Publisher publisher)
        {
            publisher = new(this);
        }

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

        public readonly struct Publisher
        {
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
