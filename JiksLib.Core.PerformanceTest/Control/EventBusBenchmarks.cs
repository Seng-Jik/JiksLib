using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
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

        [IterationSetup]
        public void IterationSetup()
        {
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;
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
}