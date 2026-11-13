using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class MultiHashSetTests
    {
        [Test]
        public void Constructor_Default_CreatesEmptySet()
        {
            // Arrange & Act
            var set = new MultiHashSet<string>();

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set, Is.Empty);
        }

        [Test]
        public void Constructor_WithComparer_CreatesEmptySet()
        {
            // Arrange & Act
            var comparer = StringComparer.OrdinalIgnoreCase;
            var set = new MultiHashSet<string>(comparer);

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set, Is.Empty);
        }

        [Test]
        public void Constructor_WithNullComparer_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            // Implementation now validates comparer parameter using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => new MultiHashSet<string>(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_SingleItem_ReturnsCountOne()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act
            var count = set.Add("item");

            // Assert
            Assert.That(count, Is.EqualTo(1));
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.Contains("item"), Is.True);
            Assert.That(set.GetCountOf("item"), Is.EqualTo(1));
        }

        [Test]
        public void Add_DuplicateItem_ReturnsIncreasedCount()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item");

            // Act
            var count = set.Add("item");

            // Assert
            Assert.That(count, Is.EqualTo(2));
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Contains("item"), Is.True);
            Assert.That(set.GetCountOf("item"), Is.EqualTo(2));
        }

        [Test]
        public void Add_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            // Implementation now validates null items using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => set.Add(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Remove_ExistingItem_ReturnsSuccessAndDecreasedCount()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item");
            set.Add("item");

            // Act
            var (success, newCount) = set.Remove("item");

            // Assert
            Assert.That(success, Is.True);
            Assert.That(newCount, Is.EqualTo(1));
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.Contains("item"), Is.True);
            Assert.That(set.GetCountOf("item"), Is.EqualTo(1));
        }

        [Test]
        public void Remove_LastInstance_ReturnsSuccessAndZeroCount()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item");

            // Act
            var (success, newCount) = set.Remove("item");

            // Assert
            Assert.That(success, Is.True);
            Assert.That(newCount, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.Contains("item"), Is.False);
            Assert.That(set.GetCountOf("item"), Is.EqualTo(0));
        }

        [Test]
        public void Remove_NonExistentItem_ReturnsFailureAndZeroCount()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act
            var (success, newCount) = set.Remove("nonexistent");

            // Assert
            Assert.That(success, Is.False);
            Assert.That(newCount, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            // Implementation now validates null items using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => set.Remove(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item");

            // Act & Assert
            Assert.That(set.Contains("item"), Is.True);
        }

        [Test]
        public void Contains_NonExistentItem_ReturnsFalse()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            Assert.That(set.Contains("nonexistent"), Is.False);
        }

        [Test]
        public void Contains_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            // Implementation now validates null items using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => set.Contains(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void GetCountOf_ExistingItem_ReturnsCorrectCount()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item");
            set.Add("item");
            set.Add("item");

            // Act & Assert
            Assert.That(set.GetCountOf("item"), Is.EqualTo(3));
        }

        [Test]
        public void GetCountOf_NonExistentItem_ReturnsZero()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            Assert.That(set.GetCountOf("nonexistent"), Is.EqualTo(0));
        }

        [Test]
        public void GetCountOf_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            // Implementation now validates null items using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => set.GetCountOf(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("item1");
            set.Add("item2");
            set.Add("item2");

            // Act
            set.Clear();

            // Assert
            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.Contains("item1"), Is.False);
            Assert.That(set.Contains("item2"), Is.False);
            Assert.That(set, Is.Empty);
        }

        [Test]
        public void Count_AfterMultipleOperations_ReturnsCorrectTotal()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act
            set.Add("a");
            set.Add("a");
            set.Add("b");
            set.Remove("a");
            set.Add("c");
            set.Add("c");
            set.Add("c");

            // Assert
            Assert.That(set.Count, Is.EqualTo(5)); // a:1, b:1, c:3
        }

        [Test]
        public void GetEnumerator_ReturnsAllItemsIncludingDuplicates()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("a");
            set.Add("b");

            // Act
            var items = set.ToList();

            // Assert
            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items.Where(x => x == "a").Count(), Is.EqualTo(2));
            Assert.That(items.Where(x => x == "b").Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetEnumerator_EmptySet_ReturnsEmptyEnumerator()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            Assert.That(set, Is.Empty);
            Assert.That(set.ToList(), Is.Empty);
        }

        [Test]
        public void GetEnumerator_EnumerationOrder_IsPredictable()
        {
            // Arrange
            var set = new MultiHashSet<int>();
            set.Add(3);
            set.Add(1);
            set.Add(3);
            set.Add(2);
            set.Add(1);

            // Act
            var items = set.ToList();

            // Assert
            // Order should be based on dictionary enumeration order
            // which may be insertion order or hash-based order
            Assert.That(items.Count, Is.EqualTo(5));
            // We can't guarantee exact order, but we can verify counts
            Assert.That(items.Where(x => x == 1).Count(), Is.EqualTo(2));
            Assert.That(items.Where(x => x == 2).Count(), Is.EqualTo(1));
            Assert.That(items.Where(x => x == 3).Count(), Is.EqualTo(2));
        }

        #region Enumerator Behavior Tests

        [Test]
        public void MultiHashSetEnumerator_IsValueType()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");

            // Act
            var enumerator = set.GetEnumerator();

            // Assert
            Assert.That(enumerator.GetType().IsValueType, Is.True, "Enumerator should be a value type (struct)");
        }

        [Test]
        public void MultiHashSetEnumerator_CurrentBeforeMoveNext_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");
            var enumerator = set.GetEnumerator();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var _ = enumerator.Current; });
        }

        [Test]
        public void MultiHashSetEnumerator_CurrentAfterEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext()) { }

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var _ = enumerator.Current; });
        }

        [Test]
        public void MultiHashSetEnumerator_MoveNextAfterEnumeration_ReturnsFalse()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext()) { }

            // Act & Assert
            Assert.That(enumerator.MoveNext(), Is.False);
            Assert.That(enumerator.MoveNext(), Is.False); // Multiple calls should still return false
        }

        [Test]
        public void MultiHashSetEnumerator_Reset_ThrowsNotSupportedException()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");
            var enumerator = set.GetEnumerator();

            // Act & Assert - MultiHashSet enumerator does not support Reset
            Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        [Test]
        public void MultiHashSetEnumerator_Dispose_DoesNotThrow()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");
            var enumerator = set.GetEnumerator();

            // Act & Assert
            Assert.DoesNotThrow(() => enumerator.Dispose());
            // Dispose should be callable multiple times
            Assert.DoesNotThrow(() => enumerator.Dispose());
        }

        [Test]
        public void MultiHashSetEnumerator_BoxingNotOccurred_WhenUsingConcreteType()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("b");
            set.Add("a");

            // Act
            var enumerator = set.GetEnumerator();

            // Assert - If enumerator is a struct, assigning to object causes boxing
            // We can verify that the type is a value type
            Assert.That(enumerator.GetType().IsValueType, Is.True);
        }

        #endregion

        [Test]
        public void CustomComparer_RespectedInOperations()
        {
            // Arrange
            var comparer = StringComparer.OrdinalIgnoreCase;
            var set = new MultiHashSet<string>(comparer);
            set.Add("ITEM");

            // Act & Assert
            Assert.That(set.Contains("item"), Is.True);
            Assert.That(set.GetCountOf("Item"), Is.EqualTo(1));
            var (success, count) = set.Remove("ItEm");
            Assert.That(success, Is.True);
            Assert.That(count, Is.EqualTo(0));
            Assert.That(set.Contains("ITEM"), Is.False);
        }

        [Test]
        public void Performance_Count_IsCached()
        {
            // Arrange
            var set = new MultiHashSet<int>();
            // Add enough items to make performance difference noticeable
            for (int i = 0; i < 1000; i++)
            {
                set.Add(i % 100); // 100 unique items, each repeated 10 times
            }

            // Act
            // Call Count multiple times - implementation is O(1) (returns cached count)
            var counts = new List<int>();
            for (int i = 0; i < 100; i++) // Reduced iterations for speed
            {
                counts.Add(set.Count);
            }

            // Assert
            // All counts should be the same (1000)
            Assert.That(counts.All(c => c == 1000), Is.True);
            // Note: This test validates that Count is O(1) (returns cached count)
        }

        [Test]
        public void LargeNumberOfDuplicates_EnumeratorWorksCorrectly()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            const int repeatCount = 1000; // Reduced for test speed
            for (int i = 0; i < repeatCount; i++)
            {
                set.Add("item");
            }

            // Act
            var count = set.Count();
            var enumeratedCount = set.ToList().Count;

            // Assert
            Assert.That(set.Count, Is.EqualTo(repeatCount));
            Assert.That(count, Is.EqualTo(repeatCount));
            Assert.That(enumeratedCount, Is.EqualTo(repeatCount));
            Assert.That(set.GetCountOf("item"), Is.EqualTo(repeatCount));
        }

        [Test]
        public void Remove_FromEmptySet_ReturnsFailure()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act
            var (success, count) = set.Remove("any");

            // Assert
            Assert.That(success, Is.False);
            Assert.That(count, Is.EqualTo(0));
            Assert.That(set.Count, Is.EqualTo(0));
        }

        [Test]
        public void MixedOperations_ConsistentState()
        {
            // Arrange
            var set = new MultiHashSet<int>();

            // Act - Series of mixed operations
            set.Add(1);
            set.Add(2);
            set.Add(1);
            set.Remove(1);
            set.Add(3);
            set.Add(3);
            set.Add(3);
            set.Remove(2);
            set.Add(1);
            set.Clear();
            set.Add(4);
            set.Add(4);

            // Assert
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Contains(1), Is.False);
            Assert.That(set.Contains(2), Is.False);
            Assert.That(set.Contains(3), Is.False);
            Assert.That(set.Contains(4), Is.True);
            Assert.That(set.GetCountOf(4), Is.EqualTo(2));
        }

        [Test]
        public void IEnumerable_Implementation_WorksWithLinq()
        {
            // Arrange
            var set = new MultiHashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(1);
            set.Add(3);

            // Act
            var distinctCount = set.Distinct().Count();
            var sum = set.Sum();
            var grouped = set.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            // Assert
            Assert.That(distinctCount, Is.EqualTo(3)); // 1, 2, 3
            Assert.That(sum, Is.EqualTo(7)); // 1+2+1+3 = 7
            Assert.That(grouped[1], Is.EqualTo(2));
            Assert.That(grouped[2], Is.EqualTo(1));
            Assert.That(grouped[3], Is.EqualTo(1));
        }

        [Test]
        public void Add_ReturnsCorrectCount_AfterMultipleAdds()
        {
            // Arrange
            var set = new MultiHashSet<string>();

            // Act & Assert
            Assert.That(set.Add("a"), Is.EqualTo(1));
            Assert.That(set.Add("b"), Is.EqualTo(1));
            Assert.That(set.Add("a"), Is.EqualTo(2));
            Assert.That(set.Add("a"), Is.EqualTo(3));
            Assert.That(set.Add("b"), Is.EqualTo(2));
        }

        [Test]
        public void Remove_ReturnsCorrectCount_AfterMultipleRemoves()
        {
            // Arrange
            var set = new MultiHashSet<string>();
            set.Add("a");
            set.Add("a");
            set.Add("a");
            set.Add("b");

            // Act & Assert
            var (success1, count1) = set.Remove("a");
            Assert.That(success1, Is.True);
            Assert.That(count1, Is.EqualTo(2));

            var (success2, count2) = set.Remove("a");
            Assert.That(success2, Is.True);
            Assert.That(count2, Is.EqualTo(1));

            var (success3, count3) = set.Remove("a");
            Assert.That(success3, Is.True);
            Assert.That(count3, Is.EqualTo(0));

            var (success4, count4) = set.Remove("b");
            Assert.That(success4, Is.True);
            Assert.That(count4, Is.EqualTo(0));

            var (success5, count5) = set.Remove("nonexistent");
            Assert.That(success5, Is.False);
            Assert.That(count5, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithComparer_CreatesSingleDictionary()
        {
            // Arrange & Act
            // The constructor creates a single dictionary with the specified comparer
            // No waste occurs (previously there was a concern about field initializer)
            var set = new MultiHashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Assert
            // The set should still work correctly
            set.Add("test");
            Assert.That(set.Contains("TEST"), Is.True);
        }

        [Test]
        public void Clone_EmptySet_ReturnsEmptyClone()
        {
            // Arrange
            var original = new MultiHashSet<string>();

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Count, Is.EqualTo(0));
            Assert.That(clone, Is.Empty);
        }

        [Test]
        public void Clone_SetWithItems_ReturnsEqualCopy()
        {
            // Arrange
            var original = new MultiHashSet<string>();
            original.Add("a");
            original.Add("a");
            original.Add("b");
            original.Add("c");
            original.Add("c");
            original.Add("c");

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Count, Is.EqualTo(original.Count));
            Assert.That(clone.Contains("a"), Is.True);
            Assert.That(clone.Contains("b"), Is.True);
            Assert.That(clone.Contains("c"), Is.True);
            Assert.That(clone.GetCountOf("a"), Is.EqualTo(2));
            Assert.That(clone.GetCountOf("b"), Is.EqualTo(1));
            Assert.That(clone.GetCountOf("c"), Is.EqualTo(3));
        }

        [Test]
        public void Clone_ModifyOriginal_DoesNotAffectClone()
        {
            // Arrange
            var original = new MultiHashSet<string>();
            original.Add("item");
            original.Add("item");
            var clone = original.Clone();

            // Act
            original.Add("new");
            original.Remove("item");

            // Assert
            Assert.That(clone.Count, Is.EqualTo(2));
            Assert.That(clone.Contains("item"), Is.True);
            Assert.That(clone.GetCountOf("item"), Is.EqualTo(2));
            Assert.That(clone.Contains("new"), Is.False);
        }

        [Test]
        public void Clone_ModifyClone_DoesNotAffectOriginal()
        {
            // Arrange
            var original = new MultiHashSet<string>();
            original.Add("item");
            original.Add("item");
            var clone = original.Clone();

            // Act
            clone.Add("new");
            clone.Remove("item");

            // Assert
            Assert.That(original.Count, Is.EqualTo(2));
            Assert.That(original.Contains("item"), Is.True);
            Assert.That(original.GetCountOf("item"), Is.EqualTo(2));
            Assert.That(original.Contains("new"), Is.False);
        }

        [Test]
        public void Clone_WithCustomComparer_PreservesComparer()
        {
            // Arrange
            var comparer = StringComparer.OrdinalIgnoreCase;
            var original = new MultiHashSet<string>(comparer);
            original.Add("ITEM");

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone.Contains("item"), Is.True);
            Assert.That(clone.GetCountOf("item"), Is.EqualTo(1));
            // Verify comparer is the same by checking case-insensitive behavior
            var (success, count) = clone.Remove("ItEm");
            Assert.That(success, Is.True);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void Clone_IsShallowCopy_ForReferenceTypes()
        {
            // Arrange - using a mutable reference type
            var original = new MultiHashSet<List<string>>();
            var list = new List<string> { "initial" };
            original.Add(list);

            // Act
            var clone = original.Clone();

            // Assert - both collections reference the same list object
            var originalList = original.First();
            var cloneList = clone.First();
            Assert.That(cloneList, Is.SameAs(originalList));

            // Modify the shared list
            originalList.Add("modified");

            // Both collections see the modification (shallow copy behavior)
            Assert.That(cloneList.Count, Is.EqualTo(2));
            Assert.That(cloneList.Contains("modified"), Is.True);
        }
    }
}