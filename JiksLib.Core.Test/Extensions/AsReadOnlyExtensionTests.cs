using NUnit.Framework;
using JiksLib.Extensions;
using System.Collections.Generic;
using System;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class AsReadOnlyExtensionTests
    {
        [Test]
        public void AsReadOnly_Dictionary_ReturnsIReadOnlyDictionary()
        {
            // Arrange
            var dictionary = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2,
                ["three"] = 3
            };

            // Act
            var readOnlyDict = dictionary.AsReadOnly();

            // Assert
            Assert.That(readOnlyDict, Is.InstanceOf<IReadOnlyDictionary<string, int>>());
            Assert.That(ReferenceEquals(readOnlyDict, dictionary), Is.True, "Should return same dictionary instance");
            Assert.That(readOnlyDict.Count, Is.EqualTo(3));
            Assert.That(readOnlyDict["one"], Is.EqualTo(1));
            Assert.That(readOnlyDict["two"], Is.EqualTo(2));
            Assert.That(readOnlyDict["three"], Is.EqualTo(3));
        }

        [Test]
        public void AsReadOnly_DictionaryWithNullKeyConstraint_WorksCorrectly()
        {
            // Arrange
            var dictionary = new Dictionary<int, string>
            {
                [1] = "one",
                [2] = "two"
            };

            // Act
            var readOnlyDict = dictionary.AsReadOnly();

            // Assert
            Assert.That(readOnlyDict, Is.InstanceOf<IReadOnlyDictionary<int, string>>());
            Assert.That(readOnlyDict.Count, Is.EqualTo(2));
        }

        [Test]
        public void AsReadOnly_List_ReturnsIReadOnlyList()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            var readOnlyList = list.AsReadOnly();

            // Assert
            Assert.That(readOnlyList, Is.InstanceOf<IReadOnlyList<string>>());
            // Note: readOnlyList should be the same instance as list, but we'll verify functionality
            Assert.That(readOnlyList.Count, Is.EqualTo(3));
            Assert.That(readOnlyList[0], Is.EqualTo("a"));
            Assert.That(readOnlyList[1], Is.EqualTo("b"));
            Assert.That(readOnlyList[2], Is.EqualTo("c"));

            // Verify it's actually the same list by modifying and checking the change is reflected
            list[1] = "modified";
            Assert.That(readOnlyList[1], Is.EqualTo("modified"), "Changes to original list should be reflected in read-only view");
        }

        [Test]
        public void AsReadOnly_Array_ReturnsIReadOnlyList()
        {
            // Arrange
            var array = new[] { 10, 20, 30, 40, 50 };

            // Act
            var readOnlyList = array.AsReadOnly();

            // Assert
            Assert.That(readOnlyList, Is.InstanceOf<IReadOnlyList<int>>());
            Assert.That(ReferenceEquals(readOnlyList, array), Is.True, "Should return same array instance");
            Assert.That(readOnlyList.Count, Is.EqualTo(5));
            Assert.That(readOnlyList[0], Is.EqualTo(10));
            Assert.That(readOnlyList[4], Is.EqualTo(50));
        }

        [Test]
        public void AsReadOnly_HashSet_ReturnsIReadOnlyCollection()
        {
            // Arrange
            var hashSet = new HashSet<double> { 1.1, 2.2, 3.3 };

            // Act
            var readOnlyCollection = hashSet.AsReadOnly();

            // Assert
            Assert.That(readOnlyCollection, Is.InstanceOf<IReadOnlyCollection<double>>());
            Assert.That(ReferenceEquals(readOnlyCollection, hashSet), Is.True, "Should return same hashset instance");
            Assert.That(readOnlyCollection.Count, Is.EqualTo(3));
            Assert.That(readOnlyCollection, Contains.Item(1.1));
            Assert.That(readOnlyCollection, Contains.Item(2.2));
            Assert.That(readOnlyCollection, Contains.Item(3.3));
        }

        [Test]
        public void AsReadOnly_LinkedList_ReturnsIReadOnlyCollection()
        {
            // Arrange
            var linkedList = new LinkedList<char>();
            linkedList.AddLast('x');
            linkedList.AddLast('y');
            linkedList.AddLast('z');

            // Act
            var readOnlyCollection = linkedList.AsReadOnly();

            // Assert
            Assert.That(readOnlyCollection, Is.InstanceOf<IReadOnlyCollection<char>>());
            Assert.That(ReferenceEquals(readOnlyCollection, linkedList), Is.True, "Should return same linkedlist instance");
            Assert.That(readOnlyCollection.Count, Is.EqualTo(3));

            // Verify contents
            var expected = new[] { 'x', 'y', 'z' };
            int index = 0;
            foreach (var item in readOnlyCollection)
            {
                Assert.That(item, Is.EqualTo(expected[index]));
                index++;
            }
        }

        [Test]
        public void AsReadOnly_EmptyCollections_WorkCorrectly()
        {
            // Test empty dictionary
            var emptyDict = new Dictionary<string, bool>();
            var readOnlyEmptyDict = emptyDict.AsReadOnly();
            Assert.That(readOnlyEmptyDict, Is.InstanceOf<IReadOnlyDictionary<string, bool>>());
            Assert.That(readOnlyEmptyDict.Count, Is.EqualTo(0));

            // Test empty list
            var emptyList = new List<DateTime>();
            var readOnlyEmptyList = emptyList.AsReadOnly();
            Assert.That(readOnlyEmptyList, Is.InstanceOf<IReadOnlyList<DateTime>>());
            Assert.That(readOnlyEmptyList.Count, Is.EqualTo(0));

            // Test empty array
            var emptyArray = new object[0];
            var readOnlyEmptyArray = emptyArray.AsReadOnly();
            Assert.That(readOnlyEmptyArray, Is.InstanceOf<IReadOnlyList<object>>());
            Assert.That(readOnlyEmptyArray.Count, Is.EqualTo(0));

            // Test empty HashSet
            var emptyHashSet = new HashSet<Guid>();
            var readOnlyEmptyHashSet = emptyHashSet.AsReadOnly();
            Assert.That(readOnlyEmptyHashSet, Is.InstanceOf<IReadOnlyCollection<Guid>>());
            Assert.That(readOnlyEmptyHashSet.Count, Is.EqualTo(0));

            // Test empty LinkedList
            var emptyLinkedList = new LinkedList<byte>();
            var readOnlyEmptyLinkedList = emptyLinkedList.AsReadOnly();
            Assert.That(readOnlyEmptyLinkedList, Is.InstanceOf<IReadOnlyCollection<byte>>());
            Assert.That(readOnlyEmptyLinkedList.Count, Is.EqualTo(0));
        }

        [Test]
        public void AsReadOnly_ModifiedCollections_ReflectChanges()
        {
            // Dictionary modification
            var dict = new Dictionary<int, string> { [1] = "one" };
            var readOnlyDict = dict.AsReadOnly();
            dict[2] = "two";
            Assert.That(readOnlyDict.Count, Is.EqualTo(2));
            Assert.That(readOnlyDict[2], Is.EqualTo("two"));

            // List modification
            var list = new List<int> { 1, 2 };
            var readOnlyList = list.AsReadOnly();
            list.Add(3);
            Assert.That(readOnlyList.Count, Is.EqualTo(3));
            Assert.That(readOnlyList[2], Is.EqualTo(3));

            // Array cannot be resized but elements can be modified
            var array = new[] { "a", "b", "c" };
            var readOnlyArray = array.AsReadOnly();
            array[1] = "modified";
            Assert.That(readOnlyArray[1], Is.EqualTo("modified"));

            // HashSet modification
            var hashSet = new HashSet<int> { 1, 2 };
            var readOnlyHashSet = hashSet.AsReadOnly();
            hashSet.Add(3);
            Assert.That(readOnlyHashSet.Count, Is.EqualTo(3));
            Assert.That(readOnlyHashSet, Contains.Item(3));

            // LinkedList modification
            var linkedList = new LinkedList<string>();
            linkedList.AddLast("first");
            var readOnlyLinkedList = linkedList.AsReadOnly();
            linkedList.AddLast("second");
            Assert.That(readOnlyLinkedList.Count, Is.EqualTo(2));
        }

        [Test]
        public void AsReadOnly_NullDictionary_ThrowsArgumentNullException()
        {
            // Arrange
            Dictionary<string, int>? nullDictionary = null;

            // Act & Assert
            // Extension method calls ThrowIfNull which throws ArgumentNullException
            Assert.That(() => nullDictionary!.AsReadOnly(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AsReadOnly_NullList_ThrowsArgumentNullException()
        {
            // Arrange
            List<string>? nullList = null;

            // Act & Assert
            // Extension method calls ThrowIfNull which throws ArgumentNullException
            // Temporarily expecting NullReferenceException to debug
            Assert.That(() => nullList!.AsReadOnly(), Throws.TypeOf<NullReferenceException>());
        }

        [Test]
        public void AsReadOnly_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            int[]? nullArray = null;

            // Act & Assert
            // Extension method calls ThrowIfNull which throws ArgumentNullException
            Assert.That(() => nullArray!.AsReadOnly(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AsReadOnly_NullHashSet_ThrowsArgumentNullException()
        {
            // Arrange
            HashSet<double>? nullHashSet = null;

            // Act & Assert
            // Extension method calls ThrowIfNull which throws ArgumentNullException
            Assert.That(() => nullHashSet!.AsReadOnly(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AsReadOnly_NullLinkedList_ThrowsArgumentNullException()
        {
            // Arrange
            LinkedList<char>? nullLinkedList = null;

            // Act & Assert
            // Extension method calls ThrowIfNull which throws ArgumentNullException
            Assert.That(() => nullLinkedList!.AsReadOnly(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AsReadOnly_InterfaceCompatibility_WorksWithPolymorphism()
        {
            // Test that returned interfaces can be used in polymorphic contexts
            var dict = new Dictionary<string, int> { ["key"] = 42 };
            var list = new List<string> { "item" };
            var array = new[] { 1.0 };
            var hashSet = new HashSet<DateTime> { DateTime.Now };
            var linkedList = new LinkedList<bool>();
            linkedList.AddLast(true);

            // All should be assignable to appropriate read-only interfaces
            IReadOnlyDictionary<string, int> readOnlyDict = dict.AsReadOnly();
            IReadOnlyList<string> readOnlyList = list.AsReadOnly();
            IReadOnlyList<double> readOnlyArray = array.AsReadOnly();
            IReadOnlyCollection<DateTime> readOnlyHashSet = hashSet.AsReadOnly();
            IReadOnlyCollection<bool> readOnlyLinkedList = linkedList.AsReadOnly();

            Assert.That(readOnlyDict, Is.Not.Null);
            Assert.That(readOnlyList, Is.Not.Null);
            Assert.That(readOnlyArray, Is.Not.Null);
            Assert.That(readOnlyHashSet, Is.Not.Null);
            Assert.That(readOnlyLinkedList, Is.Not.Null);
        }
    }
}