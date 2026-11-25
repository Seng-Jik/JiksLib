using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class RingBufferTests
    {
        #region Helpers

        /// <summary>
        /// 断言缓存内容与顺序（下标 0 是最新写入的元素）
        /// </summary>
        private static void AssertContents(RingBuffer<int> ring, params int[] expected)
        {
            Assert.That(ring.Count, Is.EqualTo(expected.Length));
            Assert.That(ring.ToArray(), Is.EqualTo(expected));

            for (int i = 0; i < expected.Length; i++)
                Assert.That(ring[i], Is.EqualTo(expected[i]), $"index {i}");
        }

        /// <summary>
        /// 用 1..count 填充缓存
        /// </summary>
        private static RingBuffer<int> CreateFilled(int capacity)
        {
            var ring = new RingBuffer<int>(capacity);
            for (int i = 1; i <= capacity; i++)
                ring.Add(i);

            return ring;
        }

        #endregion

        #region Constructor

        [Test]
        public void Constructor_WithPositiveCapacity_CreatesEmptyBuffer()
        {
            // Arrange & Act
            var ring = new RingBuffer<int>(3);

            // Assert
            Assert.That(ring.Capacity, Is.EqualTo(3));
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.IsEmpty, Is.True);
            Assert.That(ring.IsFull, Is.False);
            Assert.That(ring, Is.Empty);
        }

        [Test]
        public void Constructor_WithCapacityOne_Works()
        {
            // Arrange & Act
            var ring = new RingBuffer<int>(1);

            // Assert
            Assert.That(ring.Capacity, Is.EqualTo(1));
            Assert.That(ring.Count, Is.EqualTo(0));

            ring.Add(10);

            Assert.That(ring.IsFull, Is.True);
            AssertContents(ring, 10);
        }

        [Test]
        public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<string>(-100));
        }

        #endregion

        #region Add

        [Test]
        public void Add_ToEmptyBuffer_BecomesIndexZero()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);

            // Act
            ring.Add(42);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(1));
            Assert.That(ring.IsEmpty, Is.False);
            Assert.That(ring.IsFull, Is.False);
            AssertContents(ring, 42);
        }

        [Test]
        public void Add_MultipleItems_PutsNewestAtZeroAndOlderItemsAfterIt()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);

            // Act
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Assert
            AssertContents(ring, 30, 20, 10);
        }

        [Test]
        public void Add_WhenFilledToCapacity_ReportsFull()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(3));
            Assert.That(ring.Capacity, Is.EqualTo(3));
            Assert.That(ring.IsFull, Is.True);
            AssertContents(ring, 3, 2, 1);
        }

        [Test]
        public void Add_WhenFull_OverwritesOldestElement()
        {
            // Arrange
            var ring = CreateFilled(3); // 3, 2, 1

            // Act
            ring.Add(4);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(3));
            Assert.That(ring.IsFull, Is.True);
            AssertContents(ring, 4, 3, 2); // 1 被覆盖
        }

        [Test]
        public void Add_MultipleWraps_KeepsNewestCapacityElements()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);

            // Act
            for (int i = 0; i < 100; i++)
                ring.Add(i);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(4));
            AssertContents(ring, 99, 98, 97, 96);
        }

        [Test]
        public void Add_CapacityOne_ReplacesOnlyElement()
        {
            // Arrange
            var ring = new RingBuffer<int>(1);

            // Act
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(1));
            AssertContents(ring, 3);
        }

        [Test]
        public void Add_WithEvicted_WhenNotFull_ReturnsFalseAndDefault()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            bool evicted = ring.Add(5, out var evictedValue);

            // Assert
            Assert.That(evicted, Is.False);
            Assert.That(evictedValue, Is.EqualTo(0));
            AssertContents(ring, 5);
        }

        [Test]
        public void Add_WithEvicted_WhenFull_ReturnsOverwrittenElement()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            bool evicted = ring.Add(4, out var evictedValue);

            // Assert
            Assert.That(evicted, Is.True);
            Assert.That(evictedValue, Is.EqualTo(1));
            AssertContents(ring, 4, 3, 2);
        }

        [Test]
        public void Add_WithEvicted_CapacityOne_ReturnsPreviousElement()
        {
            // Arrange
            var ring = new RingBuffer<int>(1);
            ring.Add(1);

            // Act
            bool evicted = ring.Add(2, out var evictedValue);

            // Assert
            Assert.That(evicted, Is.True);
            Assert.That(evictedValue, Is.EqualTo(1));
            AssertContents(ring, 2);
        }

        #endregion

        #region Indexer

        [Test]
        public void Indexer_ReturnsElementsFromNewestToOldest()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Assert
            Assert.That(ring[0], Is.EqualTo(30));
            Assert.That(ring[1], Is.EqualTo(20));
            Assert.That(ring[2], Is.EqualTo(10));
        }

        [Test]
        public void Indexer_Set_ReplacesElementWithoutChangingOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Act
            ring[0] = 300;
            ring[1] = 200;
            ring[2] = 100;

            // Assert
            AssertContents(ring, 300, 200, 100);
        }

        [Test]
        public void Indexer_SetAfterWrap_ReplacesCorrectElement()
        {
            // Arrange
            var ring = CreateFilled(3); // 3, 2, 1
            ring.Add(4);                // 4, 3, 2
            ring.Add(5);                // 5, 4, 3

            // Act
            ring[1] = 40;

            // Assert
            AssertContents(ring, 5, 40, 3);
        }

        [Test]
        public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = ring[-1];
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = ring[1];
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ring[-1] = 5;
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ring[1] = 5;
            });
        }

        [Test]
        public void Indexer_OnEmptyBuffer_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = ring[0];
            });
        }

        #endregion

        #region TryIndex / TryPeek

        [Test]
        public void TryIndex_ReturnsValueForValidIndex()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Act & Assert
            Assert.That(ring.TryIndex(0, out var newest), Is.True);
            Assert.That(newest, Is.EqualTo(30));

            Assert.That(ring.TryIndex(2, out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(10));
        }

        [Test]
        public void TryIndex_ReturnsFalseForInvalidIndex()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);
            ring.Add(10);

            // Act & Assert
            Assert.That(ring.TryIndex(-1, out var negative), Is.False);
            Assert.That(negative, Is.EqualTo(0));

            Assert.That(ring.TryIndex(1, out var tooLarge), Is.False);
            Assert.That(tooLarge, Is.EqualTo(0));
        }

        [Test]
        public void TryIndex_OnEmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);

            // Act & Assert
            Assert.That(ring.TryIndex(0, out var value), Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryPeekNewest_ReturnsNewestElementWithoutRemovingIt()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);

            // Act
            bool result = ring.TryPeekNewest(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(2));
            AssertContents(ring, 2, 1);
        }

        [Test]
        public void TryPeekOldest_ReturnsOldestElementWithoutRemovingIt()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            bool result = ring.TryPeekOldest(out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(1));
            AssertContents(ring, 3, 2, 1);
        }

        [Test]
        public void TryPeekOldest_AfterWrap_ReturnsOldestElement()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);
            ring.Add(4); // 4, 3, 2（1 已被覆盖）

            // Act & Assert
            Assert.That(ring.TryPeekOldest(out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(2));

            Assert.That(ring.TryPeekNewest(out var newest), Is.True);
            Assert.That(newest, Is.EqualTo(4));

            AssertContents(ring, 4, 3, 2);
        }

        [Test]
        public void TryPeek_OnEmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.That(ring.TryPeekNewest(out var newest), Is.False);
            Assert.That(newest, Is.EqualTo(0));

            Assert.That(ring.TryPeekOldest(out var oldest), Is.False);
            Assert.That(oldest, Is.EqualTo(0));
        }

        [Test]
        public void TryPeek_WithSingleElement_ReturnsSameElementForBothEnds()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(7);

            // Act & Assert
            Assert.That(ring.TryPeekNewest(out var newest), Is.True);
            Assert.That(newest, Is.EqualTo(7));

            Assert.That(ring.TryPeekOldest(out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(7));
        }

        #endregion

        #region TryRemove

        [Test]
        public void TryRemoveNewest_RemovesIndexZeroAndKeepsOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            bool removed = ring.TryRemoveNewest(out var value);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(value, Is.EqualTo(3));
            AssertContents(ring, 2, 1);
        }

        [Test]
        public void TryRemoveOldest_RemovesTailAndKeepsOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            bool removed = ring.TryRemoveOldest(out var value);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(value, Is.EqualTo(1));
            AssertContents(ring, 3, 2);
        }

        [Test]
        public void TryRemove_OnEmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.That(ring.TryRemoveNewest(out var newest), Is.False);
            Assert.That(newest, Is.EqualTo(0));

            Assert.That(ring.TryRemoveOldest(out var oldest), Is.False);
            Assert.That(oldest, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveNewest_UntilEmpty_ThenAddWorks()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            Assert.That(ring.TryRemoveNewest(out var newest), Is.True);
            Assert.That(newest, Is.EqualTo(3));

            Assert.That(ring.TryRemoveNewest(out newest), Is.True);
            Assert.That(newest, Is.EqualTo(2));

            Assert.That(ring.TryRemoveNewest(out newest), Is.True);
            Assert.That(newest, Is.EqualTo(1));

            Assert.That(ring.IsEmpty, Is.True);
            Assert.That(ring.TryRemoveNewest(out _), Is.False);

            ring.Add(4);
            ring.Add(5);

            // Assert
            AssertContents(ring, 5, 4);
        }

        [Test]
        public void TryRemoveOldest_UntilEmpty_ThenAddWorks()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act & Assert
            Assert.That(ring.TryRemoveOldest(out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(1));

            Assert.That(ring.TryRemoveOldest(out oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(2));

            Assert.That(ring.TryRemoveOldest(out oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(3));

            Assert.That(ring.IsEmpty, Is.True);
            Assert.That(ring.TryRemoveOldest(out _), Is.False);

            ring.Add(6);
            ring.Add(7);

            AssertContents(ring, 7, 6);
        }

        [Test]
        public void TryRemove_InterleavedWithAdd_MaintainsOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3); // 3, 2, 1

            // Act & Assert
            Assert.That(ring.TryRemoveNewest(out var newest), Is.True);
            Assert.That(newest, Is.EqualTo(3));
            AssertContents(ring, 2, 1);

            ring.Add(4); // 4, 2, 1
            AssertContents(ring, 4, 2, 1);

            Assert.That(ring.TryRemoveOldest(out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(1));
            AssertContents(ring, 4, 2);

            ring.Add(5); // 5, 4, 2
            AssertContents(ring, 5, 4, 2);

            Assert.That(ring.TryRemoveNewest(out newest), Is.True);
            Assert.That(newest, Is.EqualTo(5));
            AssertContents(ring, 4, 2);
        }

        #endregion

        #region AddRange

        [Test]
        public void AddRange_ToEmptyBuffer_PutsLastElementAtZero()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);

            // Act
            ring.AddRange(new[] { 1, 2, 3 });

            // Assert
            AssertContents(ring, 3, 2, 1);
        }

        [Test]
        public void AddRange_AfterExistingElements_PreservesOverallOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(1);
            ring.Add(2); // 2, 1

            // Act
            ring.AddRange(new[] { 3, 4 });

            // Assert
            AssertContents(ring, 4, 3, 2, 1);
        }

        [Test]
        public void AddRange_WithArraySegmentOffset_CopiesOnlySegment()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            var array = new[] { 99, 1, 2, 3, 98 };

            // Act
            ring.AddRange(new ArraySegment<int>(array, 1, 3));

            // Assert
            AssertContents(ring, 3, 2, 1);
        }

        [Test]
        public void AddRange_ExactlyCapacity_ReplacesAllElements()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1); // 1

            // Act
            ring.AddRange(new[] { 2, 3, 4 });

            // Assert
            Assert.That(ring.IsFull, Is.True);
            AssertContents(ring, 4, 3, 2);
        }

        [Test]
        public void AddRange_LargerThanCapacity_KeepsOnlyLastCapacityElements()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            ring.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7 });

            // Assert
            Assert.That(ring.Count, Is.EqualTo(3));
            AssertContents(ring, 7, 6, 5);
        }

        [Test]
        public void AddRange_LargerThanCapacity_OnWrappedBuffer_KeepsOnlyLastCapacityElements()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.AddRange(new[] { 1, 2, 3, 4, 5 }); // 5, 4, 3, 2, 1
            ring.Add(6);                             // 6, 5, 4, 3, 2
            ring.Add(7);                             // 7, 6, 5, 4, 3

            // Act
            ring.AddRange(new[] { 8, 9, 10, 11, 12, 13, 14, 15 });

            // Assert
            AssertContents(ring, 15, 14, 13, 12, 11);
        }

        [Test]
        public void AddRange_LargerThanCapacity_WithArraySegmentOffset_UsesLastCapacityItems()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            var source = new[] { 99, 1, 2, 3, 4, 5, 98 };

            // Act
            ring.AddRange(new ArraySegment<int>(source, 1, 5)); // 1..5，只保留 3,4,5

            // Assert
            AssertContents(ring, 5, 4, 3);
        }

        [Test]
        public void AddRange_EmptyArray_DoesNothing()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act
            ring.AddRange(Array.Empty<int>());

            // Assert
            AssertContents(ring, 1);
        }

        [Test]
        public void AddRange_EmptyArraySegment_DoesNothing()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act
            ring.AddRange(new ArraySegment<int>(new int[3], 1, 0));

            // Assert
            AssertContents(ring, 1);
        }

        [Test]
        public void AddRange_Enumerable_AddsInOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            IEnumerable<int> items = Enumerable.Range(1, 3).Select(x => x * 10);

            // Act
            ring.AddRange(items);

            // Assert
            AssertContents(ring, 30, 20, 10);
        }

        [Test]
        public void AddRange_EnumerableLargerThanCapacity_KeepsOnlyLastCapacityElements()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            ring.AddRange(Enumerable.Range(1, 10));

            // Assert
            AssertContents(ring, 10, 9, 8);
        }

        [Test]
        public void AddRange_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ring.AddRange((int[])null!));
        }

        [Test]
        public void AddRange_NullEnumerable_ThrowsArgumentNullException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ring.AddRange((IEnumerable<int>)null!));
        }

        [Test]
        public void AddRange_DefaultArraySegment_ThrowsArgumentNullException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ring.AddRange(default(ArraySegment<int>)));
        }

        #endregion

        #region Search / Remove

        [Test]
        public void IndexOf_ReturnsLogicalIndexFromNewestToOldest()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Act & Assert
            Assert.That(ring.IndexOf(30), Is.EqualTo(0));
            Assert.That(ring.IndexOf(20), Is.EqualTo(1));
            Assert.That(ring.IndexOf(10), Is.EqualTo(2));
            Assert.That(ring.IndexOf(99), Is.EqualTo(-1));
        }

        [Test]
        public void IndexOf_WithDuplicates_ReturnsNewestOccurrence()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(10);
            ring.Add(20);
            ring.Add(10);

            // Act & Assert
            Assert.That(ring.IndexOf(10), Is.EqualTo(0));
            Assert.That(ring.LastIndexOf(10), Is.EqualTo(2));
        }

        [Test]
        public void LastIndexOf_ReturnsOldestOccurrence()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(1);
            ring.Add(2);
            ring.Add(1);
            ring.Add(2);

            // Act & Assert
            Assert.That(ring.LastIndexOf(2), Is.EqualTo(2));
            Assert.That(ring.LastIndexOf(1), Is.EqualTo(3));
            Assert.That(ring.LastIndexOf(99), Is.EqualTo(-1));
        }

        [Test]
        public void IndexOf_OnEmptyBuffer_ReturnsMinusOne()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.That(ring.IndexOf(1), Is.EqualTo(-1));
            Assert.That(ring.LastIndexOf(1), Is.EqualTo(-1));
        }

        [Test]
        public void Contains_ReturnsWhetherElementExists()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act & Assert
            Assert.That(ring.Contains(1), Is.True);
            Assert.That(ring.Contains(2), Is.False);
        }

        [Test]
        public void Contains_WithNullReferenceElement_Works()
        {
            // Arrange
            var ring = new RingBuffer<string>(3);
            ring.Add("a");
            ring.Add(null!);

            // Act & Assert
            Assert.That(ring.Contains(null!), Is.True);
            Assert.That(ring.IndexOf(null!), Is.EqualTo(0));
            Assert.That(ring.Contains("a"), Is.True);
        }

        [Test]
        public void Remove_RemovesNewestMatchingElement()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(1);
            ring.Add(2);
            ring.Add(1);
            ring.Add(2); // 2, 1, 2, 1

            // Act
            bool removed = ring.Remove(2);

            // Assert
            Assert.That(removed, Is.True);
            AssertContents(ring, 1, 2, 1);
            Assert.That(ring.IndexOf(2), Is.EqualTo(1));
        }

        [Test]
        public void Remove_NonExistingElement_ReturnsFalseAndDoesNotChangeBuffer()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);

            // Act
            bool removed = ring.Remove(99);

            // Assert
            Assert.That(removed, Is.False);
            AssertContents(ring, 2, 1);
        }

        [Test]
        public void RemoveAt_AtVariousPositions_KeepsOrder()
        {
            // Arrange
            var ring = CreateFilled(5); // 5, 4, 3, 2, 1

            // Act & Assert
            ring.RemoveAt(0); // 移除最新
            AssertContents(ring, 4, 3, 2, 1);

            ring.RemoveAt(2); // 移除中间
            AssertContents(ring, 4, 3, 1);

            ring.RemoveAt(2); // 移除末尾（最旧）
            AssertContents(ring, 4, 3);
        }

        [Test]
        public void RemoveAt_AfterWrap_KeepsOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);
            ring.Add(4); // 4, 3, 2, 1
            ring.Add(5); // 5, 4, 3, 2
            ring.Add(6); // 6, 5, 4, 3

            // Act
            ring.RemoveAt(1);

            // Assert
            AssertContents(ring, 6, 4, 3);
        }

        [Test]
        public void RemoveAt_LastElement_MakesBufferEmptyAndAddWorks()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act
            ring.RemoveAt(0);

            // Assert
            Assert.That(ring.IsEmpty, Is.True);
            Assert.That(ring.Count, Is.EqualTo(0));

            ring.Add(2);
            ring.Add(3);

            AssertContents(ring, 3, 2);
        }

        [Test]
        public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => ring.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ring.RemoveAt(1));
        }

        #endregion

        #region Clear

        [Test]
        public void Clear_RemovesAllElements()
        {
            // Arrange
            var ring = CreateFilled(4); // 4, 3, 2, 1

            // Act
            ring.Clear();

            // Assert
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.IsEmpty, Is.True);
            Assert.That(ring.IsFull, Is.False);
            Assert.That(ring.ToArray(), Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = ring[0];
            });
        }

        [Test]
        public void Clear_EmptyBuffer_DoesNothing()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            ring.Clear();

            // Assert
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.Capacity, Is.EqualTo(3));
        }

        [Test]
        public void Clear_ThenAdd_Works()
        {
            // Arrange
            var ring = CreateFilled(3); // 3, 2, 1

            // Act
            ring.Clear();
            ring.Add(10);
            ring.Add(20);

            // Assert
            AssertContents(ring, 20, 10);
        }

        #endregion

        #region CopyTo / ToArray

        [Test]
        public void CopyTo_ArraySegment_CopiesNewestFirst()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);
            ring.Add(4); // 4, 3, 2, 1

            var destination = new int[6];
            var segment = new ArraySegment<int>(destination, 1, 4);

            // Act
            ring.CopyTo(segment);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 0, 4, 3, 2, 1, 0 }));
        }

        [Test]
        public void CopyTo_AfterWrap_CopiesAcrossBufferBoundary()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            for (int i = 1; i <= 7; i++)
                ring.Add(i); // 7, 6, 5, 4, 3

            var destination = new int[7];

            // Act
            ring.CopyTo(new ArraySegment<int>(destination, 1, 5));

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 0, 7, 6, 5, 4, 3, 0 }));
        }

        [Test]
        public void CopyTo_ArraySegmentTooSmall_ThrowsArgumentException()
        {
            // Arrange
            var ring = CreateFilled(3);
            var destination = new int[4];

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ring.CopyTo(new ArraySegment<int>(destination, 0, 2)));
        }

        [Test]
        public void CopyTo_DefaultArraySegment_ThrowsArgumentNullException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ring.CopyTo(default(ArraySegment<int>)));
        }

        [Test]
        public void CopyTo_ArrayWithIndex_CopiesToSpecifiedPosition()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3); // 3, 2, 1

            var destination = new int[5];

            // Act
            ring.CopyTo(destination, 1);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 0, 3, 2, 1, 0 }));
        }

        [Test]
        public void CopyTo_EmptyBuffer_DoesNotThrow()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            var destination = new int[0];

            // Act & Assert
            Assert.DoesNotThrow(() => ring.CopyTo(destination, 0));
        }

        [Test]
        public void CopyTo_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ring.CopyTo(null!, 0));
        }

        [Test]
        public void CopyTo_NegativeArrayIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => ring.CopyTo(new int[3], -1));
        }

        [Test]
        public void CopyTo_ArrayIndexGreaterThanLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => ring.CopyTo(new int[3], 4));
        }

        [Test]
        public void CopyTo_DestinationTooSmall_ThrowsArgumentException()
        {
            // Arrange
            var ring = CreateFilled(3);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ring.CopyTo(new int[2], 0));
            Assert.Throws<ArgumentException>(() => ring.CopyTo(new int[3], 1));
        }

        [Test]
        public void ToArray_ReturnsElementsNewestFirst()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(10);
            ring.Add(20);
            ring.Add(30);

            // Act
            var result = ring.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 30, 20, 10 }));
        }

        [Test]
        public void ToArray_OnEmptyBuffer_ReturnsEmptyArray()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            var result = ring.ToArray();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ToArray_ReturnsIndependentArray()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);

            // Act
            var result = ring.ToArray();
            result[0] = 99;

            // Assert
            Assert.That(ring[0], Is.EqualTo(2));
        }

        [Test]
        public void ToArray_AfterWrap_ReturnsCurrentContentInOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(4);
            for (int i = 0; i < 10; i++)
                ring.Add(i);

            // Act
            var result = ring.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 9, 8, 7, 6 }));
        }

        #endregion

        #region Enumeration / Interfaces

        [Test]
        public void GetEnumerator_EnumeratesFromNewestToOldest()
        {
            // Arrange
            var ring = new RingBuffer<int>(5);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            // Act
            var values = new List<int>();
            foreach (var value in ring)
                values.Add(value);

            // Assert
            Assert.That(values, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void GetEnumerator_OnEmptyBuffer_YieldsNothing()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);

            // Act
            var values = ring.ToList();

            // Assert
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void GetEnumerator_AfterWrap_EnumeratesInOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            for (int i = 0; i < 7; i++)
                ring.Add(i);

            // Act
            var values = ring.ToList();

            // Assert
            Assert.That(values, Is.EqualTo(new[] { 6, 5, 4 }));
        }

        [Test]
        public void Enumerator_CurrentBeforeStart_ThrowsInvalidOperationException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            var enumerator = ring.GetEnumerator();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = enumerator.Current;
            });
        }

        [Test]
        public void Enumerator_CurrentAfterEnd_ThrowsInvalidOperationException()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);

            var enumerator = ring.GetEnumerator();
            while (enumerator.MoveNext()) { }

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = enumerator.Current;
            });
        }

        [Test]
        public void Enumerator_Reset_RestartsEnumeration()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);

            var enumerator = ring.GetEnumerator();
            var firstPass = new List<int>();
            while (enumerator.MoveNext())
                firstPass.Add(enumerator.Current);

            // Act
            enumerator.Reset();
            var secondPass = new List<int>();
            while (enumerator.MoveNext())
                secondPass.Add(enumerator.Current);

            // Assert
            Assert.That(firstPass, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(secondPass, Is.EqualTo(firstPass));
        }

        [Test]
        public void NonGenericEnumerable_EnumeratesInSameOrder()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);
            ring.Add(3);

            IEnumerable enumerable = ring;
            var values = new List<int>();

            // Act
            foreach (int value in enumerable)
                values.Add(value);

            // Assert
            Assert.That(values, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void AsReadOnly_ReturnsSameInstanceAsReadOnlyList()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            ring.Add(1);
            ring.Add(2);

            // Act
            IReadOnlyList<int> view = ring.AsReadOnly();

            // Assert
            Assert.That(view, Is.SameAs(ring));
            Assert.That(view.Count, Is.EqualTo(2));
            Assert.That(view[0], Is.EqualTo(2));
            Assert.That(view[1], Is.EqualTo(1));
        }

        [Test]
        public void IReadOnlyList_ReflectsBufferChanges()
        {
            // Arrange
            var ring = new RingBuffer<int>(3);
            IReadOnlyList<int> view = ring;
            ring.Add(1);

            // Act
            ring.Add(2);

            // Assert
            Assert.That(view.Count, Is.EqualTo(2));
            Assert.That(view[0], Is.EqualTo(2));
        }

        #endregion

        #region Reference Types

        [Test]
        public void ReferenceType_OverwriteAndRemove_UpdatesElements()
        {
            // Arrange
            var ring = new RingBuffer<string>(2);
            ring.Add("a");
            ring.Add("b");

            // Act
            ring.Add("c");

            // Assert
            Assert.That(ring.ToArray(), Is.EqualTo(new[] { "c", "b" }));

            Assert.That(ring.TryRemoveOldest(out var oldest), Is.True);
            Assert.That(oldest, Is.EqualTo("b"));

            ring.Add("d");

            Assert.That(ring.ToArray(), Is.EqualTo(new[] { "d", "c" }));
        }

        [Test]
        public void Clear_ReferenceTypes_AllowsReuse()
        {
            // Arrange
            var ring = new RingBuffer<string>(3);
            ring.Add("a");
            ring.Add("b");
            ring.Add("c");

            // Act
            ring.Clear();
            ring.Add("d");

            // Assert
            Assert.That(ring.ToArray(), Is.EqualTo(new[] { "d" }));
        }

        #endregion

        #region OnOverwrite

        [Test]
        public void OnOverwrite_WhenNotFull_DoesNotFire()
        {
            var ring = new RingBuffer<int>(3);
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            ring.Add(1);
            ring.Add(2);

            Assert.That(fired, Is.Empty);
        }

        [Test]
        public void OnOverwrite_WhenFull_FiresWithOldestElement()
        {
            var ring = CreateFilled(3); // 3, 2, 1
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            ring.Add(4);

            Assert.That(fired, Is.EqualTo(new[] { 1 }));
            AssertContents(ring, 4, 3, 2);
        }

        [Test]
        public void OnOverwrite_AddWithEvicted_FiresOnce()
        {
            var ring = CreateFilled(2); // 2, 1
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            bool overwritten = ring.Add(3, out var evicted);

            Assert.That(overwritten, Is.True);
            Assert.That(evicted, Is.EqualTo(1));
            Assert.That(fired, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void OnOverwrite_AddRange_PartialOverwrite_FiresOldestFirst()
        {
            var ring = new RingBuffer<int>(5);
            ring.AddRange(new[] { 1, 2, 3, 4 }); // 4, 3, 2, 1
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            ring.AddRange(new[] { 5, 6, 7 }); // 覆盖 1、2

            Assert.That(fired, Is.EqualTo(new[] { 1, 2 }));
            AssertContents(ring, 7, 6, 5, 4, 3);
        }

        [Test]
        public void OnOverwrite_AddRange_LargerThanCapacity_FiresForAllExistingElements()
        {
            var ring = new RingBuffer<int>(3);
            ring.AddRange(new[] { 1, 2, 3 }); // 3, 2, 1
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            ring.AddRange(new[] { 4, 5, 6, 7 });

            Assert.That(fired, Is.EqualTo(new[] { 1, 2, 3 }));
            AssertContents(ring, 7, 6, 5);
        }

        [Test]
        public void OnOverwrite_AddRange_WhenNotFull_DoesNotFire()
        {
            var ring = new RingBuffer<int>(4);
            var fired = new List<int>();
            ring.OnOverwrite += fired.Add;

            ring.AddRange(new[] { 1, 2, 3 });

            Assert.That(fired, Is.Empty);
        }

        [Test]
        public void OnOverwrite_HandlerThrows_ExceptionPropagatesAfterWrite()
        {
            var ring = new RingBuffer<int>(1);
            ring.Add(1);
            ring.OnOverwrite += _ => throw new InvalidOperationException("boom");

            Assert.That(
                () => ring.Add(2),
                Throws.TypeOf<InvalidOperationException>());

            AssertContents(ring, 2);
        }

        #endregion

        #region Stress / Model

        [Test]
        public void Stress_RandomOperations_MatchReferenceModel()
        {
            // Arrange
            const int Capacity = 5;
            const int Operations = 5000;

            var random = new Random(123456);
            var ring = new RingBuffer<int>(Capacity);
            var model = new List<int>(Capacity);
            var rangeBuffer = new int[7];

            for (int step = 0; step < Operations; step++)
            {
                switch (random.Next(9))
                {
                    case 0:
                        {
                            int value = random.Next(20);
                            ring.Add(value);
                            model.Insert(0, value);
                            if (model.Count > Capacity) model.RemoveAt(model.Count - 1);
                            break;
                        }
                    case 1:
                        {
                            int value = random.Next(20);
                            bool expectedEvicted = model.Count == Capacity;
                            int expectedEvictedValue = expectedEvicted
                                ? model[model.Count - 1]
                                : 0;

                            bool evicted = ring.Add(value, out var evictedValue);

                            Assert.That(evicted, Is.EqualTo(expectedEvicted), $"step {step}");
                            Assert.That(
                                evictedValue,
                                Is.EqualTo(expectedEvictedValue),
                                $"step {step}");

                            model.Insert(0, value);
                            if (model.Count > Capacity) model.RemoveAt(model.Count - 1);
                            break;
                        }
                    case 2:
                        {
                            bool removed = ring.TryRemoveNewest(out var value);
                            Assert.That(removed, Is.EqualTo(model.Count > 0), $"step {step}");

                            if (removed)
                            {
                                Assert.That(value, Is.EqualTo(model[0]), $"step {step}");
                                model.RemoveAt(0);
                            }
                            else
                            {
                                Assert.That(value, Is.EqualTo(0), $"step {step}");
                            }

                            break;
                        }
                    case 3:
                        {
                            bool removed = ring.TryRemoveOldest(out var value);
                            Assert.That(removed, Is.EqualTo(model.Count > 0), $"step {step}");

                            if (removed)
                            {
                                Assert.That(
                                    value,
                                    Is.EqualTo(model[model.Count - 1]),
                                    $"step {step}");
                                model.RemoveAt(model.Count - 1);
                            }
                            else
                            {
                                Assert.That(value, Is.EqualTo(0), $"step {step}");
                            }

                            break;
                        }
                    case 4:
                        {
                            if (model.Count > 0)
                            {
                                int index = random.Next(model.Count);
                                Assert.That(
                                    ring.TryIndex(index, out var value),
                                    Is.True,
                                    $"step {step}");
                                Assert.That(value, Is.EqualTo(model[index]), $"step {step}");

                                int newValue = random.Next(20);
                                ring[index] = newValue;
                                model[index] = newValue;
                            }

                            break;
                        }
                    case 5:
                        {
                            int count = random.Next(rangeBuffer.Length + 1);
                            for (int i = 0; i < count; i++)
                                rangeBuffer[i] = random.Next(20);

                            ring.AddRange(new ArraySegment<int>(rangeBuffer, 0, count));

                            for (int i = 0; i < count; i++)
                            {
                                model.Insert(0, rangeBuffer[i]);
                                if (model.Count > Capacity) model.RemoveAt(model.Count - 1);
                            }

                            break;
                        }
                    case 6:
                        {
                            int value = random.Next(20);
                            int expectedIndex = model.IndexOf(value);
                            bool removed = ring.Remove(value);

                            Assert.That(
                                removed,
                                Is.EqualTo(expectedIndex >= 0),
                                $"step {step}");

                            if (expectedIndex >= 0)
                                model.RemoveAt(expectedIndex);

                            break;
                        }
                    case 7:
                        {
                            if (model.Count > 0)
                            {
                                int index = random.Next(model.Count);
                                ring.RemoveAt(index);
                                model.RemoveAt(index);
                            }

                            break;
                        }
                    default:
                        {
                            if (random.Next(5) == 0)
                            {
                                ring.Clear();
                                model.Clear();
                            }

                            break;
                        }
                }

                Assert.That(ring.Count, Is.EqualTo(model.Count), $"step {step}");
                Assert.That(ring.IsEmpty, Is.EqualTo(model.Count == 0), $"step {step}");
                Assert.That(
                    ring.IsFull,
                    Is.EqualTo(model.Count == Capacity),
                    $"step {step}");
                Assert.That(ring.ToArray(), Is.EqualTo(model.ToArray()), $"step {step}");

                for (int i = 0; i < model.Count; i++)
                    Assert.That(ring[i], Is.EqualTo(model[i]), $"step {step}, index {i}");
            }
        }

        [Test]
        public void Stress_RepeatedOverwrite_KeepsNewestElements()
        {
            // Arrange
            const int Capacity = 7;
            const int Operations = 1000;

            var ring = new RingBuffer<int>(Capacity);

            // Act
            for (int i = 0; i < Operations; i++)
                ring.Add(i);

            // Assert
            Assert.That(ring.Count, Is.EqualTo(Capacity));

            for (int i = 0; i < Capacity; i++)
                Assert.That(ring[i], Is.EqualTo(Operations - 1 - i));
        }

        #endregion
    }
}
