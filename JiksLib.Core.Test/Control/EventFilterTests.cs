using JiksLib.Control;
using JiksLib.Core.Control;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class EventFilterTests
    {
        sealed class MockPublisher : ISafeEventPublisher<int>
        {
            public int? PublishedEvent { get; private set; }
            public IList<Exception>? ExceptionsOutput { get; private set; }
            public int PublishCallCount { get; private set; }

            public void Publish(int @event, IList<Exception>? exceptionsOutput)
            {
                PublishedEvent = @event;
                ExceptionsOutput = exceptionsOutput;
                PublishCallCount++;
            }
        }

        sealed class MockRefPublisher : ISafeEventPublisher<MutableEvent>
        {
            public MutableEvent? PublishedEvent { get; private set; }
            public int PublishCallCount { get; private set; }

            public void Publish(MutableEvent @event, IList<Exception>? exceptionsOutput)
            {
                PublishedEvent = @event;
                PublishCallCount++;
            }
        }

        sealed class MutableEvent
        {
            public int Value;
        }

        #region 构造函数测试

        [Test]
        public void Constructor_WithPublisherAndFilter_DoesNotThrow()
        {
            // Arrange
            var publisher = new MockPublisher();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _ = new EventFilter<MockPublisher, int>(
                    publisher,
                    static (ref int e) => true);
            });
        }

        #endregion

        #region Publish - 过滤器返回 true

        [Test]
        public void Publish_FilterReturnsTrue_ForwardsEventToInnerPublisher()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                static (ref int e) => true);
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(42, exceptions);

            // Assert
            Assert.That(publisher.PublishCallCount, Is.EqualTo(1));
            Assert.That(publisher.PublishedEvent, Is.EqualTo(42));
        }

        [Test]
        public void Publish_FilterReturnsTrue_PassesExceptionsOutputToInnerPublisher()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                static (ref int e) => true);
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(10, exceptions);

            // Assert
            Assert.That(publisher.ExceptionsOutput, Is.SameAs(exceptions));
        }

        [Test]
        public void Publish_FilterReturnsTrue_WithNullExceptionsOutput_DoesNotThrow()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                static (ref int e) => true);

            // Act & Assert
            Assert.DoesNotThrow(() => filter.Publish(99, null));
            Assert.That(publisher.PublishCallCount, Is.EqualTo(1));
        }

        #endregion

        #region Publish - 过滤器返回 false

        [Test]
        public void Publish_FilterReturnsFalse_DoesNotForwardEvent()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                static (ref int e) => false);
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(42, exceptions);

            // Assert
            Assert.That(publisher.PublishCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Publish_FilterReturnsFalse_WithNullExceptionsOutput_DoesNotThrow()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                static (ref int e) => false);

            // Act & Assert
            Assert.DoesNotThrow(() => filter.Publish(99, null));
            Assert.That(publisher.PublishCallCount, Is.EqualTo(0));
        }

        #endregion

        #region Publish - 过滤器修改事件

        [Test]
        public void Publish_FilterModifiesEvent_ForwardsModifiedEvent()
        {
            // Arrange
            var publisher = new MockRefPublisher();
            var filter = new EventFilter<MockRefPublisher, MutableEvent>(
                publisher,
                (ref MutableEvent e) =>
                {
                    e.Value = 100;
                    return true;
                });
            var evt = new MutableEvent { Value = 1 };
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(evt, exceptions);

            // Assert
            Assert.That(publisher.PublishCallCount, Is.EqualTo(1));
            Assert.That(publisher.PublishedEvent, Is.Not.Null);
            Assert.That(publisher.PublishedEvent!.Value, Is.EqualTo(100));
        }

        [Test]
        public void Publish_FilterModifiesStructEvent_ForwardsModifiedStruct()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) =>
                {
                    e = 999;
                    return true;
                });
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(1, exceptions);

            // Assert
            Assert.That(publisher.PublishedEvent, Is.EqualTo(999));
        }

        [Test]
        public void Publish_FilterModifiesEventAndReturnsFalse_DoesNotForward()
        {
            // Arrange
            var publisher = new MockRefPublisher();
            var filter = new EventFilter<MockRefPublisher, MutableEvent>(
                publisher,
                (ref MutableEvent e) =>
                {
                    e.Value = 200;
                    return false;
                });
            var evt = new MutableEvent { Value = 1 };
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(evt, exceptions);

            // Assert
            Assert.That(publisher.PublishCallCount, Is.EqualTo(0));
        }

        #endregion

        #region Publish - 过滤器抛出异常

        [Test]
        public void Publish_FilterThrowsException_AddsExceptionToOutput()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filterException = new InvalidOperationException("filter error");
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) => throw filterException);
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(42, exceptions);

            // Assert
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.That(exceptions[0], Is.SameAs(filterException));
        }

        [Test]
        public void Publish_FilterThrowsException_DoesNotForwardEvent()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) => throw new InvalidOperationException());
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(42, exceptions);

            // Assert
            Assert.That(publisher.PublishCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Publish_FilterThrowsException_WithNullExceptionsOutput_DoesNotThrow()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) => throw new InvalidOperationException());

            // Act & Assert
            Assert.DoesNotThrow(() => filter.Publish(42, null));
            Assert.That(publisher.PublishCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Publish_FilterThrowsException_DoesNotAffectSubsequentPublish()
        {
            // Arrange
            var publisher = new MockPublisher();
            var callCount = 0;
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException();
                    return true;
                });
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(1, exceptions);
            filter.Publish(2, exceptions);

            // Assert
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.That(publisher.PublishCallCount, Is.EqualTo(1));
            Assert.That(publisher.PublishedEvent, Is.EqualTo(2));
        }

        #endregion

        #region Publish - 异常收集综合场景

        [Test]
        public void Publish_MultipleExceptions_CollectsAll()
        {
            // Arrange
            var publisher = new MockPublisher();
            var filter = new EventFilter<MockPublisher, int>(
                publisher,
                (ref int e) => throw new InvalidOperationException("error"));
            var exceptions = new List<Exception>();

            // Act
            filter.Publish(1, exceptions);
            filter.Publish(2, exceptions);

            // Assert
            Assert.That(exceptions, Has.Count.EqualTo(2));
        }

        #endregion

        #region 值类型特性测试

        [Test]
        public void EventFilter_IsReadonlyStruct()
        {
            // Assert
            Assert.That(typeof(EventFilter<ISafeEventPublisher<int>, int>)
                .IsValueType, Is.True);
        }

        #endregion
    }
}
