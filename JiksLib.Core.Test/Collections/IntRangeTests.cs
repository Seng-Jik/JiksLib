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

        #region Equality Tests

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(1, true, 5, false);

            // Act & Assert
            Assert.That(range1.Equals(range2), Is.True);
            Assert.That(range1.Equals((object)range2), Is.True);
        }

        [Test]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(2, true, 5, false);
            var range3 = new IntRange(1, false, 5, false);
            var range4 = new IntRange(1, true, 6, false);
            var range5 = new IntRange(1, true, 5, true);

            // Act & Assert
            Assert.That(range1.Equals(range2), Is.False);
            Assert.That(range1.Equals(range3), Is.False);
            Assert.That(range1.Equals(range4), Is.False);
            Assert.That(range1.Equals(range5), Is.False);
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            // Arrange
            var range = new IntRange(1, true, 5, false);

            // Act & Assert
            Assert.That(range.Equals(null), Is.False);
        }

        [Test]
        public void GetHashCode_SameValues_SameHashCode()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(1, true, 5, false);

            // Act
            var hash1 = range1.GetHashCode();
            var hash2 = range2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void GetHashCode_DifferentValues_DifferentHashCode()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(2, true, 5, false);
            var range3 = new IntRange(1, false, 5, false);
            var range4 = new IntRange(1, true, 6, false);
            var range5 = new IntRange(1, true, 5, true);

            // Act
            var hash1 = range1.GetHashCode();
            var hash2 = range2.GetHashCode();
            var hash3 = range3.GetHashCode();
            var hash4 = range4.GetHashCode();
            var hash5 = range5.GetHashCode();

            // Assert
            // Hash codes should ideally be different, but collisions can happen
            // At least verify they are not all equal
            Assert.That(hash1, Is.Not.EqualTo(hash2));
            Assert.That(hash1, Is.Not.EqualTo(hash3));
            Assert.That(hash1, Is.Not.EqualTo(hash4));
            Assert.That(hash1, Is.Not.EqualTo(hash5));
        }

        [Test]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(1, true, 5, false);

            // Act & Assert
            Assert.That(range1 == range2, Is.True);
        }

        [Test]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(2, true, 5, false);

            // Act & Assert
            Assert.That(range1 == range2, Is.False);
        }

        [Test]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(1, true, 5, false);

            // Act & Assert
            Assert.That(range1 != range2, Is.False);
        }

        [Test]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);
            var range2 = new IntRange(2, true, 5, false);

            // Act & Assert
            Assert.That(range1 != range2, Is.True);
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_IncludesCorrectBrackets()
        {
            // Arrange
            var range1 = new IntRange(1, true, 5, false);  // [1, 5)
            var range2 = new IntRange(1, false, 5, true);  // (1, 5]
            var range3 = new IntRange(1, true, 5, true);   // [1, 5]
            var range4 = new IntRange(1, false, 5, false); // (1, 5)

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
            var range = new IntRange(-5, true, 5, false);

            // Act & Assert
            Assert.That(range.ToString(), Is.EqualTo("[-5, 5)"));
        }

        [Test]
        public void ToString_SingleValueIncluded_FormatsCorrectly()
        {
            // Arrange
            var range = new IntRange(5, true, 5, true);

            // Act & Assert
            Assert.That(range.ToString(), Is.EqualTo("[5, 5]"));
        }

        #endregion
    }
}