using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
    /// <summary>
    /// StaticValueEventBus 与 ValueEventBus 对比基准测试
    /// 测试StaticValueEventBus是否比ValueEventBus有性能优势
    /// </summary>
    [MemoryDiagnoser]
    public class StaticValueEventBusComparisonBenchmarks
    {
        // 为StaticValueEventBus使用独立的事件接口
        public interface IStaticComparisonEvent { }
        public struct StaticComparisonEvent : IStaticComparisonEvent
        {
            public int Value { get; set; }
        }

        // 为ValueEventBus使用独立的事件接口
        public interface IValueComparisonEvent { }
        public struct ValueComparisonEvent : IValueComparisonEvent
        {
            public int Value { get; set; }
        }

        private ValueEventBus<IValueComparisonEvent> valueEventBus = null!;
        private ValueEventBus<IValueComparisonEvent>.Publisher valuePublisher;

        private List<JiksLib.Control.EventHandler<StaticComparisonEvent>> staticListeners = null!;
        private List<JiksLib.Control.EventHandler<ValueComparisonEvent>> valueListeners = null!;

        [Params(1, 10, 100, 1000)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // 初始化ValueEventBus
            valueEventBus = new ValueEventBus<IValueComparisonEvent>(out var valuePub);
            valuePublisher = valuePub;

            // 预创建监听器
            staticListeners = new List<JiksLib.Control.EventHandler<StaticComparisonEvent>>(ListenerCount);
            valueListeners = new List<JiksLib.Control.EventHandler<ValueComparisonEvent>>(ListenerCount);

            for (int i = 0; i < ListenerCount; i++)
            {
                staticListeners.Add(e => { /* 空操作 */ });
                valueListeners.Add(e => { /* 空操作 */ });
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            staticListeners.Clear();
            valueListeners.Clear();
        }

        [IterationSetup(Target = nameof(PublishEvent_WithListeners_Comparison))]
        public void IterationSetup_WithListeners()
        {
            // 为StaticValueEventBus添加监听器
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.AddListener<StaticComparisonEvent>(listener);
            }

            // 为ValueEventBus添加监听器
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener<ValueComparisonEvent>(listener);
            }
        }

        [IterationCleanup(Target = nameof(PublishEvent_WithListeners_Comparison))]
        public void IterationCleanup_WithListeners()
        {
            // 清理StaticValueEventBus监听器
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.RemoveListener(listener);
            }

            // 清理ValueEventBus监听器
            foreach (var listener in valueListeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 对比基准测试：添加监听器
        /// StaticValueEventBus vs ValueEventBus
        /// </summary>
        [Benchmark]
        public void AddListeners_Comparison()
        {
            // StaticValueEventBus - 添加监听器
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.AddListener<StaticComparisonEvent>(listener);
            }

            // ValueEventBus - 添加监听器
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener<ValueComparisonEvent>(listener);
            }
        }

        /// <summary>
        /// 对比基准测试：移除监听器
        /// StaticValueEventBus vs ValueEventBus
        /// </summary>
        [Benchmark]
        public void RemoveListeners_Comparison()
        {
            // 先添加监听器
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.AddListener<StaticComparisonEvent>(listener);
            }
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener<ValueComparisonEvent>(listener);
            }

            // 然后移除监听器
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.RemoveListener(listener);
            }
            foreach (var listener in valueListeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 对比基准测试：发布事件（无监听器）
        /// 测试无监听器时的发布性能
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners_Comparison()
        {
            // StaticValueEventBus - 发布事件（无监听器）
            var staticEvent = new StaticComparisonEvent { Value = 42 };
            StaticValueEventBus<IStaticComparisonEvent>.Publisher.Publish(staticEvent, null);

            // ValueEventBus - 发布事件（无监听器）
            var valueEvent = new ValueComparisonEvent { Value = 42 };
            valuePublisher.Publish(valueEvent, null);
        }

        /// <summary>
        /// 对比基准测试：发布事件（有监听器）
        /// 监听器在IterationSetup中添加
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners_Comparison()
        {
            // StaticValueEventBus - 发布事件（有监听器）
            var staticEvent = new StaticComparisonEvent { Value = 42 };
            StaticValueEventBus<IStaticComparisonEvent>.Publisher.Publish(staticEvent, null);

            // ValueEventBus - 发布事件（有监听器）
            var valueEvent = new ValueComparisonEvent { Value = 42 };
            valuePublisher.Publish(valueEvent, null);
        }

        /// <summary>
        /// 单独测试：StaticValueEventBus发布事件（有监听器）
        /// 用于精确测量StaticValueEventBus的性能
        /// </summary>
        [Benchmark]
        public void PublishEvent_StaticOnly_WithListeners()
        {
            var staticEvent = new StaticComparisonEvent { Value = 42 };
            StaticValueEventBus<IStaticComparisonEvent>.Publisher.Publish(staticEvent, null);
        }

        /// <summary>
        /// 单独测试：ValueEventBus发布事件（有监听器）
        /// 用于精确测量ValueEventBus的性能
        /// </summary>
        [Benchmark]
        public void PublishEvent_ValueOnly_WithListeners()
        {
            var valueEvent = new ValueComparisonEvent { Value = 42 };
            valuePublisher.Publish(valueEvent, null);
        }

        /// <summary>
        /// 单独测试：StaticValueEventBus添加监听器
        /// 用于精确测量添加性能
        /// </summary>
        [Benchmark]
        public void AddListeners_StaticOnly()
        {
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.AddListener<StaticComparisonEvent>(listener);
            }

            // 立即移除以避免状态累积
            foreach (var listener in staticListeners)
            {
                StaticValueEventBus<IStaticComparisonEvent>.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 单独测试：ValueEventBus添加监听器
        /// 用于精确测量添加性能
        /// </summary>
        [Benchmark]
        public void AddListeners_ValueOnly()
        {
            foreach (var listener in valueListeners)
            {
                valueEventBus.AddListener<ValueComparisonEvent>(listener);
            }

            // 立即移除以避免状态累积
            foreach (var listener in valueListeners)
            {
                valueEventBus.RemoveListener(listener);
            }
        }
    }
}