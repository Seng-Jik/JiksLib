using NUnit.Framework;
using JiksLib.Extensions;
using System;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class AnythingExtensionTests
    {
        [Test]
        public void ThrowIfNull_WithNonNullValue_ReturnsValue()
        {
            // Arrange
            string value = "test";

            // Act
            var result = value.ThrowIfNull();

            // Assert
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void ThrowIfNull_WithNonNullValueType_ReturnsValue()
        {
            // Arrange
            int value = 42;

            // Act
            var result = value.ThrowIfNull();

            // Assert
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void ThrowIfNull_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            string? value = null;

            // Act & Assert
            Assert.That(() => value.ThrowIfNull(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ThrowIfNull_WithNullValueAndCustomMessage_ThrowsWithCustomMessage()
        {
            // Arrange
            string? value = null;
            var customMessage = "Custom error message";

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull(customMessage));
            Assert.That(exception.Message, Contains.Substring(customMessage));
        }

        [Test]
        public void ThrowIfNull_WithNullableValueTypeNotNull_ReturnsValue()
        {
            // Arrange
            int? value = 100;

            // Act
            var result = value.ThrowIfNull();

            // Assert
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void ThrowIfNull_WithNullableValueTypeNull_ThrowsArgumentNullException()
        {
            // Arrange
            int? value = null;

            // Act & Assert
            Assert.That(() => value.ThrowIfNull(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ThrowIfNull_WithObjectNotNull_ReturnsObject()
        {
            // Arrange
            var obj = new object();

            // Act
            var result = obj.ThrowIfNull();

            // Assert
            Assert.That(result, Is.SameAs(obj));
        }

        [Test]
        public void ThrowIfNull_WithObjectNull_ThrowsArgumentNullException()
        {
            // Arrange
            object? obj = null;

            // Act & Assert
            Assert.That(() => obj.ThrowIfNull(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ThrowIfNull_WithDefaultMessage_ThrowsWithDefaultMessage()
        {
            // Arrange
            string? value = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull());
            Assert.That(exception.Message, Contains.Substring("Value must not be null."));
        }

        [Test]
        public void ThrowIfNull_ChainUsage_WorksCorrectly()
        {
            // Arrange
            string value = "hello";

            // Act
            var result = value.ThrowIfNull().ToUpper();

            // Assert
            Assert.That(result, Is.EqualTo("HELLO"));
        }
    }
}