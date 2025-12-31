using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class IntRangeTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidParameters_InitializesProperties()
        {
            // Arrange
            int min = 1;
            bool includeMin = true;
            int max = 10;
            bool includeMax = false;

            // Act
            var range = new IntRange(min, includeMin, max, includeMax);

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
            int min = 10;
            int max = 1;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new IntRange(min, true, max, true));
            Assert.That(ex!.Message, Does.Contain("Min must be less than or equal to Max."));
        }

        [Test]
        public void Constructor_MinEqualMax_DoesNotThrow()
        {
            // Arrange
            int value = 5;

            // Act & Assert
            Assert.DoesNotThrow(() =>
                new IntRange(value, true, value, true));
        }

        [Test]
        public void Constructor_TwoParameterOverload_InitializesWithDefaultInclusion()
        {
            // Arrange
            int minInclude = 1;
            int maxExclude = 10;

            // Act
            var range = new IntRange(minInclude, maxExclude);

            // Assert
            Assert.That(range.Min, Is.EqualTo(minInclude));
            Assert.That(range.IncludeMin, Is.True);
            Assert.That(range.Max, Is.EqualTo(maxExclude));
            Assert.That(range.IncludeMax, Is.False);
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ValueInsideRange_ReturnsTrue()
        {
            // Arrange
            var range = new IntRange(1, true, 10, true);

            // Act & Assert
            Assert.That(range.Contains(5), Is.True);
        }

        [Test]
        public void Contains_ValueBelowRange_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(5, true, 10, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False);
        }

        [Test]
        public void Contains_ValueAboveRange_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(1, true, 5, true);

            // Act & Assert
            Assert.That(range.Contains(10), Is.False);
        }

        [Test]
        public void Contains_ValueAtMinWithIncludeMin_ReturnsTrue()
        {
            // Arrange
            var range = new IntRange(1, true, 10, false);

            // Act & Assert
            Assert.That(range.Contains(1), Is.True);
        }

        [Test]
        public void Contains_ValueAtMinWithoutIncludeMin_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(1, false, 10, true);

            // Act & Assert
            Assert.That(range.Contains(1), Is.False);
        }

        [Test]
        public void Contains_ValueAtMaxWithIncludeMax_ReturnsTrue()
        {
            // Arrange
            var range = new IntRange(1, false, 10, true);

            // Act & Assert
            Assert.That(range.Contains(10), Is.True);
        }

        [Test]
        public void Contains_ValueAtMaxWithoutIncludeMax_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(1, true, 10, false);

            // Act & Assert
            Assert.That(range.Contains(10), Is.False);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithBothIncluded_ReturnsTrue()
        {
            // Arrange
            var range = new IntRange(5, true, 5, true);

            // Act & Assert
            Assert.That(range.Contains(5), Is.True);
        }

        [Test]
        public void Contains_ValueAtMinAndMaxEqualWithoutInclusion_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(5, false, 5, false);

            // Act & Assert
            Assert.That(range.Contains(5), Is.False);
        }

        #endregion

        #region Count Property Tests

        [Test]
        public void Count_RangeWithBothIncluded_ReturnsCorrectCount()
        {
            // Arrange
            var range = new IntRange(1, true, 5, true);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(5)); // 1,2,3,4,5
        }

        [Test]
        public void Count_RangeWithMinIncludedMaxExcluded_ReturnsCorrectCount()
        {
            // Arrange
            var range = new IntRange(1, true, 5, false);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(4)); // 1,2,3,4
        }

        [Test]
        public void Count_RangeWithMinExcludedMaxIncluded_ReturnsCorrectCount()
        {
            // Arrange
            var range = new IntRange(1, false, 5, true);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(4)); // 2,3,4,5
        }

        [Test]
        public void Count_RangeWithBothExcluded_ReturnsCorrectCount()
        {
            // Arrange
            var range = new IntRange(1, false, 5, false);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(3)); // 2,3,4
        }

        [Test]
        public void Count_RangeWithSingleValueIncluded_ReturnsOne()
        {
            // Arrange
            var range = new IntRange(5, true, 5, true);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(1)); // 5
        }

        [Test]
        public void Count_RangeWithSingleValueExcluded_ReturnsZero()
        {
            // Arrange
            var range = new IntRange(5, false, 5, false);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(0));
        }

        [Test]
        public void Count_RangeWithNegativeValues_ReturnsCorrectCount()
        {
            // Arrange
            var range = new IntRange(-5, true, 5, true);

            // Act & Assert
            Assert.That(range.Count, Is.EqualTo(11)); // -5,-4,-3,-2,-1,0,1,2,3,4,5
        }

        #endregion

        #region Enumeration Tests

        [Test]
        public void GetEnumerator_IteratesThroughAllValues()
        {
            // Arrange
            var range = new IntRange(1, true, 5, false);
            var expected = new[] { 1, 2, 3, 4 };

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetEnumerator_RangeWithBothIncluded_IteratesCorrectly()
        {
            // Arrange
            var range = new IntRange(1, true, 3, true);
            var expected = new[] { 1, 2, 3 };

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetEnumerator_RangeWithBothExcluded_IteratesCorrectly()
        {
            // Arrange
            var range = new IntRange(1, false, 4, false);
            var expected = new[] { 2, 3 };

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetEnumerator_RangeWithSingleValueIncluded_IteratesSingleValue()
        {
            // Arrange
            var range = new IntRange(5, true, 5, true);
            var expected = new[] { 5 };

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetEnumerator_RangeWithSingleValueExcluded_IteratesNoValues()
        {
            // Arrange
            var range = new IntRange(5, false, 5, false);

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetEnumerator_RangeWithNegativeValues_IteratesCorrectly()
        {
            // Arrange
            var range = new IntRange(-2, true, 2, true);
            var expected = new[] { -2, -1, 0, 1, 2 };

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetEnumerator_EmptyRange_IteratesNoValues()
        {
            // Arrange
            var range = new IntRange(1, false, 2, false);

            // Act
            var result = range.ToArray();

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region IReadOnlyCollection Implementation Tests

        [Test]
        public void IEnumerableGetEnumerator_ReturnsCorrectEnumerator()
        {
            // Arrange
            var range = new IntRange(1, true, 3, true);
            System.Collections.IEnumerable enumerable = range;

            // Act
            var list = new System.Collections.ArrayList();
            foreach (int item in enumerable)
            {
                list.Add(item);
            }

            // Assert
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Count_MatchesEnumerableCount()
        {
            // Arrange
            var range = new IntRange(1, true, 5, true);

            // Act
            int countFromProperty = range.Count;
            int countFromEnumeration = range.Count();

            // Assert
            Assert.That(countFromProperty, Is.EqualTo(countFromEnumeration));
        }

        #endregion
    }
}