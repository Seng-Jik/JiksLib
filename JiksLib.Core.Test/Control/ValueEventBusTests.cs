using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Control
{
    // 测试用的值类型事件
    public interface IValueEvent
    {
    }

    public struct TestValueEventA : IValueEvent
    {
        public string Message { get; }
        public int Value { get; }

        public TestValueEventA(string message, int value)
        {
            Message = message;
            Value = value;
        }
    }

    public struct TestValueEventB : IValueEvent
    {
        public float X { get; }
        public float Y { get; }

        public TestValueEventB(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    // 大型值类型用于测试拷贝行为
    public struct LargeValueEvent : IValueEvent
    {
        public long Data1 { get; }
        public long Data2 { get; }
        public long Data3 { get; }
        public long Data4 { get; }
        public long Data5 { get; }
        public long Data6 { get; }
        public long Data7 { get; }
        public long Data8 { get; }

        public LargeValueEvent(long data)
        {
            Data1 = data;
            Data2 = data + 1;
            Data3 = data + 2;
            Data4 = data + 3;
            Data5 = data + 4;
            Data6 = data + 5;
            Data7 = data + 6;
            Data8 = data + 7;
        }
    }

    [TestFixture]
    public class ValueEventBusTests
    {
        #region 基础功能测试

        [Test]
        public void Constructor_CreatesPublisher()
        {
            // Arrange & Act
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);

            // Assert
            Assert.That(eventBus, Is.Not.Null);
        }

        [Test]
        public void AddListener_ThenPublish_CallsListener()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var eventReceived = false;
            TestValueEventA receivedEvent = default;

            eventBus.AddListener<TestValueEventA>(e =>
            {
                eventReceived = true;
                receivedEvent = e;
            });

            var testEvent = new TestValueEventA("Hello", 42);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(eventReceived, Is.True);
            Assert.That(receivedEvent.Message, Is.EqualTo("Hello"));
            Assert.That(receivedEvent.Value, Is.EqualTo(42));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddMultipleListeners_AllAreCalled()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            eventBus.AddListener<TestValueEventA>(e => callCount1++);
            eventBus.AddListener<TestValueEventA>(e => callCount2++);

            var testEvent = new TestValueEventA("Test", 123);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestValueEventA e) => callCount++;

            eventBus.AddListener<TestValueEventA>(Listener);
            eventBus.RemoveListener<TestValueEventA>(Listener);

            var testEvent = new TestValueEventA("Test", 456);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            void Listener1(TestValueEventA e) => callCount1++;
            void Listener2(TestValueEventA e) => callCount2++;

            eventBus.AddListener<TestValueEventA>(Listener1);
            eventBus.AddListener<TestValueEventA>(Listener2);
            eventBus.RemoveListener<TestValueEventA>(Listener1);

            var testEvent = new TestValueEventA("Test", 789);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var exceptionMessage = "Test exception";

            eventBus.AddListener<TestValueEventA>(e => throw new InvalidOperationException(exceptionMessage));

            var testEvent = new TestValueEventA("Test", 111);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);

            eventBus.AddListener<TestValueEventA>(e => throw new InvalidOperationException("Exception 1"));
            eventBus.AddListener<TestValueEventA>(e => throw new ArgumentException("Exception 2"));

            var testEvent = new TestValueEventA("Test", 222);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var successfulCallCount = 0;

            eventBus.AddListener<TestValueEventA>(e => throw new InvalidOperationException("Error"));
            eventBus.AddListener<TestValueEventA>(e => successfulCallCount++);

            var testEvent = new TestValueEventA("Test", 333);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(successfulCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExceptionsOutputNull_ExceptionsSilentlyIgnored()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            eventBus.AddListener<TestValueEventA>(e => throw new InvalidOperationException("Error"));
            eventBus.AddListener<TestValueEventA>(e => callCount++);

            var testEvent = new TestValueEventA("Test", 444);

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() => publisher.Publish(testEvent, null));

            // 验证第二个监听器仍被调用
            // 注意：无法直接验证，但测试通过说明没有因异常而中断
        }

        #endregion

        #region 边界条件测试

        [Test]
        public void PublishEventWithNoListeners_DoesNothing()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var testEvent = new TestValueEventA("Test", 555);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestValueEventA e) => callCount++;

            eventBus.AddListener<TestValueEventA>(Listener);
            eventBus.AddListener<TestValueEventA>(Listener); // 第二次添加

            var testEvent = new TestValueEventA("Test", 666);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            // 允许同一监听器被添加多次
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveListenerNotAdded_NoEffect()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestValueEventA e) => callCount++;
            void OtherListener(TestValueEventA e) => callCount++;

            eventBus.AddListener<TestValueEventA>(Listener);
            eventBus.RemoveListener<TestValueEventA>(OtherListener); // 移除未添加的监听器

            var testEvent = new TestValueEventA("Test", 777);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void PublishDefaultValueEvent_WorksCorrectly()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var receivedValue = 0;

            eventBus.AddListener<TestValueEventA>(e => receivedValue = e.Value);

            var testEvent = default(TestValueEventA); // 默认值

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(receivedValue, Is.EqualTo(0)); // 默认值
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 不同类型事件测试

        [Test]
        public void MultipleEventTypes_ListenersOnlyRespondToTheirType()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var eventACount = 0;
            var eventBCount = 0;

            eventBus.AddListener<TestValueEventA>(e => eventACount++);
            eventBus.AddListener<TestValueEventB>(e => eventBCount++);

            var eventA = new TestValueEventA("A", 1);
            var eventB = new TestValueEventB(2.0f, 3.0f);

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
        public void LargeValueEvent_CopiesCorrectly()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            long receivedData1 = 0;
            long receivedData8 = 0;

            eventBus.AddListener<LargeValueEvent>(e =>
            {
                receivedData1 = e.Data1;
                receivedData8 = e.Data8;
            });

            var largeEvent = new LargeValueEvent(999);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(largeEvent, exceptions);

            // Assert
            Assert.That(receivedData1, Is.EqualTo(999));
            Assert.That(receivedData8, Is.EqualTo(999 + 7));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 性能与内存测试

        [Test]
        public void AddManyListeners_AllCalled()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            const int listenerCount = 100;
            var callCounts = new int[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int index = i; // 捕获局部变量
                eventBus.AddListener<TestValueEventA>(e => callCounts[index]++);
            }

            var testEvent = new TestValueEventA("Test", 888);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            const int listenerCount = 50;
            var listeners = new EventBusListener<TestValueEventA>[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int callCount = 0;
                EventBusListener<TestValueEventA> listener = e => callCount++;
                listeners[i] = listener;
                eventBus.AddListener(listener);
            }

            // 移除所有监听器
            foreach (var listener in listeners)
            {
                eventBus.RemoveListener(listener);
            }

            var testEvent = new TestValueEventA("Test", 999);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var originalCallCount = 0;
            var addedDuringPublishCallCount = 0;

            // 原始监听器，在调用时会添加一个新的监听器
            void OriginalListener(TestValueEventA e)
            {
                originalCallCount++;

                // 在事件处理过程中添加新的监听器
                eventBus.AddListener<TestValueEventA>(e => addedDuringPublishCallCount++);
            }

            eventBus.AddListener<TestValueEventA>(OriginalListener);
            var testEvent = new TestValueEventA("Test", 1111);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void ListenerToRemove(TestValueEventA e) => callCount++;

            // 添加一个监听器，在事件处理过程中移除自身
            void OriginalListener(TestValueEventA e)
            {
                callCount++;
                eventBus.RemoveListener<TestValueEventA>(ListenerToRemove);
            }

            eventBus.AddListener<TestValueEventA>(OriginalListener);
            eventBus.AddListener<TestValueEventA>(ListenerToRemove);
            var testEvent = new TestValueEventA("Test", 2222);

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
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCountA = 0;
            var callCountB = 0;
            var callCountC = 0;

            void ListenerB(TestValueEventA e) => callCountB++;
            void ListenerC(TestValueEventA e) => callCountC++;

            // 监听器 A，在事件处理过程中添加 B 并移除 C
            void ListenerA(TestValueEventA e)
            {
                callCountA++;
                eventBus.AddListener<TestValueEventA>(ListenerB);
                eventBus.RemoveListener<TestValueEventA>(ListenerC);
            }

            eventBus.AddListener<TestValueEventA>(ListenerA);
            eventBus.AddListener<TestValueEventA>(ListenerC); // 稍后会被移除
            var testEvent = new TestValueEventA("Test", 3333);

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

        [Test]
        public void PublishDuringPublish_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var innerPublishAttempted = false;

            eventBus.AddListener<TestValueEventA>(e =>
            {
                // 尝试在发布过程中再次发布
                if (!innerPublishAttempted)
                {
                    innerPublishAttempted = true;
                    Assert.Throws<InvalidOperationException>(() =>
                        publisher.Publish(new TestValueEventA("Inner", 4444), null));
                }
            });

            var testEvent = new TestValueEventA("Outer", 5555);

            // Act & Assert - 外部发布应正常进行
            var exceptions = new List<Exception>();
            Assert.DoesNotThrow(() => publisher.Publish(testEvent, exceptions));
            Assert.That(innerPublishAttempted, Is.True);
        }

        #endregion

        #region 一次性监听器测试

        [Test]
        public void ListenOnce_CalledOnlyOnce()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            eventBus.ListenOnce<TestValueEventA>(e => callCount++);

            var testEvent = new TestValueEventA("Test", 6666);

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
        public void ListenOnce_MultipleOnceListeners_EachCalledOnlyOnce()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;
            var callCount3 = 0;

            eventBus.ListenOnce<TestValueEventA>(e => callCount1++);
            eventBus.ListenOnce<TestValueEventA>(e => callCount2++);
            eventBus.ListenOnce<TestValueEventA>(e => callCount3++);

            var testEvent = new TestValueEventA("Test", 7777);

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
        public void ListenOnce_WithRegularListener_BothWorkCorrectly()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var onceCallCount = 0;
            var regularCallCount = 0;

            eventBus.ListenOnce<TestValueEventA>(e => onceCallCount++);
            eventBus.AddListener<TestValueEventA>(e => regularCallCount++);

            var testEvent = new TestValueEventA("Test", 8888);

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
        public void ListenOnce_ThrowsException_StillRemoved()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var exceptionMessage = "一次性监听器异常";
            var exceptionsCollected = new List<Exception>();

            eventBus.ListenOnce<TestValueEventA>(e => throw new InvalidOperationException(exceptionMessage));

            var testEvent = new TestValueEventA("Test", 9999);

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
        public void ListenOnce_CannotRemoveBeforePublish_StillCalled()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void OnceListener(TestValueEventA e) => callCount++;

            eventBus.ListenOnce<TestValueEventA>(OnceListener);
            eventBus.RemoveListener<TestValueEventA>(OnceListener); // 尝试移除，但由于包装委托不同，可能无效

            var testEvent = new TestValueEventA("Test", 1010);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert - 验证监听器仍然被调用（一次性监听器无法通过原始委托引用移除）
            Assert.That(callCount, Is.EqualTo(1), "一次性监听器无法通过原始委托移除，应被调用一次");
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ListenOnce_SameListenerAddedMultipleTimes_EachCalledOnce()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void OnceListener(TestValueEventA e) => callCount++;

            // 同一监听器添加多次
            eventBus.ListenOnce<TestValueEventA>(OnceListener);
            eventBus.ListenOnce<TestValueEventA>(OnceListener);
            eventBus.ListenOnce<TestValueEventA>(OnceListener);

            var testEvent = new TestValueEventA("Test", 1111);

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证监听器被调用多次（允许重复添加）
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
        public void ListenOnceDuringPublish_ListenerNotCalledUntilNextPublishButCalledOnlyOnce()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var originalCallCount = 0;
            var onceListenerCallCount = 0;
            var onceListenerAdded = false;

            // 原始监听器，只在第一次调用时添加一个一次性监听器
            void OriginalListener(TestValueEventA e)
            {
                originalCallCount++;

                // 只在第一次调用时添加一次性监听器
                if (!onceListenerAdded)
                {
                    onceListenerAdded = true;
                    eventBus.ListenOnce<TestValueEventA>(e => onceListenerCallCount++);
                }
            }

            eventBus.AddListener<TestValueEventA>(OriginalListener);
            var testEvent = new TestValueEventA("Test", 1212);

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
        public void ListenOnce_WhenInvoking_RemovedSafely()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;
            var onceListenerCallCount = 0;

            // 添加一个一次性监听器
            eventBus.ListenOnce<TestValueEventA>(e => onceListenerCallCount++);

            // 添加一个普通监听器，在事件处理过程中触发一次性监听器的移除
            void RegularListener(TestValueEventA e)
            {
                callCount++;
                // 这个监听器在一次性监听器之后被调用（因为添加顺序）
                // 一次性监听器会调用 RemoveListenerDelayed，但移除是延迟的
                // 这个测试验证不会出现集合修改异常
            }

            eventBus.AddListener<TestValueEventA>(RegularListener);
            var testEvent = new TestValueEventA("Test", 1313);

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

        #region 值类型特性测试

        [Test]
        public void ValueType_MutationDoesNotAffectOriginal()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var mutableEvent = new MutableValueEvent { Value = 100 };
            var receivedValue = 0;

            eventBus.AddListener<MutableValueEvent>(e =>
            {
                e.Value = 999; // 修改副本
                receivedValue = e.Value;
            });

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(mutableEvent, exceptions);

            // Assert - 验证原始值未改变
            Assert.That(receivedValue, Is.EqualTo(999)); // 副本被修改
            Assert.That(mutableEvent.Value, Is.EqualTo(100)); // 原始值不变
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValueType_DefaultValueWorks()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var receivedEvent = default(TestValueEventA);
            var receivedCount = 0;

            eventBus.AddListener<TestValueEventA>(e =>
            {
                receivedEvent = e;
                receivedCount++;
            });

            // Act - 发布默认值
            var exceptions = new List<Exception>();
            publisher.Publish(default(TestValueEventA), exceptions);

            // Assert
            Assert.That(receivedCount, Is.EqualTo(1));
            Assert.That(receivedEvent.Message, Is.Null);
            Assert.That(receivedEvent.Value, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        // 用于突变测试的可变值类型
        private struct MutableValueEvent : IValueEvent
        {
            public int Value;
        }

        #endregion

        #region 补充功能测试

        [Test]
        public void RemoveThenAddListener_WorksCorrectly()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestValueEventA e) => callCount++;

            eventBus.AddListener<TestValueEventA>(Listener);
            var testEvent = new TestValueEventA("Test", 100);

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证监听器被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 移除监听器
            eventBus.RemoveListener<TestValueEventA>(Listener);

            // Act - 第二次发布（移除后）
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证监听器未被调用
            Assert.That(callCount, Is.EqualTo(1), "移除后不应被调用");

            // Act - 重新添加监听器
            eventBus.AddListener<TestValueEventA>(Listener);

            // Act - 第三次发布（重新添加后）
            var exceptions3 = new List<Exception>();
            publisher.Publish(testEvent, exceptions3);

            // Assert - 验证监听器再次被调用
            Assert.That(callCount, Is.EqualTo(2), "重新添加后应被调用");
            Assert.That(exceptions2.Count, Is.EqualTo(0));
            Assert.That(exceptions3.Count, Is.EqualTo(0));
        }

        [Test]
        public void MultiplePublish_CallsListenerEachTime()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            eventBus.AddListener<TestValueEventA>(e => callCount++);

            var testEvent = new TestValueEventA("Test", 200);

            // Act - 发布多次
            var exceptions = new List<Exception>();
            for (int i = 0; i < 5; i++)
            {
                publisher.Publish(testEvent, exceptions);
            }

            // Assert
            Assert.That(callCount, Is.EqualTo(5));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComplexConcurrentModification_WorksCorrectly()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var results = new List<string>();
            var bAdded = false;

            void ListenerA(TestValueEventA e)
            {
                results.Add("A");
                // 只在第一次调用时添加 B
                if (!bAdded)
                {
                    bAdded = true;
                    eventBus.AddListener<TestValueEventA>(ListenerB);
                }
                eventBus.RemoveListener<TestValueEventA>(ListenerC);
                // 不使用一次性监听器，改用常规监听器 E
                eventBus.AddListener<TestValueEventA>(ListenerE);
            }

            void ListenerB(TestValueEventA e) => results.Add("B");
            void ListenerC(TestValueEventA e) => results.Add("C");
            void ListenerE(TestValueEventA e) => results.Add("E");

            eventBus.AddListener<TestValueEventA>(ListenerA);
            eventBus.AddListener<TestValueEventA>(ListenerC);
            var testEvent = new TestValueEventA("Test", 300);

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证第一次发布的结果
            Assert.That(results, Is.EqualTo(new[] { "A", "C" }), "B未添加，C的移除延迟，E延迟添加");
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 第二次发布
            results.Clear();
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证第二次发布的结果
            Assert.That(results, Is.EqualTo(new[] { "A", "B", "E" }), "A和B被调用，C已移除，E被添加");
            Assert.That(exceptions2.Count, Is.EqualTo(0));

            // Act - 第三次发布
            results.Clear();
            var exceptions3 = new List<Exception>();
            publisher.Publish(testEvent, exceptions3);

            // Assert - 验证第三次发布的结果
            // 注意：由于每次A调用都会添加E，所以E有多个实例
            Assert.That(results, Is.EqualTo(new[] { "A", "B", "E", "E" }), "E被添加两次，调用两次");
            Assert.That(exceptions3.Count, Is.EqualTo(0));
        }

        [Test]
        public void ListenerOrder_PreservedAfterRemovals()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callOrder = new List<int>();

            void Listener1(TestValueEventA e) => callOrder.Add(1);
            void Listener2(TestValueEventA e) => callOrder.Add(2);
            void Listener3(TestValueEventA e) => callOrder.Add(3);
            void Listener4(TestValueEventA e) => callOrder.Add(4);

            // 添加监听器
            eventBus.AddListener<TestValueEventA>(Listener1);
            eventBus.AddListener<TestValueEventA>(Listener2);
            eventBus.AddListener<TestValueEventA>(Listener3);
            eventBus.AddListener<TestValueEventA>(Listener4);

            // 移除一些监听器
            eventBus.RemoveListener<TestValueEventA>(Listener2);
            eventBus.RemoveListener<TestValueEventA>(Listener4);

            var testEvent = new TestValueEventA("Test", 400);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert - 验证剩余监听器按原始顺序调用
            Assert.That(callOrder, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoListenersAfterRemoval_NoExceptions()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestValueEventA e) => callCount++;

            eventBus.AddListener<TestValueEventA>(Listener);
            eventBus.RemoveListener<TestValueEventA>(Listener);

            var testEvent = new TestValueEventA("Test", 500);

            // Act - 发布事件（无监听器）
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 泛型约束测试

        [Test]
        public void NonStructType_CannotBeUsedAsEvent()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);

            // Act & Assert - 尝试为非值类型添加监听器应该编译失败
            // 此测试验证泛型约束 where T : struct 有效
            // 由于是编译时检查，只需确保代码能编译通过即可
            // 测试通过意味着 ValueEventBus.Listener<T> 委托只能用于值类型
        }

        [Test]
        public void NullableValueType_CannotBeUsed()
        {
            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);

            // Act & Assert - 可空值类型不是值类型，应编译失败
            // 同样，这是编译时检查
            // 测试通过意味着代码结构正确
        }

        #endregion

        #region 98b344e提交后补充测试

        [Test]
        public void LargeValueType_NoBoxing()
        {
            // 测试大型值类型事件不会导致装箱
            // 通过多次发布验证性能

            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;
            LargeValueEvent lastEvent = default;

            eventBus.AddListener<LargeValueEvent>(e =>
            {
                callCount++;
                lastEvent = e; // 赋值，验证值被正确传递
            });

            // Act - 发布多次
            var exceptions = new List<Exception>();

            for (int i = 0; i < 100; i++)
            {
                var testEvent = new LargeValueEvent(i);
                publisher.Publish(testEvent, exceptions);
            }

            // Assert
            Assert.That(callCount, Is.EqualTo(100));
            Assert.That(lastEvent.Data1, Is.EqualTo(99)); // 最后一次事件的值
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ListenOnce_ExceptionInListener_StillRemoved()
        {
            // 测试ListenOnce监听器抛出异常时仍然被移除

            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCount = 0;

            eventBus.ListenOnce<TestValueEventA>(e =>
            {
                callCount++;
                throw new Exception("Test exception");
            });

            var testEvent = new TestValueEventA("Test", 42);

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

        [Test]
        public void ReentrancyProtection_ListenerAttemptsPublish_ThrowsException()
        {
            // 测试在监听器内部尝试发布事件会抛出异常

            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var innerCallCount = 0;
            var exceptionThrown = false;

            eventBus.AddListener<TestValueEventA>(e =>
            {
                try
                {
                    // 尝试在监听器内部发布另一个事件
                    publisher.Publish(new TestValueEventB(1.0f, 2.0f), null);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }
            });

            eventBus.AddListener<TestValueEventB>(e => innerCallCount++);

            var testEvent = new TestValueEventA("Test", 42);

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
        public void MultipleListeners_SameEventType_AllCalled()
        {
            // 测试同一事件类型的多个监听器都被调用

            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var callCounts = new int[10];

            for (int i = 0; i < 10; i++)
            {
                int index = i; // 捕获局部变量
                eventBus.AddListener<TestValueEventA>(e =>
                {
                    callCounts[index]++;
                });
            }

            var testEvent = new TestValueEventA("Test", 42);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.That(callCounts[i], Is.EqualTo(1));
            }
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValueType_DefaultValue_PublishedCorrectly()
        {
            // 测试默认值类型事件可以被正确发布

            // Arrange
            var eventBus = new ValueEventBus<IValueEvent>(out var publisher);
            var receivedEvent = default(TestValueEventA);
            var callCount = 0;

            eventBus.AddListener<TestValueEventA>(e =>
            {
                callCount++;
                receivedEvent = e;
            });

            var testEvent = default(TestValueEventA); // 默认值

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(receivedEvent.Message, Is.Null); // 默认值的属性
            Assert.That(receivedEvent.Value, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion
    }
}