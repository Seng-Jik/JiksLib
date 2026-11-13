using NUnit.Framework;
using JiksLib.Control;
using System;

namespace JiksLib.Test.Control
{
    /// <summary>
    /// 监听器容量预热的测试
    /// 覆盖 SafeEvent 的容量构造函数，以及 EventBus / ValueEventBus 的 WarmupListenerSlot
    /// </summary>
    [TestFixture]
    public class ListenerCapacityTests
    {
        #region SafeEvent 容量构造

        [Test]
        public void SafeEvent_WithCapacity_AddAndPublish_CallsAllListeners()
        {
            // Arrange
            const int capacity = 8;
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher, capacity);
            int callCount = 0;

            // Act
            for (int i = 0; i < capacity; i++)
            {
                safeEvent.AddListener(_ => callCount++);
            }

            publisher.Publish(new TestSafeEvent("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(capacity));
        }

        [Test]
        public void SafeEvent_WithCapacity_AddingMoreThanCapacity_StillWorks()
        {
            // Arrange
            const int capacity = 4;
            const int listenerCount = 6;
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher, capacity);
            int callCount = 0;

            // Act
            for (int i = 0; i < listenerCount; i++)
            {
                safeEvent.AddListener(_ => callCount++);
            }

            publisher.Publish(new TestSafeEvent("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(listenerCount));
        }

        [Test]
        public void SafeEvent_WithZeroCapacity_AddAndPublish_Works()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher, 0);
            int callCount = 0;

            // Act
            safeEvent.AddListener(_ => callCount++);
            publisher.Publish(new TestSafeEvent("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void SafeEvent_WithNegativeCapacity_Throws()
        {
            Assert.That(
                () => new SafeEvent<TestSafeEvent>(out _, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

        #region EventBus 预热

        [Test]
        public void EventBus_WarmupListenerSlot_ThenAddAndPublish_CallsListener()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestEventA>(4);
            bus.AddListener<TestEventA>(_ => callCount++);
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void EventBus_WarmupListenerSlot_RegistersUpToCapacityListeners()
        {
            // Arrange
            const int capacity = 64;
            var bus = new EventBus<TestEventBase>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestEventA>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                bus.AddListener<TestEventA>(_ => callCount++);
            }

            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(capacity));
        }

        [Test]
        public void EventBus_WarmupListenerSlot_AddingMoreThanCapacity_StillWorks()
        {
            // Arrange
            const int capacity = 4;
            const int listenerCount = 6;
            var bus = new EventBus<TestEventBase>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestEventA>(capacity);
            for (int i = 0; i < listenerCount; i++)
            {
                bus.AddListener<TestEventA>(_ => callCount++);
            }

            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(listenerCount));
        }

        [Test]
        public void EventBus_WarmupListenerSlot_WithZeroCapacity_StillWorks()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestEventA>(0);
            bus.AddListener<TestEventA>(_ => callCount++);
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void EventBus_WarmupListenerSlot_ThenRemoveListener_Works()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            int callCount = 0;
            JiksLib.Control.EventHandler<TestEventA> listener = _ => callCount++;

            // Act
            bus.WarmupListenerSlot<TestEventA>(4);
            bus.AddListener(listener);
            bus.RemoveListener(listener);
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
        }

        [Test]
        public void EventBus_WarmupListenerSlot_AfterListenerAdded_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out _);
            bus.AddListener<TestEventA>(_ => { });

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestEventA>(4),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void EventBus_WarmupListenerSlot_CalledTwice_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out _);
            bus.WarmupListenerSlot<TestEventA>(4);

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestEventA>(8),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void EventBus_WarmupListenerSlot_WithNegativeCapacity_Throws()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out _);

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestEventA>(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EventBus_WarmupListenerSlot_DoesNotAffectOtherEventTypes()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool testEventBCalled = false;

            // Act
            bus.WarmupListenerSlot<TestEventA>(4);
            bus.AddListener<TestEventB>(_ => testEventBCalled = true);
            publisher.Publish(new TestEventB(1), null);

            // Assert
            Assert.That(testEventBCalled, Is.True);
            Assert.That(
                () => publisher.Publish(new TestEventA("message"), null),
                Throws.Nothing);
        }

        [Test]
        public void EventBus_WarmupListenerSlot_BaseType_ReceivesDerivedEvent()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bool called = false;

            // Act
            bus.WarmupListenerSlot<TestEventBase>(4);
            bus.AddListener<TestEventBase>(_ => called = true);
            publisher.Publish(new TestEventA("message"), null);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void EventBus_WarmupListenerSlot_WithoutListeners_PublishDoesNotThrow()
        {
            // Arrange
            var bus = new EventBus<TestEventBase>(out var publisher);
            bus.WarmupListenerSlot<TestEventA>(4);

            // Act & Assert
            Assert.That(
                () => publisher.Publish(new TestEventA("message"), null),
                Throws.Nothing);
        }

        #endregion

        #region ValueEventBus 预热

        [Test]
        public void ValueEventBus_WarmupListenerSlot_ThenAddAndPublish_CallsListener()
        {
            // Arrange
            var bus = new ValueEventBus<IValueEvent>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestValueEventA>(4);
            bus.AddListener<TestValueEventA>(_ => callCount++);
            publisher.Publish(new TestValueEventA("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_RegistersUpToCapacityListeners()
        {
            // Arrange
            const int capacity = 64;
            var bus = new ValueEventBus<IValueEvent>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestValueEventA>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                bus.AddListener<TestValueEventA>(_ => callCount++);
            }

            publisher.Publish(new TestValueEventA("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(capacity));
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_AddingMoreThanCapacity_StillWorks()
        {
            // Arrange
            const int capacity = 4;
            const int listenerCount = 6;
            var bus = new ValueEventBus<IValueEvent>(out var publisher);
            int callCount = 0;

            // Act
            bus.WarmupListenerSlot<TestValueEventA>(capacity);
            for (int i = 0; i < listenerCount; i++)
            {
                bus.AddListener<TestValueEventA>(_ => callCount++);
            }

            publisher.Publish(new TestValueEventA("message", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(listenerCount));
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_AfterListenerAdded_Throws()
        {
            // Arrange
            var bus = new ValueEventBus<IValueEvent>(out _);
            bus.AddListener<TestValueEventA>(_ => { });

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestValueEventA>(4),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_CalledTwice_Throws()
        {
            // Arrange
            var bus = new ValueEventBus<IValueEvent>(out _);
            bus.WarmupListenerSlot<TestValueEventA>(4);

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestValueEventA>(8),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_WithNegativeCapacity_Throws()
        {
            // Arrange
            var bus = new ValueEventBus<IValueEvent>(out _);

            // Act & Assert
            Assert.That(
                () => bus.WarmupListenerSlot<TestValueEventA>(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ValueEventBus_WarmupListenerSlot_DoesNotAffectOtherEventTypes()
        {
            // Arrange
            var bus = new ValueEventBus<IValueEvent>(out var publisher);
            bool testValueEventBCalled = false;

            // Act
            bus.WarmupListenerSlot<TestValueEventA>(4);
            bus.AddListener<TestValueEventB>(_ => testValueEventBCalled = true);
            publisher.Publish(new TestValueEventB(1f, 2f), null);

            // Assert
            Assert.That(testValueEventBCalled, Is.True);
            Assert.That(
                () => publisher.Publish(new TestValueEventA("message", 1), null),
                Throws.Nothing);
        }

        #endregion
    }
}
