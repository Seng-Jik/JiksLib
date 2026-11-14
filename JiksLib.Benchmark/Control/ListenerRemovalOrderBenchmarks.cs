using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// 对比正序删除（FIFO）与逆序删除（LIFO）的开销。
    /// IterationSetup 预置监听器，Benchmark 内只执行 Remove。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class ListenerRemovalOrderBenchmarks
    {
        private List<JiksLib.Control.EventHandler<TestEvent>> refListeners = null!;
        private List<JiksLib.Control.EventHandler<TestValueEvent>> valueListeners = null!;

        private EventBus<TestEvent> refEventBus = null!;
        private ValueEventBus<IValueEvent> valueEventBus = null!;

        [Params(1, 10, 100, 1000)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            refListeners = new List<JiksLib.Control.EventHandler<TestEvent>>(ListenerCount);
            valueListeners = new List<JiksLib.Control.EventHandler<TestValueEvent>>(ListenerCount);

            for (int i = 0; i < ListenerCount; ++i)
            {
                refListeners.Add(_ => { });
                valueListeners.Add(_ => { });
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            refEventBus = new EventBus<TestEvent>(out _);
            valueEventBus = new ValueEventBus<IValueEvent>(out _);

            foreach (var listener in refListeners)
                refEventBus.AddListener(listener);

            foreach (var listener in valueListeners)
                valueEventBus.AddListener<TestValueEvent>(listener);
        }

        [Benchmark]
        public void RemoveForward_EventBus()
        {
            for (int i = 0; i < refListeners.Count; ++i)
                refEventBus.RemoveListener(refListeners[i]);
        }

        [Benchmark]
        public void RemoveReverse_EventBus()
        {
            for (int i = refListeners.Count - 1; i >= 0; --i)
                refEventBus.RemoveListener(refListeners[i]);
        }

        [Benchmark]
        public void RemoveForward_ValueEventBus()
        {
            for (int i = 0; i < valueListeners.Count; ++i)
                valueEventBus.RemoveListener(valueListeners[i]);
        }

        [Benchmark]
        public void RemoveReverse_ValueEventBus()
        {
            for (int i = valueListeners.Count - 1; i >= 0; --i)
                valueEventBus.RemoveListener(valueListeners[i]);
        }
    }
}
