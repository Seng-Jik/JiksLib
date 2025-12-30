using NUnit.Framework;
using JiksLib.Control;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class UnitTypeTests
    {
        [Test]
        public void DefaultConstructor_CreatesInstance()
        {
            // Arrange & Act
            var unit = new UnitType();

            // Assert - For value types, we can verify the instance is created
            // by checking self-equality or using other properties
            Assert.That(unit.Equals(unit), Is.True);
        }

        [Test]
        public void Default_ReturnsInstance()
        {
            // Arrange & Act
            var unit = default(UnitType);

            // Assert - For value types, default creates a valid instance
            Assert.That(unit.Equals(unit), Is.True);
        }

        [Test]
        public void Equality_WithDefault_AreEqual()
        {
            // Arrange
            var unit1 = new UnitType();
            var unit2 = new UnitType();

            // Act & Assert
            Assert.That(unit1.Equals(unit2), Is.True);
        }

        [Test]
        public void GetHashCode_ReturnsSameValueForDifferentInstances()
        {
            // Arrange
            var unit1 = new UnitType();
            var unit2 = new UnitType();

            // Act
            var hashCode1 = unit1.GetHashCode();
            var hashCode2 = unit2.GetHashCode();

            // Assert
            Assert.That(hashCode1, Is.EqualTo(hashCode2));
        }

        [Test]
        public void ToString_ReturnsExpectedValue()
        {
            // Arrange
            var unit = new UnitType();

            // Act
            var result = unit.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("JiksLib.Control.UnitType"));
        }
    }
}