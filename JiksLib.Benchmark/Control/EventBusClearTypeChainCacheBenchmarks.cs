using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    /// <summary>
    /// EventBus 清空 TypeChain 缓存后首次发布的成本对比。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class EventBusClearTypeChainCacheBenchmarks
    {
        private static readonly JiksLib.Control.EventHandler<TestEvent> Listener =
            _ => { };

        private EventBus<TestEvent> eventBus = null!;
        private EventBus<TestEvent>.Publisher publisher;

        [IterationSetup(Target = nameof(PublishWithPrewarmedCache))]
        public void SetupPrewarmed()
        {
            var cache = EventBus<TestEvent>.TypeChain.MakeTypeChainCache();
            eventBus = new EventBus<TestEvent>(out var pub, cache);
            publisher = pub;

            eventBus.AddListener(Listener);
            publisher.WarmupPublish<DerivedTestEvent>();
        }

        [IterationSetup(Target = nameof(PublishAfterClearTypeChainCache))]
        public void SetupForClear()
        {
            var cache = EventBus<TestEvent>.TypeChain.MakeTypeChainCache();
            eventBus = new EventBus<TestEvent>(out var pub, cache);
            publisher = pub;

            eventBus.AddListener(Listener);
            publisher.WarmupPublish<DerivedTestEvent>();
        }

        /// <summary>
        /// 基准测试：使用已预热的 TypeChain 直接发布。
        /// </summary>
        [Benchmark(Baseline = true)]
        public void PublishWithPrewarmedCache()
        {
            publisher.Publish(new DerivedTestEvent(), null);
        }

        /// <summary>
        /// 基准测试：清空 TypeChain 缓存后首次发布，包含重建类型链的成本。
        /// </summary>
        [Benchmark]
        public void PublishAfterClearTypeChainCache()
        {
            publisher.ClearTypeChainCache();
            publisher.Publish(new DerivedTestEvent(), null);
        }
    }
}
