using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
    /// <summary>
    /// EventBus 与 ValueEventBus 对比基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class EventBusComparisonBenchmarks
    {
        private EventBus<TestEvent> refEventBus = null!;
        private EventBus<TestEvent>.Publisher refPublisher;
        private ValueEventBus<IValueEvent> valueEventBus = null!;
        private ValueEventBus<IValueEvent>.Publisher valuePublisher;
        private List<EventBusListener<TestEvent>> refListeners = null!;
        private List<EventBusListener<TestValueEvent>> valueListeners = null!;

        [Params(1)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            refEventBus = new EventBus<TestEvent>(out var refPub);
            refPublisher = refPub;
            valueEventBus = new ValueEventBus<IValueEvent>(out var valuePub);
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

        [IterationSetup(Target = nameof(PublishEvent_Comparison))]
        public void IterationSetup_Comparison()
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
        }

        [IterationCleanup(Target = nameof(PublishEvent_Comparison))]
        public void IterationCleanup_Comparison()
        {
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
        /// 监听器在 GlobalSetup 中添加
        /// </summary>
        [Benchmark]
        public void PublishEvent_Comparison()
        {
            // 发布引用类型事件
            var refEvt = new TestEvent { Value = 42 };
            refPublisher.Publish(refEvt, null);

            // 发布值类型事件
            var valueEvt = new TestValueEvent { Value = 42 };
            valuePublisher.Publish(valueEvt, null);
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