using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class BidirectionalDictionaryTests
    {
        [Test]
        public void Constructor_Default_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dict = new BidirectionalDictionary<string, int>();

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
            Assert.That(dict.Keys, Is.Empty);
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Constructor_WithKeyComparerAndValueComparer_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(keyComparer, valueComparer);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
        }

        [Test]
        public void Constructor_WithCapacity_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dict = new BidirectionalDictionary<string, int>(100);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
        }

        [Test]
        public void Constructor_WithCapacityAndComparers_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(100, keyComparer, valueComparer);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
        }

        [Test]
        public void Constructor_CopyFrom_CreatesEqualDictionary()
        {
            // Arrange
            var original = new BidirectionalDictionary<string, int>();
            original.Add("key1", 100);
            original.Add("key2", 200);

            // Act
            var copy = new BidirectionalDictionary<string, int>(original);

            // Assert
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.Count, Is.EqualTo(2));
            Assert.That(copy.ContainsKey("key1"), Is.True);
            Assert.That(copy.ContainsKey("key2"), Is.True);
            Assert.That(copy.ContainsValue(100), Is.True);
            Assert.That(copy.ContainsValue(200), Is.True);
        }

        [Test]
        public void Constructor_CopyFromWithComparers_CreatesDictionaryWithNewComparers()
        {
            // Arrange
            var original = new BidirectionalDictionary<string, int>();
            original.Add("KEY1", 100);
            original.Add("KEY2", 200);

            // Act
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var copy = new BidirectionalDictionary<string, int>(original, keyComparer, valueComparer);

            // Assert
            Assert.That(copy.Count, Is.EqualTo(2));
            Assert.That(copy.ContainsKey("key1"), Is.True);
            Assert.That(copy.ContainsKey("key2"), Is.True);
        }

#if NETCOREAPP
        [Test]
        public void Constructor_WithKeyValuePairs_CreatesDictionary()
        {
            // Arrange
            var pairs = new List<KeyValuePair<string, int>>
            {
                new("key1", 100),
                new("key2", 200)
            };

            // Act
            var dict = new BidirectionalDictionary<string, int>(pairs);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.ContainsKey("key2"), Is.True);
            Assert.That(dict.ContainsValue(100), Is.True);
            Assert.That(dict.ContainsValue(200), Is.True);
        }

        [Test]
        public void Constructor_WithKeyValuePairsAndComparers_CreatesDictionary()
        {
            // Arrange
            var pairs = new List<KeyValuePair<string, int>>
            {
                new("KEY1", 100),
                new("KEY2", 200)
            };

            // Act
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(pairs, keyComparer, valueComparer);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.ContainsKey("key2"), Is.True);
        }
#endif

        [Test]
        public void Add_SingleKeyValuePair_AddsSuccessfully()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            dict.Add("key1", 100);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.ContainsValue(100), Is.True);
            Assert.That(dict["key1"], Is.EqualTo(100));
        }

        [Test]
        public void Add_DuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(() => dict.Add("key1", 200),
                Throws.ArgumentException.With.Message.Contain("An item with the same key has already been added."));
        }

        [Test]
        public void Add_DuplicateValue_ThrowsArgumentException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(() => dict.Add("key2", 100),
                Throws.ArgumentException.With.Message.Contain("An item with the same value has already been added."));
        }

        [Test]
        public void Add_KeyValuePairOverload_AddsSuccessfully()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            var kvp = new KeyValuePair<string, int>("key1", 100);

            // Act
            dict.Add(kvp);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.Contains(kvp), Is.True);
        }

        [Test]
        public void TryAdd_Successful_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.TryAdd("key1", 100);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("key1"), Is.True);
            Assert.That(dict.ContainsValue(100), Is.True);
        }

        [Test]
        public void TryAdd_DuplicateKey_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.TryAdd("key1", 200);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["key1"], Is.EqualTo(100));
        }

        [Test]
        public void TryAdd_DuplicateValue_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.TryAdd("key2", 100);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("key2"), Is.False);
        }

        [Test]
        public void Indexer_Get_ExistingKey_ReturnsValue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict["key1"], Is.EqualTo(100));
        }

        [Test]
        public void Indexer_Get_NonExistentKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict["nonexistent"], Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Indexer_Set_NewKeyValue_SetsSuccessfully()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            dict["key1"] = 100;

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["key1"], Is.EqualTo(100));
            Assert.That(dict.ContainsValue(100), Is.True);
        }

        [Test]
        public void Indexer_Set_UpdateExistingKey_UpdatesValue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            dict["key1"] = 200;

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["key1"], Is.EqualTo(200));
            Assert.That(dict.ContainsValue(200), Is.True);
            Assert.That(dict.ContainsValue(100), Is.False);
        }

        [Test]
        public void Indexer_Set_WithValueAlreadyMappedToDifferentKey_ThrowsArgumentException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act & Assert - Setting key1 to a value that already exists (200) should throw
            Assert.That(() => dict["key1"] = 200,
                Throws.ArgumentException.With.Message.Contain("Value is already mapped to a different key."));

            // Verify original mappings are preserved
            Assert.That(dict["key1"], Is.EqualTo(100));
            Assert.That(dict["key2"], Is.EqualTo(200));
            Assert.That(dict.ContainsValue(100), Is.True);
            Assert.That(dict.ContainsValue(200), Is.True);
            Assert.That(dict.TryGetKeyByValue(100, out var keyFor100), Is.True);
            Assert.That(keyFor100, Is.EqualTo("key1"));
            Assert.That(dict.TryGetKeyByValue(200, out var keyFor200), Is.True);
            Assert.That(keyFor200, Is.EqualTo("key2"));
        }

        [Test]
        public void Remove_ByKey_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key1");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.ContainsValue(100), Is.False);
        }

        [Test]
        public void Remove_ByKey_ExistingKeyWithOutParameter_ReturnsTrueAndOutputsValue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.Remove("key1", out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(100));
            Assert.That(dict.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_ByKey_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.Remove("nonexistent");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Remove_ByKey_NonExistentKeyWithOutParameter_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.Remove("nonexistent", out var value);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(default(int)));
        }

        [Test]
        public void RemoveByValue_ExistingValue_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.RemoveByValue(100);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.ContainsKey("key1"), Is.False);
            Assert.That(dict.ContainsValue(100), Is.False);
        }

        [Test]
        public void RemoveByValue_ExistingValueWithOutParameter_ReturnsTrueAndOutputsKey()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.RemoveByValue(100, out var key);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(key, Is.EqualTo("key1"));
            Assert.That(dict.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveByValue_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.RemoveByValue(999);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RemoveByValue_NonExistentValueWithOutParameter_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.RemoveByValue(999, out var key);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(key, Is.EqualTo(default(string)));
        }

        [Test]
        public void TryGetValue_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.TryGetValue("key1", out var value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(100));
        }

        [Test]
        public void TryGetValue_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.TryGetValue("nonexistent", out var value);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(default(int)));
        }

        [Test]
        public void TryGetKeyByValue_ExistingValue_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var result = dict.TryGetKeyByValue(100, out var key);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(key, Is.EqualTo("key1"));
        }

        [Test]
        public void TryGetKeyByValue_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act
            var result = dict.TryGetKeyByValue(999, out var key);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(key, Is.EqualTo(default(string)));
        }

        [Test]
        public void ContainsKey_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.ContainsKey("key1"), Is.True);
        }

        [Test]
        public void ContainsKey_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.ContainsKey("nonexistent"), Is.False);
        }

        [Test]
        public void ContainsValue_ExistingValue_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act & Assert
            Assert.That(dict.ContainsValue(100), Is.True);
        }

        [Test]
        public void ContainsValue_NonExistentValue_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act & Assert
            Assert.That(dict.ContainsValue(999), Is.False);
        }

        [Test]
        public void Contains_KeyValuePair_ExistingPair_ReturnsTrue()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            var kvp = new KeyValuePair<string, int>("key1", 100);

            // Act & Assert
            Assert.That(dict.Contains(kvp), Is.True);
        }

        [Test]
        public void Contains_KeyValuePair_NonExistentPair_ReturnsFalse()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            var kvp = new KeyValuePair<string, int>("key1", 200);

            // Act & Assert
            Assert.That(dict.Contains(kvp), Is.False);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            dict.Clear();

            // Assert
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict, Is.Empty);
            Assert.That(dict.Keys, Is.Empty);
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Keys_Property_ReturnsAllKeys()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

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
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            var values = dict.Values.ToList();

            // Assert
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(100), Is.True);
            Assert.That(values.Contains(200), Is.True);
        }

        [Test]
        public void KeyComparer_Property_ReturnsCorrectComparer()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(keyComparer, valueComparer);

            // Act & Assert
            Assert.That(dict.KeyComparer, Is.SameAs(keyComparer));
        }

        [Test]
        public void ValueComparer_Property_ReturnsCorrectComparer()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(keyComparer, valueComparer);

            // Act & Assert
            Assert.That(dict.ValueComparer, Is.SameAs(valueComparer));
        }

        [Test]
        public void Reversed_Property_ReturnsReversedDictionary()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            var reversed = dict.Reversed;

            // Assert
            Assert.That(reversed.Count, Is.EqualTo(2));
            Assert.That(reversed.ContainsKey(100), Is.True);
            Assert.That(reversed.ContainsKey(200), Is.True);
            Assert.That(reversed[100], Is.EqualTo("key1"));
            Assert.That(reversed[200], Is.EqualTo("key2"));
        }

        [Test]
        public void AsReadOnly_ReturnsSelfAsReadOnlyInterface()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);

            // Act
            var readOnly = dict.AsReadOnly();

            // Assert
            Assert.That(readOnly, Is.SameAs(dict));
            Assert.That(readOnly.Count, Is.EqualTo(1));
            Assert.That(readOnly.ContainsKey("key1"), Is.True);
        }

        [Test]
        public void CopyAndReverse_CreatesReversedDictionary()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            var reversed = dict.CopyAndReverse();

            // Assert - types are different so they cannot be the same instance
            Assert.That((object)reversed, Is.Not.SameAs((object)dict));
            Assert.That(reversed.Count, Is.EqualTo(2));
            Assert.That(reversed.ContainsKey(100), Is.True);
            Assert.That(reversed.ContainsKey(200), Is.True);
            Assert.That(reversed[100], Is.EqualTo("key1"));
            Assert.That(reversed[200], Is.EqualTo("key2"));
            // Verify it's truly reversed
            Assert.That(reversed.ContainsValue("key1"), Is.True);
            Assert.That(reversed.ContainsValue("key2"), Is.True);
        }

        [Test]
        public void GetEnumerator_ReturnsAllKeyValuePairs()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            var pairs = Enumerable.ToList<KeyValuePair<string, int>>(dict);

            // Assert
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs.Any(p => p.Key == "key1" && p.Value == 100), Is.True);
            Assert.That(pairs.Any(p => p.Key == "key2" && p.Value == 200), Is.True);
        }

        [Test]
        public void GetEnumerator_NonGeneric_ReturnsAllKeyValuePairs()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Act
            var enumerator = ((IEnumerable)dict).GetEnumerator();
            var pairs = new List<KeyValuePair<string, int>>();
            while (enumerator.MoveNext())
            {
                pairs.Add((KeyValuePair<string, int>)enumerator.Current);
            }

            // Assert
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs.Any(p => p.Key == "key1" && p.Value == 100), Is.True);
            Assert.That(pairs.Any(p => p.Key == "key2" && p.Value == 200), Is.True);
        }

        [Test]
        public void CustomKeyComparer_RespectedInOperations()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = EqualityComparer<int>.Default;
            var dict = new BidirectionalDictionary<string, int>(keyComparer, valueComparer);
            dict.Add("KEY", 100);

            // Act & Assert
            Assert.That(dict.ContainsKey("key"), Is.True);
            Assert.That(dict["key"], Is.EqualTo(100));
            Assert.That(dict.TryGetValue("key", out var value), Is.True);
            Assert.That(value, Is.EqualTo(100));
        }

        [Test]
        public void CustomValueComparer_RespectedInOperations()
        {
            // Arrange
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.OrdinalIgnoreCase;
            var dict = new BidirectionalDictionary<string, string>(keyComparer, valueComparer);
            dict.Add("key1", "VALUE");

            // Act & Assert
            Assert.That(dict.ContainsValue("value"), Is.True);
            Assert.That(dict.TryGetKeyByValue("value", out var key), Is.True);
            Assert.That(key, Is.EqualTo("key1"));
        }

        [Test]
        public void MixedOperations_ConsistentState()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act - Series of mixed operations
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Remove("a");
            dict["b"] = 3;
            dict.TryAdd("c", 4);
            dict.RemoveByValue(4);
            dict["d"] = 5;
            dict.Clear();
            dict.Add("e", 6);

            // Assert
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict.ContainsKey("e"), Is.True);
            Assert.That(dict.ContainsValue(6), Is.True);
            Assert.That(dict.ContainsKey("a"), Is.False);
            Assert.That(dict.ContainsKey("b"), Is.False);
            Assert.That(dict.ContainsKey("c"), Is.False);
            Assert.That(dict.ContainsKey("d"), Is.False);
        }

        [Test]
        public void LargeNumberOfOperations_PerformanceAndConsistency()
        {
            // Arrange
            var dict = new BidirectionalDictionary<int, int>();
            const int operationCount = 1000;

            // Act - add many items
            for (int i = 0; i < operationCount; i++)
            {
                dict.Add(i, i * 10);
            }

            // Assert - verify counts
            Assert.That(dict.Count, Is.EqualTo(operationCount));

            // Remove half of the items
            int removedCount = 0;
            for (int i = 0; i < operationCount; i += 2)
            {
                if (dict.Remove(i))
                    removedCount++;
            }

            Assert.That(dict.Count, Is.EqualTo(operationCount - removedCount));

            // Verify remaining items
            for (int i = 1; i < operationCount; i += 2)
            {
                Assert.That(dict.ContainsKey(i), Is.True);
                Assert.That(dict[i], Is.EqualTo(i * 10));
            }
        }

        [Test]
        public void NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, int>();

            // Act & Assert
            Assert.That(() => dict.Add(null!, 100), Throws.ArgumentNullException);
            Assert.That(() => dict.ContainsKey(null!), Throws.ArgumentNullException);
            Assert.That(() => dict.Remove(null!), Throws.ArgumentNullException);
            Assert.That(() => dict[null!] = 100, Throws.ArgumentNullException);
            Assert.That(() => _ = dict[null!], Throws.ArgumentNullException);
        }

        [Test]
        public void NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var dict = new BidirectionalDictionary<string, string>();

            // Act & Assert
            Assert.That(() => dict.Add("key1", null!), Throws.ArgumentNullException);
            Assert.That(() => dict.ContainsValue(null!), Throws.ArgumentNullException);
            Assert.That(() => dict.RemoveByValue(null!), Throws.ArgumentNullException);
            Assert.That(() => dict["key1"] = null!, Throws.ArgumentNullException);
        }
    }
}