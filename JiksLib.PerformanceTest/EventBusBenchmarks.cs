using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JiksLib.Control;

namespace JiksLib.PerformanceTest
{
    /// <summary>
    /// 测试事件类（引用类型）
    /// </summary>
    public class TestEvent
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// 测试事件派生类
    /// </summary>
    public class DerivedTestEvent : TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 测试值类型事件
    /// </summary>
    public struct TestValueEvent
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// EventBus 性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class EventBusBenchmarks
    {
        private EventBus<TestEvent> eventBus = null!;
        private EventBus<TestEvent>.Publisher publisher;
        private List<EventBusListener<TestEvent>> listeners = null!;
        private List<EventBusListener<DerivedTestEvent>> derivedListeners = null!;

        [Params(1)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;
            listeners = new List<EventBusListener<TestEvent>>(ListenerCount);
            derivedListeners = new List<EventBusListener<DerivedTestEvent>>(ListenerCount);

            // 预创建监听器
            for (int i = 0; i < ListenerCount; i++)
            {
                int index = i;
                listeners.Add(e => { /* 空操作 */ });
                derivedListeners.Add(e => { /* 空操作 */ });
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            listeners.Clear();
            derivedListeners.Clear();
        }

        /// <summary>
        /// 基准测试：添加监听器
        /// </summary>
        [Benchmark]
        public void AddListeners()
        {
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：移除监听器
        /// </summary>
        [Benchmark]
        public void RemoveListeners()
        {
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：发布事件（无监听器）
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners()
        {
            var evt = new TestEvent { Value = 42 };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：发布事件（有监听器）
        /// 先添加监听器，然后发布事件
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            // 先添加监听器
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }

            // 发布事件
            var evt = new TestEvent { Value = 42 };
            publisher.Publish(evt, null);

            // 清理监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：发布派生类事件（有基类监听器）
        /// </summary>
        [Benchmark]
        public void PublishDerivedEvent_WithBaseListeners()
        {
            // 添加基类事件监听器
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }

            // 发布派生类事件
            var evt = new DerivedTestEvent { Value = 42, Message = "Test" };
            publisher.Publish(evt, null);

            // 清理监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：继承层次结构的影响
        /// 添加派生类监听器和基类监听器
        /// </summary>
        [Benchmark]
        public void PublishEvent_MixedListeners()
        {
            // 添加基类监听器
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }

            // 添加派生类监听器
            foreach (var listener in derivedListeners)
            {
                eventBus.AddListener(listener);
            }

            // 发布基类事件
            var baseEvt = new TestEvent { Value = 42 };
            publisher.Publish(baseEvt, null);

            // 发布派生类事件
            var derivedEvt = new DerivedTestEvent { Value = 42, Message = "Test" };
            publisher.Publish(derivedEvt, null);

            // 清理监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
            foreach (var listener in derivedListeners)
            {
                eventBus.RemoveListener(listener);
            }
        }
    }

    /// <summary>
    /// ValueEventBus 性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class ValueEventBusBenchmarks
    {
        private ValueEventBus valueEventBus = null!;
        private ValueEventBus.Publisher publisher;
        private List<EventBusListener<TestValueEvent>> listeners = null!;

        [Params(1)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            valueEventBus = new ValueEventBus(out var pub);
            publisher = pub;
            listeners = new List<EventBusListener<TestValueEvent>>(ListenerCount);

            // 预创建监听器
            for (int i = 0; i < ListenerCount; i++)
            {
                int index = i;
                listeners.Add(e => { /* 空操作 */ });
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            listeners.Clear();
        }

        /// <summary>
        /// 基准测试：添加监听器
        /// </summary>
        [Benchmark]
        public void AddListeners()
        {
            foreach (var listener in listeners)
            {
                valueEventBus.AddListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：移除监听器
        /// </summary>
        [Benchmark]
        public void RemoveListeners()
        {
            foreach (var listener in listeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：发布事件（无监听器）
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners()
        {
            var evt = new TestValueEvent { Value = 42 };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：发布事件（有监听器）
        /// 先添加监听器，然后发布事件
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            // 先添加监听器
            foreach (var listener in listeners)
            {
                valueEventBus.AddListener(listener);
            }

            // 发布事件
            var evt = new TestValueEvent { Value = 42 };
            publisher.Publish(evt, null);

            // 清理监听器
            foreach (var listener in listeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }
    }

    /// <summary>
    /// EventBus 与 ValueEventBus 对比基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class EventBusComparisonBenchmarks
    {
        private EventBus<TestEvent> refEventBus = null!;
        private EventBus<TestEvent>.Publisher refPublisher;
        private ValueEventBus valueEventBus = null!;
        private ValueEventBus.Publisher valuePublisher;
        private List<EventBusListener<TestEvent>> refListeners = null!;
        private List<EventBusListener<TestValueEvent>> valueListeners = null!;

        [Params(1)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            refEventBus = new EventBus<TestEvent>(out var refPub);
            refPublisher = refPub;
            valueEventBus = new ValueEventBus(out var valuePub);
            valuePublisher = valuePub;

            refListeners = new List<EventBusListener<TestEvent>>(ListenerCount);
            valueListeners = new List<EventBusListener<TestValueEvent>>(ListenerCount);

            // 预创建监听器
            for (int i = 0; i < ListenerCount; i++)
            {
                refListeners.Add(e => { /* 空操作 */ });
                valueListeners.Add(e => { /* 空操作 */ });
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            refListeners.Clear();
            valueListeners.Clear();
        }

        /// <summary>
        /// 对比基准测试：添加监听器（引用类型 vs 值类型）
        /// </summary>
        [Benchmark]
        public void AddListeners_Comparison()
        {
            // 引用类型事件总线
            foreach (var listener in refListeners)
            {
                refEventBus.AddListener(listener);
            }

            // 值类型事件总线
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener(listener);
            }
        }

        /// <summary>
        /// 对比基准测试：发布事件（引用类型 vs 值类型）
        /// </summary>
        [Benchmark]
        public void PublishEvent_Comparison()
        {
            // 为引用类型事件总线添加监听器
            foreach (var listener in refListeners)
            {
                refEventBus.AddListener(listener);
            }

            // 为值类型事件总线添加监听器
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener(listener);
            }

            // 发布引用类型事件
            var refEvt = new TestEvent { Value = 42 };
            refPublisher.Publish(refEvt, null);

            // 发布值类型事件
            var valueEvt = new TestValueEvent { Value = 42 };
            valuePublisher.Publish(valueEvt, null);

            // 清理监听器
            foreach (var listener in refListeners)
            {
                refEventBus.RemoveListener(listener);
            }
            foreach (var listener in valueListeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 对比基准测试：仅发布事件（无监听器）
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners_Comparison()
        {
            // 发布引用类型事件
            var refEvt = new TestEvent { Value = 42 };
            refPublisher.Publish(refEvt, null);

            // 发布值类型事件
            var valueEvt = new TestValueEvent { Value = 42 };
            valuePublisher.Publish(valueEvt, null);
        }
    }
}