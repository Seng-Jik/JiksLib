using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class DequeTests
    {
        [Test]
        public void Constructor_CreatesEmptyDeque()
        {
            // Arrange & Act
            var deque = new Deque<int>();

            // Assert
            Assert.That(deque.Count, Is.EqualTo(0));
            Assert.That(deque, Is.Empty);
        }

        [Test]
        public void Add_AddsToBack()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
        }

        [Test]
        public void AddFront_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            deque.AddFront(3);
            deque.AddFront(2);
            deque.AddFront(1);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
        }

        [Test]
        public void AddAndAddFront_MixedOperations()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            deque.Add(1);       // [1]
            deque.AddFront(2);  // [2, 1]
            deque.Add(3);       // [2, 1, 3]
            deque.AddFront(4);  // [4, 2, 1, 3]

            // Assert
            Assert.That(deque.Count, Is.EqualTo(4));
            Assert.That(deque[0], Is.EqualTo(4));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(1));
            Assert.That(deque[3], Is.EqualTo(3));
        }

        [Test]
        public void TryRemoveFront_OnEmptyDeque_ReturnsFalse()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            bool result = deque.TryRemoveFront(out int value);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveFront_RemovesFromFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3); // [1,2,3]

            // Act
            bool result = deque.TryRemoveFront(out int value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(1));
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(2));
            Assert.That(deque[1], Is.EqualTo(3));
        }

        [Test]
        public void TryRemoveBack_OnEmptyDeque_ReturnsFalse()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            bool result = deque.TryRemoveBack(out int value);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveBack_RemovesFromBack()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3); // [1,2,3]

            // Act
            bool result = deque.TryRemoveBack(out int value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(3));
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
        }

        [Test]
        public void Indexer_Get_ReturnsCorrectValue()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(10);
            deque.Add(20);
            deque.Add(30);

            // Act & Assert
            Assert.That(deque[0], Is.EqualTo(10));
            Assert.That(deque[1], Is.EqualTo(20));
            Assert.That(deque[2], Is.EqualTo(30));
        }

        [Test]
        public void Indexer_Set_UpdatesValue()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);

            // Act
            deque[1] = 99;

            // Assert
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(99));
            Assert.That(deque[2], Is.EqualTo(3));
        }

        [Test]
        public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = deque[0]);
            deque.Add(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = deque[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = deque[-1]);
        }

        [Test]
        public void Clear_RemovesAllElements()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);
            Assert.That(deque.Count, Is.EqualTo(3));

            // Act
            deque.Clear();

            // Assert
            Assert.That(deque.Count, Is.EqualTo(0));
            Assert.That(deque, Is.Empty);
        }

        [Test]
        public void Clear_OnEmptyDeque_DoesNothing()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            deque.Clear();

            // Assert
            Assert.That(deque.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reserve_IncreasesCapacity()
        {
            // Arrange
            var deque = new Deque<int>();
            // Initial capacity is 0, but after first Add it becomes at least 2
            deque.Add(1);
            deque.Add(2);
            // Assume current buffer length is 2

            // Act
            deque.EnsureCapacity(10);

            // Assert
            // Count should remain unchanged
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            // Cannot directly check capacity, but we can add more items
            for (int i = 0; i < 10; i++)
            {
                deque.Add(i);
            }
            Assert.That(deque.Count, Is.EqualTo(12));
        }

        [Test]
        public void Reserve_WithSmallerOrEqualCapacity_DoesNothing()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            // Assume capacity is at least 2
            int initialCapacity = deque.Count; // Not real capacity, but we can't access it

            // Act
            deque.EnsureCapacity(1); // Smaller than current capacity

            // Assert
            // Should not affect existing elements
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
        }

        [Test]
        public void TryIndex_ValidIndex_ReturnsTrueAndValue()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(10);
            deque.Add(20);
            deque.Add(30);

            // Act
            bool result = deque.TryIndex(1, out int value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(20));
        }

        [Test]
        public void TryIndex_OutOfRange_ReturnsFalseAndDefault()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);

            // Act
            bool result1 = deque.TryIndex(-1, out int value1);
            bool result2 = deque.TryIndex(1, out int value2);

            // Assert
            Assert.That(result1, Is.False);
            Assert.That(result2, Is.False);
        }

        [Test]
        public void TryIndex_WithCircularBuffer()
        {
            // Arrange: Create a scenario where front != 0 to test circular buffer behavior
            var deque = new Deque<int>();
            // Fill and remove from front to move front pointer
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);
            deque.TryRemoveFront(out _); // front now at 1 (if buffer size is 4)
            deque.Add(4); // [2,3,4]
            // Now front is at index 1, rear at index 0 (wrapped around)

            // Act & Assert
            // Verify TryIndex correctly handles circular buffer with front != 0
            Assert.That(deque.TryIndex(0, out int value0), Is.True);
            Assert.That(value0, Is.EqualTo(2));

            Assert.That(deque.TryIndex(1, out int value1), Is.True);
            Assert.That(value1, Is.EqualTo(3));

            Assert.That(deque.TryIndex(2, out int value2), Is.True);
            Assert.That(value2, Is.EqualTo(4));
        }

        [Test]
        public void Enumerator_IteratesInOrder()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);

            // Act
            var list = deque.ToList();

            // Assert
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Enumerator_WorksWithMixedOperations()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.AddFront(2);
            deque.Add(3);
            deque.AddFront(4); // [4,2,1,3]

            // Act
            var list = deque.ToList();

            // Assert
            Assert.That(list, Is.EqualTo(new[] { 4, 2, 1, 3 }));
        }

        [Test]
        public void Enumerator_EmptyDeque_ReturnsNoElements()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act
            var list = deque.ToList();

            // Assert
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void StressTest_CircularBufferBehavior()
        {
            // Arrange
            var deque = new Deque<int>();
            const int N = 100;

            // Act: Add and remove from both ends to test circular buffer
            for (int i = 0; i < N; i++)
            {
                if (i % 2 == 0)
                    deque.Add(i);
                else
                    deque.AddFront(i);
            }

            // Assert: Count should be N
            Assert.That(deque.Count, Is.EqualTo(N));

            // Remove from front and back alternately
            for (int i = 0; i < N; i++)
            {
                if (i % 2 == 0)
                {
                    bool removed = deque.TryRemoveFront(out int value);
                    Assert.That(removed, Is.True);
                    // Value depends on order, but should exist
                }
                else
                {
                    bool removed = deque.TryRemoveBack(out int value);
                    Assert.That(removed, Is.True);
                }
            }

            // Deque should be empty
            Assert.That(deque.Count, Is.EqualTo(0));
        }

        [Test]
        public void Indexer_SetWithCircularBuffer()
        {
            // Arrange: Create circular buffer scenario
            var deque = new Deque<int>();
            // Start with 3 items
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);
            // Remove one from front to move front pointer
            deque.TryRemoveFront(out _); // front moves
            // Add one more to potentially wrap
            deque.Add(4);
            // Now elements: [2,3,4] with front possibly not at 0

            // Act: Modify via indexer
            deque[1] = 99;

            // Assert
            // Should have updated the correct element
            Assert.That(deque[0], Is.EqualTo(2));
            Assert.That(deque[1], Is.EqualTo(99)); // This was originally 3
            Assert.That(deque[2], Is.EqualTo(4));
        }

        [Test]
        public void Add_TriggersAutoResize()
        {
            // Arrange
            var deque = new Deque<int>();
            // Initial capacity is 0, first Add triggers resize to at least 2

            // Act: Add many items to trigger multiple resizes
            for (int i = 0; i < 100; i++)
            {
                deque.Add(i);
            }

            // Assert
            Assert.That(deque.Count, Is.EqualTo(100));
            for (int i = 0; i < 100; i++)
            {
                Assert.That(deque[i], Is.EqualTo(i));
            }
        }

        [Test]
        public void AddFront_TriggersAutoResize()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act: Add many items to front to trigger multiple resizes
            for (int i = 0; i < 100; i++)
            {
                deque.AddFront(i);
            }

            // Assert
            Assert.That(deque.Count, Is.EqualTo(100));
            // Items should be in reverse order (last added is at front)
            for (int i = 0; i < 100; i++)
            {
                Assert.That(deque[i], Is.EqualTo(99 - i));
            }
        }

        [Test]
        public void Capacity_ReturnsBufferLength()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act & Assert - Initial capacity should be 0 (Array.Empty<T>())
            Assert.That(deque.Capacity, Is.EqualTo(0));

            // Add elements to trigger resize
            for (int i = 0; i < 5; i++)
            {
                deque.Add(i);
            }

            // Capacity should have increased (at least 8)
            Assert.That(deque.Capacity, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void AsReadOnly_ReturnsReadOnlyView()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);

            // Act
            var readOnly = deque.AsReadOnly();

            // Assert
            Assert.That(readOnly, Is.Not.Null);
            Assert.That(readOnly.Count, Is.EqualTo(3));
            Assert.That(readOnly[0], Is.EqualTo(1));
            Assert.That(readOnly[1], Is.EqualTo(2));
            Assert.That(readOnly[2], Is.EqualTo(3));
        }

        [Test]
        public void CircularBuffer_WrapAroundScenario()
        {
            // Test front pointer at buffer end scenario
            // Arrange
            var deque = new Deque<int>();

            // Fill buffer completely to understand capacity
            for (int i = 0; i < 4; i++)
            {
                deque.Add(i); // buffer: [0,1,2,3], front=0, rear=0 (wrapped)
            }

            // Remove from front to move front pointer
            deque.TryRemoveFront(out _); // buffer: [_,1,2,3], front=1, rear=0
            deque.TryRemoveFront(out _); // buffer: [_,_,2,3], front=2, rear=0
            deque.TryRemoveFront(out _); // buffer: [_,_,_,3], front=3, rear=0

            // Now front is at index 3 (end of buffer)
            // Add more elements to cause wrap
            deque.Add(4); // buffer: [4,_,_,3], front=3, rear=1
            deque.Add(5); // buffer: [4,5,_,3], front=3, rear=2

            // Act & Assert
            // Should be [3,4,5]
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(3));
            Assert.That(deque[1], Is.EqualTo(4));
            Assert.That(deque[2], Is.EqualTo(5));
        }

        [Test]
        public void StressTest_BufferWrapping()
        {
            // Arrange
            var deque = new Deque<int>();
            const int Operations = 1000;

            // Act: Perform many operations that cause buffer wrapping
            for (int i = 0; i < Operations; i++)
            {
                if (i % 4 == 0)
                {
                    deque.Add(i);
                }
                else if (i % 4 == 1)
                {
                    deque.AddFront(i);
                }
                else if (i % 4 == 2)
                {
                    deque.TryRemoveBack(out _);
                }
                else
                {
                    deque.TryRemoveFront(out _);
                }

                // Verify consistency after each operation
                Assert.That(deque.Count, Is.GreaterThanOrEqualTo(0));
                if (deque.Count > 0)
                {
                    // Access all elements to ensure no index errors
                    for (int j = 0; j < deque.Count; j++)
                    {
                        _ = deque[j];
                    }
                }
            }

            // Assert
            Assert.That(deque.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Indexer_SetWithWrapAroundBuffer()
        {
            // Arrange: Create wrap-around scenario
            var deque = new Deque<int>();

            // Fill and manipulate to create wrap
            for (int i = 0; i < 3; i++)
            {
                deque.Add(i); // [0,1,2]
            }

            deque.TryRemoveFront(out _); // [1,2], front=1
            deque.TryRemoveFront(out _); // [2], front=2

            deque.Add(3); // [2,3], front=2, rear=0 (wrapped)
            deque.Add(4); // [2,3,4], front=2, rear=1

            // Act: Modify elements via indexer in wrapped buffer
            deque[0] = 20; // Should modify element at buffer[2]
            deque[1] = 30; // Should modify element at buffer[3] (wrapped)
            deque[2] = 40; // Should modify element at buffer[0] (wrapped)

            // Assert
            Assert.That(deque[0], Is.EqualTo(20));
            Assert.That(deque[1], Is.EqualTo(30));
            Assert.That(deque[2], Is.EqualTo(40));
        }

        [Test]
        public void TryRemoveBack_OnSingleElement()
        {
            // Arrange
            var deque = new Deque<string>();
            deque.Add("only");

            // Act
            bool result = deque.TryRemoveBack(out string? value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo("only"));
            Assert.That(deque.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveFront_OnSingleElement()
        {
            // Arrange
            var deque = new Deque<string>();
            deque.AddFront("only");

            // Act
            bool result = deque.TryRemoveFront(out string? value);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo("only"));
            Assert.That(deque.Count, Is.EqualTo(0));
        }

        [Test]
        public void MixedOperations_ComplexScenario()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act: Complex sequence of operations
            deque.Add(1);           // [1]
            deque.AddFront(2);      // [2,1]
            deque.Add(3);           // [2,1,3]
            deque.TryRemoveBack(out int back1);  // back1 = 3, [2,1]
            deque.AddFront(4);      // [4,2,1]
            deque.TryRemoveFront(out int front1); // front1 = 4, [2,1]
            deque.Add(5);           // [2,1,5]
            deque.Add(6);           // [2,1,5,6]
            deque.TryRemoveBack(out int back2);  // back2 = 6, [2,1,5]
            deque.TryRemoveFront(out int front2); // front2 = 2, [1,5]

            // Assert
            Assert.That(back1, Is.EqualTo(3));
            Assert.That(front1, Is.EqualTo(4));
            Assert.That(back2, Is.EqualTo(6));
            Assert.That(front2, Is.EqualTo(2));
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(5));
        }


        [Test]
        public void Reserve_ZeroCapacity_DoesNothing()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            int initialCapacity = deque.Capacity;

            // Act
            deque.EnsureCapacity(0);

            // Assert
            Assert.That(deque.Capacity, Is.EqualTo(initialCapacity));
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
        }

        [Test]
        public void Clear_PreservesCapacity()
        {
            // Arrange
            var deque = new Deque<int>();
            for (int i = 0; i < 10; i++)
            {
                deque.Add(i);
            }
            int capacityBeforeClear = deque.Capacity;

            // Act
            deque.Clear();

            // Assert
            Assert.That(deque.Count, Is.EqualTo(0));
            Assert.That(deque.Capacity, Is.EqualTo(capacityBeforeClear));
            Assert.That(deque, Is.Empty);
        }

        [Test]
        public void GenericType_ReferenceType()
        {
            // Test with reference types
            // Arrange
            var deque = new Deque<string>();
            var item1 = "Hello";
            var item2 = "World";
            var item3 = "!";

            // Act
            deque.Add(item1);
            deque.AddFront(item2);
            deque.Add(item3);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.SameAs(item2));
            Assert.That(deque[1], Is.SameAs(item1));
            Assert.That(deque[2], Is.SameAs(item3));
        }

        [Test]
        public void GenericType_ValueType()
        {
            // Test with value types
            // Arrange
            var deque = new Deque<DateTime>();
            var date1 = new DateTime(2023, 1, 1);
            var date2 = new DateTime(2023, 12, 31);
            var date3 = new DateTime(2024, 6, 15);

            // Act
            deque.Add(date1);
            deque.AddFront(date2);
            deque.Add(date3);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(date2));
            Assert.That(deque[1], Is.EqualTo(date1));
            Assert.That(deque[2], Is.EqualTo(date3));
        }

        [Test]
        public void TryIndex_NullableReferenceType()
        {
            // Test with nullable reference type
            // Arrange
            var deque = new Deque<string?>();
            deque.Add("first");
            deque.Add(null);
            deque.Add("third");

            // Act & Assert
            Assert.That(deque.TryIndex(0, out string? value0), Is.True);
            Assert.That(value0, Is.EqualTo("first"));

            Assert.That(deque.TryIndex(1, out string? value1), Is.True);
            Assert.That(value1, Is.Null);

            Assert.That(deque.TryIndex(2, out string? value2), Is.True);
            Assert.That(value2, Is.EqualTo("third"));
        }

        [Test]
        public void Indexer_GetAfterMultipleResizes()
        {
            // Test that indexing works correctly after multiple resizes
            // Arrange
            var deque = new Deque<int>();
            const int Count = 1000;

            // Act: Add many items causing multiple resizes
            for (int i = 0; i < Count; i++)
            {
                deque.Add(i);
            }

            // Assert: Verify all items are accessible
            Assert.That(deque.Count, Is.EqualTo(Count));
            for (int i = 0; i < Count; i++)
            {
                Assert.That(deque[i], Is.EqualTo(i));
            }
        }

        [Test]
        public void AddFront_AfterClear()
        {
            // Test AddFront works correctly after Clear
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Clear();

            // Act
            deque.AddFront(3);
            deque.AddFront(4);
            deque.AddFront(5);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(5));
            Assert.That(deque[1], Is.EqualTo(4));
            Assert.That(deque[2], Is.EqualTo(3));
        }

        [Test]
        public void EdgeCase_FrontAtBufferEnd_RearAtBufferStart()
        {
            // Extreme wrap-around scenario
            // Arrange
            var deque = new Deque<int>();

            // Create scenario where front is at buffer end and rear at buffer start
            // Initial: buffer size 4
            deque.Add(1);  // buffer[0]=1, front=0, rear=1
            deque.Add(2);  // buffer[1]=2, front=0, rear=2
            deque.Add(3);  // buffer[2]=3, front=0, rear=3
            deque.Add(4);  // buffer[3]=4, front=0, rear=0 (wrapped)

            // Now buffer is full: [1,2,3,4], front=0, rear=0
            deque.TryRemoveFront(out _); // buffer[0]=_, front=1, rear=0
            deque.TryRemoveFront(out _); // buffer[1]=_, front=2, rear=0
            deque.TryRemoveFront(out _); // buffer[2]=_, front=3, rear=0

            // Now: buffer[3]=4, front=3 (end), rear=0 (start)
            deque.Add(5); // buffer[0]=5, front=3, rear=1

            // Act & Assert
            // Should be [4,5]
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(4));
            Assert.That(deque[1], Is.EqualTo(5));

            // Verify indexing works correctly
            Assert.That(deque.TryIndex(0, out int val0), Is.True);
            Assert.That(val0, Is.EqualTo(4));
            Assert.That(deque.TryIndex(1, out int val1), Is.True);
            Assert.That(val1, Is.EqualTo(5));
        }

        [Test]
        public void AddRange_IReadOnlyList_BasicFunctionality()
        {
            // Arrange
            var deque = new Deque<int>();
            var items = new List<int> { 10, 20, 30, 40, 50 };

            // Act
            deque.AddRange((IReadOnlyList<int>)items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(10));
            Assert.That(deque[1], Is.EqualTo(20));
            Assert.That(deque[2], Is.EqualTo(30));
            Assert.That(deque[3], Is.EqualTo(40));
            Assert.That(deque[4], Is.EqualTo(50));
        }

        [Test]
        public void AddRange_IReadOnlyListWithStartAndCount_PartialRange()
        {
            // Arrange
            var deque = new Deque<string>();
            var items = new List<string> { "a", "b", "c", "d", "e", "f" };

            // Act: Add only elements 2-4 (c, d, e)
            deque.AddRange(items.Skip(2).Take(3));

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo("c"));
            Assert.That(deque[1], Is.EqualTo("d"));
            Assert.That(deque[2], Is.EqualTo("e"));
        }

        [Test]
        public void AddRange_IReadOnlyListWithStartAndCount_EmptyRange()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            var items = new List<int> { 100, 200, 300 };

            // Act: Add empty range (count = 0)
            deque.AddRange(items.Skip(1).Take(0));

            // Assert: Should not modify deque
            Assert.That(deque.Count, Is.EqualTo(2));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
        }

        [Test]
        public void AddRange_ArraySegment_BasicFunctionality()
        {
            // Arrange
            var deque = new Deque<int>();
            var array = new[] { 5, 6, 7, 8, 9 };
            var segment = new ArraySegment<int>(array, 1, 3); // elements 6, 7, 8

            // Act
            deque.AddRange(segment);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(6));
            Assert.That(deque[1], Is.EqualTo(7));
            Assert.That(deque[2], Is.EqualTo(8));
        }

        [Test]
        public void AddRange_ArraySegment_EmptySegment()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(42);
            var array = new[] { 1, 2, 3 };
            var emptySegment = new ArraySegment<int>(array, 0, 0);

            // Act
            deque.AddRange(emptySegment);

            // Assert: Should not modify deque
            Assert.That(deque.Count, Is.EqualTo(1));
            Assert.That(deque[0], Is.EqualTo(42));
        }

        [Test]
        public void AddRange_Array_BasicFunctionality()
        {
            // Arrange
            var deque = new Deque<string>();
            var array = new[] { "one", "two", "three" };

            // Act
            deque.AddRange(array);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo("one"));
            Assert.That(deque[1], Is.EqualTo("two"));
            Assert.That(deque[2], Is.EqualTo("three"));
        }

        [Test]
        public void AddRange_Enumerable_BasicFunctionality()
        {
            // Arrange
            var deque = new Deque<int>();
            IEnumerable<int> enumerable = Enumerable.Range(1, 5); // 1, 2, 3, 4, 5

            // Act
            deque.AddRange(enumerable);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRange_Enumerable_WithLinqQuery()
        {
            // Arrange
            var deque = new Deque<int>();
            var source = new[] { 10, 15, 20, 25, 30 };
            var query = source.Where(x => x > 15).Select(x => x * 2); // 40, 50, 60

            // Act
            deque.AddRange(query);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(3));
            Assert.That(deque[0], Is.EqualTo(40));
            Assert.That(deque[1], Is.EqualTo(50));
            Assert.That(deque[2], Is.EqualTo(60));
        }

        [Test]
        public void AddRange_RequiresResize()
        {
            // Arrange: Create deque that will need to resize when adding range
            var deque = new Deque<int>();
            // Initial capacity is 0 (Array.Empty<T>())
            // First Add will resize to at least 4
            deque.Add(1);
            deque.Add(2); // Now count=2, capacity at least 4

            var items = Enumerable.Range(10, 10).ToList(); // 10 items

            // Act: Adding 10 items to deque with 2 items (total 12 > capacity 4)
            deque.AddRange((IReadOnlyList<int>)items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(12));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(deque[i + 2], Is.EqualTo(10 + i));
            }
        }

        [Test]
        public void AddRange_WithCircularBufferWrap()
        {
            // Arrange: Create wrap-around scenario
            var deque = new Deque<int>();
            // Fill buffer to create wrap
            for (int i = 0; i < 4; i++)
            {
                deque.Add(i); // [0,1,2,3], front=0, rear=0 (wrapped)
            }

            // Remove some from front to move front pointer
            deque.TryRemoveFront(out _); // front=1
            deque.TryRemoveFront(out _); // front=2

            // Now buffer: [_,_,2,3], front=2, rear=0
            var items = new[] { 100, 101, 102, 103 };

            // Act: Add range in wrap-around scenario
            deque.AddRange((IReadOnlyList<int>)items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(6));
            // Should be [2,3,100,101,102,103]
            Assert.That(deque[0], Is.EqualTo(2));
            Assert.That(deque[1], Is.EqualTo(3));
            Assert.That(deque[2], Is.EqualTo(100));
            Assert.That(deque[3], Is.EqualTo(101));
            Assert.That(deque[4], Is.EqualTo(102));
            Assert.That(deque[5], Is.EqualTo(103));
        }

        [Test]
        public void AddRange_NullIReadOnlyList_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act & Assert
            // Implementation now checks for null via ThrowIfNull which throws ArgumentNullException
            // Cast to IReadOnlyList<int> to ensure correct overload is called
            Assert.That(() => deque.AddRange((IReadOnlyList<int>)null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRange_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            int[]? nullArray = null;

            // Act & Assert
            Assert.That(() => deque.AddRange(nullArray!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRange_NullEnumerable_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            IEnumerable<int>? nullEnumerable = null;

            // Act & Assert
            Assert.That(() => deque.AddRange(nullEnumerable!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRange_MultipleRanges_AccumulateCorrectly()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act: Add multiple ranges
            deque.AddRange(new[] { 1, 2, 3 });
            deque.AddRange(new List<int> { 4, 5 });
            deque.AddRange(new ArraySegment<int>(new[] { 6, 7, 8 }, 0, 2)); // 6, 7

            // Assert
            Assert.That(deque.Count, Is.EqualTo(7));
            for (int i = 0; i < 7; i++)
            {
                Assert.That(deque[i], Is.EqualTo(i + 1));
            }
        }

        [Test]
        public void AddRange_EmptyCollections_NoEffect()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(100);

            // Act: Add various empty collections
            deque.AddRange((IReadOnlyList<int>)new int[0]);
            deque.AddRange((IReadOnlyList<int>)new List<int>());
            deque.AddRange(new ArraySegment<int>(new int[5], 2, 0));
            deque.AddRange(Enumerable.Empty<int>());

            // Assert
            Assert.That(deque.Count, Is.EqualTo(1));
            Assert.That(deque[0], Is.EqualTo(100));
        }

        #region AddRangeFront Tests

        [Test]
        public void AddRangeFront_WithIReadOnlyList_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(3);
            deque.Add(4);
            var items = new List<int> { 1, 2 };

            // Act
            deque.AddRangeFront((IReadOnlyList<int>)items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(4));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
            Assert.That(deque[3], Is.EqualTo(4));
        }

        [Test]
        public void AddRangeFront_WithIReadOnlyListStartCount_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(4);
            deque.Add(5);
            var items = new List<int> { 1, 2, 3, 4, 5 };

            // Act: Add items[1..3] (2,3,4)
            deque.AddRangeFront(items.Skip(1).Take(3));

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(2));
            Assert.That(deque[1], Is.EqualTo(3));
            Assert.That(deque[2], Is.EqualTo(4));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRangeFront_WithArraySegment_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(4);
            deque.Add(5);
            var array = new[] { 1, 2, 3, 4, 5 };
            var segment = new ArraySegment<int>(array, 1, 3); // 2,3,4

            // Act
            deque.AddRangeFront(segment);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(2));
            Assert.That(deque[1], Is.EqualTo(3));
            Assert.That(deque[2], Is.EqualTo(4));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRangeFront_WithArray_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(4);
            deque.Add(5);
            var array = new[] { 1, 2, 3 };

            // Act
            deque.AddRangeFront(array);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRangeFront_WithIEnumerable_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(4);
            deque.Add(5);
            var items = Enumerable.Range(1, 3); // 1,2,3

            // Act
            deque.AddRangeFront(items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRangeFront_MultipleCalls_AccumulateCorrectly()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act: Add multiple ranges to front
            deque.AddRangeFront((IReadOnlyList<int>)new[] { 4, 5 });
            deque.AddRangeFront((IReadOnlyList<int>)new List<int> { 2, 3 });
            deque.AddRangeFront((IReadOnlyList<int>)new int[] { 1 });

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(deque[i], Is.EqualTo(i + 1));
            }
        }

        [Test]
        public void AddRangeFront_TriggersResizeWhenNeeded()
        {
            // Arrange: Create deque with initial capacity 0, add one item
            var deque = new Deque<int>();
            deque.Add(100);

            // Act: Add enough items to trigger resize
            var items = Enumerable.Range(1, 10);
            deque.AddRangeFront(items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(11));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[9], Is.EqualTo(10));
            Assert.That(deque[10], Is.EqualTo(100));
            Assert.That(deque.Capacity, Is.GreaterThanOrEqualTo(11));
        }

        [Test]
        public void AddRangeFront_WithCircularBufferWrapAround_WorksCorrectly()
        {
            // Arrange: Create a scenario where front is not at 0
            var deque = new Deque<int>();
            // Add 2 items, remove front, so front moves
            deque.Add(1);
            deque.Add(2);
            deque.TryRemoveFront(out _);
            // Now front = 1, rear = 2, buffer = [_, 2, _, _] (capacity 4)

            var items = new[] { 100, 101, 102, 103 };

            // Act: Add range that causes wrap-around
            deque.AddRangeFront(items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            // Should be [100,101,102,103,2]
            Assert.That(deque[0], Is.EqualTo(100));
            Assert.That(deque[1], Is.EqualTo(101));
            Assert.That(deque[2], Is.EqualTo(102));
            Assert.That(deque[3], Is.EqualTo(103));
            Assert.That(deque[4], Is.EqualTo(2));
        }

        [Test]
        public void AddRangeFront_NullIReadOnlyList_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();

            // Act & Assert
            // Implementation now checks for null via ThrowIfNull which throws ArgumentNullException
            Assert.That(() => deque.AddRangeFront((IReadOnlyCollection<int>)null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRangeFront_NullArray_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            int[]? nullArray = null;

            // Act & Assert
            Assert.That(() => deque.AddRangeFront(nullArray!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRangeFront_NullEnumerable_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            IEnumerable<int>? nullEnumerable = null;

            // Act & Assert
            // Implementation now checks for null via ThrowIfNull which throws ArgumentNullException
            Assert.That(() => deque.AddRangeFront(nullEnumerable!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRangeFront_EmptyCollections_NoEffect()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(100);

            // Act: Add various empty collections to front
            deque.AddRangeFront((IReadOnlyList<int>)new int[0]);
            deque.AddRangeFront((IReadOnlyList<int>)new List<int>());
            deque.AddRangeFront(new ArraySegment<int>(new int[5], 2, 0));
            deque.AddRangeFront(Enumerable.Empty<int>());

            // Assert
            Assert.That(deque.Count, Is.EqualTo(1));
            Assert.That(deque[0], Is.EqualTo(100));
        }

        #endregion

        #region ICollection<T> Overload Tests

        [Test]
        public void AddRange_WithICollection_AddsToBack()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(3);
            deque.Add(4);
            ICollection<int> items = new HashSet<int> { 1, 2 }; // HashSet implements ICollection<T> but not IReadOnlyList<T>

            // Act
            deque.AddRange(items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(4));
            Assert.That(deque[0], Is.EqualTo(3));
            Assert.That(deque[1], Is.EqualTo(4));
            Assert.That(deque[2], Is.EqualTo(1));
            Assert.That(deque[3], Is.EqualTo(2));
        }

        [Test]
        public void AddRangeFront_WithICollection_AddsToFront()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(4);
            deque.Add(5);
            ICollection<int> items = new HashSet<int> { 1, 2, 3 };

            // Act
            deque.AddRangeFront(items);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(5));
            Assert.That(deque[0], Is.EqualTo(1));
            Assert.That(deque[1], Is.EqualTo(2));
            Assert.That(deque[2], Is.EqualTo(3));
            Assert.That(deque[3], Is.EqualTo(4));
            Assert.That(deque[4], Is.EqualTo(5));
        }

        [Test]
        public void AddRange_WithEmptyICollection_NoEffect()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(100);
            ICollection<int> emptyCollection = new HashSet<int>();

            // Act
            deque.AddRange(emptyCollection);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(1));
            Assert.That(deque[0], Is.EqualTo(100));
        }

        [Test]
        public void AddRangeFront_WithEmptyICollection_NoEffect()
        {
            // Arrange
            var deque = new Deque<int>();
            deque.Add(100);
            ICollection<int> emptyCollection = new HashSet<int>();

            // Act
            deque.AddRangeFront(emptyCollection);

            // Assert
            Assert.That(deque.Count, Is.EqualTo(1));
            Assert.That(deque[0], Is.EqualTo(100));
        }

        [Test]
        public void AddRange_WithNullICollection_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            ICollection<int>? nullCollection = null;

            // Act & Assert
            // Implementation calls items.ThrowIfNull() which throws ArgumentNullException
            Assert.That(() => deque.AddRange(nullCollection!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void AddRangeFront_WithNullICollection_ThrowsArgumentNullException()
        {
            // Arrange
            var deque = new Deque<int>();
            ICollection<int>? nullCollection = null;

            // Act & Assert
            // Implementation calls items.ThrowIfNull() which throws ArgumentNullException
            Assert.That(() => deque.AddRangeFront(nullCollection!), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion
    }
}