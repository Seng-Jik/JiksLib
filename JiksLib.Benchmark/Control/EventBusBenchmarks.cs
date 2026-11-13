using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// EventBus 性能基准测试
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class EventBusBenchmarks
    {
        private EventBus<TestEvent> eventBus = null!;
        private EventBus<TestEvent>.Publisher publisher;
        private List<JiksLib.Control.EventHandler<TestEvent>> listeners = null!;
        private List<JiksLib.Control.EventHandler<DerivedTestEvent>> derivedListeners = null!;

        [Params(1, 10, 100, 1000)]
        public int ListenerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            listeners = new List<JiksLib.Control.EventHandler<TestEvent>>(ListenerCount);
            derivedListeners = new List<JiksLib.Control.EventHandler<DerivedTestEvent>>(ListenerCount);

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

        [IterationSetup(Target = nameof(AddListeners_WithWarmup))]
        public void IterationSetup_AddListenersWithWarmup()
        {
            // 初始化 eventBus，并预热监听器存储以避免基准内部因扩容产生分配
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;
            eventBus.WarmupListenerSlot<TestEvent>(ListenerCount);
        }

        [IterationSetup(Target = nameof(PublishEvent_NoListeners))]
        public void IterationSetup_NoListeners()
        {
            // 初始化 eventBus
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;

            // 预热类型链缓存：避免第一次发布时在基准内部构建 TypeChain
            publisher.WarmupPublish(typeof(TestEvent));
        }

        [IterationSetup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationSetup_WithListeners()
        {
            // 初始化 eventBus
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;

            // 添加监听器
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }

            // 预热类型链缓存：避免第一次发布时在基准内部构建 TypeChain
            publisher.WarmupPublish(typeof(TestEvent));
        }

        [IterationCleanup(Target = nameof(PublishEvent_WithListeners))]
        public void IterationCleanup_WithListeners()
        {
            // 移除监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
        }

        [IterationSetup(Target = nameof(PublishDerivedEvent_WithBaseListeners))]
        public void IterationSetup_DerivedWithBaseListeners()
        {
            // 初始化 eventBus
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;

            // 添加基类监听器
            foreach (var listener in listeners)
            {
                eventBus.AddListener(listener);
            }

            // 预热类型链缓存：避免第一次发布时在基准内部构建 TypeChain
            publisher.WarmupPublish(typeof(DerivedTestEvent));
        }

        [IterationCleanup(Target = nameof(PublishDerivedEvent_WithBaseListeners))]
        public void IterationCleanup_DerivedWithBaseListeners()
        {
            // 移除基类监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
        }

        [IterationSetup(Target = nameof(PublishEvent_MixedListeners))]
        public void IterationSetup_MixedListeners()
        {
            // 初始化 eventBus
            eventBus = new EventBus<TestEvent>(out var pub);
            publisher = pub;

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

            // 预热类型链缓存：两种事件类型分别预热，避免第一次发布时在基准内部构建 TypeChain
            publisher.WarmupPublish(typeof(TestEvent));
            publisher.WarmupPublish(typeof(DerivedTestEvent));
        }

        [IterationCleanup(Target = nameof(PublishEvent_MixedListeners))]
        public void IterationCleanup_MixedListeners()
        {
            // 移除基类监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }
            // 移除派生类监听器
            foreach (var listener in derivedListeners)
            {
                eventBus.RemoveListener(listener);
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
                eventBus.AddListener(listener);
            }

            // 立即移除监听器以避免内存累积
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
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
                eventBus.AddListener(listener);
            }

            // 移除监听器
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
        /// 监听器在迭代设置中添加，在迭代清理中移除
        /// </summary>
        [Benchmark]
        public void PublishEvent_WithListeners()
        {
            // 发布事件（监听器已在 IterationSetup 中添加）
            var evt = new TestEvent { Value = 42 };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：发布派生类事件（有基类监听器）
        /// 监听器在迭代设置中添加，在迭代清理中移除
        /// </summary>
        [Benchmark]
        public void PublishDerivedEvent_WithBaseListeners()
        {
            // 发布派生类事件（监听器已在 IterationSetup 中添加）
            var evt = new DerivedTestEvent { Value = 42, Message = "Test" };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：继承层次结构的影响
        /// 监听器在迭代设置中添加，在迭代清理中移除
        /// </summary>
        [Benchmark]
        public void PublishEvent_MixedListeners()
        {
            // 发布基类事件（监听器已在 IterationSetup 中添加）
            var baseEvt = new TestEvent { Value = 42 };
            publisher.Publish(baseEvt, null);

            // 发布派生类事件（监听器已在 IterationSetup 中添加）
            var derivedEvt = new DerivedTestEvent { Value = 42, Message = "Test" };
            publisher.Publish(derivedEvt, null);
        }
    }
}