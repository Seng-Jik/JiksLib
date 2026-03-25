using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
    /// <summary>
    /// StaticValueEventBus 性能基准测试
    /// 注意：由于StaticValueEventBus是静态类，每个测试使用独立的事件接口以避免状态污染
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class StaticValueEventBusBenchmarks
    {
        // 为每个基准测试使用独立的事件接口，避免静态状态污染
        public interface IStaticPerfEvent { }
        public struct StaticPerfTestEvent : IStaticPerfEvent
        {
            public int Value { get; set; }
        }

        private List<JiksLib.Control.EventHandler<StaticPerfTestEvent>> listeners = null!;

        [Params(1, 10, 100, 1000)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            listeners = new List<JiksLib.Control.EventHandler<StaticPerfTestEvent>>(ListenerCount);

            // 预创建监听器
            for (int i = 0; i < ListenerCount; i++)
            {
                int index = i;
                listeners.Add(e => { /* 空操作 */ });
            }
        }

        // StaticValueEventBus是静态的，不需要IterationSetup来创建实例
        // 但需要清理监听器以避免状态累积

        [IterationSetup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationSetup_WithListeners()
        {
            // 添加监听器
            foreach (var listener in listeners)
            {
                StaticValueEventBus<IStaticPerfEvent>.AddListener<StaticPerfTestEvent>(listener);
            }
        }

        [IterationCleanup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationCleanup_WithListeners()
        {
            // 移除监听器
            foreach (var listener in listeners)
            {
                StaticValueEventBus<IStaticPerfEvent>.RemoveListener(listener);
            }

            // 发布一次空事件，触发SafeEvent的清理机制
            // 这能确保从列表中移除null条目，避免静态状态累积
            var evt = new StaticPerfTestEvent { Value = 0 };
            StaticValueEventBus<IStaticPerfEvent>.Publisher.Publish(evt, null);
        }

        [IterationCleanup(Target = nameof(AddListeners))]
        public void IterationCleanup_AddListeners()
        {
            // 在AddListeners测试中，监听器已经在测试方法中被移除
            // 但为了安全，再次清理
            foreach (var listener in listeners)
            {
                try
                {
                    StaticValueEventBus<IStaticPerfEvent>.RemoveListener(listener);
                }
                catch
                {
                    // 忽略移除失败
                }
            }
        }

        [IterationCleanup(Target = nameof(RemoveListeners))]
        public void IterationCleanup_RemoveListeners()
        {
            // 在RemoveListeners测试中，监听器已经在测试方法中被移除
            // 但为了安全，再次清理
            foreach (var listener in listeners)
            {
                try
                {
                    StaticValueEventBus<IStaticPerfEvent>.RemoveListener(listener);
                }
                catch
                {
                    // 忽略移除失败
                }
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
                StaticValueEventBus<IStaticPerfEvent>.AddListener<StaticPerfTestEvent>(listener);
            }

            // 立即移除监听器以避免内存累积
            foreach (var listener in listeners)
            {
                StaticValueEventBus<IStaticPerfEvent>.RemoveListener(listener);
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
                StaticValueEventBus<IStaticPerfEvent>.AddListener<StaticPerfTestEvent>(listener);
            }

            // 移除监听器
            foreach (var listener in listeners)
            {
                StaticValueEventBus<IStaticPerfEvent>.RemoveListener(listener);
            }
        }

        /// <summary>
        /// 基准测试：发布事件（无监听器）
        /// 注意：由于StaticValueEventBus是静态的，可能受到其他测试的影响
        /// 使用独立的事件接口避免状态污染
        /// </summary>
        [Benchmark]
        public void PublishEvent_NoListeners()
        {
            var evt = new StaticPerfTestEvent { Value = 42 };
            StaticValueEventBus<IStaticPerfEvent>.Publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：发布事件（有监听器）
        /// 监听器在迭代设置中添加，在迭代清理中移除
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            // 发布事件（监听器已在 IterationSetup 中添加）
            var evt = new StaticPerfTestEvent { Value = 42 };
            StaticValueEventBus<IStaticPerfEvent>.Publisher.Publish(evt, null);
        }
    }
}