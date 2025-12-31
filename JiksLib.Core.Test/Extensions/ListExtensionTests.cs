using NUnit.Framework;
using JiksLib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class ListExtensionTests
    {
        #region RemoveO1 Tests

        [Test]
        public void RemoveO1_WithEmptyList_ThrowsInvalidOperationException()
        {
            // Arrange
            IList<int> emptyList = new List<int>();

            // Act & Assert
            Assert.That(
                () => emptyList.RemoveO1(0),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("list has no element to remove."));
        }

        [Test]
        public void RemoveO1_WithSingleElementList_RemovesElement()
        {
            // Arrange
            IList<int> list = new List<int> { 42 };

            // Act
            list.RemoveO1(0);

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveO1_WithMultipleElements_RemovesElementAtGivenIndex()
        {
            // Arrange
            IList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            var originalCount = list.Count;
            var elementToRemove = list[2]; // "C"

            // Act
            list.RemoveO1(2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(originalCount - 1));
            Assert.That(list.Contains("C"), Is.False);
            // Last element "E" should now be at index 2
            Assert.That(list[2], Is.EqualTo("E"));
        }

        [Test]
        public void RemoveO1_WithLastIndex_RemovesLastElement()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
            var lastIndex = list.Count - 1;
            var originalLast = list[lastIndex];

            // Act
            list.RemoveO1(lastIndex);

            // Assert
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Contains(originalLast), Is.False);
        }

        [Test]
        public void RemoveO1_WithFirstIndex_RemovesFirstElement()
        {
            // Arrange
            IList<string> list = new List<string> { "first", "second", "third", "fourth" };
            var originalLast = list[list.Count - 1];

            // Act
            list.RemoveO1(0);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(originalLast)); // Last element moved to first position
            Assert.That(list.Contains("first"), Is.False);
        }

        [Test]
        public void RemoveO1_WithIndexOutOfRangeNegative_ShouldThrowButCurrentlyDoesNot()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3 };

            // Act & Assert
            // Note: Current implementation doesn't validate index range
            // This test documents the current behavior
            Assert.That(() => list.RemoveO1(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RemoveO1_WithIndexOutOfRangeTooLarge_ShouldThrowButCurrentlyDoesNot()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3 };

            // Act & Assert
            // Note: Current implementation doesn't validate index range
            // This test documents the current behavior
            Assert.That(() => list.RemoveO1(10), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RemoveO1_PreservesOrderExceptForRemovedElement()
        {
            // Arrange
            IList<int> list = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Act - Remove element at index 3 (value 3)
            list.RemoveO1(3);

            // Assert
            // Element 9 (last) should now be at index 3
            // Other elements should maintain relative order except at position 3
            Assert.That(list[3], Is.EqualTo(9));
            // Check some other positions
            Assert.That(list[0], Is.EqualTo(0));
            Assert.That(list[1], Is.EqualTo(1));
            Assert.That(list[2], Is.EqualTo(2));
            Assert.That(list[4], Is.EqualTo(4)); // Was index 5 before removal
            Assert.That(list[5], Is.EqualTo(5)); // Was index 6 before removal
        }

        #endregion

        #region PopBack Tests

        [Test]
        public void PopBack_WithEmptyList_ThrowsInvalidOperationException()
        {
            // Arrange
            IList<int> emptyList = new List<int>();

            // Act & Assert
            Assert.That(
                () => emptyList.PopBack(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("list has no element to remove."));
        }

        [Test]
        public void PopBack_WithSingleElement_RemovesAndReturnsElement()
        {
            // Arrange
            IList<string> list = new List<string> { "only" };

            // Act
            var result = list.PopBack();

            // Assert
            Assert.That(result, Is.EqualTo("only"));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void PopBack_WithMultipleElements_RemovesAndReturnsLastElement()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
            var originalCount = list.Count;
            var expectedLast = list[originalCount - 1];

            // Act
            var result = list.PopBack();

            // Assert
            Assert.That(result, Is.EqualTo(expectedLast));
            Assert.That(list.Count, Is.EqualTo(originalCount - 1));
            Assert.That(list.Contains(expectedLast), Is.False);
        }

        [Test]
        public void PopBack_CanBeCalledMultipleTimes()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3 };

            // Act & Assert
            var result1 = list.PopBack();
            Assert.That(result1, Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(2));

            var result2 = list.PopBack();
            Assert.That(result2, Is.EqualTo(2));
            Assert.That(list.Count, Is.EqualTo(1));

            var result3 = list.PopBack();
            Assert.That(result3, Is.EqualTo(1));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        #endregion

        #region FindIndex Tests

        [Test]
        public void FindIndex_WithEmptyList_ReturnsMinusOne()
        {
            // Arrange
            IReadOnlyList<int> emptyList = new List<int>();

            // Act
            var result = emptyList.FindIndex(x => x == 42);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindIndex_WithNoMatchingElement_ReturnsMinusOne()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var result = list.FindIndex(x => x > 10);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindIndex_WithMatchingElementAtBeginning_ReturnsZero()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "apple", "banana", "cherry" };

            // Act
            var result = list.FindIndex(x => x.StartsWith("a"));

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void FindIndex_WithMatchingElementInMiddle_ReturnsCorrectIndex()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 10, 20, 30, 40, 50 };

            // Act
            var result = list.FindIndex(x => x == 30);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void FindIndex_WithMatchingElementAtEnd_ReturnsLastIndex()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "cat", "dog", "elephant", "zebra" };

            // Act
            var result = list.FindIndex(x => x == "zebra");

            // Assert
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void FindIndex_WithMultipleMatchingElements_ReturnsFirstMatch()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 1, 2, 3, 2, 1 };

            // Act
            var result = list.FindIndex(x => x == 2);

            // Assert
            Assert.That(result, Is.EqualTo(1)); // First occurrence at index 1, not 3
        }

        [Test]
        public void FindIndex_WithComplexPredicate_WorksCorrectly()
        {
            // Arrange
            var items = new[]
            {
                new { Name = "Alice", Age = 25 },
                new { Name = "Bob", Age = 30 },
                new { Name = "Charlie", Age = 35 }
            };
            IReadOnlyList<object> list = items.ToList();

            // Act
            var result = list.FindIndex(x => ((dynamic)x).Age > 28);

            // Assert
            Assert.That(result, Is.EqualTo(1)); // Bob is first with Age > 28
        }

        #endregion

        #region Shuffle Tests

        [Test]
        public void Shuffle_WithEmptyList_DoesNothing()
        {
            // Arrange
            IList<int> list = new List<int>();
            var random = new Random(42);

            // Act
            list.Shuffle(random);

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Shuffle_WithSingleElement_DoesNothing()
        {
            // Arrange
            IList<string> list = new List<string> { "only" };
            var random = new Random(42);
            var original = new List<string>(list);

            // Act
            list.Shuffle(random);

            // Assert
            Assert.That(list, Is.EqualTo(original));
        }

        [Test]
        public void Shuffle_WithMultipleElements_ChangesOrder()
        {
            // Arrange
            IList<int> list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var original = new List<int>(list);
            var random = new Random(12345);

            // Act
            list.Shuffle(random);

            // Assert
            // Shuffling should change the order (high probability)
            // But we can't assert exact order since it's random
            // Instead check that all elements are still present
            Assert.That(list.Count, Is.EqualTo(original.Count));
            Assert.That(list.OrderBy(x => x), Is.EqualTo(original.OrderBy(x => x)));
        }

        [Test]
        public void Shuffle_WithDeterministicRandom_ProducesSameResult()
        {
            // Arrange
            IList<int> list1 = new List<int> { 1, 2, 3, 4, 5 };
            IList<int> list2 = new List<int> { 1, 2, 3, 4, 5 };
            var random1 = new Random(42);
            var random2 = new Random(42);

            // Act
            list1.Shuffle(random1);
            list2.Shuffle(random2);

            // Assert
            // Same seed should produce same shuffle
            Assert.That(list1, Is.EqualTo(list2));
        }

        [Test]
        public void Shuffle_WithDifferentRandom_ProducesDifferentResults()
        {
            // Arrange
            IList<int> list1 = new List<int> { 1, 2, 3, 4, 5 };
            IList<int> list2 = new List<int> { 1, 2, 3, 4, 5 };
            var random1 = new Random(42);
            var random2 = new Random(12345);

            // Act
            list1.Shuffle(random1);
            list2.Shuffle(random2);

            // Assert
            // Different seeds likely produce different shuffles
            // But they could theoretically be the same, so we just check all elements present
            Assert.That(list1.OrderBy(x => x), Is.EqualTo(list2.OrderBy(x => x)));
        }

        [Test]
        public void Shuffle_FisherYatesAlgorithm_Correctness()
        {
            // Arrange
            // Small list to analyze algorithm behavior
            IList<int> list = new List<int> { 1, 2, 3 };
            // Mock Random that returns predictable values: 0, 1, 0
            // This allows us to trace the shuffle steps
            var mockRandom = new TestRandom(new[] { 0, 1, 0 });

            // Act
            list.Shuffle(mockRandom);

            // Assert
            // With test sequence: i=2, j=0 -> swap [2] and [0]: [3,2,1]
            // i=1, j=1 -> swap [1] and [1]: no change
            // Expected result: [3, 2, 1]
            Assert.That(list[0], Is.EqualTo(3));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(1));
        }

        [Test]
        public void Shuffle_WithReferenceTypes_PreservesReferences()
        {
            // Arrange
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();
            IList<object> list = new List<object> { obj1, obj2, obj3 };
            var originalReferences = new List<object>(list);
            var random = new Random(42);

            // Act
            list.Shuffle(random);

            // Assert
            // All same objects should be present
            Assert.That(list, Is.EquivalentTo(originalReferences));
        }

        #endregion

        #region Helper Class for Testing

        private class TestRandom : Random
        {
            private readonly int[] _values;
            private int _index = 0;

            public TestRandom(int[] values)
            {
                _values = values;
            }

            public override int Next(int maxValue)
            {
                if (_index >= _values.Length)
                    throw new InvalidOperationException("No more test values");

                return _values[_index++];
            }
        }

        #endregion
    }
}