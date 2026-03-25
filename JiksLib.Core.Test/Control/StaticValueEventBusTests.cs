using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Control
{
    // 测试用的值类型事件接口 - 用于StaticValueEventBus
    public interface IStaticValueEvent { }

    public struct StaticTestValueEventA : IStaticValueEvent
    {
        public string Message { get; }
        public int Value { get; }

        public StaticTestValueEventA(string message, int value)
        {
            Message = message;
            Value = value;
        }
    }

    public struct StaticTestValueEventB : IStaticValueEvent
    {
        public float X { get; }
        public float Y { get; }

        public StaticTestValueEventB(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [TestFixture]
    public class StaticValueEventBusTests
    {
        #region 基础功能测试

        [Test]
        public void AddListener_ThenPublish_CallsListener()
        {
            // Arrange
            var callCount = 0;
            StaticTestValueEventA receivedEvent = default;

            JiksLib.Control.EventHandler<StaticTestValueEventA> handler = e =>
            {
                callCount++;
                receivedEvent = e;
            };

            StaticValueEventBus<IStaticValueEvent>.AddListener(handler);

            try
            {
                var testEvent = new StaticTestValueEventA("Hello", 42);

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IStaticValueEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(receivedEvent.Message, Is.EqualTo("Hello"));
                Assert.That(receivedEvent.Value, Is.EqualTo(42));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handler);
            }
        }

        [Test]
        public void AddListener_RemoveListener_ThenPublish_DoesNotCallListener()
        {
            // Arrange
            var callCount = 0;
            JiksLib.Control.EventHandler<StaticTestValueEventA> handler = e => callCount++;

            StaticValueEventBus<IStaticValueEvent>.AddListener(handler);
            StaticValueEventBus<IStaticValueEvent>.RemoveListener(handler);

            var testEvent = new StaticTestValueEventA("Test", 123);

            // Act
            var exceptions = new List<Exception>();
            StaticValueEventBus<IStaticValueEvent>.Publisher.Publish(testEvent, exceptions);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddMultipleListeners_AllCalled()
        {
            // Arrange
            var callCount1 = 0;
            var callCount2 = 0;
            var callCount3 = 0;

            JiksLib.Control.EventHandler<StaticTestValueEventA> handler1 = e => callCount1++;
            JiksLib.Control.EventHandler<StaticTestValueEventA> handler2 = e => callCount2++;
            JiksLib.Control.EventHandler<StaticTestValueEventA> handler3 = e => callCount3++;

            StaticValueEventBus<IStaticValueEvent>.AddListener(handler1);
            StaticValueEventBus<IStaticValueEvent>.AddListener(handler2);
            StaticValueEventBus<IStaticValueEvent>.AddListener(handler3);

            try
            {
                var testEvent = new StaticTestValueEventA("Multi", 999);

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IStaticValueEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount1, Is.EqualTo(1));
                Assert.That(callCount2, Is.EqualTo(1));
                Assert.That(callCount3, Is.EqualTo(1));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handler1);
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handler2);
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handler3);
            }
        }

        [Test]
        public void DifferentEventTypes_OnlyCorrectListenerCalled()
        {
            // Arrange
            var callCountA = 0;
            var callCountB = 0;

            JiksLib.Control.EventHandler<StaticTestValueEventA> handlerA = e => callCountA++;
            JiksLib.Control.EventHandler<StaticTestValueEventB> handlerB = e => callCountB++;

            StaticValueEventBus<IStaticValueEvent>.AddListener(handlerA);
            StaticValueEventBus<IStaticValueEvent>.AddListener(handlerB);

            try
            {
                var testEventA = new StaticTestValueEventA("EventA", 1);
                var testEventB = new StaticTestValueEventB(2.5f, 3.7f);

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IStaticValueEvent>.Publisher.Publish(testEventA, exceptions);
                StaticValueEventBus<IStaticValueEvent>.Publisher.Publish(testEventB, exceptions);

                // Assert
                Assert.That(callCountA, Is.EqualTo(1));
                Assert.That(callCountB, Is.EqualTo(1));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handlerA);
                StaticValueEventBus<IStaticValueEvent>.RemoveListener(handlerB);
            }
        }

        #endregion

        #region 异常处理测试

        // 为异常测试使用独立的事件接口
        private interface IExceptionTestEvent { }
        private struct ExceptionTestEvent : IExceptionTestEvent
        {
            public string Message { get; }
            public ExceptionTestEvent(string message) => Message = message;
        }

        [Test]
        public void ListenerThrowsException_ExceptionCollected()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var callCount = 0;

            JiksLib.Control.EventHandler<ExceptionTestEvent> handler = e =>
            {
                callCount++;
                throw exception;
            };

            StaticValueEventBus<IExceptionTestEvent>.AddListener(handler);

            try
            {
                var testEvent = new ExceptionTestEvent("Error");

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IExceptionTestEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(exceptions.Count, Is.EqualTo(1));
                Assert.That(exceptions[0], Is.SameAs(exception));
            }
            finally
            {
                StaticValueEventBus<IExceptionTestEvent>.RemoveListener(handler);
            }
        }

        [Test]
        public void MultipleListenersThrowExceptions_AllExceptionsCollected()
        {
            // Arrange
            var exception1 = new InvalidOperationException("Exception 1");
            var exception2 = new ArgumentException("Exception 2");

            JiksLib.Control.EventHandler<ExceptionTestEvent> handler1 = e => throw exception1;
            JiksLib.Control.EventHandler<ExceptionTestEvent> handler2 = e => throw exception2;

            StaticValueEventBus<IExceptionTestEvent>.AddListener(handler1);
            StaticValueEventBus<IExceptionTestEvent>.AddListener(handler2);

            try
            {
                var testEvent = new ExceptionTestEvent("Errors");

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IExceptionTestEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(exceptions.Count, Is.EqualTo(2));
                Assert.That(exceptions, Contains.Item(exception1));
                Assert.That(exceptions, Contains.Item(exception2));
            }
            finally
            {
                StaticValueEventBus<IExceptionTestEvent>.RemoveListener(handler1);
                StaticValueEventBus<IExceptionTestEvent>.RemoveListener(handler2);
            }
        }

        [Test]
        public void ExceptionsOutputNull_ExceptionsIgnored()
        {
            // Arrange
            var exceptionThrown = false;

            JiksLib.Control.EventHandler<ExceptionTestEvent> handler = e =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Should be ignored");
            };

            StaticValueEventBus<IExceptionTestEvent>.AddListener(handler);

            try
            {
                var testEvent = new ExceptionTestEvent("Silent");

                // Act & Assert (不应该抛出异常)
                StaticValueEventBus<IExceptionTestEvent>.Publisher.Publish(testEvent, null);
                Assert.That(exceptionThrown, Is.True); // 监听器应该被调用
            }
            finally
            {
                StaticValueEventBus<IExceptionTestEvent>.RemoveListener(handler);
            }
        }

        #endregion

        #region 边界条件测试

        private interface IBoundaryTestEvent { }
        private struct BoundaryTestEvent : IBoundaryTestEvent
        {
            public int Value { get; }
            public BoundaryTestEvent(int value) => Value = value;
        }

        [Test]
        public void NoListeners_Publish_Succeeds()
        {
            // Arrange
            var testEvent = new BoundaryTestEvent(123);

            // Act & Assert (不应该抛出异常)
            var exceptions = new List<Exception>();
            StaticValueEventBus<IBoundaryTestEvent>.Publisher.Publish(testEvent, exceptions);
            Assert.That(exceptions.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveNonExistentListener_NoError()
        {
            // Arrange
            JiksLib.Control.EventHandler<BoundaryTestEvent> handler = e => { };

            // Act & Assert (不应该抛出异常)
            Assert.DoesNotThrow(() =>
                StaticValueEventBus<IBoundaryTestEvent>.RemoveListener(handler));
        }

        [Test]
        public void AddSameListenerMultipleTimes_CalledMultipleTimes()
        {
            // Arrange
            var callCount = 0;
            JiksLib.Control.EventHandler<BoundaryTestEvent> handler = e => callCount++;

            StaticValueEventBus<IBoundaryTestEvent>.AddListener(handler);
            StaticValueEventBus<IBoundaryTestEvent>.AddListener(handler); // 再次添加

            try
            {
                var testEvent = new BoundaryTestEvent(777);

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IBoundaryTestEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                // 允许同一监听器被添加多次
                Assert.That(callCount, Is.EqualTo(2));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IBoundaryTestEvent>.RemoveListener(handler);
            }
        }

        #endregion

        #region ISafeEventPublisher接口测试

        private interface IInterfaceTestEvent { }
        private struct InterfaceTestEvent : IInterfaceTestEvent
        {
            public string Message { get; }
            public InterfaceTestEvent(string message) => Message = message;
        }

        [Test]
        public void PublisherImplementsISafeEventPublisher()
        {
            // Arrange
            var publisher = StaticValueEventBus<IInterfaceTestEvent>.Publisher.Singleton;

            // Act & Assert
            Assert.That(publisher, Is.InstanceOf<ISafeEventPublisher<IInterfaceTestEvent>>());
        }

        [Test]
        public void ISafeEventPublisherPublish_CallsCorrectListener()
        {
            // Arrange
            var callCount = 0;
            InterfaceTestEvent receivedEvent = default;
            JiksLib.Control.EventHandler<InterfaceTestEvent> handler = e =>
            {
                callCount++;
                receivedEvent = e;
            };

            StaticValueEventBus<IInterfaceTestEvent>.AddListener(handler);

            try
            {
                var testEvent = new InterfaceTestEvent("Interface");
                var publisher = (ISafeEventPublisher<IInterfaceTestEvent>)StaticValueEventBus<IInterfaceTestEvent>.Publisher.Singleton;

                // Act
                var exceptions = new List<Exception>();
                publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(receivedEvent.Message, Is.EqualTo("Interface"));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IInterfaceTestEvent>.RemoveListener(handler);
            }
        }

        #endregion

        #region 静态类特性测试

        private interface IStaticClassTestEvent { }

        private interface IIsolatedEvent1 { }
        private struct IsolatedEvent1 : IIsolatedEvent1 { }

        private interface IIsolatedEvent2 { }
        private struct IsolatedEvent2 : IIsolatedEvent2 { }

        private interface IUnregisteredEvent { }
        private struct UnregisteredEvent : IUnregisteredEvent { }

        private interface IBoxedTestEvent { }
        private struct BoxedTestEvent : IBoxedTestEvent
        {
            public int Value { get; }
            public BoxedTestEvent(int value) => Value = value;
        }

        private interface ICacheTestEvent { }
        private struct CacheTestEvent : ICacheTestEvent { }

        private interface IManyListenersEvent { }
        private struct ManyListenersEvent : IManyListenersEvent { }

        private struct StaticClassTestEvent : IStaticClassTestEvent
        {
            public int Value { get; }
            public StaticClassTestEvent(int value) => Value = value;
        }

        [Test]
        public void StaticClass_NoInstanceCreationNeeded()
        {
            // Arrange
            var callCount = 0;
            JiksLib.Control.EventHandler<StaticClassTestEvent> handler = e => callCount++;

            StaticValueEventBus<IStaticClassTestEvent>.AddListener(handler);

            try
            {
                var testEvent = new StaticClassTestEvent(111);

                // Act
                var exceptions = new List<Exception>();
                StaticValueEventBus<IStaticClassTestEvent>.Publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IStaticClassTestEvent>.RemoveListener(handler);
            }
        }

        [Test]
        public void DifferentGenericInstances_Isolated()
        {
            // 验证不同泛型实例之间互不干扰
            var callCount1 = 0;
            var callCount2 = 0;

            JiksLib.Control.EventHandler<IsolatedEvent1> handler1 = e => callCount1++;
            JiksLib.Control.EventHandler<IsolatedEvent2> handler2 = e => callCount2++;

            StaticValueEventBus<IIsolatedEvent1>.AddListener(handler1);
            StaticValueEventBus<IIsolatedEvent2>.AddListener(handler2);

            try
            {
                // 发布第一个事件，只应触发handler1
                StaticValueEventBus<IIsolatedEvent1>.Publisher.Publish(new IsolatedEvent1(), null);
                Assert.That(callCount1, Is.EqualTo(1));
                Assert.That(callCount2, Is.EqualTo(0));

                // 发布第二个事件，只应触发handler2
                StaticValueEventBus<IIsolatedEvent2>.Publisher.Publish(new IsolatedEvent2(), null);
                Assert.That(callCount1, Is.EqualTo(1)); // 保持不变
                Assert.That(callCount2, Is.EqualTo(1));
            }
            finally
            {
                StaticValueEventBus<IIsolatedEvent1>.RemoveListener(handler1);
                StaticValueEventBus<IIsolatedEvent2>.RemoveListener(handler2);
            }
        }

        [Test]
        public void PublisherSingleton_AlwaysSameInstance()
        {
            // Arrange
            var publisher1 = StaticValueEventBus<IStaticClassTestEvent>.Publisher.Singleton;
            var publisher2 = StaticValueEventBus<IStaticClassTestEvent>.Publisher.Singleton;

            // Act & Assert
            Assert.That(publisher1, Is.SameAs(publisher2));
        }

        [Test]
        public void PublishUnregisteredEventType_NoException()
        {
            // 测试发布未注册的事件类型（无监听器）
            // 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                var evt = new UnregisteredEvent();
                StaticValueEventBus<IUnregisteredEvent>.Publisher.Publish(evt, null);
            });
        }

        [Test]
        public void ISafeEventPublisherPublish_WithBoxedValue_CallsCorrectListener()
        {
            // 测试通过ISafeEventPublisher接口发布事件（会进行装箱）
            var callCount = 0;
            BoxedTestEvent receivedEvent = default;
            JiksLib.Control.EventHandler<BoxedTestEvent> handler = e =>
            {
                callCount++;
                receivedEvent = e;
            };

            StaticValueEventBus<IBoxedTestEvent>.AddListener(handler);

            try
            {
                var testEvent = new BoxedTestEvent(999);
                var publisher = (ISafeEventPublisher<IBoxedTestEvent>)StaticValueEventBus<IBoxedTestEvent>.Publisher.Singleton;

                // Act
                var exceptions = new List<Exception>();
                publisher.Publish(testEvent, exceptions);

                // Assert
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(receivedEvent.Value, Is.EqualTo(999));
                Assert.That(exceptions.Count, Is.EqualTo(0));
            }
            finally
            {
                StaticValueEventBus<IBoxedTestEvent>.RemoveListener(handler);
            }
        }

        [Test]
        public void WeakPublisherDictionary_CachedAfterFirstListener()
        {
            // 测试weakPublishers字典在添加第一个监听器后被缓存
            var callCount1 = 0;
            var callCount2 = 0;

            JiksLib.Control.EventHandler<CacheTestEvent> handler1 = e => callCount1++;
            JiksLib.Control.EventHandler<CacheTestEvent> handler2 = e => callCount2++;

            // 第一次添加监听器
            StaticValueEventBus<ICacheTestEvent>.AddListener(handler1);

            try
            {
                // 发布事件
                StaticValueEventBus<ICacheTestEvent>.Publisher.Publish(new CacheTestEvent(), null);
                Assert.That(callCount1, Is.EqualTo(1));

                // 移除第一个监听器
                StaticValueEventBus<ICacheTestEvent>.RemoveListener(handler1);

                // 添加第二个监听器（应该使用缓存的weak publisher）
                StaticValueEventBus<ICacheTestEvent>.AddListener(handler2);

                // 发布事件
                StaticValueEventBus<ICacheTestEvent>.Publisher.Publish(new CacheTestEvent(), null);
                Assert.That(callCount2, Is.EqualTo(1));
                Assert.That(callCount1, Is.EqualTo(1)); // 保持不变
            }
            finally
            {
                StaticValueEventBus<ICacheTestEvent>.RemoveListener(handler2);
            }
        }

        [Test]
        public void ManyListeners_AllCalled()
        {
            // 测试大量监听器场景
            const int listenerCount = 100;
            var callCounts = new int[listenerCount];
            var handlers = new List<JiksLib.Control.EventHandler<ManyListenersEvent>>();

            // 创建大量监听器
            for (int i = 0; i < listenerCount; i++)
            {
                int index = i; // 捕获局部变量
                handlers.Add(e => callCounts[index]++);
                StaticValueEventBus<IManyListenersEvent>.AddListener(handlers[i]);
            }

            try
            {
                // 发布事件
                StaticValueEventBus<IManyListenersEvent>.Publisher.Publish(new ManyListenersEvent(), null);

                // 验证所有监听器都被调用
                for (int i = 0; i < listenerCount; i++)
                {
                    Assert.That(callCounts[i], Is.EqualTo(1), $"监听器 {i} 未被调用");
                }
            }
            finally
            {
                // 清理所有监听器
                foreach (var handler in handlers)
                {
                    StaticValueEventBus<IManyListenersEvent>.RemoveListener(handler);
                }
            }
        }


        #endregion
    }
}