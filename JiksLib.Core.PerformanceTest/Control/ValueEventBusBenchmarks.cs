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
        private ValueEventBus<IValueEvent> valueEventBus = null!;
        private ValueEventBus<IValueEvent>.Publisher publisher;
        private List<EventBusListener<TestValueEvent>> listeners = null!;

        [Params(1)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            listeners = new List<EventBusListener<TestValueEvent>>(ListenerCount);

            // 预创建监听器
            for (int i = 0; i < ListenerCount; i++)
            {
                int index = i;
                listeners.Add(e => { /* 空操作 */ });
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            valueEventBus = new ValueEventBus<IValueEvent>(out var pub);
            publisher = pub;
        }

        [IterationSetup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationSetup_WithListeners()
        {
            // 初始化 valueEventBus
            valueEventBus = new ValueEventBus<IValueEvent>(out var pub);
            publisher = pub;

            // 添加监听器
            foreach (var listener in listeners)
            {
                valueEventBus.AddListener<TestValueEvent>(listener);
            }
        }

        [IterationCleanup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationCleanup_WithListeners()
        {
            // 移除监听器
            foreach (var listener in listeners)
            {
                valueEventBus.RemoveListener(listener);
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
                valueEventBus.AddListener<TestValueEvent>(listener);
            }

            // 立即移除监听器以避免内存累积
            foreach (var listener in listeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：移除监听器
        /// </summary>
        [Benchmark]
        public void RemoveListeners()
        {
            // 先添加监听器以便测试移除操作
            foreach (var listener in listeners)
            {
                valueEventBus.AddListener(listener);
            }

            // 移除监听器
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
        /// 监听器在迭代设置中添加，在迭代清理中移除
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            // 发布事件（监听器已在 IterationSetup 中添加）
            var evt = new TestValueEvent { Value = 42 };
            publisher.Publish(evt, null);
        }
    }
}