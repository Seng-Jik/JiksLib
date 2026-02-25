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
            Assert.That(list.FirstSlot, Is.EqualTo(-1));
            Assert.That(list.LastSlot, Is.EqualTo(-1));
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
        public void AddFirst_ReturnsSlotIndex()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            int slot1 = list.AddFirst(1);
            int slot2 = list.AddFirst(2);
            int slot3 = list.AddFirst(3);

            // Assert
            Assert.That(list.FirstSlot, Is.EqualTo(slot3)); // 最后一个添加的应该在前面
            Assert.That(list.Get(slot3, out int next3, out int prev3), Is.EqualTo(3));
            Assert.That(prev3, Is.EqualTo(-1)); // 应该是第一个元素
            Assert.That(next3, Is.EqualTo(slot2)); // 应该指向slot2
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
        public void AddLast_ReturnsSlotIndex()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            int slot1 = list.AddLast(1);
            int slot2 = list.AddLast(2);
            int slot3 = list.AddLast(3);

            // Assert
            Assert.That(list.LastSlot, Is.EqualTo(slot3)); // 最后一个添加的应该在后面
            Assert.That(list.Get(slot1, out int next1, out int prev1), Is.EqualTo(1));
            Assert.That(prev1, Is.EqualTo(-1)); // 应该是第一个元素
            Assert.That(next1, Is.EqualTo(slot2)); // 应该指向slot2
        }

        [Test]
        public void Add_CallsAddLast()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();

            // Act
            list.AddLast(1);
            list.AddLast(2);

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
            int firstSlot = list.FirstSlot;

            // Act
            list.AddAfter(firstSlot, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void AddAfter_ReturnsSlotIndex()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            int slot1 = list.AddLast(1);
            list.AddLast(3);

            // Act
            int newSlot = list.AddAfter(slot1, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Get(newSlot, out int nextSlot, out int prevSlot), Is.EqualTo(2));
            Assert.That(prevSlot, Is.EqualTo(slot1)); // 应该在slot1之后
            Assert.That(nextSlot, Is.Not.EqualTo(-1)); // 应该指向原来的第二个元素
        }

        [Test]
        public void AddBefore_AddsItemBeforeSpecifiedSlot()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(3);
            int lastSlot = list.LastSlot;

            // Act
            list.AddBefore(lastSlot, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void AddBefore_ReturnsSlotIndex()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            int slot2 = list.AddLast(3);

            // Act
            int newSlot = list.AddBefore(slot2, 2);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Get(newSlot, out int nextSlot, out int prevSlot), Is.EqualTo(2));
            Assert.That(nextSlot, Is.EqualTo(slot2)); // 应该在slot2之前
            Assert.That(prevSlot, Is.Not.EqualTo(-1)); // 应该指向原来的第一个元素
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
        public void FindSlot_ReturnsSlotIndexOfItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            list.AddLast(2);

            // Act & Assert
            Assert.That(list.FindSlot(2), Is.EqualTo(list.FirstSlot + 1)); // 第一个2的位置
            Assert.That(list.FindSlot(4), Is.EqualTo(-1));
        }

        [Test]
        public void FindLastSlot_ReturnsLastSlotIndexOfItem()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            list.AddLast(2);

            // Act & Assert
            Assert.That(list.FindLastSlot(2), Is.EqualTo(list.LastSlot)); // 最后一个2的位置
            Assert.That(list.FindLastSlot(4), Is.EqualTo(-1));
        }

        [Test]
        public void Get_ReturnsItemWithNeighborSlots()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            int slot1 = list.AddLast(1);
            int slot2 = list.AddLast(2);
            int slot3 = list.AddLast(3);

            // Act
            int value = list.Get(slot2, out int nextSlot, out int prevSlot);

            // Assert
            Assert.That(value, Is.EqualTo(2));
            Assert.That(nextSlot, Is.EqualTo(slot3));
            Assert.That(prevSlot, Is.EqualTo(slot1));
        }

        [Test]
        public void Get_OnFirstSlot_ReturnsCorrectNeighbors()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            int slot1 = list.AddLast(1);
            list.AddLast(2);

            // Act
            int value = list.Get(slot1, out int nextSlot, out int prevSlot);

            // Assert
            Assert.That(value, Is.EqualTo(1));
            Assert.That(nextSlot, Is.Not.EqualTo(-1));
            Assert.That(prevSlot, Is.EqualTo(-1)); // 第一个元素没有前驱
        }

        [Test]
        public void Get_OnLastSlot_ReturnsCorrectNeighbors()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            int slot2 = list.AddLast(2);

            // Act
            int value = list.Get(slot2, out int nextSlot, out int prevSlot);

            // Assert
            Assert.That(value, Is.EqualTo(2));
            Assert.That(nextSlot, Is.EqualTo(-1)); // 最后一个元素没有后继
            Assert.That(prevSlot, Is.Not.EqualTo(-1));
        }

        [Test]
        public void RemoveBySlot_ReturnsItemAndUpdatesNeighbors()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            int slot1 = list.AddLast(1);
            int slot2 = list.AddLast(2);
            int slot3 = list.AddLast(3);

            // Act
            int removed = list.RemoveBySlot(slot2, out int prevSlot, out int nextSlot);

            // Assert
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(prevSlot, Is.EqualTo(slot1));
            Assert.That(nextSlot, Is.EqualTo(slot3));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void RemoveBySlot_OnFirstSlot_UpdatesFirstSlot()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            int slot1 = list.AddLast(1);
            list.AddLast(2);

            // Act
            int removed = list.RemoveBySlot(slot1, out int prevSlot, out int nextSlot);

            // Assert
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(prevSlot, Is.EqualTo(-1));
            Assert.That(list.FirstSlot, Is.Not.EqualTo(slot1));
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveBySlot_OnLastSlot_UpdatesLastSlot()
        {
            // Arrange
            var list = new ArrayLinkedList<int>();
            list.AddLast(1);
            int slot2 = list.AddLast(2);

            // Act
            int removed = list.RemoveBySlot(slot2, out int prevSlot, out int nextSlot);

            // Assert
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(nextSlot, Is.EqualTo(-1));
            Assert.That(list.LastSlot, Is.Not.EqualTo(slot2));
            Assert.That(list.Count, Is.EqualTo(1));
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
            Assert.That(list.FirstSlot, Is.EqualTo(-1));
            Assert.That(list.LastSlot, Is.EqualTo(-1));
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

    }
}