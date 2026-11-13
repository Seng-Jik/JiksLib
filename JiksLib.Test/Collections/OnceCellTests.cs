using NUnit.Framework;
using JiksLib.Collections;
using System;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class OnceCellTests
    {
        [Test]
        public void HasSet_Initially_False()
        {
            // Arrange & Act
            var cell = new OnceCell<int>();

            // Assert
            Assert.That(cell.HasSet, Is.False);
        }

        [Test]
        public void Value_SetFirstTime_Succeeds()
        {
            // Arrange
            var cell = new OnceCell<string>();
            var expected = "test value";

            // Act
            cell.Value = expected;

            // Assert
            Assert.That(cell.HasSet, Is.True);
            Assert.That(cell.Value, Is.EqualTo(expected));
        }

        [Test]
        public void Value_SetSecondTime_ThrowsInvalidOperationException()
        {
            // Arrange
            var cell = new OnceCell<int>();
            cell.Value = 10;

            // Act & Assert
            Assert.That(() => cell.Value = 20, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Value_GetBeforeSet_ThrowsInvalidOperationException()
        {
            // Arrange
            var cell = new OnceCell<string>();

            // Act & Assert
            Assert.That(() => _ = cell.Value, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Value_Set_TriggersOnSetEvent()
        {
            // Arrange
            var cell = new OnceCell<int>();
            var eventTriggered = false;
            int capturedValue = 0;
            cell.OnSet += (value) =>
            {
                eventTriggered = true;
                capturedValue = value;
            };

            // Act
            var newValue = 42;
            cell.Value = newValue;

            // Assert
            Assert.That(eventTriggered, Is.True);
            Assert.That(capturedValue, Is.EqualTo(newValue));
        }

        [Test]
        public void Value_SetWithNull_Succeeds()
        {
            // Arrange
            var cell = new OnceCell<string?>();

            // Act
            cell.Value = null;

            // Assert
            Assert.That(cell.HasSet, Is.True);
            Assert.That(cell.Value, Is.Null);
        }

        [Test]
        public void Value_GetAfterSet_ReturnsCorrectValue()
        {
            // Arrange
            var cell = new OnceCell<double>();
            var expected = 3.14159;
            cell.Value = expected;

            // Act
            var result = cell.Value;

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Value_GetMultipleTimesAfterSet_ReturnsSameValue()
        {
            // Arrange
            var cell = new OnceCell<string>();
            var expected = "once";
            cell.Value = expected;

            // Act
            var result1 = cell.Value;
            var result2 = cell.Value;
            var result3 = cell.Value;

            // Assert
            Assert.That(result1, Is.EqualTo(expected));
            Assert.That(result2, Is.EqualTo(expected));
            Assert.That(result3, Is.EqualTo(expected));
        }

        [Test]
        public void OnSet_Event_OnlyTriggeredOnce()
        {
            // Arrange
            var cell = new OnceCell<int>();
            var eventCount = 0;
            cell.OnSet += (_) => eventCount++;

            // Act - Try to set value (should succeed)
            cell.Value = 10;

            // Try to set value again (should fail and not trigger event)
            try { cell.Value = 20; } catch { }

            // Assert
            Assert.That(eventCount, Is.EqualTo(1));
        }
    }
}