using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class MultiDictionaryTests
    {
        [Test]
        public void Constructor_Default_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dict = new MultiDictionary<string, int>();

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
            Assert.That(dict.Keys, Is.Empty);
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Constructor_WithKeyComparer_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new MultiDictionary<string, int>(comparer);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
        }

        [Test]
        public void Constructor_WithKeyAndValueComparer_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new MultiDictionary<string, int>(keyComparer, valueComparer);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
        }

        [Test]
        public void Constructor_WithNullKeyComparer_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            // Implementation now validates comparer parameters using ThrowIfNull
            // This test verifies the expected behavior
            Assert.That(() => new MultiDictionary<string, int>(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullValueComparer_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            // Implementation now validates comparer parameters using ThrowIfNull
            // This test verifies the expected behavior
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.That(() => new MultiDictionary<string, int>(keyComparer, null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_SingleKeyValuePair_AddsSuccessfully()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act
            dict.Add("key1", 100);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.Contains("key1", 100), Is.True);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(1));
            Assert.That(dict.GetCountOfKeyValuePair("key1", 100), Is.EqualTo(1));
        }

        [Test]
        public void Add_MultipleValuesToSameKey_AddsAllValues()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key1", 100); // Duplicate value

            // Assert
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.Contains("key1", 100), Is.True);
            Assert.That(dict.Contains("key1", 200), Is.True);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(3));
            Assert.That(dict.GetCountOfKeyValuePair("key1", 100), Is.EqualTo(2));
            Assert.That(dict.GetCountOfKeyValuePair("key1", 200), Is.EqualTo(1));
        }

        [Test]
        public void Add_MultipleKeys_AddsSuccessfully()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act
            dict.Add("key1", 100);
            dict.Add("key2", 200);
            dict.Add("key2", 300);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.ContainsKey("key2"), Is.True);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(1));
            Assert.That(dict.GetValueCountOfKey("key2"), Is.EqualTo(2));
        }

        [Test]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict.Add(null!, 100), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();

            // Act & Assert
            Assert.That(() => dict.Add("key1", null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_KeyValuePairOverload_AddsSuccessfully()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>("key1", 100);

            // Act
            dict.Add(kvp);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.Contains(kvp), Is.True);
        }

        [Test]
        public void Remove_ExistingKeyValuePair_ReturnsTrueAndDecreasesCount()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key1", 100);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.Contains("key1", 100), Is.True); // Still one more 100
            Assert.That(dict.GetCountOfKeyValuePair("key1", 100), Is.EqualTo(1));
        }

        [Test]
        public void Remove_LastInstanceOfValue_RemovesEmptySet()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key1", 100);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.Contains("key1", 100), Is.False);
        }

        [Test]
        public void Remove_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key2", 200);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key1", 200);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict.Remove(null!, 100), Throws.ArgumentNullException);
        }

        [Test]
        public void Remove_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();
            dict.Add("key1", "value1");

            // Act & Assert
            Assert.That(() => dict.Remove("key1", null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Remove_KeyValuePairOverload_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            var kvp = new KeyValuePair<string, int>("key1", 100);

            // Act
            var result = dict.Remove(kvp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_EntireKey_RemovesAllValuesForThatKey()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key2", 300);

            // Act
            var result = dict.Remove("key1");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.Contains("key1", 100), Is.False);
            Assert.That(dict.Contains("key1", 200), Is.False);
            Assert.That(dict.Contains("key2", 300), Is.True);
        }

        [Test]
        public void Remove_NonExistentEntireKey_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key2");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_NullKeyForEntireKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict.Remove(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Indexer_ExistingKey_ReturnsValues()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act
            var values = dict["key1"].ToList();

            // Assert
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(100), Is.True);
            Assert.That(values.Contains(200), Is.True);
        }

        [Test]
        public void Indexer_NonExistentKey_ReturnsEmptyEnumerable()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act
            var values = dict["nonexistent"].ToList();

            // Assert
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void Indexer_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict[null!], Throws.ArgumentNullException);
        }

        [Test]
        public void Keys_Property_ReturnsAllKeys()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);
            dict.Add("key2", 300);

            // Act
            var keys = dict.Keys.ToList();

            // Assert
            Assert.That(keys.Count, Is.EqualTo(2));
            Assert.That(keys.Contains("key1"), Is.True);
            Assert.That(keys.Contains("key2"), Is.True);
        }

        [Test]
        public void Values_Property_ReturnsAllValues()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key2", 300);

            // Act
            var values = dict.Values.ToList();

            // Assert
            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values.Contains(100), Is.True);
            Assert.That(values.Contains(200), Is.True);
            Assert.That(values.Contains(300), Is.True);
        }

        [Test]
        public void ContainsKey_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.ContainsKey("key1"), Is.True);
        }

        [Test]
        public void ContainsKey_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.ContainsKey("nonexistent"), Is.False);
        }

        [Test]
        public void ContainsKey_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict.ContainsKey(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void ContainsValue_ExistingValue_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act & Assert
            Assert.That(dict.ContainsValue(100), Is.True);
            Assert.That(dict.ContainsValue(200), Is.True);
        }

        [Test]
        public void ContainsValue_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.ContainsValue(999), Is.False);
        }

        [Test]
        public void ContainsValue_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();

            // Act & Assert
            Assert.That(() => dict.ContainsValue(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Contains_ExistingKeyValuePair_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act & Assert
            Assert.That(dict.Contains("key1", 100), Is.True);
            Assert.That(dict.Contains("key1", 200), Is.True);
        }

        [Test]
        public void Contains_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.Contains("nonexistent", 100), Is.False);
        }

        [Test]
        public void Contains_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.Contains("key1", 999), Is.False);
        }

        [Test]
        public void Contains_KeyValuePairOverload_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            var kvp = new KeyValuePair<string, int>("key1", 100);

            // Act & Assert
            Assert.That(dict.Contains(kvp), Is.True);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key2", 300);

            // Act
            dict.Clear();

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
            Assert.That(dict.Keys, Is.Empty);
            Assert.That(dict.Values, Is.Empty);
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.ContainsKey("key2"), Is.False);
        }

        [Test]
        public void GetEnumerator_ReturnsAllKeyValuePairs()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key2", 300);

            // Act
            var pairs = dict.ToList();

            // Assert
            Assert.That(pairs.Count, Is.EqualTo(3));
            Assert.That(pairs.Count(p => p.Key == "key1" && p.Value == 100), Is.EqualTo(1));
            Assert.That(pairs.Count(p => p.Key == "key1" && p.Value == 200), Is.EqualTo(1));
            Assert.That(pairs.Count(p => p.Key == "key2" && p.Value == 300), Is.EqualTo(1));
        }

        [Test]
        public void CustomKeyComparer_RespectedInOperations()
        {
            // Arrange
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new MultiDictionary<string, int>(comparer);
            dict.Add("KEY", 100);

            // Act & Assert
            Assert.That(dict.ContainsKey("key"), Is.True);
            Assert.That(dict.Contains("key", 100), Is.True);
            Assert.That(dict.GetValueCountOfKey("key"), Is.EqualTo(1));
        }

        [Test]
        public void CustomValueComparer_RespectedInOperations()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.OrdinalIgnoreCase;
            var dict = new MultiDictionary<string, string>(keyComparer, valueComparer);
            dict.Add("key1", "VALUE");

            // Act & Assert
            Assert.That(dict.ContainsValue("value"), Is.True);
            Assert.That(dict.Contains("key1", "value"), Is.True);
            Assert.That(dict.GetCountOfKeyValuePair("key1", "value"), Is.EqualTo(1));
        }

        [Test]
        public void Clone_EmptyDictionary_ReturnsEmptyClone()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Count, Is.EqualTo(0));
            Assert.That(clone, Is.Empty);
        }

        [Test]
        public void Clone_DictionaryWithItems_ReturnsEqualCopy()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();
            original.Add("key1", 100);
            original.Add("key1", 200);
            original.Add("key2", 300);
            original.Add("key2", 300); // Duplicate value

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Count, Is.EqualTo(original.Count));
            Assert.That(clone.ContainsKey("key1"), Is.True);
            Assert.That(clone.ContainsKey("key2"), Is.True);
            Assert.That(clone.Contains("key1", 100), Is.True);
            Assert.That(clone.Contains("key1", 200), Is.True);
            Assert.That(clone.Contains("key2", 300), Is.True);
            Assert.That(clone.GetCountOfKeyValuePair("key2", 300), Is.EqualTo(2));
        }

        [Test]
        public void Clone_ModifyOriginal_DoesNotAffectClone()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();
            original.Add("key1", 100);
            original.Add("key1", 200);
            var clone = original.Clone();

            // Act
            original.Add("key1", 300);
            original.Remove("key1", 100);

            // Assert
            Assert.That(clone.Count, Is.EqualTo(2));
            Assert.That(clone.Contains("key1", 100), Is.True);
            Assert.That(clone.Contains("key1", 200), Is.True);
            Assert.That(clone.Contains("key1", 300), Is.False);
            Assert.That(clone.GetValueCountOfKey("key1"), Is.EqualTo(2));
        }

        [Test]
        public void Clone_ModifyClone_DoesNotAffectOriginal()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();
            original.Add("key1", 100);
            original.Add("key1", 200);
            var clone = original.Clone();

            // Act
            clone.Add("key1", 300);
            clone.Remove("key1", 100);

            // Assert
            Assert.That(original.Count, Is.EqualTo(2));
            Assert.That(original.Contains("key1", 100), Is.True);
            Assert.That(original.Contains("key1", 200), Is.True);
            Assert.That(original.Contains("key1", 300), Is.False);
            Assert.That(original.GetValueCountOfKey("key1"), Is.EqualTo(2));
        }

        [Test]
        public void Clone_WithCustomComparers_PreservesComparers()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.OrdinalIgnoreCase;
            var original = new MultiDictionary<string, string>(keyComparer, valueComparer);
            original.Add("KEY", "VALUE");

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone.ContainsKey("key"), Is.True);
            Assert.That(clone.Contains("key", "value"), Is.True);
        }

        [Test]
        public void ICloneable_Clone_ReturnsObjectOfCorrectType()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();
            original.Add("key1", 100);

            // Act
            var clone = ((ICloneable)original).Clone();

            // Assert
            Assert.That(clone, Is.InstanceOf<MultiDictionary<string, int>>());
            var typedClone = (MultiDictionary<string, int>)clone;
            Assert.That(typedClone.Count, Is.EqualTo(1));
            Assert.That(typedClone.Contains("key1", 100), Is.True);
        }

        [Test]
        public void Clone_IsShallowCopy_ForReferenceTypes()
        {
            // Arrange - using mutable reference types
            var original = new MultiDictionary<string, List<string>>();
            var list = new List<string> { "initial" };
            original.Add("key1", list);

            // Act
            var clone = original.Clone();

            // Assert - both dictionaries reference the same list object
            var originalList = original["key1"].First();
            var cloneList = clone["key1"].First();
            Assert.That(cloneList, Is.SameAs(originalList));

            // Modify the shared list
            originalList.Add("modified");

            // Both dictionaries see the modification (shallow copy behavior)
            Assert.That(cloneList.Count, Is.EqualTo(2));
            Assert.That(cloneList.Contains("modified"), Is.True);
        }

        [Test]
        public void GetValueCountOfKey_NonExistentKey_ReturnsZero()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.GetValueCountOfKey("nonexistent"), Is.EqualTo(0));
        }

        [Test]
        public void GetValueCountOfKeyValuePair_NonExistentPair_ReturnsZero()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.GetCountOfKeyValuePair("key1", 999), Is.EqualTo(0));
            Assert.That(dict.GetCountOfKeyValuePair("key2", 100), Is.EqualTo(0));
        }

        [Test]
        public void MixedOperations_ConsistentState()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act - Series of mixed operations
            dict.Add("a", 1);
            dict.Add("a", 2);
            dict.Add("b", 3);
            dict.Remove("a", 1);
            dict.Add("c", 4);
            dict.Add("c", 4); // Duplicate
            dict.Remove("b");
            dict.Add("a", 5);
            dict.Clear();
            dict.Add("d", 6);
            dict.Add("d", 6);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.ContainsKey("a"), Is.False);
            Assert.That(dict.ContainsKey("b"), Is.False);
            Assert.That(dict.ContainsKey("c"), Is.False);
            Assert.That(dict.ContainsKey("d"), Is.True);
            Assert.That(dict.GetValueCountOfKey("d"), Is.EqualTo(2));
            Assert.That(dict.GetCountOfKeyValuePair("d", 6), Is.EqualTo(2));
        }

        [Test]
        public void ContainsValue_EmptyDictionary_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.ContainsValue(100), Is.False);
        }

        [Test]
        public void ContainsValue_ValueInMultipleKeys_ReturnsTrue()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 100);
            dict.Add("key3", 200);

            // Act & Assert
            Assert.That(dict.ContainsValue(100), Is.True);
        }

        [Test]
        public void GetCountOfKeyValuePair_MultipleOccurrences_ReturnsCorrectCount()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 100);
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act & Assert
            Assert.That(dict.GetCountOfKeyValuePair("key1", 100), Is.EqualTo(3));
            Assert.That(dict.GetCountOfKeyValuePair("key1", 200), Is.EqualTo(1));
        }

        [Test]
        public void GetEnumerator_NonGeneric_ReturnsCorrectEnumerator()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act
            var enumerator = ((System.Collections.IEnumerable)dict).GetEnumerator();
            var pairs = new List<KeyValuePair<string, int>>();
            while (enumerator.MoveNext())
            {
                pairs.Add((KeyValuePair<string, int>)enumerator.Current);
            }

            // Assert
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs.Any(p => p.Key == "key1" && p.Value == 100), Is.True);
            Assert.That(pairs.Any(p => p.Key == "key1" && p.Value == 200), Is.True);
        }

        [Test]
        public void Remove_KeyWithMultipleValues_RemovesAllValuesAndDecreasesCountCorrectly()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key1", 300);
            dict.Add("key2", 400);
            var originalCount = dict.Count;

            // Act
            var result = dict.Remove("key1");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(originalCount - 3));
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.ContainsKey("key2"), Is.True);
            Assert.That(dict.Contains("key2", 400), Is.True);
        }

        [Test]
        public void Remove_KeyFromEmptyDictionary_ReturnsFalse()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.Remove("nonexistent"), Is.False);
        }

        [Test]
        public void Add_DuplicateKeyValuePairMultipleTimes_IncreasesCountCorrectly()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();

            // Act
            dict.Add("key1", 100);
            dict.Add("key1", 100);
            dict.Add("key1", 100);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.GetCountOfKeyValuePair("key1", 100), Is.EqualTo(3));
        }

        [Test]
        public void Indexer_ReturnsIndependentEnumerable()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act
            var values1 = dict["key1"].ToList();
            var values2 = dict["key1"].ToList();

            // Assert - both enumerations should be independent and contain the same values
            Assert.That(values1.Count, Is.EqualTo(2));
            Assert.That(values2.Count, Is.EqualTo(2));
            Assert.That(values1.Contains(100), Is.True);
            Assert.That(values1.Contains(200), Is.True);
            Assert.That(values2.Contains(100), Is.True);
            Assert.That(values2.Contains(200), Is.True);
        }

        [Test]
        public void Indexer_ModifyDictionaryWhileEnumerating_DoesNotAffectEnumeration()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);

            // Act
            var values = dict["key1"].ToList(); // Materialize the enumeration

            // Modify dictionary after enumeration is materialized
            dict.Add("key1", 300);

            // Assert - the materialized list should not contain the new value
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(100), Is.True);
            Assert.That(values.Contains(200), Is.True);
            Assert.That(values.Contains(300), Is.False);
        }

        [Test]
        public void Keys_Property_ReflectsCurrentState()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act - get keys before modification
            var keysBefore = dict.Keys.ToList();

            // Modify dictionary
            dict.Add("key2", 200);
            dict.Remove("key1");

            // Get keys after modification
            var keysAfter = dict.Keys.ToList();

            // Assert
            Assert.That(keysBefore.Count, Is.EqualTo(1));
            Assert.That(keysBefore.Contains("key1"), Is.True);

            Assert.That(keysAfter.Count, Is.EqualTo(1));
            Assert.That(keysAfter.Contains("key2"), Is.True);
            Assert.That(keysAfter.Contains("key1"), Is.False);
        }

        [Test]
        public void Values_Property_ReflectsCurrentState()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);

            // Act - get values before modification
            var valuesBefore = dict.Values.ToList();

            // Modify dictionary
            dict.Add("key1", 200);
            dict.Add("key2", 300);

            // Get values after modification
            var valuesAfter = dict.Values.ToList();

            // Assert
            Assert.That(valuesBefore.Count, Is.EqualTo(1));
            Assert.That(valuesBefore.Contains(100), Is.True);

            Assert.That(valuesAfter.Count, Is.EqualTo(3));
            Assert.That(valuesAfter.Contains(100), Is.True);
            Assert.That(valuesAfter.Contains(200), Is.True);
            Assert.That(valuesAfter.Contains(300), Is.True);
        }

        [Test]
        public void CustomKeyComparer_CaseInsensitive_AllOperationsRespectComparer()
        {
            // Arrange
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new MultiDictionary<string, int>(comparer);

            // Act
            dict.Add("KEY", 100);
            dict.Add("key", 200);
            dict.Add("Key", 300);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.ContainsKey("key"), Is.True);
            Assert.That(dict.GetValueCountOfKey("KEY"), Is.EqualTo(3));
            Assert.That(dict.GetCountOfKeyValuePair("Key", 100), Is.EqualTo(1));

            // Remove using different casing
            var removed = dict.Remove("key", 200);
            Assert.That(removed, Is.True);
            Assert.That(dict.Count, Is.EqualTo(2));
        }

        [Test]
        public void CustomValueComparer_CaseInsensitive_AllOperationsRespectComparer()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.OrdinalIgnoreCase;
            var dict = new MultiDictionary<string, string>(keyComparer, valueComparer);

            // Act
            dict.Add("key1", "VALUE");
            dict.Add("key1", "value");
            dict.Add("key1", "Value");

            // Assert
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.ContainsValue("VALUE"), Is.True);
            Assert.That(dict.Contains("key1", "value"), Is.True);
            Assert.That(dict.GetCountOfKeyValuePair("KEY1", "Value"), Is.EqualTo(3));

            // Remove using different casing
            var removed = dict.Remove("KEY1", "VALUE");
            Assert.That(removed, Is.True);
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.GetCountOfKeyValuePair("key1", "value"), Is.EqualTo(2));
        }

        [Test]
        public void Clone_ThenClearOriginal_CloneUnaffected()
        {
            // Arrange
            var original = new MultiDictionary<string, int>();
            original.Add("key1", 100);
            original.Add("key1", 200);
            original.Add("key2", 300);
            var clone = original.Clone();

            // Act
            original.Clear();

            // Assert
            Assert.That(original.Count, Is.EqualTo(0));
            Assert.That(original.ContainsKey("key1"), Is.False);

            Assert.That(clone.Count, Is.EqualTo(3));
            Assert.That(clone.ContainsKey("key1"), Is.True);
            Assert.That(clone.Contains("key1", 100), Is.True);
            Assert.That(clone.Contains("key1", 200), Is.True);
            Assert.That(clone.Contains("key2", 300), Is.True);
        }

        [Test]
        public void GetValueCountOfKey_AfterRemovingValues_ReturnsUpdatedCount()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key1", 200);
            dict.Add("key1", 300);

            // Act & Assert - initial count
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(3));

            // Remove one value
            dict.Remove("key1", 200);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(2));

            // Remove another value
            dict.Remove("key1", 100);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(1));

            // Remove last value (key should be removed)
            dict.Remove("key1", 300);
            Assert.That(dict.GetValueCountOfKey("key1"), Is.EqualTo(0));
            Assert.That(dict.ContainsKey("key1"), Is.False);
        }

        [Test]
        public void Contains_KeyValuePairOverload_WithNullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>(null!, 100);

            // Act & Assert
            Assert.That(() => dict.Contains(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void Contains_KeyValuePairOverload_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();
            dict.Add("key1", "value1");
            var kvp = new KeyValuePair<string, string>("key1", null!);

            // Act & Assert
            Assert.That(() => dict.Contains(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void GetCountOfKeyValuePair_KeyValuePairOverload_WithNullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>(null!, 100);

            // Act & Assert
            Assert.That(() => dict.GetCountOfKeyValuePair(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void GetCountOfKeyValuePair_KeyValuePairOverload_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();
            dict.Add("key1", "value1");
            var kvp = new KeyValuePair<string, string>("key1", null!);

            // Act & Assert
            Assert.That(() => dict.GetCountOfKeyValuePair(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void Remove_KeyValuePairOverload_WithNullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>(null!, 100);

            // Act & Assert
            Assert.That(() => dict.Remove(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void Remove_KeyValuePairOverload_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();
            dict.Add("key1", "value1");
            var kvp = new KeyValuePair<string, string>("key1", null!);

            // Act & Assert
            Assert.That(() => dict.Remove(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_KeyValuePairOverload_WithNullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>(null!, 100);

            // Act & Assert
            Assert.That(() => dict.Add(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void Add_KeyValuePairOverload_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new MultiDictionary<string, string>();
            var kvp = new KeyValuePair<string, string>("key1", null!);

            // Act & Assert
            Assert.That(() => dict.Add(kvp), Throws.ArgumentNullException);
        }

        [Test]
        public void LargeNumberOfOperations_PerformanceAndConsistency()
        {
            // Arrange
            var dict = new MultiDictionary<int, int>();
            const int operationCount = 1000;

            // Act - add many items
            for (int i = 0; i < operationCount; i++)
            {
                dict.Add(i % 100, i); // 100 keys, each with multiple values
            }

            // Assert - verify counts
            Assert.That(dict.Count, Is.EqualTo(operationCount));

            // Remove half of the items
            int removedCount = 0;
            for (int i = 0; i < operationCount; i += 2)
            {
                if (dict.Remove(i % 100, i))
                    removedCount++;
            }

            Assert.That(dict.Count, Is.EqualTo(operationCount - removedCount));

            // Verify remaining items
            for (int i = 1; i < operationCount; i += 2)
            {
                Assert.That(dict.Contains(i % 100, i), Is.True);
            }
        }
    }
}