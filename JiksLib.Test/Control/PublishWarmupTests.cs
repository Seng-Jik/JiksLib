using NUnit.Framework;
using JiksLib.Control;
using System;

namespace JiksLib.Test.Control
{
    /// <summary>
    /// EventBus.Publisher.WarmupPublish 的测试
    /// 覆盖类型链预热的正常路径、参数校验、重复预热以及按运行时类型预热的行为
    /// </summary>
    [TestFixture]
    public class PublishWarmupTests
    {
        [Test]
        public void WarmupPublish_ThenPublish_CallsListener()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool called = false;
            bus.AddListener<TestEventA>(_ => called = true);

            // Act
            publisher.WarmupPublish(typeof(TestEventA));
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void WarmupPublish_GenericOverload_ThenPublish_CallsListener()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool called = false;
            bus.AddListener<TestEventA>(_ => called = true);

            // Act
            publisher.WarmupPublish<TestEventA>();
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void WarmupPublish_BeforeAddListener_StillWorks()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool called = false;

            // Act
            publisher.WarmupPublish(typeof(TestEventA));
            bus.AddListener<TestEventA>(_ => called = true);
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void WarmupPublish_WithoutListeners_PublishDoesNotThrow()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            publisher.WarmupPublish(typeof(TestEventA));

            // Act & Assert
            Assert.That(
                () => publisher.Publish(new TestEventA("message"), null),
                Throws.Nothing);
        }

        [Test]
        public void WarmupPublish_NullType_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);

            // Act & Assert
            Assert.That(
                () => publisher.WarmupPublish(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void WarmupPublish_UnrelatedType_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);

            // Act & Assert
            Assert.That(
                () => publisher.WarmupPublish(typeof(string)),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void WarmupPublish_CalledTwice_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            publisher.WarmupPublish(typeof(TestEventA));

            // Act & Assert
            Assert.That(
                () => publisher.WarmupPublish(typeof(TestEventA)),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WarmupPublish_AfterPublish_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            publisher.Publish(new TestEventA("message"), null);

            // Act & Assert：类型链已由首次发布缓存，此时不能再预热
            Assert.That(
                () => publisher.WarmupPublish(typeof(TestEventA)),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WarmupPublish_BaseType_DoesNotCoverDerivedType()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool called = false;
            bus.AddListener<TestEventA>(_ => called = true);

            // Act：只预热基类，派生类型在发布时按需构建类型链
            publisher.WarmupPublish(typeof(TestEventA));
            publisher.Publish(new TestEventDerived("message"), null);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void WarmupPublish_InterfaceBase_Works()
        {
            // Arrange
            var bus = new EventBus<IEventInterface>(out var publisher);
            bool called = false;
            bus.AddListener<EventImplementingInterface>(_ => called = true);

            // Act
            publisher.WarmupPublish(typeof(EventImplementingInterface));
            publisher.Publish(new EventImplementingInterface(), null);

            // Assert
            Assert.That(called, Is.True);
        }
    }
}
