using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class ArrayLinkedListTests
    {
        [Test]
        public void Constructor_CreatesEmptyList()
        {
            // Arrange & Act
            var list = new ArrayLinkedList<int>();

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list, Is.Empty);
            Assert.That(list.FirstSlotIndex, Is.EqualTo(-1));
            Assert.That(list.LastSlotIndex, Is.EqualTo(-1));
            Assert.That(list.Capacity, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithCapacity_CreatesWithSpecifiedCapacity()
        {
            // Arrange & Act
            var list = new ArrayLinkedList<int>(10);

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayLinkedList<int>(-1));
        }

        [Test]
        public void AddFirst_AddsItemToFront()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            list.AddFirst(1);
            list.AddFirst(2);
            list.AddFirst(3);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void AddLast_AddsItemToBack()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Add_CallsAddLast()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            list.Add(1);
            list.Add(2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void AddAfter_AddsItemAfterSpecifiedSlot()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(3);
            int firstSlot = list.FirstSlotIndex;

            // Act
            list.AddAfter(firstSlot, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void AddBefore_AddsItemBeforeSpecifiedSlot()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(3);
            int lastSlot = list.LastSlotIndex;

            // Act
            list.AddBefore(lastSlot, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Indexer_Get_ReturnsCorrectItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act & Assert
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
        }

        [Test]
        public void Indexer_Set_UpdatesItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act
            list[1] = 99;

            // Assert
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 99, 3 }));
        }

        [Test]
        public void Indexer_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[1]);
        }

        [Test]
        public void Contains_ReturnsTrueWhenItemExists()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act & Assert
            Assert.That(list.Contains(2), Is.True);
            Assert.That(list.Contains(4), Is.False);
        }

        [Test]
        public void Find_ReturnsIndexOfItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            list.AddLast(2);

            // Act & Assert
            Assert.That(list.Find(2), Is.EqualTo(1));
            Assert.That(list.Find(4), Is.EqualTo(-1));
        }

        [Test]
        public void FindLast_ReturnsLastIndexOfItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            list.AddLast(2);

            // Act & Assert
            Assert.That(list.FindLast(2), Is.EqualTo(3));
            Assert.That(list.FindLast(4), Is.EqualTo(-1));
        }

        [Test]
        public void Remove_RemovesFirstOccurrence()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            list.AddLast(2);

            // Act
            bool removed = list.Remove(2);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3, 2 }));
        }

        [Test]
        public void Remove_WhenItemNotFound_ReturnsFalse()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);

            // Act
            bool removed = list.Remove(3);

            // Assert
            Assert.That(removed, Is.False);
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryRemoveFirst_RemovesAndReturnsFirstItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act
            bool success = list.TryRemoveFirst(out int item);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(item, Is.EqualTo(1));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void TryRemoveFirst_OnEmptyList_ReturnsFalse()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            bool success = list.TryRemoveFirst(out int item);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(item, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveLast_RemovesAndReturnsLastItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act
            bool success = list.TryRemoveLast(out int item);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(item, Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TryRemoveLast_OnEmptyList_ReturnsFalse()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            bool success = list.TryRemoveLast(out int item);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(item, Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            // Act
            list.Clear();

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list, Is.Empty);
            Assert.That(list.FirstSlotIndex, Is.EqualTo(-1));
            Assert.That(list.LastSlotIndex, Is.EqualTo(-1));
        }

        [Test]
        public void CopyTo_CopiesToArray()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            int[] array = new int[5];

            // Act
            list.CopyTo(array, 1);

            // Assert
            Assert.That(array, Is.EqualTo(new[] { 0, 1, 2, 3, 0 }));
        }

        [Test]
        public void CopyTo_ArraySegment_CopiesToArraySegment()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            int[] array = new int[5];
            var segment = new ArraySegment<int>(array, 1, 3);

            // Act
            list.CopyTo(segment);

            // Assert
            Assert.That(array, Is.EqualTo(new[] { 0, 1, 2, 3, 0 }));
        }

        [Test]
        public void EnsureCapacity_IncreasesCapacityWhenNeeded()
        {
            // Arrange
            var list = new ArrayLinkedList<int>(2);
            list.AddLast(1);
            list.AddLast(2);

            // Act
            list.EnsureCapacity(10);

            // Assert
            Assert.That(list.Capacity, Is.GreaterThanOrEqualTo(10));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TrimExcess_ReducesCapacityWhenAppropriate()
        {
            // Arrange
            var list = new ArrayLinkedList<int>(20);
            for (int i = 0; i < 5; i++)
                list.AddLast(i);

            // Act
            list.TrimExcess();

            // Assert
            Assert.That(list.Capacity, Is.LessThanOrEqualTo(10)); // Should be around Count
            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }

        [Test]
        public void GetEnumerator_EnumeratesAllItems()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            var result = new List<int>();

            // Act
            foreach (var item in list)
                result.Add(item);

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void GetReversedEnumerator_EnumeratesInReverse()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            var result = new List<int>();

            // Act
            using (var enumerator = list.GetReversedEnumerator())
            {
                while (enumerator.MoveNext())
                    result.Add(enumerator.Current);
            }

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Constructor_FromOtherArrayLinkedList_CreatesShallowCopy()
        {
            // Arrange
            var original = new ArrayLinkedList<int>();
            original.AddLast(1);
            original.AddLast(2);
            original.AddLast(3);

            // Act
            var copy = new ArrayLinkedList<int>(original);

            // Assert
            Assert.That(copy.Count, Is.EqualTo(3));
            Assert.That(copy.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));

            // Modify original and ensure copy is not affected
            original.AddLast(4);
            Assert.That(copy.Count, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_FromCollection_CreatesListWithItems()
        {
            // Arrange
            var collection = new List<int> { 1, 2, 3 };

            // Act
            var list = new ArrayLinkedList<int>((IEnumerable<int>)collection);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void AsReadOnly_ReturnsReadOnlyCollection()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);

            // Act
            var readOnly = list.AsReadOnly();

            // Assert
            Assert.That(readOnly.Count, Is.EqualTo(2));
            Assert.That(readOnly, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void ICollection_InterfaceImplementation()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            System.Collections.ICollection collection = list;

            // Act & Assert
            Assert.That(collection.IsSynchronized, Is.False);
            Assert.That(collection.SyncRoot, Is.SameAs(list));

            Array array = new int[5];
            collection.CopyTo(array, 1);
            Assert.That(array, Is.EqualTo(new object[] { 0, 1, 2, 0, 0 }));
        }
    }
}