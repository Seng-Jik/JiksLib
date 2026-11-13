using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class SparseSetTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_Default_CreatesEmptySet()
        {
            // Arrange & Act
            var set = new SparseSet<int>();

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(-1));
            Assert.That(set.Capacity, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithCapacity_CreatesEmptySetWithCapacity()
        {
            // Arrange & Act
            var set = new SparseSet<int>(10);

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(-1));
            Assert.That(set.Capacity, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_WithZeroCapacity_CreatesEmptySet()
        {
            // Arrange & Act
            var set = new SparseSet<int>(0);

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(-1));
            Assert.That(set.Capacity, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => new SparseSet<int>(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_WithCollection_CopiesAllElements()
        {
            // Arrange
            var source = new[] { 10, 20, 30, 40, 50 };

            // Act
            var set = new SparseSet<int>(source);

            // Assert
            Assert.That(set.Count, Is.EqualTo(5));
            Assert.That(set.MaxIndex, Is.EqualTo(4));
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(10));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(20));
            Assert.That(set.TryIndex(4, out var v4), Is.True);
            Assert.That(v4, Is.EqualTo(50));
        }

        [Test]
        public void Constructor_WithEmptyCollection_CreatesEmptySet()
        {
            // Arrange & Act
            var set = new SparseSet<int>(Array.Empty<int>());

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(-1));
        }

        [Test]
        public void Constructor_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => new SparseSet<int>(null!),
                Throws.ArgumentNullException);
        }

        #endregion

        #region Add Tests

        [Test]
        public void Add_ToEmptySet_ReturnsZeroAndIncrementsCount()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            int index = set.Add(42);

            // Assert
            Assert.That(index, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.TryIndex(0, out var value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Add_MultipleItems_AssignsSequentialIndices()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            int i0 = set.Add(10);
            int i1 = set.Add(20);
            int i2 = set.Add(30);

            // Assert
            Assert.That(i0, Is.EqualTo(0));
            Assert.That(i1, Is.EqualTo(1));
            Assert.That(i2, Is.EqualTo(2));
            Assert.That(set.Count, Is.EqualTo(3));
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(10));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(20));
            Assert.That(set.TryIndex(2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(30));
        }

        [Test]
        public void Add_AfterRemove_ReusesFreedIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2
            set.RemoveAt(1); // 移除 index 1

            // Act
            int newIndex = set.Add(40);

            // Assert
            Assert.That(newIndex, Is.EqualTo(1)); // 复用被释放的下标
            Assert.That(set.Count, Is.EqualTo(3));
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(10));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(40));
            Assert.That(set.TryIndex(2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(30));
        }

        [Test]
        public void Add_TriggersAutoResize_WhenCapacityExceeded()
        {
            // Arrange
            var set = new SparseSet<int>(2);

            // Act
            set.Add(1);
            set.Add(2);
            set.Add(3); // 触发扩容

            // Assert
            Assert.That(set.Count, Is.EqualTo(3));
            Assert.That(set.Capacity, Is.GreaterThanOrEqualTo(4));
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(1));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(2));
            Assert.That(set.TryIndex(2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(3));
        }

        [Test]
        public void Add_WithNullReferenceType_WorksCorrectly()
        {
            // Arrange
            var set = new SparseSet<string?>();

            // Act
            int index = set.Add(null);

            // Assert
            Assert.That(index, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.TryIndex(0, out var value), Is.True);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void Add_ThenUpdateViaIndexer_ReflectsChange()
        {
            // Arrange
            var set = new SparseSet<int>();
            int index = set.Add(10);

            // Act
            set[index] = 99;

            // Assert
            Assert.That(set.TryIndex(index, out var value), Is.True);
            Assert.That(value, Is.EqualTo(99));
        }

        #endregion

        #region Indexer Tests

        [Test]
        public void Indexer_Get_ExistingElement_ReturnsValue()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act & Assert
            Assert.That(set[0], Is.EqualTo(10));
            Assert.That(set[1], Is.EqualTo(20));
            Assert.That(set[2], Is.EqualTo(30));
        }

        [Test]
        public void Indexer_Get_RemovedElement_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.RemoveAt(0);

            // Act & Assert
            Assert.That(
                () => set[0],
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Indexer_Get_NegativeIndex_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act & Assert
            Assert.That(
                () => set[-1],
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Indexer_Get_IndexGreaterThanMaxIndex_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(
                () => set[5],
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void Indexer_Set_ExistingElement_UpdatesValue()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);

            // Act
            set[0] = 99;
            set[1] = 88;

            // Assert
            Assert.That(set[0], Is.EqualTo(99));
            Assert.That(set[1], Is.EqualTo(88));
        }

        [Test]
        public void Indexer_Set_RemovedElement_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.RemoveAt(0);

            // Act & Assert
            Assert.That(
                () => set[0] = 99,
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        #endregion

        #region TryIndex Tests

        [Test]
        public void TryIndex_ExistingElement_ReturnsTrueAndValue()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(42);

            // Act
            bool result = set.TryIndex(0, out int value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void TryIndex_RemovedElement_ReturnsFalseAndDefault()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.RemoveAt(0);

            // Act
            bool result = set.TryIndex(0, out int value);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryIndex_NegativeIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.TryIndex(-1, out int value);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryIndex_IndexGreaterThanMaxIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.TryIndex(100, out int value);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region RemoveAt Tests

        [Test]
        public void RemoveAt_ExistingElement_ReturnsTrueAndDecrementsCount()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            bool result = set.RemoveAt(1);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void RemoveAt_LastElement_ReturnsTrue()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            bool result = set.RemoveAt(2);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.TryIndex(2, out _), Is.False);
        }

        [Test]
        public void RemoveAt_ThenAccessRemovedIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.RemoveAt(0);

            // Act & Assert
            Assert.That(set.TryIndex(0, out _), Is.False);
        }

        [Test]
        public void RemoveAt_OtherElementsStillAccessible_AfterRemoval()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2
            set.Add(40); // index 3

            // Act
            set.RemoveAt(1);

            // Assert
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(10));
            Assert.That(set.TryIndex(2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(30));
            Assert.That(set.TryIndex(3, out var v3), Is.True);
            Assert.That(v3, Is.EqualTo(40));
        }

        [Test]
        public void RemoveAt_IndexGreaterThanMaxIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.RemoveAt(5);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(set.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_NegativeIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.RemoveAt(-1);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(set.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_AlreadyRemovedIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.RemoveAt(0);

            // Act
            bool result = set.RemoveAt(0);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RemoveAt_AllElements_LeavesEmptySet()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            set.RemoveAt(0);
            set.RemoveAt(1);
            set.RemoveAt(2);

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(2)); // MaxIndex 不减少
        }

        #endregion

        #region Remove Tests

        [Test]
        public void Remove_ExistingValue_ReturnsTrueAndDecrementsCount()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            bool result = set.Remove(20);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void Remove_NonExistingValue_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);

            // Act
            bool result = set.Remove(99);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void Remove_FromEmptySet_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            bool result = set.Remove(10);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Remove_WhenMultipleMatching_RemovesOne()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(10); // 重复值

            // Act
            bool result = set.Remove(10);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(set.Count, Is.EqualTo(2));
            // 还有一个 10 存在
            Assert.That(set.Contains(10), Is.True);
        }

        #endregion

        #region IndexOf Tests

        [Test]
        public void IndexOf_ExistingValue_ReturnsSparseIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            int index = set.IndexOf(20);

            // Assert
            Assert.That(index, Is.EqualTo(1));
            Assert.That(set.TryIndex(index, out var value), Is.True);
            Assert.That(value, Is.EqualTo(20));
        }

        [Test]
        public void IndexOf_NonExistingValue_ReturnsNegativeOne()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);

            // Act
            int index = set.IndexOf(99);

            // Assert
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void IndexOf_EmptySet_ReturnsNegativeOne()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            int index = set.IndexOf(10);

            // Assert
            Assert.That(index, Is.EqualTo(-1));
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ExistingValue_ReturnsTrue()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act & Assert
            Assert.That(set.Contains(10), Is.True);
            Assert.That(set.Contains(20), Is.True);
            Assert.That(set.Contains(30), Is.True);
        }

        [Test]
        public void Contains_NonExistingValue_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(set.Contains(99), Is.False);
        }

        [Test]
        public void Contains_EmptySet_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act & Assert
            Assert.That(set.Contains(10), Is.False);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_NonEmptySet_ResetsCountAndMaxIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            set.Clear();

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.MaxIndex, Is.EqualTo(-1));
        }

        [Test]
        public void Clear_ThenAdd_WorksAsNewSet()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Clear();

            // Act
            int index = set.Add(99);

            // Assert
            Assert.That(index, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.TryIndex(0, out var value), Is.True);
            Assert.That(value, Is.EqualTo(99));
        }

        [Test]
        public void Clear_EmptySet_DoesNotThrow()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act & Assert
            Assert.DoesNotThrow(() => set.Clear());
            Assert.That(set.Count, Is.EqualTo(0));
        }

        #endregion

        #region EnsureCapacity Tests

        [Test]
        public void EnsureCapacity_GreaterThanCurrent_IncreasesCapacity()
        {
            // Arrange
            var set = new SparseSet<int>(4);
            set.Add(1);
            set.Add(2);

            // Act
            set.EnsureCapacity(8);

            // Assert
            Assert.That(set.Capacity, Is.GreaterThanOrEqualTo(8));
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(1));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(2));
        }

        [Test]
        public void EnsureCapacity_LessThanOrEqualToCurrent_DoesNotChangeCapacity()
        {
            // Arrange
            var set = new SparseSet<int>(10);
            int originalCapacity = set.Capacity;

            // Act
            set.EnsureCapacity(5);

            // Assert
            Assert.That(set.Capacity, Is.EqualTo(originalCapacity));
        }

        [Test]
        public void EnsureCapacity_Negative_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act & Assert
            Assert.That(
                () => set.EnsureCapacity(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

        #region InnerBufferIndexToSparseSetIndex Tests

        [Test]
        public void InnerBufferIndexToSparseSetIndex_ValidIndex_ReturnsSparseIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // sparse index 0
            set.Add(20); // sparse index 1
            set.Add(30); // sparse index 2

            // Act & Assert
            Assert.That(set.InnerBufferIndexToSparseSetIndex(0), Is.EqualTo(0));
            Assert.That(set.InnerBufferIndexToSparseSetIndex(1), Is.EqualTo(1));
            Assert.That(set.InnerBufferIndexToSparseSetIndex(2), Is.EqualTo(2));
        }

        [Test]
        public void InnerBufferIndexToSparseSetIndex_NegativeIndex_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(
                () => set.InnerBufferIndexToSparseSetIndex(-1),
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void InnerBufferIndexToSparseSetIndex_IndexExceedsCount_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(
                () => set.InnerBufferIndexToSparseSetIndex(5),
                Throws.TypeOf<IndexOutOfRangeException>());
        }

        #endregion

        #region SparseSetIndexToInnerBufferIndex Tests

        [Test]
        public void SparseSetIndexToInnerBufferIndex_ValidIndex_ReturnsTrueAndBufferIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            bool result = set.SparseSetIndexToInnerBufferIndex(1, out int innerIndex);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(innerIndex, Is.EqualTo(1));
        }

        [Test]
        public void SparseSetIndexToInnerBufferIndex_NegativeIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.SparseSetIndexToInnerBufferIndex(-1, out _);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void SparseSetIndexToInnerBufferIndex_IndexGreaterThanMaxIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act
            bool result = set.SparseSetIndexToInnerBufferIndex(100, out _);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void SparseSetIndexToInnerBufferIndex_RemovedIndex_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.RemoveAt(0);

            // Act
            bool result = set.SparseSetIndexToInnerBufferIndex(0, out _);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region UnorderedEnumerator Tests

        [Test]
        public void UnorderedEnumerator_IteratesAllElements()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            // Act
            var list = new List<int>();
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            Assert.That(list, Is.EquivalentTo(new[] { 10, 20, 30 }));
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void UnorderedEnumerator_EmptySet_DoesNotMoveNext()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            var enumerator = set.GetEnumerator();

            // Assert
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void UnorderedEnumerator_AfterRemoval_IteratesRemainingElements()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            set.RemoveAt(1); // 移除 20

            // Act
            var list = new List<int>();
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            Assert.That(list, Is.EquivalentTo(new[] { 10, 30 }));
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public void UnorderedEnumerator_Reset_RestartsEnumeration()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext()) { } // 遍历到底

            // Act
            enumerator.Reset();
            var list = new List<int>();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            Assert.That(list, Is.EquivalentTo(new[] { 10, 20 }));
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public void UnorderedEnumerator_Current_BeforeMoveNext_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            var enumerator = set.GetEnumerator();

            // Act & Assert
            Assert.That(
                () => _ = enumerator.Current,
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void UnorderedEnumerator_Current_AfterEnd_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext()) { }

            // Act & Assert
            Assert.That(
                () => _ = enumerator.Current,
                Throws.TypeOf<InvalidOperationException>());
        }

        #endregion

        #region OrderedEnumerator Tests

        [Test]
        public void OrderedEnumerator_IteratesAllElements_InSparseIndexOrder()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2

            // Act
            var list = new List<int>();
            var enumerator = set.GetOrderedEnumerator;
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            Assert.That(list, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void OrderedEnumerator_EmptySet_DoesNotMoveNext()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            var enumerator = set.GetOrderedEnumerator;

            // Assert
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void OrderedEnumerator_SkipsRemovedGaps()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2
            set.RemoveAt(1); // 移除 index 1，留下空隙
            set.Add(40);       // 复用 index 1

            // Act
            var list = new List<int>();
            var enumerator = set.GetOrderedEnumerator;
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            // 有序迭代按稀疏下标 0, 1, 2 顺序
            Assert.That(list, Is.EqualTo(new[] { 10, 40, 30 }));
        }

        [Test]
        public void OrderedEnumerator_Reset_RestartsEnumeration()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            var enumerator = set.GetOrderedEnumerator;
            while (enumerator.MoveNext()) { }

            // Act
            enumerator.Reset();
            var list = new List<int>();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            // Assert
            Assert.That(list, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void OrderedEnumerator_Current_BeforeMoveNext_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            var enumerator = set.GetOrderedEnumerator;

            // Act & Assert
            Assert.That(
                () => _ = enumerator.Current,
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void OrderedEnumerator_Current_AfterEnd_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            var enumerator = set.GetOrderedEnumerator;
            while (enumerator.MoveNext()) { }

            // Act & Assert
            Assert.That(
                () => _ = enumerator.Current,
                Throws.TypeOf<InvalidOperationException>());
        }

        #endregion

        #region CopyToOrdered Tests

        [Test]
        public void CopyToOrdered_CopiesAllElementsInSparseIndexOrder()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            var target = new int[3];

            // Act
            set.CopyToOrdered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void CopyToOrdered_WithGaps_SkipsGaps()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2
            set.RemoveAt(1); // 移除 index 1
            set.Add(40);       // index 1 被复用为 40
            var target = new int[3];

            // Act
            set.CopyToOrdered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 10, 40, 30 }));
        }

        [Test]
        public void CopyToOrdered_TargetSmallerThanElements_CopiesPartially()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            var target = new int[2];

            // Act
            set.CopyToOrdered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void CopyToOrdered_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(
                () => set.CopyToOrdered(new ArraySegment<int>(null!)),
                Throws.ArgumentNullException);
        }

        [Test]
        public void CopyToOrdered_EmptySet_CopiesNothing()
        {
            // Arrange
            var set = new SparseSet<int>();
            var target = new int[3] { 1, 2, 3 };

            // Act
            set.CopyToOrdered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 1, 2, 3 })); // 原数组未变
        }

        #endregion

        #region CopyToUnordered Tests

        [Test]
        public void CopyToUnordered_CopiesAllElements()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            var target = new int[3];

            // Act
            set.CopyToUnordered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EquivalentTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void CopyToUnordered_TargetSmallerThanCount_CopiesPartially()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            var target = new int[2];

            // Act
            set.CopyToUnordered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target[0], Is.EqualTo(10));
            Assert.That(target[1], Is.EqualTo(20));
        }

        [Test]
        public void CopyToUnordered_WithOffset_CopiesToCorrectPosition()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            var target = new int[4] { -1, -1, -1, -1 };

            // Act
            set.CopyToUnordered(new ArraySegment<int>(target, 1, 2));

            // Assert
            Assert.That(target[0], Is.EqualTo(-1));
            Assert.That(target[1], Is.EqualTo(10));
            Assert.That(target[2], Is.EqualTo(20));
            Assert.That(target[3], Is.EqualTo(-1));
        }

        [Test]
        public void CopyToUnordered_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);

            // Act & Assert
            Assert.That(
                () => set.CopyToUnordered(new ArraySegment<int>(null!)),
                Throws.ArgumentNullException);
        }

        [Test]
        public void CopyToUnordered_EmptySet_CopiesNothing()
        {
            // Arrange
            var set = new SparseSet<int>();
            var target = new int[3] { 1, 2, 3 };

            // Act
            set.CopyToUnordered(new ArraySegment<int>(target));

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        #endregion

        #region ICollection<T> Interface Tests

        [Test]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            // Arrange
            var set = new SparseSet<int>();

            // Act
            bool isReadOnly = ((ICollection<int>)set).IsReadOnly;

            // Assert
            Assert.That(isReadOnly, Is.False);
        }

        [Test]
        public void ICollection_Add_AddsItem()
        {
            // Arrange
            ICollection<int> set = new SparseSet<int>();

            // Act
            set.Add(42);

            // Assert
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.Contains(42), Is.True);
        }

        [Test]
        public void ICollection_CopyTo_CopiesOrderedElements()
        {
            // Arrange
            ICollection<int> set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            var target = new int[3];

            // Act
            set.CopyTo(target, 0);

            // Assert
            Assert.That(target, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void ICollection_CopyTo_NegativeArrayIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ICollection<int> set = new SparseSet<int>();
            set.Add(10);
            var target = new int[1];

            // Act & Assert
            Assert.That(
                () => set.CopyTo(target, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

        #region IEnumerable Tests

        [Test]
        public void IEnumerable_Generic_GetEnumerator_IteratesElements()
        {
            // Arrange
            IEnumerable<int> set = new SparseSet<int>();
            ((ICollection<int>)set).Add(10);
            ((ICollection<int>)set).Add(20);
            ((ICollection<int>)set).Add(30);

            // Act
            var list = new List<int>();
            foreach (var item in set)
                list.Add(item);

            // Assert
            Assert.That(list, Is.EquivalentTo(new[] { 10, 20, 30 }));
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void IEnumerable_NonGeneric_GetEnumerator_IteratesElements()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            IEnumerable nonGeneric = set;

            // Act
            var list = new List<int>();
            foreach (int item in nonGeneric)
                list.Add(item);

            // Assert
            Assert.That(list, Is.EquivalentTo(new[] { 10, 20 }));
            Assert.That(list.Count, Is.EqualTo(2));
        }

        #endregion

        #region Edge Cases and Stress Tests

        [Test]
        public void Remove_LastElementThenAdd_ReusesIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);

            // Act
            set.RemoveAt(1); // 移除最后一个元素
            int newIndex = set.Add(30);

            // Assert
            Assert.That(newIndex, Is.EqualTo(1)); // 复用下标
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(10));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(30));
        }

        [Test]
        public void Remove_FirstElementThenAdd_ReusesIndex()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2

            // Act
            set.RemoveAt(0); // 移除第一个
            int newIndex = set.Add(40);

            // Assert
            Assert.That(newIndex, Is.EqualTo(0)); // 复用下标 0
            Assert.That(set.TryIndex(0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo(40));
            Assert.That(set.TryIndex(1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(20));
            Assert.That(set.TryIndex(2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(30));
        }

        [Test]
        public void StressTest_AddRemoveAlternating_MaintainsIntegrity()
        {
            // Arrange
            var set = new SparseSet<int>();
            var reference = new Dictionary<int, int>(); // sparseIndex → value

            // Act - 反复增删
            for (int i = 0; i < 100; i++)
            {
                int idx = set.Add(i);
                reference[idx] = i;
            }

            // 删除所有偶数下标的元素
            foreach (var kv in reference.ToList())
            {
                if (kv.Key % 2 == 0)
                {
                    Assert.That(set.RemoveAt(kv.Key), Is.True);
                    reference.Remove(kv.Key);
                }
            }

            // Assert - 验证剩余元素
            Assert.That(set.Count, Is.EqualTo(reference.Count));
            foreach (var kv in reference)
            {
                Assert.That(set.TryIndex(kv.Key, out var value), Is.True);
                Assert.That(value, Is.EqualTo(kv.Value));
            }
        }

        [Test]
        public void StressTest_AddRemoveRepeatedly_MaintainsIntegrity()
        {
            // Arrange
            var set = new SparseSet<int>();
            var reference = new Dictionary<int, int>();
            var rand = new Random(42);

            // Act
            for (int i = 0; i < 200; i++)
            {
                if (rand.Next(2) == 0 || reference.Count == 0)
                {
                    // 添加
                    int value = rand.Next(1000);
                    int idx = set.Add(value);
                    reference[idx] = value;
                }
                else
                {
                    // 删除随机一个
                    var keys = reference.Keys.ToArray();
                    int idxToRemove = keys[rand.Next(keys.Length)];
                    Assert.That(set.RemoveAt(idxToRemove), Is.True);
                    reference.Remove(idxToRemove);
                }

                // 始终验证一致性
                Assert.That(set.Count, Is.EqualTo(reference.Count));
            }

            // Assert - 最终验证
            foreach (var kv in reference)
            {
                Assert.That(set.TryIndex(kv.Key, out var value), Is.True);
                Assert.That(value, Is.EqualTo(kv.Value));
            }

            // 验证被删除的下标不可访问
            for (int i = 0; i <= set.MaxIndex; i++)
            {
                if (!reference.ContainsKey(i))
                    Assert.That(set.TryIndex(i, out _), Is.False);
            }
        }

        [Test]
        public void InnerBuffer_AfterRemoval_ContainsValidElementsUpToCount()
        {
            // Arrange
            var set = new SparseSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);
            set.Add(40);
            set.RemoveAt(1); // 移除 20
            set.RemoveAt(2); // 移除 30（注意：移除后 40 被移动）

            // Act & Assert
            Assert.That(set.Count, Is.EqualTo(2));
            // InnerBuffer 前 Count 个元素包含所有有效值
            var buffer = set.InnerBuffer;
            var values = new List<int>();
            for (int i = 0; i < set.Count; i++)
                values.Add(buffer[i]);

            Assert.That(values, Is.EquivalentTo(new[] { 10, 40 }));
        }

        [Test]
        public void SparseSet_WithReferenceType_PreservesIdentity()
        {
            // Arrange
            var obj1 = new object();
            var obj2 = new object();
            var set = new SparseSet<object>();
            int i1 = set.Add(obj1);
            int i2 = set.Add(obj2);

            // Act & Assert
            Assert.That(set.TryIndex(i1, out var r1), Is.True);
            Assert.That(r1, Is.SameAs(obj1));
            Assert.That(set.TryIndex(i2, out var r2), Is.True);
            Assert.That(r2, Is.SameAs(obj2));
        }

        [Test]
        public void SparseSet_WithCustomEqualityComparer_UsesDefaultComparer()
        {
            // Arrange
            var set = new SparseSet<string>();
            set.Add("hello");
            set.Add("world");

            // Act & Assert
            Assert.That(set.Contains("hello"), Is.True);
            Assert.That(set.Contains("HELLO"), Is.False); // 区分大小写
        }

        #endregion
    }
}
