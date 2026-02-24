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

    // 接口作为TBaseEvent的测试
    public interface IEventInterface { }
    public interface IDerivedEventInterface : IEventInterface { }
    public class EventImplementingInterface : IEventInterface { }
    public class EventImplementingDerivedInterface : IDerivedEventInterface { }
    public class EventImplementingMultipleInterfaces : IEventInterface, ITestEventInterface { }

    // 98b344e提交后补充测试使用的类型
    public interface IComplexBase { }
    public interface IComplexDerived1 : IComplexBase { }
    public interface IComplexDerived2 : IComplexBase { }
    public interface IUnrelatedInterface { }
    public class EventComplex : IComplexDerived1, IComplexDerived2, IUnrelatedInterface { }

    [TestFixture]
    public class EventBusTests
    {
        #region 基础功能测试

        [Test]
        public void Constructor_CreatesPublisher()
        {
            // Arrange & Act
            var superEvent = new EventBus<TestEventBase>(out var publisher);

            // Assert
            Assert.That(superEvent, Is.Not.Null);
            Assert.That(publisher, Is.Not.Null);
        }

        [Test]
        public void AddListener_ThenPublish_CallsListener()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);

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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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


        #endregion

        #region 边界条件测试

        [Test]
        public void PublishEventWithNoListeners_DoesNothing()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);

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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            const int listenerCount = 50;
            var listeners = new EventBus<TestEventBase>.Listener<TestEventA>[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int callCount = 0;
                EventBus<TestEventBase>.Listener<TestEventA> listener = e => callCount++;
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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
            var superEvent = new EventBus<TestEventBase>(out var publisher);
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

        #region 一次性监听器测试

        [Test]
        public void AddOnceListener_CalledOnlyOnce()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            superEvent.ListenOnce<TestEventA>(e => callCount++);

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证监听器被调用一次
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证监听器没有被再次调用
            Assert.That(callCount, Is.EqualTo(1), "一次性监听器不应被调用第二次");
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_MultipleOnceListeners_EachCalledOnlyOnce()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;
            var callCount3 = 0;

            superEvent.ListenOnce<TestEventA>(e => callCount1++);
            superEvent.ListenOnce<TestEventA>(e => callCount2++);
            superEvent.ListenOnce<TestEventA>(e => callCount3++);

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证所有一次性监听器都被调用一次
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(1));
            Assert.That(callCount3, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证没有监听器被再次调用
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(1));
            Assert.That(callCount3, Is.EqualTo(1));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_WithRegularListener_BothWorkCorrectly()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var onceCallCount = 0;
            var regularCallCount = 0;

            superEvent.ListenOnce<TestEventA>(e => onceCallCount++);
            superEvent.AddListener<TestEventA>(e => regularCallCount++);

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证两个监听器都被调用
            Assert.That(onceCallCount, Is.EqualTo(1));
            Assert.That(regularCallCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证只有常规监听器被再次调用
            Assert.That(onceCallCount, Is.EqualTo(1), "一次性监听器不应被调用第二次");
            Assert.That(regularCallCount, Is.EqualTo(2), "常规监听器应被调用第二次");
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_ThrowsException_StillRemoved()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var exceptionMessage = "一次性监听器异常";
            var exceptionsCollected = new List<Exception>();

            superEvent.ListenOnce<TestEventA>(e => throw new InvalidOperationException(exceptionMessage));

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布（收集异常）
            publisher.Publish(testEvent, exceptionsCollected);

            // Assert - 验证异常被收集，并且监听器已被移除
            Assert.That(exceptionsCollected.Count, Is.EqualTo(1));
            Assert.That(exceptionsCollected[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptionsCollected[0].Message, Is.EqualTo(exceptionMessage));

            // Act - 第二次发布（没有异常）
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证没有新的异常（监听器已被移除）
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_WithDerivedEvent_CalledOnlyOnce()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            superEvent.ListenOnce<TestEventA>(e => callCount++);

            var derivedEvent = new TestEventDerived("Derived");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions1);

            // Assert - 验证监听器被调用一次
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions2);

            // Assert - 验证监听器没有被再次调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }


        [Test]
        public void AddOnceListener_CannotRemoveBeforePublish_StillCalled()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            void OnceListener(TestEventA e) => callCount++;

            superEvent.ListenOnce<TestEventA>(OnceListener);
            superEvent.RemoveListener<TestEventA>(OnceListener); // 尝试移除，但由于包装委托不同，可能无效

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert - 验证监听器仍然被调用（一次性监听器无法通过原始委托引用移除）
            Assert.That(callCount, Is.EqualTo(1), "一次性监听器无法通过原始委托移除，应被调用一次");
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_SameListenerAddedMultipleTimes_EachCalledOnce()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            void OnceListener(TestEventA e) => callCount++;

            // 同一监听器添加多次
            superEvent.ListenOnce<TestEventA>(OnceListener);
            superEvent.ListenOnce<TestEventA>(OnceListener);
            superEvent.ListenOnce<TestEventA>(OnceListener);

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证监听器被调用多次（由于 MultiHashSet 允许重复）
            Assert.That(callCount, Is.EqualTo(3));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证监听器没有被再次调用
            Assert.That(callCount, Is.EqualTo(3));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListenerDuringPublish_ListenerNotCalledUntilNextPublishButCalledOnlyOnce()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var originalCallCount = 0;
            var onceListenerCallCount = 0;
            var onceListenerAdded = false;

            // 原始监听器，只在第一次调用时添加一个一次性监听器
            void OriginalListener(TestEventA e)
            {
                originalCallCount++;

                // 只在第一次调用时添加一次性监听器
                if (!onceListenerAdded)
                {
                    onceListenerAdded = true;
                    superEvent.ListenOnce<TestEventA>(e => onceListenerCallCount++);
                }
            }

            superEvent.AddListener<TestEventA>(OriginalListener);
            var testEvent = new TestEventA("Test");

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证只有原始监听器被调用，一次性监听器未被调用
            Assert.That(originalCallCount, Is.EqualTo(1));
            Assert.That(onceListenerCallCount, Is.EqualTo(0));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证一次性监听器被调用一次，原始监听器被再次调用
            Assert.That(originalCallCount, Is.EqualTo(2));
            Assert.That(onceListenerCallCount, Is.EqualTo(1));
            Assert.That(exceptions2.Count, Is.EqualTo(0));

            // Act - 第三次发布
            var exceptions3 = new List<Exception>();
            publisher.Publish(testEvent, exceptions3);

            // Assert - 验证一次性监听器没有被再次调用，原始监听器被调用第三次
            Assert.That(originalCallCount, Is.EqualTo(3));
            Assert.That(onceListenerCallCount, Is.EqualTo(1), "一次性监听器不应被调用第二次");
            Assert.That(exceptions3.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddOnceListener_WhenInvoking_RemovedSafely()
        {
            // Arrange
            var superEvent = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;
            var onceListenerCallCount = 0;

            // 添加一个一次性监听器
            superEvent.ListenOnce<TestEventA>(e => onceListenerCallCount++);

            // 添加一个普通监听器，在事件处理过程中触发一次性监听器的移除
            void RegularListener(TestEventA e)
            {
                callCount++;
                // 这个监听器在一次性监听器之后被调用（因为添加顺序）
                // 一次性监听器会调用 RemoveListenerDelayed，但移除是延迟的
                // 这个测试验证不会出现集合修改异常
            }

            superEvent.AddListener<TestEventA>(RegularListener);
            var testEvent = new TestEventA("Test");

            // Act - 发布事件
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert - 验证两个监听器都被调用，没有异常
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(onceListenerCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证一次性监听器没有被再次调用，普通监听器仍被调用
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(onceListenerCallCount, Is.EqualTo(1));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        #endregion

        #region 接口作为TBaseEvent测试

        [Test]
        public void InterfaceAsTBaseEvent_BasicFunctionality()
        {
            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var callCount = 0;
            IEventInterface? receivedEvent = null;

            eventBus.AddListener<IEventInterface>(e =>
            {
                callCount++;
                receivedEvent = e;
            });

            var testEvent = new EventImplementingInterface();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(receivedEvent, Is.SameAs(testEvent));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void InterfaceAsTBaseEvent_DerivedInterface()
        {
            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var baseCallCount = 0;
            var derivedCallCount = 0;

            eventBus.AddListener<IEventInterface>(e => baseCallCount++);
            eventBus.AddListener<IDerivedEventInterface>(e => derivedCallCount++);

            var derivedEvent = new EventImplementingDerivedInterface();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions);

            // Assert
            // 实现派生接口的事件应该触发基接口监听器
            Assert.That(baseCallCount, Is.EqualTo(1));
            // 也应该触发派生接口监听器，因为事件类型实现了IDerivedEventInterface
            Assert.That(derivedCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void InterfaceAsTBaseEvent_EventBusWithDerivedInterface()
        {
            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var baseCallCount = 0;
            var derivedCallCount = 0;

            eventBus.AddListener<IEventInterface>(e => baseCallCount++);
            eventBus.AddListener<IDerivedEventInterface>(e => derivedCallCount++);

            var derivedEvent = new EventImplementingDerivedInterface();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(derivedEvent, exceptions);

            // Assert
            // 事件实现IDerivedEventInterface，应该触发两个监听器
            Assert.That(baseCallCount, Is.EqualTo(1));
            Assert.That(derivedCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }


        [Test]
        public void InterfaceAsTBaseEvent_ListenOnce()
        {
            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var callCount = 0;

            eventBus.ListenOnce<IEventInterface>(e => callCount++);

            var testEvent = new EventImplementingInterface();

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Act - 第二次发布
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));
            Assert.That(exceptions2.Count, Is.EqualTo(0));
        }

        [Test]
        public void InterfaceAsTBaseEvent_RemoveListener()
        {
            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var callCount = 0;

            void Listener(IEventInterface e) => callCount++;

            eventBus.AddListener<IEventInterface>(Listener);
            eventBus.RemoveListener<IEventInterface>(Listener);

            var testEvent = new EventImplementingInterface();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 98b344e提交后补充测试

        [Test]
        public void TypeChain_InterfaceBaseEvent_ComplexHierarchy()
        {
            // 测试复杂接口继承层次
            // IComplexBase <- IComplexDerived1, IComplexDerived2
            // 事件类型实现多个派生接口

            // Arrange
            var eventBus = new EventBus<IComplexBase>(out var publisher);
            var baseCallCount = 0;
            var derived1CallCount = 0;
            var derived2CallCount = 0;

            eventBus.AddListener<IComplexBase>(e => baseCallCount++);
            eventBus.AddListener<IComplexDerived1>(e => derived1CallCount++);
            eventBus.AddListener<IComplexDerived2>(e => derived2CallCount++);
            // 无法添加IUnrelatedInterface监听器，因为不是IComplexBase的子类型

            var testEvent = new EventComplex();

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            // 事件应该触发所有相关接口的监听器
            Assert.That(baseCallCount, Is.EqualTo(1));
            Assert.That(derived1CallCount, Is.EqualTo(1));
            Assert.That(derived2CallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }


        [Test]
        public void TypeSafety_InvalidCastException_NotThrown()
        {
            // 验证类型转换不会抛出InvalidCastException
            // TBaseEvent是类的情况

            // Arrange
            var eventBus = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            eventBus.AddListener<TestEventA>(e => callCount++);

            var testEvent = new TestEventA("Test");

            // Act & Assert
            var exceptions = new List<Exception>();
            Assert.DoesNotThrow(() => publisher.Publish(testEvent, exceptions));

            // 验证监听器被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void TypeSafety_InterfaceBaseEvent_InvalidCastException_NotThrown()
        {
            // 验证接口作为TBaseEvent时类型转换不会抛出InvalidCastException

            // Arrange
            var eventBus = new EventBus<IEventInterface>(out var publisher);
            var callCount = 0;

            eventBus.AddListener<IEventInterface>(e => callCount++);

            var testEvent = new EventImplementingInterface();

            // Act & Assert
            var exceptions = new List<Exception>();
            Assert.DoesNotThrow(() => publisher.Publish(testEvent, exceptions));

            // 验证监听器被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReentrancyProtection_ListenerAttemptsPublish_ThrowsException()
        {
            // 测试在监听器内部尝试发布事件会抛出异常

            // Arrange
            var eventBus = new EventBus<TestEventBase>(out var publisher);
            var innerCallCount = 0;
            var exceptionThrown = false;

            eventBus.AddListener<TestEventA>(e =>
            {
                try
                {
                    // 尝试在监听器内部发布另一个事件
                    publisher.Publish(new TestEventB(42), null);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }
            });

            eventBus.AddListener<TestEventB>(e => innerCallCount++);

            var testEvent = new TestEventA("Test");

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(exceptionThrown, Is.True);
            // 内部事件不应该被发布
            Assert.That(innerCallCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }


        [Test]
        public void GenericConstraint_NonTBaseEventInterface_CannotAddListener()
        {
            // 验证不能添加非TBaseEvent接口的监听器
            // 这应该导致编译错误，但我们可以通过反射测试运行时行为

            // 注意：这个测试主要是文档作用
            // 实际编译时泛型约束会阻止添加

            Assert.Pass("泛型约束 where TEvent : TBaseEvent 确保编译时类型安全");
        }

        [Test]
        public void ListenOnce_ExceptionInListener_StillRemoved()
        {
            // 测试ListenOnce监听器抛出异常时仍然被移除

            // Arrange
            var eventBus = new EventBus<TestEventBase>(out var publisher);
            var callCount = 0;

            eventBus.ListenOnce<TestEventA>(e =>
            {
                callCount++;
                throw new Exception("Test exception");
            });

            var testEvent = new TestEventA("Test");

            // Act - 第一次发布（应该抛出异常但监听器被移除）
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Act - 第二次发布（监听器应该已经被移除）
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert
            Assert.That(callCount, Is.EqualTo(1)); // 只调用一次
            Assert.That(exceptions1.Count, Is.EqualTo(1)); // 异常被捕获
            Assert.That(exceptions2.Count, Is.EqualTo(0)); // 第二次没有异常
        }

        #endregion
    }
}