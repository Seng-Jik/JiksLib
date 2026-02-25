using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    internal interface IEventBusListenerSet<TBaseEvent>
    {
        internal abstract void Invoke(
            TBaseEvent baseEvent,
            IList<Exception>? exceptionsOutput);
    }

    internal sealed class EventBusListenerSet<T, TBaseEvent> :
        IEventBusListenerSet<TBaseEvent>
        where T : notnull, TBaseEvent
        where TBaseEvent : notnull
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

        void IEventBusListenerSet<TBaseEvent>.Invoke(
            TBaseEvent baseEvent,
            IList<Exception>? exceptionsOutput)
        {
            Invoke((T)baseEvent, exceptionsOutput);
        }
    }
}
