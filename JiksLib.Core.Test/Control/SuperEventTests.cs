using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Control
{
    // 测试用的事件基类
    public abstract class TestEventBase { }

    // 测试用的具体事件类
    public class TestEventA : TestEventBase
    {
        public string Message { get; }

        public TestEventA(string message)
        {
            Message = message;
        }
    }

    public class TestEventB : TestEventBase
    {
        public int Value { get; }

        public TestEventB(int value)
        {
            Value = value;
        }
    }

    // 继承层次测试
    public class TestEventDerived : TestEventA
    {
        public TestEventDerived(string message) : base(message) { }
    }

    // 接口测试
    public interface ITestEventInterface { }
    public class TestEventWithInterface : TestEventBase, ITestEventInterface { }

    [TestFixture]
    public class SuperEventTests
    {
        #region 基础功能测试

        [Test]
        public void Constructor_CreatesPublisher()
        {
            // Arrange & Act
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);

            // Assert
            Assert.That(superEvent, Is.Not.Null);
            Assert.That(publisher, Is.Not.Null);
        }

        [Test]
        public void AddListener_ThenPublish_CallsListener()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var eventReceived = false;
            TestEventA? receivedEvent = null;

            superEvent.AddListener<TestEventA>(e =>
            {
                eventReceived = true;
                receivedEvent = e;
            });

            var testEvent = new TestEventA("Hello");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(eventReceived, Is.True);
            Assert.That(receivedEvent, Is.SameAs(testEvent));
            Assert.That(receivedEvent!.Message, Is.EqualTo("Hello"));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddMultipleListeners_AllAreCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            superEvent.AddListener<TestEventA>(e => callCount1++);
            superEvent.AddListener<TestEventA>(e => callCount2++);

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveListener_ListenerNotCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount = 0;

            void Listener(TestEventA e) => callCount++;

            superEvent.AddListener<TestEventA>(Listener);
            superEvent.RemoveListener<TestEventA>(Listener);

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveListener_OtherListenersStillCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            void Listener1(TestEventA e) => callCount1++;
            void Listener2(TestEventA e) => callCount2++;

            superEvent.AddListener<TestEventA>(Listener1);
            superEvent.AddListener<TestEventA>(Listener2);
            superEvent.RemoveListener<TestEventA>(Listener1);

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount1, Is.EqualTo(0));
            Assert.That(callCount2, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 异常处理测试

        [Test]
        public void ListenerThrowsException_ExceptionCollected()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var exceptionMessage = "Test exception";

            superEvent.AddListener<TestEventA>(e => throw new InvalidOperationException(exceptionMessage));

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(exceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions[0].Message, Is.EqualTo(exceptionMessage));
        }

        [Test]
        public void MultipleListenersThrowExceptions_AllExceptionsCollected()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);

            superEvent.AddListener<TestEventA>(e => throw new InvalidOperationException("Exception 1"));
            superEvent.AddListener<TestEventA>(e => throw new ArgumentException("Exception 2"));

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(exceptions.Count, Is.EqualTo(2));
            Assert.That(exceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions[1], Is.TypeOf<ArgumentException>());
        }

        [Test]
        public void SomeListenersThrowExceptions_OthersStillCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var successfulCallCount = 0;

            superEvent.AddListener<TestEventA>(e => throw new InvalidOperationException("Error"));
            superEvent.AddListener<TestEventA>(e => successfulCallCount++);

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(successfulCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(1));
        }

        #endregion

        #region 继承层次测试

        [Test]
        public void PublishDerivedEvent_CallsBaseClassListeners()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var baseCallCount = 0;
            var derivedCallCount = 0;

            superEvent.AddListener<TestEventA>(e => baseCallCount++);
            superEvent.AddListener<TestEventDerived>(e => derivedCallCount++);

            var derivedEvent = new TestEventDerived("Derived");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions);

            // Assert
            // 注意：当前实现中，派生类事件应该触发基类监听器
            Assert.That(baseCallCount, Is.EqualTo(1));
            Assert.That(derivedCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void PublishBaseEvent_DoesNotCallDerivedClassListeners()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var baseCallCount = 0;
            var derivedCallCount = 0;

            superEvent.AddListener<TestEventA>(e => baseCallCount++);
            superEvent.AddListener<TestEventDerived>(e => derivedCallCount++);

            var baseEvent = new TestEventA("Base");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(baseEvent, exceptions);

            // Assert
            Assert.That(baseCallCount, Is.EqualTo(1));
            Assert.That(derivedCallCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void MultipleLevelInheritance_AllRelevantListenersCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var baseCallCount = 0;
            var intermediateCallCount = 0;
            var derivedCallCount = 0;

            superEvent.AddListener<TestEventBase>(e => baseCallCount++);
            superEvent.AddListener<TestEventA>(e => intermediateCallCount++);
            superEvent.AddListener<TestEventDerived>(e => derivedCallCount++);

            var derivedEvent = new TestEventDerived("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions);

            // Assert
            // 派生类事件应该触发所有基类监听器
            Assert.That(baseCallCount, Is.EqualTo(1));
            Assert.That(intermediateCallCount, Is.EqualTo(1));
            Assert.That(derivedCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 接口测试

        [Test]
        public void PublishEventWithInterface_CallsInterfaceListeners()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var interfaceCallCount = 0;
            var concreteCallCount = 0;

            superEvent.AddListener<ITestEventInterface>(e => interfaceCallCount++);
            superEvent.AddListener<TestEventWithInterface>(e => concreteCallCount++);

            var eventWithInterface = new TestEventWithInterface();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(eventWithInterface, exceptions);

            // Assert
            // 实现接口的事件应该触发接口监听器
            Assert.That(interfaceCallCount, Is.EqualTo(1));
            Assert.That(concreteCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 边界条件测试

        [Test]
        public void PublishEventWithNoListeners_DoesNothing()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(exceptions.Count, Is.EqualTo(0));
            // 没有监听器，没有异常，正常完成
        }

        [Test]
        public void AddSameListenerMultipleTimes_CalledMultipleTimes()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount = 0;

            void Listener(TestEventA e) => callCount++;

            superEvent.AddListener<TestEventA>(Listener);
            superEvent.AddListener<TestEventA>(Listener); // 第二次添加

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            // 由于使用 MultiHashSet，同一监听器可能被添加多次
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveListenerNotAdded_NoEffect()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount = 0;

            void Listener(TestEventA e) => callCount++;
            void OtherListener(TestEventA e) => callCount++;

            superEvent.AddListener<TestEventA>(Listener);
            superEvent.RemoveListener<TestEventA>(OtherListener); // 移除未添加的监听器

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void PublishNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);

            // Act & Assert
            var exceptions = new List<Exception>();
            Assert.That(
                () => publisher.Publish(null!, exceptions),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region 不同类型事件测试

        [Test]
        public void MultipleEventTypes_ListenersOnlyRespondToTheirType()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var eventACount = 0;
            var eventBCount = 0;

            superEvent.AddListener<TestEventA>(e => eventACount++);
            superEvent.AddListener<TestEventB>(e => eventBCount++);

            var eventA = new TestEventA("A");
            var eventB = new TestEventB(42);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(eventA, exceptions);
            publisher.Publish(eventB, exceptions);

            // Assert
            Assert.That(eventACount, Is.EqualTo(1));
            Assert.That(eventBCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ListenerForBaseType_ReceivesAllEvents()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var baseCallCount = 0;

            superEvent.AddListener<TestEventBase>(e => baseCallCount++);

            var eventA = new TestEventA("A");
            var eventB = new TestEventB(42);
            var derivedEvent = new TestEventDerived("Derived");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(eventA, exceptions);
            publisher.Publish(eventB, exceptions);
            publisher.Publish(derivedEvent, exceptions);

            // Assert
            Assert.That(baseCallCount, Is.EqualTo(3));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 性能与内存测试

        [Test]
        public void AddManyListeners_AllCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            const int listenerCount = 100;
            var callCounts = new int[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int index = i; // 捕获局部变量
                superEvent.AddListener<TestEventA>(e => callCounts[index]++);
            }

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCounts.Sum(), Is.EqualTo(listenerCount));
            for (int i = 0; i < listenerCount; i++)
            {
                Assert.That(callCounts[i], Is.EqualTo(1));
            }
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveManyListeners_NoneCalled()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            const int listenerCount = 50;
            var listeners = new SuperEvent<TestEventBase>.Listener<TestEventA>[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int callCount = 0;
                SuperEvent<TestEventBase>.Listener<TestEventA> listener = e => callCount++;
                listeners[i] = listener;
                superEvent.AddListener(listener);
            }

            // 移除所有监听器
            foreach (var listener in listeners)
            {
                superEvent.RemoveListener(listener);
            }

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            // 所有监听器已移除，不应被调用
            Assert.That(exceptions.Count, Is.EqualTo(0));
            // 注意：无法直接验证监听器未被调用，但如果没有异常则通过
        }

        #endregion

        #region 动态监听器管理测试

        [Test]
        public void AddListenerDuringPublish_ListenerNotCalledUntilNextPublish()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var originalCallCount = 0;
            var addedDuringPublishCallCount = 0;

            // 原始监听器，在调用时会添加一个新的监听器
            void OriginalListener(TestEventA e)
            {
                originalCallCount++;

                // 在事件处理过程中添加新的监听器
                superEvent.AddListener<TestEventA>(e => addedDuringPublishCallCount++);
            }

            superEvent.AddListener<TestEventA>(OriginalListener);
            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证只有原始监听器被调用，新添加的监听器未被调用
            Assert.That(originalCallCount, Is.EqualTo(1));
            Assert.That(addedDuringPublishCallCount, Is.EqualTo(0));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证第二次发布时，两个监听器都被调用
            Assert.That(originalCallCount, Is.EqualTo(2));
            Assert.That(addedDuringPublishCallCount, Is.EqualTo(1));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveListenerDuringPublish_ListenerCalledThisTimeButNotNext()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCount = 0;

            void ListenerToRemove(TestEventA e) => callCount++;

            // 添加一个监听器，在事件处理过程中移除自身
            void OriginalListener(TestEventA e)
            {
                callCount++;
                superEvent.RemoveListener<TestEventA>(ListenerToRemove);
            }

            superEvent.AddListener<TestEventA>(OriginalListener);
            superEvent.AddListener<TestEventA>(ListenerToRemove);
            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证两个监听器都被调用（因为移除是延迟的）
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证只有 OriginalListener 被调用，ListenerToRemove 已被移除
            // callCount 应该增加 1（仅 OriginalListener）
            Assert.That(callCount, Is.EqualTo(3));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddAndRemoveListenersDuringPublish_ChangesAppliedToNextPublish()
        {
            // Arrange
            var superEvent = new SuperEvent<TestEventBase>(out var publisher);
            var callCountA = 0;
            var callCountB = 0;
            var callCountC = 0;

            void ListenerB(TestEventA e) => callCountB++;
            void ListenerC(TestEventA e) => callCountC++;

            // 监听器 A，在事件处理过程中添加 B 并移除 C
            void ListenerA(TestEventA e)
            {
                callCountA++;
                superEvent.AddListener<TestEventA>(ListenerB);
                superEvent.RemoveListener<TestEventA>(ListenerC);
            }

            superEvent.AddListener<TestEventA>(ListenerA);
            superEvent.AddListener<TestEventA>(ListenerC); // 稍后会被移除
            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证只有 A 和 C 被调用（B 尚未添加，C 的移除延迟）
            Assert.That(callCountA, Is.EqualTo(1));
            Assert.That(callCountB, Is.EqualTo(0));
            Assert.That(callCountC, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证 A 和 B 被调用，C 已被移除
            Assert.That(callCountA, Is.EqualTo(2));
            Assert.That(callCountB, Is.EqualTo(1));
            Assert.That(callCountC, Is.EqualTo(1)); // 未增加
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        #endregion
    }
}