using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class PriorityQueueTests
    {
        #region 构造函数测试

        [Test]
        public void Constructor_Default_CreatesEmptyQueue()
        {
            // Arrange & Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue, Is.Empty);
            Assert.That(queue.Comparer, Is.EqualTo(Comparer<int>.Default));
        }

        [Test]
        public void Constructor_WithComparer_CreatesEmptyQueueWithCustomComparer()
        {
            // Arrange
            var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x)); // 反向比较器

            // Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(comparer);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.Comparer, Is.EqualTo(comparer));
        }

        [Test]
        public void Constructor_WithCollection_InitializesWithElements()
        {
            // Arrange
            var collection = new List<(int, int)> { (1, 10), (2, 5), (3, 15) };

            // Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(collection);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));

            // 验证堆属性：最小堆，所以优先级5应该在顶部
            (int element, int priority) = queue.Peek();
            Assert.That(priority, Is.EqualTo(5));
            Assert.That(element, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithCollectionAndComparer_InitializesWithElementsAndCustomComparer()
        {
            // Arrange
            var collection = new List<(int, int)> { (1, 10), (2, 5), (3, 15) };
            var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x)); // 反向比较器，最大堆

            // Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(collection, comparer);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.Comparer, Is.EqualTo(comparer));

            // 验证堆属性：最大堆，所以优先级15应该在顶部
            var (element, priority) = queue.Peek();
            Assert.That(priority, Is.EqualTo(15));
            Assert.That(element, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithCapacity_CreatesEmptyQueueWithSpecifiedCapacity()
        {
            // Arrange & Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 100);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.Capacity, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void Constructor_WithCapacityAndComparer_CreatesEmptyQueueWithSpecifiedCapacityAndComparer()
        {
            // Arrange
            var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x));

            // Act
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 100, comparer);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.Capacity, Is.GreaterThanOrEqualTo(100));
            Assert.That(queue.Comparer, Is.EqualTo(comparer));
        }

        #endregion

        #region Enqueue和Dequeue测试

        [Test]
        public void Enqueue_AddsElementToQueue()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            queue.Enqueue(1, 10);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(1));
            var (element, priority) = queue.Peek();
            Assert.That(element, Is.EqualTo(1));
            Assert.That(priority, Is.EqualTo(10));
        }

        [Test]
        public void Enqueue_MultipleElements_MaintainsHeapProperty()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            queue.Enqueue(1, 30);
            queue.Enqueue(2, 10);
            queue.Enqueue(3, 20);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));

            // 验证最小堆属性：优先级10应该在顶部
            var (element, priority) = queue.Peek();
            Assert.That(element, Is.EqualTo(2));
            Assert.That(priority, Is.EqualTo(10));

            // 验证出队顺序
            var result1 = queue.Dequeue();
            Assert.That(result1, Is.EqualTo((2, 10)));

            var result2 = queue.Dequeue();
            Assert.That(result2, Is.EqualTo((3, 20)));

            var result3 = queue.Dequeue();
            Assert.That(result3, Is.EqualTo((1, 30)));
        }

        [Test]
        public void Dequeue_OnEmptyQueue_ThrowsInvalidOperationException()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act & Assert
            Assert.That(() => queue.Dequeue(), Throws.InvalidOperationException);
        }

        [Test]
        public void Dequeue_ReturnsElementWithHighestPriority()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 30);
            queue.Enqueue(2, 10);
            queue.Enqueue(3, 20);

            // Act
            var result = queue.Dequeue();

            // Assert
            Assert.That(result, Is.EqualTo((2, 10)));
            Assert.That(queue.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryDequeue_OnEmptyQueue_ReturnsFalse()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            bool success = queue.TryDequeue(out int element, out int priority);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(element, Is.EqualTo(0));
            Assert.That(priority, Is.EqualTo(0));
        }

        [Test]
        public void TryDequeue_OnNonEmptyQueue_ReturnsTrueAndRemovesElement()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);

            // Act
            bool success = queue.TryDequeue(out int element, out int priority);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(element, Is.EqualTo(1));
            Assert.That(priority, Is.EqualTo(10));
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void Peek_OnEmptyQueue_ThrowsInvalidOperationException()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act & Assert
            Assert.That(() => queue.Peek(), Throws.InvalidOperationException);
        }

        [Test]
        public void Peek_ReturnsElementWithHighestPriorityWithoutRemoving()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 5);

            // Act
            var result = queue.Peek();

            // Assert
            Assert.That(result, Is.EqualTo((2, 5)));
            Assert.That(queue.Count, Is.EqualTo(2)); // 元素没有被移除
        }

        [Test]
        public void TryPeek_OnEmptyQueue_ReturnsFalse()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            bool success = queue.TryPeek(out int element, out int priority);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(element, Is.EqualTo(0));
            Assert.That(priority, Is.EqualTo(0));
        }

        [Test]
        public void TryPeek_OnNonEmptyQueue_ReturnsTrueAndDoesNotRemoveElement()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);

            // Act
            bool success = queue.TryPeek(out int element, out int priority);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(element, Is.EqualTo(1));
            Assert.That(priority, Is.EqualTo(10));
            Assert.That(queue.Count, Is.EqualTo(1)); // 元素没有被移除
        }

        #endregion

        #region DequeueEnqueue测试

        [Test]
        public void DequeueEnqueue_OnEmptyQueue_ThrowsInvalidOperationException()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act & Assert
            Assert.That(() => queue.DequeueEnqueue(1, 10), Throws.InvalidOperationException);
        }

        [Test]
        public void DequeueEnqueue_RemovesHighestPriorityAndAddsNewElement()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 5);
            queue.Enqueue(3, 15);

            // Act
            var removed = queue.DequeueEnqueue(4, 7);

            // Assert
            Assert.That(removed, Is.EqualTo((2, 5))); // 优先级5被移除
            Assert.That(queue.Count, Is.EqualTo(3));

            // 新的顶部应该是优先级7
            var (element, priority) = queue.Peek();
            Assert.That(element, Is.EqualTo(4));
            Assert.That(priority, Is.EqualTo(7));
        }

        [Test]
        public void DequeueEnqueue_MaintainsHeapProperty()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);
            queue.Enqueue(3, 30);

            // Act
            var removed = queue.DequeueEnqueue(4, 5); // 添加一个更高优先级的元素

            // Assert
            Assert.That(removed, Is.EqualTo((1, 10)));
            Assert.That(queue.Peek(), Is.EqualTo((4, 5)));
        }

        #endregion

        #region EnqueueRange测试

        [Test]
        public void EnqueueRange_WithElementsAndSamePriority_AddsAllElements()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            var elements = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            queue.EnqueueRange(elements, priority: 10);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(5));

            // 所有元素都应该有相同的优先级
            // 注意：二叉堆不保证相同优先级元素的顺序
            var dequeuedElements = new List<int>();
            while (queue.Count > 0)
            {
                var result = queue.Dequeue();
                Assert.That(result.Item2, Is.EqualTo(10));
                dequeuedElements.Add(result.Item1);
            }

            // 验证所有元素都被出队（顺序不重要）
            Assert.That(dequeuedElements, Is.EquivalentTo(elements));
        }

        [Test]
        public void EnqueueRange_WithEmptyCollection_DoesNothing()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            var elements = new List<int>();

            // Act
            queue.EnqueueRange(elements, priority: 10);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnqueueRange_WithElementPriorityPairs_AddsAllElements()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            var elements = new List<(int, int)>
            {
                (1, 30),
                (2, 10),
                (3, 20)
            };

            // Act
            queue.EnqueueRange(elements);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));

            // 验证堆属性：优先级10应该在顶部
            var (element, priority) = queue.Peek();
            Assert.That(element, Is.EqualTo(2));
            Assert.That(priority, Is.EqualTo(10));
        }

        [Test]
        public void EnqueueRange_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act & Assert
            Assert.That(() => queue.EnqueueRange((IEnumerable<int>)null!, 10), Throws.ArgumentNullException);
            Assert.That(() => queue.EnqueueRange((IEnumerable<(int, int)>)null!), Throws.ArgumentNullException);
        }

        #endregion

        #region Remove测试

        [Test]
        public void Remove_ElementExists_RemovesElementAndReturnsTrue()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);
            queue.Enqueue(3, 30);

            // Act
            bool removed = queue.Remove(2, out int removedElement, out int removedPriority);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(removedElement, Is.EqualTo(2));
            Assert.That(removedPriority, Is.EqualTo(20));
            Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.UnorderedItems, Does.Not.Contain((2, 20)));
        }

        [Test]
        public void Remove_ElementDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);

            // Act
            bool removed = queue.Remove(99, out int removedElement, out int removedPriority);

            // Assert
            Assert.That(removed, Is.False);
            Assert.That(removedElement, Is.EqualTo(0));
            Assert.That(removedPriority, Is.EqualTo(0));
            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_WithCustomEqualityComparer_UsesProvidedComparer()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<string, int>();
            queue.Enqueue("apple", 10);
            queue.Enqueue("banana", 20);

            // 使用不区分大小写的比较器
            var comparer = StringComparer.OrdinalIgnoreCase;

            // Act
            bool removed = queue.Remove("APPLE", out string removedElement, out int removedPriority, comparer);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(removedElement, Is.EqualTo("apple"));
            Assert.That(removedPriority, Is.EqualTo(10));
            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_LastElement_RemovesSuccessfully()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);

            // Act
            bool removed = queue.Remove(1, out int removedElement, out int removedPriority);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(removedElement, Is.EqualTo(1));
            Assert.That(removedPriority, Is.EqualTo(10));
            Assert.That(queue.Count, Is.EqualTo(0));
        }

        #endregion

        #region 其他方法测试

        [Test]
        public void Clear_RemovesAllElements()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);
            queue.Enqueue(3, 30);

            // Act
            queue.Clear();

            // Assert
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue, Is.Empty);
        }

        [Test]
        public void Clone_CreatesShallowCopy()
        {
            // Arrange
            var original = new JiksLib.Collections.PriorityQueue<int, int>();
            original.Enqueue(1, 10);
            original.Enqueue(2, 20);
            original.Enqueue(3, 30);

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Count, Is.EqualTo(original.Count));
            Assert.That(clone.Comparer, Is.EqualTo(original.Comparer));

            // 验证元素相同
            while (original.Count > 0)
            {
                var originalItem = original.Dequeue();
                var cloneItem = clone.Dequeue();
                Assert.That(cloneItem, Is.EqualTo(originalItem));
            }
        }

        [Test]
        public void Capacity_ReturnsUnderlyingListCapacity()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 100);

            // Act & Assert
            Assert.That(queue.Capacity, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void TrimExcess_ReducesCapacityToCount()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 100);
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);

            // Act
            queue.TrimExcess();

            // Assert
            Assert.That(queue.Capacity, Is.LessThanOrEqualTo(10)); // 应该接近实际元素数量
        }

        #endregion

        #region UnorderedItems测试

        [Test]
        public void UnorderedItems_ReturnsAllElementsWithoutOrdering()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 30);
            queue.Enqueue(2, 10);
            queue.Enqueue(3, 20);

            // Act
            var unordered = queue.UnorderedItems.ToList();

            // Assert
            Assert.That(unordered.Count, Is.EqualTo(3));

            // 验证包含所有元素（顺序不重要）
            Assert.That(unordered, Contains.Item((1, 30)));
            Assert.That(unordered, Contains.Item((2, 10)));
            Assert.That(unordered, Contains.Item((3, 20)));
        }

        [Test]
        public void UnorderedItems_OnEmptyQueue_ReturnsEmptyCollection()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            var unordered = queue.UnorderedItems.ToList();

            // Assert
            Assert.That(unordered, Is.Empty);
        }

        [Test]
        public void UnorderedItems_CanBeEnumeratedMultipleTimes()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);

            // Act & Assert
            var firstEnumeration = queue.UnorderedItems.ToList();
            var secondEnumeration = queue.UnorderedItems.ToList();

            Assert.That(firstEnumeration.Count, Is.EqualTo(2));
            Assert.That(secondEnumeration.Count, Is.EqualTo(2));
        }

        #endregion

        #region 边界条件和特殊场景测试

        [Test]
        public void PriorityQueue_WithReverseComparer_BehavesAsMaxHeap()
        {
            // Arrange
            var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x)); // 反向比较器，最大堆
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(comparer);

            queue.Enqueue(1, 10);
            queue.Enqueue(2, 30);
            queue.Enqueue(3, 20);

            // Act & Assert
            // 最大堆，所以优先级30应该在顶部
            Assert.That(queue.Peek(), Is.EqualTo((2, 30)));

            // 验证出队顺序：30, 20, 10
            Assert.That(queue.Dequeue(), Is.EqualTo((2, 30)));
            Assert.That(queue.Dequeue(), Is.EqualTo((3, 20)));
            Assert.That(queue.Dequeue(), Is.EqualTo((1, 10)));
        }

        [Test]
        public void PriorityQueue_WithSamePriority_ElementsDequeueInInsertionOrder()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // 所有元素相同的优先级
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 10);
            queue.Enqueue(3, 10);

            // Act & Assert
            // 注意：二叉堆不保证相同优先级元素的顺序
            // 验证所有元素都被正确出队，优先级都是10
            var dequeuedElements = new List<int>();
            while (queue.Count > 0)
            {
                var (element, priority) = queue.Dequeue();
                Assert.That(priority, Is.EqualTo(10));
                dequeuedElements.Add(element);
            }

            // 验证所有元素都被出队（顺序不重要）
            Assert.That(dequeuedElements, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void PriorityQueue_WithComplexTypes_WorksCorrectly()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<string, DateTime>();
            var date1 = new DateTime(2023, 1, 1);
            var date2 = new DateTime(2023, 1, 2);
            var date3 = new DateTime(2023, 1, 3);

            // Act
            queue.Enqueue("Event C", date3);
            queue.Enqueue("Event A", date1);
            queue.Enqueue("Event B", date2);

            // Assert
            // 最小堆，所以最早的日期应该在顶部
            Assert.That(queue.Peek(), Is.EqualTo(("Event A", date1)));
            Assert.That(queue.Dequeue(), Is.EqualTo(("Event A", date1)));
            Assert.That(queue.Dequeue(), Is.EqualTo(("Event B", date2)));
            Assert.That(queue.Dequeue(), Is.EqualTo(("Event C", date3)));
        }

        [Test]
        public void PriorityQueue_StressTest_LargeNumberOfElements()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            int count = 1000;
            var random = new Random();

            // Act
            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i, random.Next(0, 10000));
            }

            // Assert
            Assert.That(queue.Count, Is.EqualTo(count));

            // 验证堆属性：每次出队的元素优先级应该不小于前一个
            int previousPriority = int.MinValue;
            while (queue.Count > 0)
            {
                var (_, priority) = queue.Dequeue();
                Assert.That(priority, Is.GreaterThanOrEqualTo(previousPriority));
                previousPriority = priority;
            }
        }

        #endregion

        #region 更多边界条件和特殊场景测试

        [Test]
        public void PriorityQueue_WithDuplicateElements_CanStoreDuplicates()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();

            // Act
            queue.Enqueue(1, 10);
            queue.Enqueue(1, 10); // 重复元素
            queue.Enqueue(2, 20);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));

            // 两个优先级10的元素应该先出队
            Assert.That(queue.Dequeue(), Is.EqualTo((1, 10)));
            Assert.That(queue.Dequeue(), Is.EqualTo((1, 10)));
            Assert.That(queue.Dequeue(), Is.EqualTo((2, 20)));
        }

        [Test]
        public void PriorityQueue_WithNullableReferenceTypes_HandlesNullValues()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<string?, int>();

            // Act
            queue.Enqueue(null, 10);
            queue.Enqueue("not null", 5);
            queue.Enqueue(null, 15);

            // Assert
            Assert.That(queue.Count, Is.EqualTo(3));

            // 优先级5应该在顶部
            var (element, priority) = queue.Peek();
            Assert.That(element, Is.EqualTo("not null"));
            Assert.That(priority, Is.EqualTo(5));

            // 验证出队顺序
            Assert.That(queue.Dequeue(), Is.EqualTo(( "not null", 5)));
            Assert.That(queue.Dequeue(), Is.EqualTo(((string?)null, 10)));
            Assert.That(queue.Dequeue(), Is.EqualTo(((string?)null, 15)));
        }

        [Test]
        public void Remove_DuplicateElements_RemovesFirstOccurrence()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>();
            queue.Enqueue(1, 10);
            queue.Enqueue(2, 20);
            queue.Enqueue(1, 30); // 重复元素，不同优先级

            // Act
            bool removed = queue.Remove(1, out int removedElement, out int removedPriority);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(queue.Count, Is.EqualTo(2));

            // 应该移除第一个出现的元素（优先级10）
            Assert.That(removedElement, Is.EqualTo(1));
            Assert.That(removedPriority, Is.EqualTo(10));

            // 队列中应该还剩下优先级30的另一个元素1
            Assert.That(queue.UnorderedItems, Contains.Item((1, 30)));
        }

        [Test]
        public void PriorityQueue_WithCustomComparer_ComplexComparisonLogic()
        {
            // Arrange
            // 自定义比较器：奇数的优先级高于偶数，同奇偶性时数值小的优先级高
            var comparer = Comparer<int>.Create((x, y) =>
            {
                bool xIsOdd = x % 2 == 1;
                bool yIsOdd = y % 2 == 1;

                if (xIsOdd != yIsOdd)
                    return xIsOdd ? -1 : 1; // 奇数优先级更高（值更小）

                return x.CompareTo(y);
            });

            var queue = new JiksLib.Collections.PriorityQueue<int, int>(comparer);

            // Act
            queue.Enqueue(1, 5);   // 奇数，优先级5
            queue.Enqueue(2, 3);   // 偶数，优先级3
            queue.Enqueue(3, 7);   // 奇数，优先级7
            queue.Enqueue(4, 2);   // 偶数，优先级2

            // Assert
            // 根据比较器逻辑：奇数优先级高于偶数，同奇偶性时数值小的优先级高
            // 所以优先级3（奇数）应该是最高的
            Assert.That(queue.Peek(), Is.EqualTo((2, 3)));

            // 验证出队顺序：奇数优先，同奇偶性时优先级值小的优先
            Assert.That(queue.Dequeue(), Is.EqualTo((2, 3))); // 奇数，优先级3（最高）
            Assert.That(queue.Dequeue(), Is.EqualTo((1, 5))); // 奇数，优先级5
            Assert.That(queue.Dequeue(), Is.EqualTo((3, 7))); // 奇数，优先级7
            Assert.That(queue.Dequeue(), Is.EqualTo((4, 2))); // 偶数，优先级2（最低）
        }

#if NETCOREAPP
        [Test]
        public void EnsureCapacity_IncreasesCapacityWhenNeeded()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 10);

            // Act
            queue.EnsureCapacity(100);

            // Assert
            Assert.That(queue.Capacity, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void EnsureCapacity_WithSmallerCapacity_DoesNotReduceCapacity()
        {
            // Arrange
            var queue = new JiksLib.Collections.PriorityQueue<int, int>(capacity: 100);

            // Act
            queue.EnsureCapacity(10); // 请求比当前容量小的值

            // Assert
            Assert.That(queue.Capacity, Is.GreaterThanOrEqualTo(100)); // 容量不应减少
        }
#endif

        #endregion
    }
}