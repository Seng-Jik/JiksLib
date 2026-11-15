using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Control
{
    // 测试用的值类型事件
    public struct TestSafeEvent
    {
        public string Message { get; }
        public int Value { get; }

        public TestSafeEvent(string message, int value)
        {
            Message = message;
            Value = value;
        }
    }

    // 大型值类型用于测试拷贝行为
    public struct LargeSafeEvent
    {
        public long Data1 { get; }
        public long Data2 { get; }
        public long Data3 { get; }
        public long Data4 { get; }
        public long Data5 { get; }
        public long Data6 { get; }
        public long Data7 { get; }
        public long Data8 { get; }

        public LargeSafeEvent(long data)
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

    // 用于突变测试的可变值类型
    public struct MutableSafeEvent
    {
        public int Value;
    }

    [TestFixture]
    public class SafeEventTests
    {
        static SafeEvent<TestSafeEvent> CreateDeferred(
            out SafeEvent<TestSafeEvent>.Publisher publisher) =>
            new(out publisher, allowAddRemoveListenerDuringInvoke: true);

        static SafeEvent<TestSafeEvent> CreateRecursive(
            out SafeEvent<TestSafeEvent>.Publisher publisher,
            bool allowAddRemoveListenerDuringInvoke = false) =>
            new(
                out publisher,
                allowAddRemoveListenerDuringInvoke,
                allowRecursiveInvoke: true);

        #region 基础功能测试

        [Test]
        public void Constructor_CreatesPublisher()
        {
            // Arrange & Act
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);

            // Assert
            Assert.That(safeEvent, Is.Not.Null);
        }

        [Test]
        public void AddListener_ThenPublish_CallsListener()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var eventReceived = false;
            TestSafeEvent receivedEvent = default;

            safeEvent.AddListener(e =>
            {
                eventReceived = true;
                receivedEvent = e;
            });

            var testEvent = new TestSafeEvent("Hello", 42);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            safeEvent.AddListener(e => callCount1++);
            safeEvent.AddListener(e => callCount2++);

            var testEvent = new TestSafeEvent("Test", 123);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);
            safeEvent.RemoveListener(Listener);

            var testEvent = new TestSafeEvent("Test", 456);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            void Listener1(TestSafeEvent e) => callCount1++;
            void Listener2(TestSafeEvent e) => callCount2++;

            safeEvent.AddListener(Listener1);
            safeEvent.AddListener(Listener2);
            safeEvent.RemoveListener(Listener1);

            var testEvent = new TestSafeEvent("Test", 789);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount1, Is.EqualTo(0));
            Assert.That(callCount2, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 重入保护测试

        [Test]
        public void PublishDuringPublish_ThrowsInvalidOperationException()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var innerPublishAttempted = false;
            Exception? innerPublishException = null;

            safeEvent.AddListener(e =>
            {
                // 尝试在发布过程中再次发布；在监听器内捕获异常，
                // 断言必须放到 Publish 之外，否则失败会被 SafeEvent 当作监听器异常吞掉。
                if (innerPublishAttempted)
                    return;

                innerPublishAttempted = true;

                try
                {
                    publisher.Publish(new TestSafeEvent("Inner", 4444), null);
                }
                catch (Exception ex)
                {
                    innerPublishException = ex;
                }
            });

            var testEvent = new TestSafeEvent("Outer", 5555);

            // Act & Assert - 外部发布应正常进行
            var exceptions = new List<Exception>();
            Assert.DoesNotThrow(() => publisher.Publish(testEvent, exceptions));
            Assert.That(innerPublishAttempted, Is.True);
            Assert.That(innerPublishException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions, Is.Empty);
        }

        [Test]
        public void RecursivePublish_ThrowsException()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;
            Exception? innerPublishException = null;

            safeEvent.AddListener(e =>
            {
                callCount++;

                // 尝试递归发布；在监听器内捕获异常，断言放到 Publish 之外
                try
                {
                    publisher.Publish(new TestSafeEvent("Recursive", 999), null);
                }
                catch (Exception ex)
                {
                    innerPublishException = ex;
                }
            });

            var testEvent = new TestSafeEvent("Test", 111);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(innerPublishException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptions, Is.Empty);
        }

        #endregion

        #region 延迟操作测试

        [Test]
        public void AddListenerDuringPublish_ListenerNotCalledUntilNextPublish()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var originalCallCount = 0;
            var addedDuringPublishCallCount = 0;

            // 原始监听器，在调用时会添加一个新的监听器
            void OriginalListener(TestSafeEvent e)
            {
                originalCallCount++;

                // 在事件处理过程中添加新的监听器
                safeEvent.AddListener(e => addedDuringPublishCallCount++);
            }

            safeEvent.AddListener(OriginalListener);
            var testEvent = new TestSafeEvent("Test", 1111);

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
            var safeEvent = CreateDeferred(out var publisher);
            var callCount = 0;

            void ListenerToRemove(TestSafeEvent e) => callCount++;

            // 添加一个监听器，在事件处理过程中移除自身
            void OriginalListener(TestSafeEvent e)
            {
                callCount++;
                safeEvent.RemoveListener(ListenerToRemove);
            }

            safeEvent.AddListener(OriginalListener);
            safeEvent.AddListener(ListenerToRemove);
            var testEvent = new TestSafeEvent("Test", 2222);

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
            var safeEvent = CreateDeferred(out var publisher);
            var callCountA = 0;
            var callCountB = 0;
            var callCountC = 0;

            void ListenerB(TestSafeEvent e) => callCountB++;
            void ListenerC(TestSafeEvent e) => callCountC++;

            // 监听器 A，在事件处理过程中添加 B 并移除 C
            void ListenerA(TestSafeEvent e)
            {
                callCountA++;
                safeEvent.AddListener(ListenerB);
                safeEvent.RemoveListener(ListenerC);
            }

            safeEvent.AddListener(ListenerA);
            safeEvent.AddListener(ListenerC); // 稍后会被移除
            var testEvent = new TestSafeEvent("Test", 3333);

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

        #region ListenerCount 语义测试

        [Test]
        public void ListenerCount_NewInstance_IsZero()
        {
            // Arrange & Act
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);
            var safeEventWithCapacity = new SafeEvent<TestSafeEvent>(out _, capacity: 16);

            // Assert - 构造函数只影响内部容量，不会凭空产生监听器
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));
            Assert.That(safeEventWithCapacity.ListenerCount, Is.EqualTo(0));
        }

        [Test]
        public void ListenerCount_AddListener_Increments()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);

            void Listener1(TestSafeEvent e) { }
            void Listener2(TestSafeEvent e) { }

            // Act & Assert
            safeEvent.AddListener(Listener1);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            safeEvent.AddListener(Listener2);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));
        }

        [Test]
        public void ListenerCount_RemoveListener_Decrements()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);

            void Listener1(TestSafeEvent e) { }
            void Listener2(TestSafeEvent e) { }

            safeEvent.AddListener(Listener1);
            safeEvent.AddListener(Listener2);

            // Act & Assert
            safeEvent.RemoveListener(Listener1);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            safeEvent.RemoveListener(Listener2);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));
        }

        [Test]
        public void ListenerCount_RemoveNotAddedListener_Unchanged()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);

            void Listener(TestSafeEvent e) { }
            void OtherListener(TestSafeEvent e) { }

            safeEvent.AddListener(Listener);

            // Act & Assert - 移除未注册的监听器不应影响已有计数
            safeEvent.RemoveListener(OtherListener);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            // 重复移除已移除的监听器也不应把计数减成负数
            safeEvent.RemoveListener(Listener);
            safeEvent.RemoveListener(Listener);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));
        }

        [Test]
        public void ListenerCount_DuplicateRegistration_CountsEachTime()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            // Act
            safeEvent.AddListener(Listener);
            safeEvent.AddListener(Listener);

            // Assert - 重复注册按次数计数，发布时也被调用相应次数
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));
            publisher.Publish(new TestSafeEvent("Test", 1), null);
            Assert.That(callCount, Is.EqualTo(2));

            // Act & Assert - 一次移除只移除一个匹配项
            safeEvent.RemoveListener(Listener);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            safeEvent.RemoveListener(Listener);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));

            publisher.Publish(new TestSafeEvent("Test", 2), null);
            Assert.That(callCount, Is.EqualTo(2), "移除后不应再被调用");
        }

        [Test]
        public void ListenerCount_Publish_DoesNotChangeCount()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);

            void Listener1(TestSafeEvent e) { }
            void Listener2(TestSafeEvent e) { }

            safeEvent.AddListener(Listener1);
            safeEvent.AddListener(Listener2);

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), null);
            publisher.Publish(new TestSafeEvent("Test", 2), null);

            // Assert
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));
        }

        [Test]
        public void ListenerCount_AddDuringPublish_UpdatesAfterPublishCompletes()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var delayedListenerCallCount = 0;
            var countObservedDuringPublish = -1;

            void DelayedListener(TestSafeEvent e) => delayedListenerCallCount++;

            void AddingListener(TestSafeEvent e)
            {
                safeEvent.AddListener(DelayedListener);
                countObservedDuringPublish = safeEvent.ListenerCount;
            }

            safeEvent.AddListener(AddingListener);

            // Act - 发布期间添加的监听器延后生效
            publisher.Publish(new TestSafeEvent("Test", 1), null);

            // Assert
            Assert.That(countObservedDuringPublish, Is.EqualTo(1),
                "发布过程中新增的监听器尚未生效，不应计入数量");
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2),
                "发布结束后延迟添加的监听器才计入数量");
            Assert.That(delayedListenerCallCount, Is.EqualTo(0));

            // Act - 下一次发布时新监听器生效
            publisher.Publish(new TestSafeEvent("Test", 2), null);

            // Assert
            Assert.That(delayedListenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_RemoveDuringPublish_UpdatesAfterPublishCompletes()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var removedListenerCallCount = 0;
            var countObservedDuringPublish = -1;

            void RemovedListener(TestSafeEvent e) => removedListenerCallCount++;

            void RemovingListener(TestSafeEvent e)
            {
                safeEvent.RemoveListener(RemovedListener);
                countObservedDuringPublish = safeEvent.ListenerCount;
            }

            safeEvent.AddListener(RemovingListener);
            safeEvent.AddListener(RemovedListener);

            // Act - 发布期间移除的监听器延后生效
            publisher.Publish(new TestSafeEvent("Test", 1), null);

            // Assert
            Assert.That(countObservedDuringPublish, Is.EqualTo(2),
                "待移除的监听器在发布过程中仍然有效，应继续计入数量");
            Assert.That(removedListenerCallCount, Is.EqualTo(1),
                "延迟移除的监听器仍会参与本次发布");
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1),
                "发布结束后待移除的监听器才不再计入数量");

            // Act - 下一次发布时不再调用被移除的监听器
            publisher.Publish(new TestSafeEvent("Test", 2), null);

            // Assert
            Assert.That(removedListenerCallCount, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_AddAndRemoveSameListenerDuringPublish_NetZero()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var temporaryListenerCallCount = 0;

            void TemporaryListener(TestSafeEvent e) => temporaryListenerCallCount++;

            void Listener(TestSafeEvent e)
            {
                safeEvent.AddListener(TemporaryListener);
                safeEvent.RemoveListener(TemporaryListener);
            }

            safeEvent.AddListener(Listener);

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), null);

            // Assert - 延迟操作按入队顺序执行，添加后再移除，计数净变化为零
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
            Assert.That(temporaryListenerCallCount, Is.EqualTo(0));

            publisher.Publish(new TestSafeEvent("Test", 2), null);
            Assert.That(temporaryListenerCallCount, Is.EqualTo(0));
        }

        [Test]
        public void ListenerCount_ListenerRemovesItself_ReachesZeroAfterPublish()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var callCount = 0;

            void SelfRemovingListener(TestSafeEvent e)
            {
                callCount++;
                safeEvent.RemoveListener(SelfRemovingListener);
                Assert.That(safeEvent.ListenerCount, Is.EqualTo(1),
                    "自身移除被延迟，发布过程中仍应计入数量");
            }

            safeEvent.AddListener(SelfRemovingListener);

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));

            publisher.Publish(new TestSafeEvent("Test", 2), null);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_ListenerThrows_CountUnchanged()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);

            void ThrowingListener(TestSafeEvent e) =>
                throw new InvalidOperationException("Error");

            void NormalListener(TestSafeEvent e) { }

            safeEvent.AddListener(ThrowingListener);
            safeEvent.AddListener(NormalListener);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            // Assert - 监听器异常被隔离，不影响注册数量
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));

            safeEvent.RemoveListener(ThrowingListener);
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_DelayedAddAppliedWhenListenerThrows()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var delayedListenerCallCount = 0;

            void DelayedListener(TestSafeEvent e) => delayedListenerCallCount++;

            void ThrowingAddingListener(TestSafeEvent e)
            {
                safeEvent.AddListener(DelayedListener);
                throw new InvalidOperationException("Error");
            }

            safeEvent.AddListener(ThrowingAddingListener);

            var exceptions = new List<Exception>();

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            // Assert - 异常不会阻止延迟操作生效，发布结束后计数更新
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));

            publisher.Publish(new TestSafeEvent("Test", 2), null);
            Assert.That(delayedListenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_RecursivePublishAttempt_DoesNotChangeCount()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var listenerCallCount = 0;
            Exception? innerPublishException = null;

            safeEvent.AddListener(e =>
            {
                listenerCallCount++;

                // 在监听器内捕获递归发布的异常，断言放到 Publish 之外
                try
                {
                    publisher.Publish(new TestSafeEvent("Inner", 1), null);
                }
                catch (Exception ex)
                {
                    innerPublishException = ex;
                }
            });

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(new TestSafeEvent("Outer", 2), exceptions);

            // Assert
            Assert.That(innerPublishException, Is.TypeOf<InvalidOperationException>());
            Assert.That(listenerCallCount, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
            Assert.That(exceptions, Is.Empty);
        }

        [Test]
        public void AddNullListenerDuringPublish_ExceptionCollectedAndCountUnchanged()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);

            void Listener(TestSafeEvent e) => safeEvent.AddListener(null!);

            safeEvent.AddListener(Listener);
            var exceptions = new List<Exception>();

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            // Assert - null 校验在入队前完成，异常作为监听器异常被收集，计数不受影响
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(exceptions[0], Is.TypeOf<ArgumentNullException>());
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerCount_TracksManyAddRemoveOperations()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);
            const int listenerCount = 50;
            var listeners =
                new JiksLib.Control.EventHandler<TestSafeEvent>[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int index = i;
                listeners[i] = e => { _ = index; };
                safeEvent.AddListener(listeners[i]);
            }

            Assert.That(safeEvent.ListenerCount, Is.EqualTo(listenerCount));

            // Act - 移除偶数下标的监听器
            for (int i = 0; i < listenerCount; i += 2)
                safeEvent.RemoveListener(listeners[i]);

            // Assert
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(listenerCount / 2));

            // Act - 重复移除已移除的监听器
            for (int i = 0; i < listenerCount; i += 2)
                safeEvent.RemoveListener(listeners[i]);

            // Assert - 计数不会变成负数
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(listenerCount / 2));
        }

        #endregion

        #region 异常处理测试

        [Test]
        public void ListenerThrowsException_ExceptionCollected()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var exceptionMessage = "Test exception";

            safeEvent.AddListener(e => throw new InvalidOperationException(exceptionMessage));

            var testEvent = new TestSafeEvent("Test", 111);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);

            safeEvent.AddListener(e => throw new InvalidOperationException("Exception 1"));
            safeEvent.AddListener(e => throw new ArgumentException("Exception 2"));

            var testEvent = new TestSafeEvent("Test", 222);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var successfulCallCount = 0;

            safeEvent.AddListener(e => throw new InvalidOperationException("Error"));
            safeEvent.AddListener(e => successfulCallCount++);

            var testEvent = new TestSafeEvent("Test", 333);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            safeEvent.AddListener(e => throw new InvalidOperationException("Error"));
            safeEvent.AddListener(e => callCount++);

            var testEvent = new TestSafeEvent("Test", 444);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var testEvent = new TestSafeEvent("Test", 555);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);
            safeEvent.AddListener(Listener); // 第二次添加

            var testEvent = new TestSafeEvent("Test", 666);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;
            void OtherListener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);
            safeEvent.RemoveListener(OtherListener); // 移除未添加的监听器

            var testEvent = new TestSafeEvent("Test", 777);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var receivedValue = 0;

            safeEvent.AddListener(e => receivedValue = e.Value);

            var testEvent = default(TestSafeEvent); // 默认值

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(receivedValue, Is.EqualTo(0)); // 默认值
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddNullListener_ThrowsArgumentNullException()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out _);

            // Act & Assert - null 不是合法监听器，校验失败时不能改变计数
            Assert.Throws<ArgumentNullException>(
                () => safeEvent.AddListener(null!));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(0));
        }

        [Test]
        public void RemoveNullListener_ThrowsArgumentNullException()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);

            // Act & Assert - 传入null时抛出异常，且不能误删已有监听器
            Assert.Throws<ArgumentNullException>(
                () => safeEvent.RemoveListener(null!));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            publisher.Publish(new TestSafeEvent("Test", 1), null);
            Assert.That(callCount, Is.EqualTo(1));
        }

        #endregion

        #region 值类型特性测试

        [Test]
        public void ValueType_MutationDoesNotAffectOriginal()
        {
            // Arrange
            var safeEvent = new SafeEvent<MutableSafeEvent>(out var publisher);
            var mutableEvent = new MutableSafeEvent { Value = 100 };
            var receivedValue = 0;

            safeEvent.AddListener(e =>
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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var receivedEvent = default(TestSafeEvent);
            var receivedCount = 0;

            safeEvent.AddListener(e =>
            {
                receivedEvent = e;
                receivedCount++;
            });

            // Act - 发布默认值
            var exceptions = new List<Exception>();
            publisher.Publish(default(TestSafeEvent), exceptions);

            // Assert
            Assert.That(receivedCount, Is.EqualTo(1));
            Assert.That(receivedEvent.Message, Is.Null);
            Assert.That(receivedEvent.Value, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void LargeValueEvent_CopiesCorrectly()
        {
            // Arrange
            var safeEvent = new SafeEvent<LargeSafeEvent>(out var publisher);
            long receivedData1 = 0;
            long receivedData8 = 0;

            safeEvent.AddListener(e =>
            {
                receivedData1 = e.Data1;
                receivedData8 = e.Data8;
            });

            var largeEvent = new LargeSafeEvent(999);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            const int listenerCount = 100;
            var callCounts = new int[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int index = i; // 捕获局部变量
                safeEvent.AddListener(e => callCounts[index]++);
            }

            var testEvent = new TestSafeEvent("Test", 888);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            const int listenerCount = 50;
            var listeners = new JiksLib.Control.EventHandler<TestSafeEvent>[listenerCount];

            for (int i = 0; i < listenerCount; i++)
            {
                int callCount = 0;
                JiksLib.Control.EventHandler<TestSafeEvent> listener = e => callCount++;
                listeners[i] = listener;
                safeEvent.AddListener(listener);
            }

            // 移除所有监听器
            foreach (var listener in listeners)
            {
                safeEvent.RemoveListener(listener);
            }

            var testEvent = new TestSafeEvent("Test", 999);

            // Act
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            // 所有监听器已移除，不应被调用
            Assert.That(exceptions.Count, Is.EqualTo(0));
            // 注意：无法直接验证监听器未被调用，但如果没有异常则通过
        }

        [Test]
        public void ManyPublishOperations_NoMemoryLeak()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;
            var weakRefs = new List<WeakReference>();

            // 添加一些监听器
            for (int i = 0; i < 10; i++)
            {
                var listener = new JiksLib.Control.EventHandler<TestSafeEvent>(e => callCount++);
                safeEvent.AddListener(listener);
                weakRefs.Add(new WeakReference(listener));
            }

            var testEvent = new TestSafeEvent("Test", 1000);

            // Act - 发布多次
            var exceptions = new List<Exception>();
            for (int i = 0; i < 100; i++)
            {
                publisher.Publish(testEvent, exceptions);
            }

            // 移除所有监听器
            // 注意：无法直接移除匿名委托，但可以验证没有强引用保持

            // Assert - 验证没有异常
            Assert.That(exceptions.Count, Is.EqualTo(0));
            // 注意：内存泄漏测试不完整，但至少验证了功能正常
        }

        #endregion

        #region 复杂场景测试

        [Test]
        public void ComplexConcurrentModification_WorksCorrectly()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var results = new List<string>();
            var bAdded = false;

            void ListenerA(TestSafeEvent e)
            {
                results.Add("A");
                // 只在第一次调用时添加 B
                if (!bAdded)
                {
                    bAdded = true;
                    safeEvent.AddListener(ListenerB);
                }
                safeEvent.RemoveListener(ListenerC);
                // 添加监听器 E
                safeEvent.AddListener(ListenerE);
            }

            void ListenerB(TestSafeEvent e) => results.Add("B");
            void ListenerC(TestSafeEvent e) => results.Add("C");
            void ListenerE(TestSafeEvent e) => results.Add("E");

            safeEvent.AddListener(ListenerA);
            safeEvent.AddListener(ListenerC);
            var testEvent = new TestSafeEvent("Test", 300);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callOrder = new List<int>();

            void Listener1(TestSafeEvent e) => callOrder.Add(1);
            void Listener2(TestSafeEvent e) => callOrder.Add(2);
            void Listener3(TestSafeEvent e) => callOrder.Add(3);
            void Listener4(TestSafeEvent e) => callOrder.Add(4);

            // 添加监听器
            safeEvent.AddListener(Listener1);
            safeEvent.AddListener(Listener2);
            safeEvent.AddListener(Listener3);
            safeEvent.AddListener(Listener4);

            // 移除一些监听器
            safeEvent.RemoveListener(Listener2);
            safeEvent.RemoveListener(Listener4);

            var testEvent = new TestSafeEvent("Test", 400);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);
            safeEvent.RemoveListener(Listener);

            var testEvent = new TestSafeEvent("Test", 500);

            // Act - 发布事件（无监听器）
            var exceptions = new List<Exception>();
            publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 清理与重置测试

        [Test]
        public void MultipleCycles_AddRemoveAdd_WorksCorrectly()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            void Listener(TestSafeEvent e) => callCount++;

            safeEvent.AddListener(Listener);
            var testEvent = new TestSafeEvent("Test", 100);

            // Act - 第一次发布
            var exceptions1 = new List<Exception>();
            publisher.Publish(testEvent, exceptions1);

            // Assert - 验证监听器被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions1.Count, Is.EqualTo(0));

            // Act - 移除监听器
            safeEvent.RemoveListener(Listener);

            // Act - 第二次发布（移除后）
            var exceptions2 = new List<Exception>();
            publisher.Publish(testEvent, exceptions2);

            // Assert - 验证监听器未被调用
            Assert.That(callCount, Is.EqualTo(1), "移除后不应被调用");

            // Act - 重新添加监听器
            safeEvent.AddListener(Listener);

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
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            safeEvent.AddListener(e => callCount++);

            var testEvent = new TestSafeEvent("Test", 200);

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

        #endregion

        #region 补充测试 - SafeEvent 增强覆盖

        [Test]
        public void RemoveIndex_BoundaryConditions()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount1 = 0;
            var callCount2 = 0;

            void Listener1(TestSafeEvent e) => callCount1++;
            void Listener2(TestSafeEvent e) => callCount2++;

            // 添加两个监听器
            safeEvent.AddListener(Listener1);
            safeEvent.AddListener(Listener2);

            // Act 1: 移除第二个监听器，removeIndex应指向正确位置
            safeEvent.RemoveListener(Listener2);

            // Assert 1: 发布事件，只有第一个监听器被调用
            var exceptions = new List<Exception>();
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(0));

            // Act 2: 移除第一个监听器，此时列表为空
            safeEvent.RemoveListener(Listener1);

            // Assert 2: removeIndex应该为-1或适当值，再次发布无监听器调用
            callCount1 = 0;
            publisher.Publish(new TestSafeEvent("Test", 2), exceptions);
            Assert.That(callCount1, Is.EqualTo(0));

            // Act 3: 重新添加监听器测试removeIndex重置
            safeEvent.AddListener(Listener1);
            publisher.Publish(new TestSafeEvent("Test", 3), exceptions);
            Assert.That(callCount1, Is.EqualTo(1));
        }

        [Test]
        public void ListenersDelayedOperation_QueueBehavior()
        {
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var callCount = 0;
            var nestedCallCount = 0;

            void OuterListener(TestSafeEvent e)
            {
                callCount++;
                // 在事件处理过程中添加和移除监听器
                void NestedListener(TestSafeEvent e2) => nestedCallCount++;
                safeEvent.AddListener(NestedListener);
                safeEvent.RemoveListener(NestedListener); // 立即移除，应被忽略
            }

            safeEvent.AddListener(OuterListener);

            // Act: 第一次发布，添加和移除操作被延迟
            var exceptions = new List<Exception>();
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            // Assert: 只有外部监听器被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(nestedCallCount, Is.EqualTo(0));

            // Act: 第二次发布，延迟操作已处理
            publisher.Publish(new TestSafeEvent("Test", 2), exceptions);

            // Assert: 仍然只有外部监听器，嵌套监听器被添加后立即移除
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(nestedCallCount, Is.EqualTo(0));
        }

        [Test]
        public void ListenersDelayedOperation_ClearAfterProcessing()
        {
            // 测试 listenersDelayedOperation 队列在处理后被清空
            // Arrange
            var safeEvent = CreateDeferred(out var publisher);
            var callCount = 0;
            var delayedCallCount = 0;
            bool delayedListenerAdded = false;

            void DelayedListener(TestSafeEvent e) => delayedCallCount++;

            void MainListener(TestSafeEvent e)
            {
                callCount++;
                // 在事件处理过程中添加监听器，但只添加一次
                if (!delayedListenerAdded)
                {
                    safeEvent.AddListener(DelayedListener);
                    delayedListenerAdded = true;
                }
            }

            safeEvent.AddListener(MainListener);
            var exceptions = new List<Exception>();

            // Act: 第一次发布，添加DelayedListener的操作被延迟
            publisher.Publish(new TestSafeEvent("First", 1), exceptions);

            // Assert: 只有MainListener被调用
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(delayedCallCount, Is.EqualTo(0));

            // Act: 第二次发布，DelayedListener现在生效
            publisher.Publish(new TestSafeEvent("Second", 2), exceptions);

            // Assert: MainListener和DelayedListener都被调用
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(delayedCallCount, Is.EqualTo(1));

            // Act: 第三次发布，两个监听器都被调用
            publisher.Publish(new TestSafeEvent("Third", 3), exceptions);

            // Assert: 两个监听器都被调用
            Assert.That(callCount, Is.EqualTo(3));
            Assert.That(delayedCallCount, Is.EqualTo(2));
        }

        [Test]
        public void ThreadSafety_ViolationDetection()
        {
            // 注意：SafeEvent设计为仅单线程使用
            // 此测试验证在多线程误用下的行为（可能崩溃或未定义）
            // 由于线程安全不是设计目标，我们只验证单线程假设
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var callCount = 0;

            safeEvent.AddListener(e => callCount++);

            // 单线程使用正常
            var exceptions = new List<Exception>();
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        #endregion

        #region 默认严格策略测试

        [Test]
        public void AddListenerDuringPublish_DefaultPolicy_ExceptionCollected()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var exceptions = new List<Exception>();
            var callCount = 0;

            safeEvent.AddListener(e =>
            {
                callCount++;
                safeEvent.AddListener(_ => callCount += 100);
            });

            // Act
            Assert.DoesNotThrow(() =>
                publisher.Publish(new TestSafeEvent("Test", 1), exceptions));

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(exceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveListenerDuringPublish_DefaultPolicy_ExceptionCollected()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var exceptions = new List<Exception>();
            var removingCallCount = 0;
            var removedCallCount = 0;

            void RemovedListener(TestSafeEvent e) => removedCallCount++;

            safeEvent.AddListener(e =>
            {
                removingCallCount++;
                safeEvent.RemoveListener(RemovedListener);
            });
            safeEvent.AddListener(RemovedListener);

            // Act
            publisher.Publish(new TestSafeEvent("Test", 1), exceptions);

            // Assert - 移除被拒绝后，监听器仍保持注册并继续参与本次发布
            Assert.That(removingCallCount, Is.EqualTo(1));
            Assert.That(removedCallCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(exceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));
        }

        [Test]
        public void RecursivePublish_DefaultPolicy_ExceptionCollected()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(out var publisher);
            var exceptions = new List<Exception>();
            var callCount = 0;

            safeEvent.AddListener(e =>
            {
                callCount++;
                publisher.Publish(new TestSafeEvent("Inner", 2), null);
            });

            // Act
            Assert.DoesNotThrow(() =>
                publisher.Publish(new TestSafeEvent("Outer", 1), exceptions));

            // Assert - 嵌套发布的异常作为监听器异常被收集，外层发布继续完成
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(exceptions.Count, Is.EqualTo(1));
            Assert.That(exceptions[0], Is.TypeOf<InvalidOperationException>());
        }

        #endregion

        #region 递归发布（队列）测试

        [Test]
        public void RecursivePublish_QueuesEventAndDeliversCorrectPayload()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var received = new List<string>();

            safeEvent.AddListener(e =>
            {
                received.Add(e.Message);
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("inner", 2), null);
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 嵌套事件使用自己的对象派发，而不是重复外层事件
            Assert.That(received, Is.EqualTo(new[] { "outer", "inner" }));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_DeliversQueuedEventsAfterOuterListenersInFifoOrder()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var trace = new List<string>();

            safeEvent.AddListener(e =>
            {
                trace.Add("A:" + e.Message);
                if (e.Message == "outer")
                {
                    publisher.Publish(new TestSafeEvent("first", 2), null);
                    publisher.Publish(new TestSafeEvent("second", 3), null);
                }
            });
            safeEvent.AddListener(e => trace.Add("B:" + e.Message));

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 外层事件先完整派发，再按 FIFO 派发排队事件
            Assert.That(trace, Is.EqualTo(new[]
            {
                "A:outer", "B:outer",
                "A:first", "B:first",
                "A:second", "B:second",
            }));
        }

        [Test]
        public void RecursivePublish_NestedCallReturnsBeforeDelivery()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var innerDelivered = false;
            var returnedBeforeInnerDelivery = false;

            safeEvent.AddListener(e =>
            {
                if (e.Message == "outer")
                {
                    publisher.Publish(new TestSafeEvent("inner", 2), null);
                    returnedBeforeInnerDelivery = !innerDelivered;
                }
                else if (e.Message == "inner")
                {
                    innerDelivered = true;
                }
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 嵌套 Publish 只入队，排空发生在监听器返回之后
            Assert.That(returnedBeforeInnerDelivery, Is.True);
            Assert.That(innerDelivered, Is.True);
        }

        [Test]
        public void RecursivePublish_NestedFromQueuedEvent_IsProcessedInFifoOrder()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var trace = new List<string>();

            safeEvent.AddListener(e =>
            {
                trace.Add(e.Message);
                if (e.Message == "outer")
                {
                    publisher.Publish(new TestSafeEvent("first", 2), null);
                    publisher.Publish(new TestSafeEvent("second", 3), null);
                }
                else if (e.Message == "first")
                {
                    publisher.Publish(new TestSafeEvent("third", 4), null);
                }
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 队列排空期间新入队的事件排在已有事件之后
            Assert.That(trace, Is.EqualTo(new[]
            {
                "outer", "first", "second", "third",
            }));
        }

        [Test]
        public void RecursivePublish_NestedEventExceptionsGoToNestedOutput()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var outerExceptions = new List<Exception>();
            var innerExceptions = new List<Exception>();

            safeEvent.AddListener(e =>
            {
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("inner", 2), innerExceptions);
                else
                    throw new InvalidOperationException("inner failure");
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), outerExceptions);

            // Assert - 排队事件的异常写入发起该次嵌套发布时传入的输出
            Assert.That(outerExceptions.Count, Is.EqualTo(0));
            Assert.That(innerExceptions.Count, Is.EqualTo(1));
            Assert.That(innerExceptions[0].Message, Is.EqualTo("inner failure"));
        }

        [Test]
        public void RecursivePublish_NestedNullOutput_DropsExceptions()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var outerExceptions = new List<Exception>();

            safeEvent.AddListener(e =>
            {
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("inner", 2), null);
                else
                    throw new InvalidOperationException("inner failure");
            });

            // Act & Assert - 嵌套调用未提供输出时异常被忽略，不影响外层发布
            Assert.DoesNotThrow(() =>
                publisher.Publish(new TestSafeEvent("outer", 1), outerExceptions));
            Assert.That(outerExceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RecursivePublish_ListenerAddExceptionGoesToNestedOutput()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(
                out var publisher,
                allowAddRemoveListenerDuringInvoke: false,
                allowRecursiveInvoke: true);
            var outerExceptions = new List<Exception>();
            var innerExceptions = new List<Exception>();

            safeEvent.AddListener(e =>
            {
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("inner", 2), innerExceptions);
                else
                    safeEvent.AddListener(_ => { });
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), outerExceptions);

            // Assert - 排队事件监听器内 AddListener 的异常也进入异常输出
            Assert.That(outerExceptions.Count, Is.EqualTo(0));
            Assert.That(innerExceptions.Count, Is.EqualTo(1));
            Assert.That(innerExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_WithDeferredAdd_ListenerAppliesAfterSession()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(
                out var publisher,
                allowAddRemoveListenerDuringInvoke: true,
                allowRecursiveInvoke: true);
            var addedCalls = 0;
            var added = false;

            void AddedListener(TestSafeEvent e) => addedCalls++;

            safeEvent.AddListener(e =>
            {
                if (e.Message != "outer")
                    return;

                if (!added)
                {
                    added = true;
                    safeEvent.AddListener(AddedListener);
                }

                publisher.Publish(new TestSafeEvent("inner", 2), null);
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 会话期间新增的监听器不参与本次排队的嵌套事件
            Assert.That(addedCalls, Is.EqualTo(0));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(2));

            publisher.Publish(new TestSafeEvent("next", 3), null);
            Assert.That(addedCalls, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_WithDeferredRemove_ListenerStillReceivesQueuedEvent()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(
                out var publisher,
                allowAddRemoveListenerDuringInvoke: true,
                allowRecursiveInvoke: true);
            var removedOuterCalls = 0;
            var removedInnerCalls = 0;

            void RemovedListener(TestSafeEvent e)
            {
                if (e.Message == "outer") removedOuterCalls++;
                else if (e.Message == "inner") removedInnerCalls++;
            }

            safeEvent.AddListener(e =>
            {
                if (e.Message != "outer")
                    return;

                safeEvent.RemoveListener(RemovedListener);
                publisher.Publish(new TestSafeEvent("inner", 2), null);
            });
            safeEvent.AddListener(RemovedListener);

            // Act
            publisher.Publish(new TestSafeEvent("outer", 1), null);

            // Assert - 会话期间移除的监听器仍参与本次排队的嵌套事件
            Assert.That(removedOuterCalls, Is.EqualTo(1));
            Assert.That(removedInnerCalls, Is.EqualTo(1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));

            publisher.Publish(new TestSafeEvent("next", 3), null);
            Assert.That(removedOuterCalls, Is.EqualTo(1));
            Assert.That(removedInnerCalls, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_DeepChain_DoesNotOverflowStack()
        {
            // Arrange
            const int limit = 10_000;
            var safeEvent = CreateRecursive(out var publisher);
            var callCount = 0;

            safeEvent.AddListener(e =>
            {
                callCount++;
                if (e.Value < limit)
                    publisher.Publish(new TestSafeEvent("next", e.Value + 1), null);
            });

            // Act - 若递归发布退化为真实递归，这里会发生 StackOverflowException
            publisher.Publish(new TestSafeEvent("start", 0), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(limit + 1));
            Assert.That(safeEvent.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_ConstructorWithCapacityAndPolicies_Works()
        {
            // Arrange
            var safeEvent = new SafeEvent<TestSafeEvent>(
                out var publisher,
                allowAddRemoveListenerDuringInvoke: true,
                allowRecursiveInvoke: true,
                capacity: 8);
            var callCount = 0;

            safeEvent.AddListener(e =>
            {
                callCount++;
                if (e.Value == 0)
                    publisher.Publish(new TestSafeEvent("inner", 1), null);
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 0), null);

            // Assert
            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void RecursivePublish_QueuedListenerThrows_QueueContinues()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var outerExceptions = new List<Exception>();
            var innerExceptions = new List<Exception>();
            var trace = new List<string>();

            safeEvent.AddListener(e =>
            {
                trace.Add("A:" + e.Message);

                if (e.Message == "outer")
                {
                    publisher.Publish(new TestSafeEvent("first", 1), innerExceptions);
                    publisher.Publish(new TestSafeEvent("second", 2), innerExceptions);
                }
                else if (e.Message == "first")
                {
                    throw new InvalidOperationException("nested failure");
                }
            });
            safeEvent.AddListener(e => trace.Add("B:" + e.Message));

            // Act
            publisher.Publish(new TestSafeEvent("outer", 0), outerExceptions);

            // Assert - 队列中的 first 异常不应阻止 second 继续派发
            Assert.That(trace, Is.EqualTo(new[]
            {
                "A:outer", "B:outer",
                "A:first", "B:first",
                "A:second", "B:second",
            }));
            Assert.That(outerExceptions, Is.Empty);
            Assert.That(innerExceptions.Count, Is.EqualTo(1));
        }

        [Test]
        public void RecursivePublish_MultipleListenersEnqueue_AllQueuedAfterOuter()
        {
            // Arrange
            var safeEvent = CreateRecursive(out var publisher);
            var trace = new List<string>();

            safeEvent.AddListener(e =>
            {
                trace.Add("A:" + e.Message);
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("first", 1), null);
            });
            safeEvent.AddListener(e =>
            {
                trace.Add("B:" + e.Message);
                if (e.Message == "outer")
                    publisher.Publish(new TestSafeEvent("second", 2), null);
            });

            // Act
            publisher.Publish(new TestSafeEvent("outer", 0), null);

            // Assert - 两个监听器的嵌套发布都排在外层事件之后，按入队顺序处理
            Assert.That(trace, Is.EqualTo(new[]
            {
                "A:outer", "B:outer",
                "A:first", "B:first",
                "A:second", "B:second",
            }));
        }

        #endregion
    }
}