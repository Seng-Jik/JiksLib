using NUnit.Framework;
using JiksLib.Collections;
using System;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class CellTests
    {
        [Test]
        public void Constructor_WithValue_SetsValue()
        {
            // Arrange
            var expected = "test value";

            // Act
            var cell = new Cell<string>(expected);

            // Assert
            Assert.That(cell.Value, Is.EqualTo(expected));
        }

        [Test]
        public void Value_Set_UpdatesValue()
        {
            // Arrange
            var cell = new Cell<int>(10);
            var newValue = 20;

            // Act
            cell.Value = newValue;

            // Assert
            Assert.That(cell.Value, Is.EqualTo(newValue));
        }

        [Test]
        public void Value_Set_TriggersOnSetEvent()
        {
            // Arrange
            var cell = new Cell<string>("initial");
            var eventTriggered = false;
            string? capturedValue = null;
            cell.OnSet += (value) =>
            {
                eventTriggered = true;
                capturedValue = value;
            };

            // Act
            var newValue = "new value";
            cell.Value = newValue;

            // Assert
            Assert.That(eventTriggered, Is.True);
            Assert.That(capturedValue, Is.EqualTo(newValue));
        }

        [Test]
        public void Value_SetMultipleTimes_TriggersEventEachTime()
        {
            // Arrange
            var cell = new Cell<int>(1);
            var eventCount = 0;
            cell.OnSet += (_) => eventCount++;

            // Act
            cell.Value = 2;
            cell.Value = 3;
            cell.Value = 4;

            // Assert
            Assert.That(eventCount, Is.EqualTo(3));
        }

        [Test]
        public void Value_Get_ReturnsCurrentValue()
        {
            // Arrange
            var expected = 42;
            var cell = new Cell<int>(expected);

            // Act
            var result = cell.Value;

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

    }
}