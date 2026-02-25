using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
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
}