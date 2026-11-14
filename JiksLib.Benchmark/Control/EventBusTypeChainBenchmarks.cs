using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Control;

namespace JiksLib.Benchmark.Control
{
    public interface IInterfaceEvent
    {
    }

    public sealed class InterfaceEvent : IInterfaceEvent
    {
    }

    public sealed class ClassAndInterfaceEvent : TestEvent, IInterfaceEvent
    {
    }

    /// <summary>
    /// EventBus 的接口基础类型、object 基础类型和共享 TypeChain 缓存基准。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class EventBusTypeChainBenchmarks
    {
        private const int ListenerCount = 10;

        private EventBus<IInterfaceEvent> interfaceBus = null!;
        private EventBus<IInterfaceEvent>.Publisher interfacePublisher;

        private EventBus<object> objectBus = null!;
        private EventBus<object>.Publisher objectPublisher;

        private EventBus<TestEvent> classBus = null!;
        private EventBus<TestEvent>.Publisher classPublisher;

        private EventBus<IInterfaceEvent> sharedInterfaceBus = null!;
        private EventBus<IInterfaceEvent>.Publisher sharedInterfacePublisher;

        [IterationSetup(Target = nameof(PublishEvent_InterfaceBase))]
        public void SetupInterfaceBase()
        {
            interfaceBus = new EventBus<IInterfaceEvent>(out var publisher);
            interfacePublisher = publisher;

            for (int i = 0; i < ListenerCount; ++i)
                interfaceBus.AddListener<IInterfaceEvent>(_ => { });

            interfacePublisher.WarmupPublish<InterfaceEvent>();
        }

        [IterationSetup(Target = nameof(PublishEvent_ObjectBase))]
        public void SetupObjectBase()
        {
            objectBus = new EventBus<object>(out var publisher);
            objectPublisher = publisher;

            for (int i = 0; i < ListenerCount; ++i)
                objectBus.AddListener<TestEvent>(_ => { });

            objectPublisher.WarmupPublish<TestEvent>();
        }

        [IterationSetup(Target = nameof(PublishEvent_SharedTypeChainCache))]
        public void SetupSharedCache()
        {
            var cache = EventBusTypeChain.MakeTypeChainCache();

            classBus = new EventBus<TestEvent>(out var classPub, cache);
            classPublisher = classPub;
            sharedInterfaceBus = new EventBus<IInterfaceEvent>(
                out var interfacePub,
                cache);
            sharedInterfacePublisher = interfacePub;

            for (int i = 0; i < ListenerCount; ++i)
            {
                classBus.AddListener<TestEvent>(_ => { });
                sharedInterfaceBus.AddListener<IInterfaceEvent>(_ => { });
            }

            classPublisher.WarmupPublish<ClassAndInterfaceEvent>();
            sharedInterfacePublisher.WarmupPublish<ClassAndInterfaceEvent>();
        }

        [Benchmark]
        public void PublishEvent_InterfaceBase()
        {
            interfacePublisher.Publish(new InterfaceEvent(), null);
        }

        [Benchmark]
        public void PublishEvent_ObjectBase()
        {
            objectPublisher.Publish(new TestEvent { Value = 42 }, null);
        }

        [Benchmark]
        public void PublishEvent_SharedTypeChainCache()
        {
            var evt = new ClassAndInterfaceEvent { Value = 42 };
            classPublisher.Publish(evt, null);
            sharedInterfacePublisher.Publish(evt, null);
        }
    }
}
