using NUnit.Framework;
using JiksLib.Collections;
using System;
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
            deque.Reserve(10);

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
            deque.Reserve(1); // Smaller than current capacity

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
            // Arrange: Create a scenario where front != 0
            var deque = new Deque<int>();
            // Fill and remove from front to move front pointer
            deque.Add(1);
            deque.Add(2);
            deque.Add(3);
            deque.TryRemoveFront(out _); // front now at 1 (if buffer size is 4?)
            deque.Add(4); // [2,3,4]
            // Now front is at index 1, rear at index 0? Actually depends on buffer size

            // Act & Assert
            // NOTE: This test may fail due to a bug in TryIndex method.
            // TryIndex currently uses buffer[index] instead of buffer[CalcIndex(index)]
            // If this test fails, it indicates the bug in TryIndex implementation.
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
    }
}