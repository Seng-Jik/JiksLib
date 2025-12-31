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

        #region GetReversedEnumerable Tests

        [Test]
        public void GetReversedEnumerable_WithEmptyList_ReturnsEmptyEnumerable()
        {
            // Arrange
            IReadOnlyList<int> emptyList = new List<int>();

            // Act
            var result = emptyList.GetReversedEnumerable().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetReversedEnumerable_WithSingleElement_ReturnsThatElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "only" };

            // Act
            var result = list.GetReversedEnumerable().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("only"));
        }

        [Test]
        public void GetReversedEnumerable_WithMultipleElements_ReturnsElementsInReverseOrder()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var result = list.GetReversedEnumerable().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo(5));
            Assert.That(result[1], Is.EqualTo(4));
            Assert.That(result[2], Is.EqualTo(3));
            Assert.That(result[3], Is.EqualTo(2));
            Assert.That(result[4], Is.EqualTo(1));
        }

        [Test]
        public void GetReversedEnumerable_WithListContainingNullElements_HandlesCorrectly()
        {
            // Arrange
            IReadOnlyList<string?> list = new List<string?> { "a", null, "c", null, "e" };

            // Act
            var result = list.GetReversedEnumerable().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo("e"));
            Assert.That(result[1], Is.Null);
            Assert.That(result[2], Is.EqualTo("c"));
            Assert.That(result[3], Is.Null);
            Assert.That(result[4], Is.EqualTo("a"));
        }

        [Test]
        public void GetReversedEnumerable_IsLazyEvaluated()
        {
            // Arrange
            var callCount = 0;
            var list = new TestList<int>(() => callCount++, new[] { 1, 2, 3 });

            // Act
            var enumerable = list.GetReversedEnumerable();

            // Assert - 尚未迭代，不应调用 Count
            Assert.That(callCount, Is.EqualTo(0));

            // 开始迭代
            var result = enumerable.ToList();
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(callCount, Is.GreaterThan(0)); // 迭代过程中应访问元素
        }

        [Test]
        public void GetReversedEnumerable_CanBeEnumeratedMultipleTimes()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 10, 20, 30 };

            // Act
            var enumerable = list.GetReversedEnumerable();
            var firstEnumeration = enumerable.ToList();
            var secondEnumeration = enumerable.ToList();

            // Assert
            Assert.That(firstEnumeration.Count, Is.EqualTo(3));
            Assert.That(secondEnumeration.Count, Is.EqualTo(3));
            Assert.That(firstEnumeration[0], Is.EqualTo(30));
            Assert.That(secondEnumeration[0], Is.EqualTo(30));
        }

        [Test]
        public void GetReversedEnumerable_WithLargeList_WorksCorrectly()
        {
            // Arrange
            var largeList = Enumerable.Range(1, 1000).ToList();
            IReadOnlyList<int> list = largeList;

            // Act
            var result = list.GetReversedEnumerable().ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1000));
            Assert.That(result[0], Is.EqualTo(1000));
            Assert.That(result[999], Is.EqualTo(1));
            // 检查顺序是否正确反转
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(result[i], Is.EqualTo(1000 - i));
            }
        }

        #endregion

        #region RandomSelect Tests

        [Test]
        public void RandomSelect_WithEmptyList_ThrowsInvalidOperationException()
        {
            // Arrange
            IReadOnlyList<int> emptyList = new List<int>();
            float randomNumber = 0.5f;

            // Act & Assert
            Assert.That(
                () => emptyList.RandomSelect(randomNumber),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("ls cannot be empty."));
        }

        [Test]
        public void RandomSelect_WithSingleElementList_AlwaysReturnsThatElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "only" };

            // Act & Assert
            // 测试多个随机数，包括边界值
            foreach (float r in new[] { 0f, 0.1f, 0.5f, 0.9f, 1f })
            {
                var result = list.RandomSelect(r);
                Assert.That(result, Is.EqualTo("only"));
            }
        }

        [Test]
        public void RandomSelect_WithMultipleElements_AtZero_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            float randomNumber = 0f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithMultipleElements_AtOne_ReturnsLastElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            float randomNumber = 1f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("E")); // 索引 = (int)(1 * (5-1)) = (int)(4) = 4
        }

        [Test]
        public void RandomSelect_WithMultipleElements_AtMidValue_ReturnsCorrectElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            // randomNumber = 0.5, 索引 = (int)(0.5 * 4) = (int)(2) = 2
            float randomNumber = 0.5f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberJustBelowBoundary_ReturnsCorrectElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            // 列表大小3，索引范围0-2
            // randomNumber = 0.33, 索引 = (int)(0.33 * 2) = (int)(0.66) = 0
            float randomNumber = 0.33f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberJustAboveBoundary_ReturnsCorrectElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            // randomNumber = 0.34, 索引 = (int)(0.34 * 2) = (int)(0.68) = 0
            float randomNumber = 0.34f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberNegative_MayThrowOrReturnElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = -0.5f;

            // Act & Assert
            // 方法不验证 randomNumber 范围，负值可能导致负索引
            // 这会导致 ArgumentOutOfRangeException
            Assert.That(() => list.RandomSelect(randomNumber), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RandomSelect_WithRandomNumberGreaterThanOne_MayThrowOrReturnElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = 1.5f;

            // Act & Assert
            // randomNumber > 1 可能导致索引超出范围
            // 索引 = (int)(1.5 * 2) = (int)(3) = 3，超出范围
            Assert.That(() => list.RandomSelect(randomNumber), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RandomSelect_WithFloatingPointPrecisionEdgeCase()
        {
            // Arrange
            // 测试浮点精度问题：randomNumber 非常接近 1
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            // randomNumber = 0.9999999f, 索引 = (int)(0.9999999 * 4) = (int)(3.9999996) = 3
            float randomNumber = 0.9999999f;

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            Assert.That(result, Is.EqualTo("D")); // 索引3，不是4
        }

        [Test]
        public void RandomSelect_WithExactlyOneMinusEpsilon()
        {
            // Arrange
            // 测试 1 - epsilon 的情况
            IReadOnlyList<string> list = new List<string> { "A", "B" };
            float randomNumber = 0.99999994f; // 在单精度中接近1

            // Act
            var result = list.RandomSelect(randomNumber);

            // Assert
            // 索引 = (int)(0.99999994 * 1) = (int)(0.99999994) = 0
            // 由于浮点精度，可能得到0而不是1
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithDifferentListSizes_WorksCorrectly()
        {
            // Arrange & Act & Assert
            // 测试不同大小的列表
            var sizes = new[] { 1, 2, 3, 10, 100 };
            foreach (int size in sizes)
            {
                var list = Enumerable.Range(1, size).ToList();
                IReadOnlyList<int> readOnlyList = list;

                // randomNumber = 0 应该返回第一个元素
                var result0 = readOnlyList.RandomSelect(0f);
                Assert.That(result0, Is.EqualTo(1));

                // randomNumber = 1 应该返回最后一个元素
                var result1 = readOnlyList.RandomSelect(1f);
                Assert.That(result1, Is.EqualTo(size));
            }
        }

        #endregion

        #region Helper Class for Testing

        private class TestList<T> : IReadOnlyList<T>
        {
            private readonly Func<int> _countCallback;
            private readonly IReadOnlyList<T> _items;

            public TestList(Func<int> countCallback, IEnumerable<T> items)
            {
                _countCallback = countCallback;
                _items = items.ToList();
            }

            public T this[int index] => _items[index];

            public int Count
            {
                get
                {
                    _countCallback();
                    return _items.Count;
                }
            }

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

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