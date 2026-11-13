using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// ValueEventBus 性能基准测试
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class ValueEventBusBenchmarks
    {
        private ValueEventBus<IValueEvent> valueEventBus = null!;
        private ValueEventBus<IValueEvent>.Publisher publisher;
        private List<JiksLib.Control.EventHandler<TestValueEvent>> listeners = null!;

        [Params(1, 10, 100, 1000)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            listeners = new List<JiksLib.Control.EventHandler<TestValueEvent>>(ListenerCount);

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

        [IterationSetup(Target = nameof(AddListeners_WithWarmup))]
        public void IterationSetup_AddListenersWithWarmup()
        {
            // 初始化 valueEventBus，并预热监听器存储以避免基准内部因扩容产生分配
            valueEventBus = new ValueEventBus<IValueEvent>(out var pub);
            publisher = pub;
            valueEventBus.WarmupListenerSlot<TestValueEvent>(ListenerCount);
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
            AddAndRemoveListeners();
        }

        /// <summary>
        /// 基准测试：预热监听器容量后添加监听器
        /// 与 AddListeners 的唯一区别是 IterationSetup 里调用了 WarmupListenerSlot，
        /// 用于对比监听器存储从空 List 扩容与预分配容量的分配差异
        /// </summary>
        [Benchmark]
        public void AddListeners_WithWarmup()
        {
            AddAndRemoveListeners();
        }

        void AddAndRemoveListeners()
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