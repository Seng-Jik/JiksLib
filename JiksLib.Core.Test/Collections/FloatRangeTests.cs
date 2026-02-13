using NUnit.Framework;
using JiksLib.Collections;
using System;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class FloatRangeTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidParameters_InitializesProperties()
        {
            // Arrange
            float min = 1.0f;
            bool includeMin = true;
            float max = 10.0f;
            bool includeMax = false;

            // Act
            var range = new FloatRange(min, includeMin, max, includeMax);

            // Assert
            Assert.That(range.Min, Is.EqualTo(min));
            Assert.That(range.IncludeMin, Is.EqualTo(includeMin));
            Assert.That(range.Max, Is.EqualTo(max));
            Assert.That(range.IncludeMax, Is.EqualTo(includeMax));
        }

        [Test]
        public void Constructor_MinGreaterThanMax_ThrowsArgumentException()
        {
            // Arrange
            float min = 10.0f;
            float max = 1.0f;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new FloatRange(min, true, max, true));
            Assert.That(ex!.Message, Does.Contain("Min must be less than or equal to Max."));
        }

        [Test]
        public void Constructor_MinEqualMax_DoesNotThrow()
        {
            // Arrange
            float value = 5.0f;

            // Act & Assert
            Assert.DoesNotThrow(() =>
                new FloatRange(value, true, value, true));
        }

        [Test]
        public void Constructor_MinEqualMaxWithFloatingPointTolerance_DoesNotThrow()
        {
            // Arrange
            float value1 = 5.0f;
            float value2 = 5.000001f; // Very close but not exactly equal

            // Act & Assert
            Assert.DoesNotThrow(() =>
                new FloatRange(value1, true, value2, true));
        }

        [Test]
        public void Constructor_TwoParameterOverload_InitializesWithDefaultInclusion()
        {
            // Arrange
            float minInclude = 1.0f;
            float maxExclude = 10.0f;

            // Act
            var range = new FloatRange(minInclude, maxExclude);

            // Assert
            Assert.That(range.Min, Is.EqualTo(minInclude));
            Assert.That(range.IncludeMin, Is.True);
            Assert.That(range.Max, Is.EqualTo(maxExclude));
            Assert.That(range.IncludeMax, Is.False);
        }

        [Test]
        public void Constructor_WithNegativeValues_InitializesCorrectly()
        {
            // Arrange
            float min = -10.5f;
            bool includeMin = true;
            float max = -1.0f;
            bool includeMax = false;

            // Act
            var range = new FloatRange(min, includeMin, max, includeMax);

            // Assert
            Assert.That(range.Min, Is.EqualTo(min));
            Assert.That(range.IncludeMin, Is.EqualTo(includeMin));
            Assert.That(range.Max, Is.EqualTo(max));
            Assert.That(range.IncludeMax, Is.EqualTo(includeMax));
        }

        [Test]
        public void Constructor_WithFractionalValues_InitializesCorrectly()
        {
            // Arrange
            float min = 0.1f;
            bool includeMin = true;
            float max = 0.9f;
            bool includeMax = false;

            // Act
            var range = new FloatRange(min, includeMin, max, includeMax);

            // Assert
            Assert.That(range.Min, Is.EqualTo(min));
            Assert.That(range.IncludeMin, Is.EqualTo(includeMin));
            Assert.That(range.Max, Is.EqualTo(max));
            Assert.That(range.IncludeMax, Is.EqualTo(includeMax));
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ValueInsideRange_ReturnsTrue()
        {
            // Arrange
            var range = new FloatRange(1.0f, true, 10.0f, true);

            // Act & Assert
            Assert.That(range.Contains(5), Is.True);
        }

        [Test]
        public void Contains_ValueBelowRange_ReturnsFalse()
        {
            // Arrange
            var range = new FloatRange(5.0f, true, 10.0f, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False);
        }

        [Test]
        public void Contains_ValueAboveRange_ReturnsFalse()
        {
            // Arrange
            var range = new FloatRange(1.0f, true, 5.0f, true);

            // Act & Assert
            Assert.That(range.Contains(10), Is.False);
        }

        [Test]
        public void Contains_ValueAtMinWithIncludeMin_ReturnsTrue()
        {
            // Arrange
            var range = new FloatRange(1.0f, true, 10.0f, false);

            // Act & Assert
            Assert.That(range.Contains(1), Is.True);
        }

        [Test]
        public void Contains_ValueAtMinWithoutIncludeMin_ReturnsFalse()
        {
            // Arrange
            var range = new FloatRange(1.0f, false, 10.0f, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False);
        }

        [Test]
        public void Contains_ValueAtMaxWithIncludeMax_ReturnsTrue()
        {
            // Arrange
            var range = new FloatRange(1.0f, false, 10.0f, true);

            // Act & Assert
            Assert.That(range.Contains(10), Is.True);
        }

        [Test]
        public void Contains_ValueAtMaxWithoutIncludeMax_ReturnsFalse()
        {
            // Arrange
            var range = new FloatRange(1.0f, true, 10.0f, false);

            // Act & Assert
            Assert.That(range.Contains(10), Is.False);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithBothIncluded_ReturnsTrue()
        {
            // Arrange
            var range = new FloatRange(5.0f, true, 5.0f, true);

            // Act & Assert
            Assert.That(range.Contains(5), Is.True);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithoutInclusion_ReturnsFalse()
        {
            // Arrange
            var range = new FloatRange(5.0f, false, 5.0f, false);

            // Act & Assert
            Assert.That(range.Contains(5), Is.False);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithFloatingPointValue_ReturnsTrue()
        {
            // Arrange
            var range = new FloatRange(5.5f, true, 5.5f, true);

            // Act & Assert
            // Test that integer values are not contained when range is at 5.5
            Assert.That(range.Contains(5), Is.False); // 5 is not 5.5
            Assert.That(range.Contains(6), Is.False); // 6 is not 5.5
            // Now we can test the exact float value
            Assert.That(range.Contains(5.5f), Is.True);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithFloatingPointValueAndInclusion_ReturnsTrueForExactValue()
        {
            // Arrange
            var range = new FloatRange(5.5f, true, 5.5f, true);

            // Act & Assert
            // Note: Contains now takes float parameter, we can pass 5.5f
            // This test shows that 5 is not contained even though min/max is 5.5
            Assert.That(range.Contains(5), Is.False);
            // Now we can also test the exact float value
            Assert.That(range.Contains(5.5f), Is.True);
        }

        [Test]
        public void Contains_ValueWithNegativeRange_ReturnsCorrectResult()
        {
            // Arrange
            var range = new FloatRange(-10.0f, true, -1.0f, true);

            // Act & Assert
            Assert.That(range.Contains(-5), Is.True);
            Assert.That(range.Contains(-15), Is.False);
            Assert.That(range.Contains(0), Is.False);
        }

        [Test]
        public void Contains_ValueWithFractionalRange_ReturnsCorrectResult()
        {
            // Arrange
            var range = new FloatRange(0.1f, true, 0.9f, true);

            // Act & Assert
            Assert.That(range.Contains(0), Is.False); // 0 < 0.1
            Assert.That(range.Contains(1), Is.False); // 1 > 0.9
            // No integer value between 0.1 and 0.9
        }

        [Test]
        public void Contains_ValueAtBoundaryWithFloatMinAndIntValue_ReturnsCorrectResult()
        {
            // Arrange
            var range = new FloatRange(1.5f, true, 5.5f, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False); // 1 < 1.5
            Assert.That(range.Contains(2), Is.True);  // 2 > 1.5 and 2 < 5.5
            Assert.That(range.Contains(5), Is.True);  // 5 > 1.5 and 5 < 5.5
            Assert.That(range.Contains(6), Is.False); // 6 > 5.5
        }

        [Test]
        public void Contains_ValueAtMinFloatWithIntValue_EdgeCase()
        {
            // Arrange
            var range = new FloatRange(1.0f, true, 5.0f, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.True);  // 1 == 1.0f and IncludeMin is true
            Assert.That(range.Contains(5), Is.True);  // 5 == 5.0f and IncludeMax is true
        }

        [Test]
        public void Contains_ValueAtMinFloatWithIntValueAndExclusion_EdgeCase()
        {
            // Arrange
            var range = new FloatRange(1.0f, false, 5.0f, false);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False); // 1 == 1.0f but IncludeMin is false
            Assert.That(range.Contains(5), Is.False); // 5 == 5.0f but IncludeMax is false
        }

        #endregion

        #region Special Cases

        [Test]
        public void Constructor_WithPositiveInfinity_DoesNotThrow()
        {
            // Arrange
            float min = 0.0f;
            float max = float.PositiveInfinity;

            // Act & Assert
            Assert.DoesNotThrow(() =>
                new FloatRange(min, true, max, true));
        }

        [Test]
        public void Constructor_WithNegativeInfinity_DoesNotThrow()
        {
            // Arrange
            float min = float.NegativeInfinity;
            float max = 0.0f;

            // Act & Assert
            Assert.DoesNotThrow(() =>
                new FloatRange(min, true, max, true));
        }

        [Test]
        public void Constructor_WithNaN_DoesNotThrow()
        {
            // Arrange
            float min = float.NaN;
            float max = float.NaN;

            // Act & Assert
            // NaN comparisons are always false, so min > max is false
            Assert.DoesNotThrow(() =>
                new FloatRange(min, true, max, true));
        }

        [Test]
        public void Contains_WithInfiniteRange_ReturnsCorrectResult()
        {
            // Arrange
            var range = new FloatRange(float.NegativeInfinity, true, float.PositiveInfinity, true);

            // Act & Assert
            Assert.That(range.Contains(int.MinValue), Is.True);
            Assert.That(range.Contains(0), Is.True);
            Assert.That(range.Contains(int.MaxValue), Is.True);
        }

        [Test]
        public void Contains_WithHalfInfiniteRange_ReturnsCorrectResult()
        {
            // Arrange
            var range = new FloatRange(float.NegativeInfinity, true, 100.0f, true);

            // Act & Assert
            Assert.That(range.Contains(int.MinValue), Is.True);
            Assert.That(range.Contains(50), Is.True);
            Assert.That(range.Contains(100), Is.True);
            Assert.That(range.Contains(101), Is.False);
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_IncludesCorrectBrackets()
        {
            // Arrange
            var range1 = new FloatRange(1.0f, true, 5.0f, false);  // [1, 5)
            var range2 = new FloatRange(1.0f, false, 5.0f, true);  // (1, 5]
            var range3 = new FloatRange(1.0f, true, 5.0f, true);   // [1, 5]
            var range4 = new FloatRange(1.0f, false, 5.0f, false); // (1, 5)

            // Act & Assert
            Assert.That(range1.ToString(), Is.EqualTo("[1, 5)"));
            Assert.That(range2.ToString(), Is.EqualTo("(1, 5]"));
            Assert.That(range3.ToString(), Is.EqualTo("[1, 5]"));
            Assert.That(range4.ToString(), Is.EqualTo("(1, 5)"));
        }

        [Test]
        public void ToString_WithNegativeValues_FormatsCorrectly()
        {
            // Arrange
            var range = new FloatRange(-5.5f, true, 5.5f, false);

            // Act & Assert
            Assert.That(range.ToString(), Is.EqualTo("[-5.5, 5.5)"));
        }

        [Test]
        public void ToString_WithFractionalValues_FormatsCorrectly()
        {
            // Arrange
            var range = new FloatRange(0.1f, true, 0.9f, false);

            // Act & Assert
            Assert.That(range.ToString(), Is.EqualTo("[0.1, 0.9)"));
        }

        [Test]
        public void ToString_WithSpecialFloatValues_FormatsCorrectly()
        {
            // Arrange
            var range1 = new FloatRange(float.NaN, true, 5.0f, false);
            var range2 = new FloatRange(float.NegativeInfinity, true, float.PositiveInfinity, true);

            // Act & Assert
            Assert.That(range1.ToString(), Is.EqualTo("[NaN, 5)"));
            Assert.That(range2.ToString(), Is.EqualTo("[-∞, ∞]"));
        }

        #endregion
    }
}