using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Collections
{
    public sealed class PriorityQueue<TElement, TPriority>
    {
        public PriorityQueue() : this(Comparer<TPriority>.Default) { }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            heap = new();
            Comparer = comparer;
        }

        public PriorityQueue(IEnumerable<(TElement, TPriority)> collection) :
            this(collection, Comparer<TPriority>.Default)
        { }

        public PriorityQueue(
            IEnumerable<(TElement, TPriority)> collection,
            IComparer<TPriority> comparer)
        {
            heap = collection.ToList();
            Comparer = comparer;
            // todo: 整理堆
        }

        public PriorityQueue(
            int capacity,
            IComparer<TPriority> comparer)
        {
            heap = new(capacity: capacity);
            Comparer = comparer;
        }

        public PriorityQueue(int capacity) :
            this(capacity, Comparer<TPriority>.Default)
        {}

        public int Capacity => heap.Capacity;

        public IComparer<TPriority> Comparer { get; private set; }

        public int Count => heap.Count;

        public void Clear() => heap.Clear();
        public (TElement, TPriority) Dequeue() => throw new NotImplementedException();
        public (TElement, TPriority) DequeueEnqueue(TElement element, TPriority priority) => throw new NotImplementedException();
        public void Enqueue(TElement element, TPriority priority) => throw new NotImplementedException();
        public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority) => throw new NotImplementedException();
        public void EnqueueRange(IEnumerable<(TElement, TPriority)> elements) => throw new NotImplementedException();
#if NETCOREAPP
        public void EnsureCapacity(int capacity) => heap.EnsureCapacity(capacity);
#endif
        public (TElement, TPriority) Peek() => throw new NotImplementedException();
        public void TrimExcess() => heap.TrimExcess();
        public bool TryDequeue(out TElement element, out TPriority priority) => throw new NotImplementedException();
        public bool TryPeek(out TElement element, out TPriority priority) => throw new NotImplementedException();
        public bool Remove(TElement element, out TElement removedElement, out TPriority removePriority, IEqualityComparer<TElement>? comparer = null)
        {
            throw new NotImplementedException();
        }

        public PriorityQueue<TElement, TPriority> Clone() => throw new NotImplementedException();
        
        // todo: unordered items https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2.unordereditems?view=net-10.0#system-collections-generic-priorityqueue-2-unordereditems

        readonly List<(TElement, TPriority)> heap;
    }
}