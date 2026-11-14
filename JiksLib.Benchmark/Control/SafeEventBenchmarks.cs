using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// SafeEvent 性能基准测试。
    /// 覆盖普通发布、监听器数量以及监听器增删路径。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class SafeEventBenchmarks
    {
        private SafeEvent<TestValueEvent> safeEvent = null!;
        private SafeEvent<TestValueEvent>.Publisher publisher;
        private List<JiksLib.Control.EventHandler<TestValueEvent>> listeners = null!;
        private List<Exception> exceptions = new();
        private TestValueEvent evt;

        [Params(1, 10, 100)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            listeners = new List<JiksLib.Control.EventHandler<TestValueEvent>>(ListenerCount);

            for (int i = 0; i < ListenerCount; ++i)
                listeners.Add(_ => { /* 空操作 */ });

            evt = new TestValueEvent { Value = 42 };
        }

        [IterationSetup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationSetup_WithListeners()
        {
            safeEvent = new SafeEvent<TestValueEvent>(
                out var pub,
                capacity: ListenerCount);
            publisher = pub;

            foreach (var listener in listeners)
                safeEvent.AddListener(listener);
        }

        [IterationSetup(Target = nameof(PublishEvent_NoListeners))]
        public void IterationSetup_NoListeners()
        {
            safeEvent = new SafeEvent<TestValueEvent>(out var pub, capacity: 0);
            publisher = pub;
        }

        [IterationSetup(Target = nameof(AddAndRemoveListeners))]
        public void IterationSetup_Empty()
        {
            safeEvent = new SafeEvent<TestValueEvent>(
                out var pub,
                capacity: ListenerCount);
            publisher = pub;
        }

        [IterationSetup(Target = nameof(AddAndRemoveListeners_DefaultCapacity))]
        public void IterationSetup_DefaultCapacity()
        {
            safeEvent = new SafeEvent<TestValueEvent>(out var pub);
            publisher = pub;
        }

        [IterationSetup(Target = nameof(PublishEvent_ListenerThrows))]
        public void IterationSetup_ListenerThrows()
        {
            safeEvent = new SafeEvent<TestValueEvent>(out var pub, capacity: 1);
            publisher = pub;
            exceptions = new List<Exception>(1);

            safeEvent.AddListener(_ =>
                throw new InvalidOperationException("benchmark failure"));
        }

        /// <summary>
        /// 基准测试：发布事件（有监听器）。
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：发布事件（无监听器）。
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners()
        {
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：使用默认容量添加后移除全部监听器。
        /// </summary>
        [Benchmark(Baseline = true)]
        public void AddAndRemoveListeners_DefaultCapacity()
        {
            AddAndRemoveListenersCore();
        }

        /// <summary>
        /// 基准测试：预分配容量后添加并移除全部监听器。
        /// 用于对比 capacity 构造函数对监听器存储扩容的影响。
        /// </summary>
        [Benchmark]
        public void AddAndRemoveListeners()
        {
            AddAndRemoveListenersCore();
        }

        void AddAndRemoveListenersCore()
        {
            foreach (var listener in listeners)
                safeEvent.AddListener(listener);

            foreach (var listener in listeners)
                safeEvent.RemoveListener(listener);
        }

        /// <summary>
        /// 基准测试：监听器抛异常时的异常收集路径。
        /// </summary>
        [Benchmark]
        public void PublishEvent_ListenerThrows()
        {
            publisher.Publish(evt, exceptions);
            exceptions.Clear();
        }
    }

    /// <summary>
    /// SafeEvent 递归发布与发布期间增删监听器的基准测试。
    /// 这两个路径都会走延迟队列，因此单独作为一组测量。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class SafeEventRecursiveBenchmarks
    {
        private static readonly JiksLib.Control.EventHandler<TestValueEvent> NoOp =
            _ => { };

        private SafeEvent<TestValueEvent> safeEvent = null!;
        private SafeEvent<TestValueEvent>.Publisher publisher;
        private TestValueEvent outer;
        private TestValueEvent inner;

        private const int RecursiveChainDepth = 8;

        [GlobalSetup]
        public void GlobalSetup()
        {
            outer = new TestValueEvent { Value = 0 };
            inner = new TestValueEvent { Value = 1 };
        }

        [IterationSetup(Target = nameof(PublishEvent_RecursiveQueue))]
        public void IterationSetup_RecursiveQueue()
        {
            safeEvent = new SafeEvent<TestValueEvent>(
                out var pub,
                allowRecursiveInvoke: true,
                capacity: 1);
            publisher = pub;

            safeEvent.AddListener(e =>
            {
                if (e.Value == 0)
                    publisher.Publish(inner, null);
            });

            // 预热递归队列，避免首次入队时的 Queue 扩容计入基准。
            publisher.Publish(outer, null);
        }

        [IterationSetup(Target = nameof(PublishEvent_DeferredListenerChanges))]
        public void IterationSetup_DeferredListenerChanges()
        {
            safeEvent = new SafeEvent<TestValueEvent>(
                out var pub,
                allowAddRemoveListenerDuringInvoke: true,
                capacity: 2);
            publisher = pub;

            safeEvent.AddListener(e =>
            {
                if (e.Value != 0)
                    return;

                safeEvent.AddListener(NoOp);
                safeEvent.RemoveListener(NoOp);
            });

            // 预热延迟增删队列，避免首次入队时的 Queue 扩容计入基准。
            publisher.Publish(outer, null);
        }

        [IterationSetup(Target = nameof(PublishEvent_RecursiveChain))]
        public void IterationSetup_RecursiveChain()
        {
            safeEvent = new SafeEvent<TestValueEvent>(
                out var pub,
                allowRecursiveInvoke: true,
                capacity: 1);
            publisher = pub;

            safeEvent.AddListener(e =>
            {
                if (e.Value < RecursiveChainDepth)
                    publisher.Publish(
                        new TestValueEvent { Value = e.Value + 1 },
                        null);
            });

            // 预热队列与整条链。
            publisher.Publish(outer, null);
        }

        /// <summary>
        /// 基准测试：外层发布 + 一个排队的嵌套事件。
        /// </summary>
        [Benchmark]
        public void PublishEvent_RecursiveQueue()
        {
            publisher.Publish(outer, null);
        }

        /// <summary>
        /// 基准测试：发布期间添加并移除一个监听器。
        /// </summary>
        [Benchmark]
        public void PublishEvent_DeferredListenerChanges()
        {
            publisher.Publish(outer, null);
        }

        /// <summary>
        /// 基准测试：深度为 8 的递归发布链，测量队列排空开销。
        /// </summary>
        [Benchmark]
        public void PublishEvent_RecursiveChain()
        {
            publisher.Publish(outer, null);
        }
    }
}
