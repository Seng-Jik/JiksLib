using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// EventBus 首次发布时，独立缓存与预共享 TypeChain 缓存的对比。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class EventBusWarmupComparisonBenchmarks
    {
        private static readonly JiksLib.Control.EventHandler<TestEvent> Listener =
            _ => { };

        private ICache<Type, EventBusTypeChain> sharedCache = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            sharedCache = EventBusTypeChain.MakeTypeChainCache();

            // 先用另一个总线预热共享缓存中的 DerivedTestEvent 类型链。
            var warmBus = new EventBus<TestEvent>(out var publisher, sharedCache);
            warmBus.AddListener(Listener);
            publisher.WarmupPublish<DerivedTestEvent>();
        }

        /// <summary>
        /// 基准测试：全新的独立 TypeChain 缓存，首次发布需要构建类型链。
        /// </summary>
        [Benchmark(Baseline = true)]
        public void FirstPublish_IndependentCache()
        {
            var bus = new EventBus<TestEvent>(out var publisher);
            bus.AddListener(Listener);
            publisher.Publish(new DerivedTestEvent(), null);
        }

        /// <summary>
        /// 基准测试：复用其他总线预热过的共享 TypeChain 缓存。
        /// </summary>
        [Benchmark]
        public void FirstPublish_SharedPrewarmedCache()
        {
            var bus = new EventBus<TestEvent>(out var publisher, sharedCache);
            bus.AddListener(Listener);
            publisher.Publish(new DerivedTestEvent(), null);
        }
    }
}
