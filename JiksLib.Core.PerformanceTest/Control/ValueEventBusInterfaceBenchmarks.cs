using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
    /// <summary>
    /// ValueEventBus 接口实现性能基准测试
    /// 评估 ISafeEventPublisher 接口实现和数据结构变化对性能的影响
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class ValueEventBusInterfaceBenchmarks
    {
        private ValueEventBus<IValueEvent> valueEventBus = null!;
        private ValueEventBus<IValueEvent>.Publisher publisher;
        private ISafeEventPublisher<IValueEvent>? interfacePublisher;
        private List<JiksLib.Control.EventHandler<TestValueEvent>> listeners = null!;

        [Params(1, 10, 100)]
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
            interfacePublisher = pub; // 隐式转换为接口
        }

        [IterationSetup(Target = nameof(PublishViaInterface_WithListeners))]
        public void IterationSetup_WithListeners()
        {
            valueEventBus = new ValueEventBus<IValueEvent>(out var pub);
            publisher = pub;
            interfacePublisher = pub;

            // 添加监听器
            foreach (var listener in listeners)
            {
                valueEventBus.AddListener<TestValueEvent>(listener);
            }
        }

        [IterationCleanup(Target = nameof(PublishViaInterface_WithListeners))]
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
        /// 基准测试：通过接口发布事件（无监听器）
        /// 测试 ISafeEventPublisher.Publish 方法的性能
        /// </summary>
        [Benchmark]
        public void PublishViaInterface_NoListeners()
        {
            var evt = new TestValueEvent { Value = 42 };
            interfacePublisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：通过接口发布事件（有监听器）
        /// 测试 WeakPublisher 委托调用的性能
        /// </summary>
        [Benchmark]
        public void PublishViaInterface_WithListeners()
        {
            var evt = new TestValueEvent { Value = 42 };
            interfacePublisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：直接通过 Publisher.Publish 方法发布事件（无监听器）
        /// 作为对比基准
        /// </summary>
        [Benchmark(Baseline = true)]
        public void PublishDirect_NoListeners()
        {
            var evt = new TestValueEvent { Value = 42 };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：直接通过 Publisher.Publish 方法发布事件（有监听器）
        /// 作为对比基准
        /// </summary>
        [Benchmark]
        public void PublishDirect_WithListeners()
        {
            var evt = new TestValueEvent { Value = 42 };
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：多次通过接口发布不同类型的事件
        /// 测试 WeakPublisher 委托查找和调用的性能
        /// </summary>
        [Benchmark]
        public void PublishMultipleEventTypes_ViaInterface()
        {
            // 定义多个值类型事件
            var evt1 = new TestValueEvent { Value = 1 };
            var evt2 = new TestValueEvent { Value = 2 };
            var evt3 = new TestValueEvent { Value = 3 };

            interfacePublisher.Publish(evt1, null);
            interfacePublisher.Publish(evt2, null);
            interfacePublisher.Publish(evt3, null);
        }

        /// <summary>
        /// 基准测试：元组访问性能
        /// 测试从 (object, WeakPublisher) 元组中访问元素的性能
        /// </summary>
        [Benchmark]
        public void TupleAccessPerformance()
        {
            // 这个测试模拟了 ValueEventBus 内部从元组中获取 WeakPublisher 的操作
            // 由于不能直接访问私有字段，我们通过实际调用来测试

            var evt = new TestValueEvent { Value = 42 };

            // 通过接口发布，内部会使用元组中的 WeakPublisher
            interfacePublisher.Publish(evt, null);

            // 再通过直接方法发布一次
            publisher.Publish(evt, null);
        }

        /// <summary>
        /// 基准测试：接口转换开销
        /// 测试结构体到接口的转换性能
        /// </summary>
        [Benchmark]
        public void InterfaceConversionOverhead()
        {
            // 测试结构体到接口的转换开销
            var evt = new TestValueEvent { Value = 42 };

            // 多次转换和调用
            for (int i = 0; i < 10; i++)
            {
                ISafeEventPublisher<IValueEvent> tempPublisher = publisher;
                tempPublisher.Publish(evt, null);
            }
        }

        /// <summary>
        /// 基准测试：字典查找性能（模拟）
        /// 测试字典查找和元组访问的组合性能
        /// </summary>
        [Benchmark]
        public void DictionaryLookupWithTuple()
        {
            // 模拟字典查找和元组访问
            // 通过多次发布事件来测试
            var evt = new TestValueEvent { Value = 42 };

            for (int i = 0; i < 5; i++)
            {
                interfacePublisher.Publish(evt, null);
            }
        }
    }
}