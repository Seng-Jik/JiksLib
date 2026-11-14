using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class ReadOnlyListSegmentTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithWholeList_CreatesFullSegment()
        {
            // Arrange
            var list = new List<int> { 10, 20, 30, 40, 50 };

            // Act
            var segment = new ReadOnlyListSegment<int>(list);

            // Assert
            Assert.That(segment.Source, Is.SameAs(list));
            Assert.That(segment.Offset, Is.EqualTo(0));
            Assert.That(segment.Count, Is.EqualTo(5));
            Assert.That(segment.IsEmpty, Is.False);
            Assert.That(segment[0], Is.EqualTo(10));
            Assert.That(segment[4], Is.EqualTo(50));
        }

        [Test]
        public void Constructor_WithOffsetAndCount_CreatesView()
        {
            // Arrange
            var list = new List<int> { 10, 20, 30, 40, 50 };

            // Act
            var segment = new ReadOnlyListSegment<int>(list, 1, 3);

            // Assert
            Assert.That(segment.Source, Is.SameAs(list));
            Assert.That(segment.Offset, Is.EqualTo(1));
            Assert.That(segment.Count, Is.EqualTo(3));
            Assert.That(segment[0], Is.EqualTo(20));
            Assert.That(segment[1], Is.EqualTo(30));
            Assert.That(segment[2], Is.EqualTo(40));
            Assert.That(segment.ToArray(), Is.EqualTo(new[] { 20, 30, 40 }));
        }

        [Test]
        public void Constructor_WithEmptyRangeAtEnd_IsValid()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act
            var segment = new ReadOnlyListSegment<int>(list, 3, 0);

            // Assert
            Assert.That(segment.IsEmpty, Is.True);
            Assert.That(segment.Offset, Is.EqualTo(3));
            Assert.That(segment.Source, Is.SameAs(list));
            Assert.That(segment.ToArray(), Is.Empty);
        }

        [Test]
        public void Constructor_WithArray_UsesArrayAsSource()
        {
            // Arrange
            var array = new[] { 1, 2, 3 };

            // Act
            var segment = new ReadOnlyListSegment<int>(array, 1, 2);

            // Assert
            Assert.That(segment.Source, Is.SameAs(array));
            Assert.That(segment[0], Is.EqualTo(2));
            Assert.That(segment[1], Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithNullList_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ReadOnlyListSegment<int>((IReadOnlyList<int>)null!));

            Assert.Throws<ArgumentNullException>(
                () => new ReadOnlyListSegment<int>(null!, 0, 0));
        }

        [Test]
        public void Constructor_WithNegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            var list = new List<int> { 1, 2, 3 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReadOnlyListSegment<int>(list, -1, 1));

            Assert.That(exception!.ParamName, Is.EqualTo("offset"));
        }

        [Test]
        public void Constructor_WithNegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var list = new List<int> { 1, 2, 3 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReadOnlyListSegment<int>(list, 0, -1));

            Assert.That(exception!.ParamName, Is.EqualTo("count"));
        }

        [Test]
        public void Constructor_WithRangeOutsideList_ThrowsArgumentOutOfRangeException()
        {
            var list = new List<int> { 1, 2, 3 };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReadOnlyListSegment<int>(list, 1, 3));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReadOnlyListSegment<int>(list, 4, 0));
        }

        [Test]
        public void DefaultSegment_IsEmpty()
        {
            // Act
            var segment = default(ReadOnlyListSegment<int>);

            // Assert
            Assert.That(segment.IsEmpty, Is.True);
            Assert.That(segment.Offset, Is.EqualTo(0));
            Assert.That(segment.Count, Is.EqualTo(0));
            Assert.That(segment.Source, Is.Not.Null);
            Assert.That(segment.Source, Is.SameAs(Array.Empty<int>()));
            Assert.That(segment.ToArray(), Is.Empty);
            Assert.That(segment, Is.EqualTo(ReadOnlyListSegment<int>.Empty));
        }

        [Test]
        public void ImplicitConversion_FromArray_CreatesFullSegment()
        {
            // Act
            ReadOnlyListSegment<int> segment = new[] { 1, 2, 3 };

            // Assert
            Assert.That(segment.Offset, Is.EqualTo(0));
            Assert.That(segment.Count, Is.EqualTo(3));
            Assert.That(segment.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(segment.Source, Is.InstanceOf<int[]>());
        }

        [Test]
        public void ImplicitConversion_FromNullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    ReadOnlyListSegment<int> _ = (int[])null!;
                });
        }

        [Test]
        public void Segment_IsLiveView()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var segment = new ReadOnlyListSegment<int>(list, 1, 2);

            // Act
            list[1] = 42;
            list[2] = 99;

            // Assert
            Assert.That(segment[0], Is.EqualTo(42));
            Assert.That(segment[1], Is.EqualTo(99));
        }

        #endregion

        #region Indexer Tests

        [Test]
        public void Indexer_ReturnsElementRelativeToSegment()
        {
            // Arrange
            var list = new List<int> { 0, 1, 2, 3, 4, 5 };
            var segment = new ReadOnlyListSegment<int>(list, 2, 3);

            // Act & Assert
            Assert.That(segment[0], Is.EqualTo(2));
            Assert.That(segment[1], Is.EqualTo(3));
            Assert.That(segment[2], Is.EqualTo(4));
        }

        [Test]
        public void Indexer_OutsideSegment_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 }, 1, 2);

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => _ = segment[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = segment[2]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = segment[100]);
        }

        [Test]
        public void Indexer_OnEmptySegment_ThrowsIndexOutOfRangeException()
        {
            var segment = new ReadOnlyListSegment<int>(Array.Empty<int>());

            Assert.Throws<IndexOutOfRangeException>(() => _ = segment[0]);
        }

        [Test]
        public void Indexer_ReferenceTypeNullElement_ReturnsNull()
        {
            // Arrange
            var list = new List<string?> { "a", null, "c" };
            var segment = new ReadOnlyListSegment<string?>(list, 1, 2);

            // Act & Assert
            Assert.That(segment[0], Is.Null);
            Assert.That(segment[1], Is.EqualTo("c"));
        }

        #endregion

        #region Slice Tests

        [Test]
        public void Slice_WithStartIndex_ReturnsRestOfSegment()
        {
            // Arrange
            var list = new List<int> { 0, 1, 2, 3, 4, 5 };
            var segment = new ReadOnlyListSegment<int>(list, 1, 4);

            // Act
            var tail = segment.Slice(2);

            // Assert
            Assert.That(tail.Offset, Is.EqualTo(3));
            Assert.That(tail.Count, Is.EqualTo(2));
            Assert.That(tail.Source, Is.SameAs(list));
            Assert.That(tail.ToArray(), Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void Slice_WithStartIndexAndCount_ReturnsView()
        {
            // Arrange
            var list = new List<int> { 0, 1, 2, 3, 4, 5 };
            var segment = new ReadOnlyListSegment<int>(list, 1, 4);

            // Act
            var middle = segment.Slice(1, 2);

            // Assert
            Assert.That(middle.Offset, Is.EqualTo(2));
            Assert.That(middle.Count, Is.EqualTo(2));
            Assert.That(middle.ToArray(), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Slice_AtEndOfSegment_IsEmptyAndKeepsSource()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var segment = new ReadOnlyListSegment<int>(list);

            // Act
            var empty = segment.Slice(3);

            // Assert
            Assert.That(empty.IsEmpty, Is.True);
            Assert.That(empty.Offset, Is.EqualTo(3));
            Assert.That(empty.Source, Is.SameAs(list));
        }

        [Test]
        public void Slice_Nested_AccumulatesOffsets()
        {
            // Arrange
            var list = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            var segment = new ReadOnlyListSegment<int>(list, 1, 6);

            // Act
            var nested = segment.Slice(2, 3).Slice(1, 1);

            // Assert
            Assert.That(nested.Offset, Is.EqualTo(4));
            Assert.That(nested.Count, Is.EqualTo(1));
            Assert.That(nested[0], Is.EqualTo(4));
        }

        [Test]
        public void Slice_InvalidRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Slice(2, 2));
        }

        [Test]
        public void Slice_OnDefaultSegment_ReturnsEmpty()
        {
            // Arrange
            var segment = default(ReadOnlyListSegment<int>);

            // Act & Assert
            Assert.That(segment.Slice(0).IsEmpty, Is.True);
            Assert.That(segment.Slice(0, 0).IsEmpty, Is.True);
        }

        #endregion

        #region CopyTo Tests

        [Test]
        public void CopyTo_CopiesWholeSegment()
        {
            // Arrange
            var list = new List<int> { 10, 20, 30, 40, 50 };
            var segment = new ReadOnlyListSegment<int>(list, 2, 3);
            var destination = new int[3];

            // Act
            segment.CopyTo(destination);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 30, 40, 50 }));
        }

        [Test]
        public void CopyTo_WithDestinationIndex_CopiesToCorrectPosition()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 }, 1, 2);
            var destination = new[] { -1, -1, -1, -1, -1 };

            // Act
            segment.CopyTo(destination, 2);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { -1, -1, 2, 3, -1 }));
        }

        [Test]
        public void CopyTo_EmptySegment_DoesNothing()
        {
            // Arrange
            var segment = default(ReadOnlyListSegment<int>);
            var destination = new[] { 7, 8 };

            // Act
            segment.CopyTo(destination);
            segment.CopyTo(destination, 2);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 7, 8 }));
        }

        [Test]
        public void CopyTo_NullDestination_ThrowsArgumentNullException()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 1 });

            Assert.Throws<ArgumentNullException>(() => segment.CopyTo(null!));
            Assert.Throws<ArgumentNullException>(() => segment.CopyTo(null!, 0));
        }

        [Test]
        public void CopyTo_InvalidDestinationIndex_ThrowsArgumentOutOfRangeException()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 });

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(new int[3], -1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(new int[3], 4));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(new int[2], 0));
        }

        [Test]
        public void CopyTo_FromArraySource_CopiesElements()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 5, 6, 7, 8 }, 1, 2);
            var destination = new int[2];

            // Act
            segment.CopyTo(destination);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 6, 7 }));
        }

        [Test]
        public void CopyTo_FromBareReadOnlyList_CopiesElements()
        {
            // Arrange
            var source = new BareReadOnlyList<int>(5, 6, 7, 8);
            var segment = new ReadOnlyListSegment<int>(source, 1, 2);
            var destination = new int[2];

            // Act
            segment.CopyTo(destination);

            // Assert
            Assert.That(destination, Is.EqualTo(new[] { 6, 7 }));
        }

        #endregion

        #region ToArray Tests

        [Test]
        public void ToArray_ReturnsSegmentElements()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3, 4 }, 1, 2);

            // Act
            var result = segment.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ToArray_EmptySegment_ReturnsEmptyArray()
        {
            Assert.That(default(ReadOnlyListSegment<int>).ToArray(), Is.Empty);
            Assert.That(new ReadOnlyListSegment<int>(new int[2], 1, 0).ToArray(), Is.Empty);
        }

        [Test]
        public void ToArray_ReturnsIndependentArray()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var segment = new ReadOnlyListSegment<int>(list);

            // Act
            var first = segment.ToArray();
            var second = segment.ToArray();
            first[0] = 100;

            // Assert
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(segment[0], Is.EqualTo(1));
            Assert.That(second[0], Is.EqualTo(1));
        }

        [Test]
        public void ToArray_FromBareReadOnlyList_CopiesElements()
        {
            // Arrange
            var source = new BareReadOnlyList<int>(1, 2, 3, 4);
            var segment = new ReadOnlyListSegment<int>(source, 2, 2);

            // Act
            var result = segment.ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
        }

        #endregion

        #region Enumeration Tests

        [Test]
        public void Enumerator_EnumeratesOnlySegmentElements()
        {
            // Arrange
            var list = new List<int> { 0, 1, 2, 3, 4, 5 };
            var segment = new ReadOnlyListSegment<int>(list, 2, 3);
            var result = new List<int>();

            // Act
            foreach (int item in segment)
                result.Add(item);

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void DefaultSegment_EnumeratesNothing()
        {
            var result = new List<int>();

            foreach (int item in default(ReadOnlyListSegment<int>))
                result.Add(item);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Enumerator_CurrentBeforeStart_ThrowsInvalidOperationException()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 });
            var enumerator = segment.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
        }

        [Test]
        public void Enumerator_CurrentAfterEnd_ThrowsInvalidOperationException()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 1 });
            var enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);

            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
        }

        [Test]
        public void Enumerator_Reset_RestartsEnumeration()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 });
            var enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(1));

            enumerator.Reset();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(1));
        }

        [Test]
        public void Enumerator_GenericEnumerator_Works()
        {
            IEnumerator<int> enumerator =
                new ReadOnlyListSegment<int>(new[] { 1, 2, 3 }, 1, 2).GetEnumerator();

            var result = new List<int>();

            while (enumerator.MoveNext())
                result.Add(enumerator.Current);

            Assert.That(result, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Enumerator_NonGenericEnumerator_Works()
        {
            IEnumerable segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 }, 1, 2);
            var result = new List<object?>();

            foreach (object? item in segment)
                result.Add(item);

            Assert.That(result, Is.EqualTo(new object?[] { 2, 3 }));
        }

        [Test]
        public void Enumerator_ReferenceTypeNullElement_Works()
        {
            var segment = new ReadOnlyListSegment<string?>(
                new List<string?> { "a", null, "c" },
                1,
                2);

            var enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.Null);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo("c"));
            Assert.That(enumerator.MoveNext(), Is.False);
        }

#if NET5_0_OR_GREATER
        [Test]
        public void Enumerator_DoesNotAllocate()
        {
            var segment = new ReadOnlyListSegment<int>(
                new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            // 预热，确保 JIT 与首次调用产生的分配不计入测量。
            Assert.That(SumByForeach(segment, 1), Is.EqualTo(55));

            long before = GC.GetAllocatedBytesForCurrentThread();

            int sum = SumByForeach(segment, 1000);

            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(sum, Is.EqualTo(55000));
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void Slice_DoesNotAllocate()
        {
            var segment = new ReadOnlyListSegment<int>(new[] { 0, 1, 2, 3, 4, 5 });

            Assert.That(SumSliceCounts(segment, 1), Is.EqualTo(3));

            long before = GC.GetAllocatedBytesForCurrentThread();

            int sum = SumSliceCounts(segment, 1000);

            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(sum, Is.EqualTo(3000));
            Assert.That(after, Is.EqualTo(before));
        }
#endif

        #endregion

        #region Equality Tests

        [Test]
        public void Equals_SameSourceAndRange_ReturnsTrue()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var left = new ReadOnlyListSegment<int>(list, 1, 2);
            var right = new ReadOnlyListSegment<int>(list, 1, 2);

            // Act & Assert
            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_SameSourceDifferentRange_ReturnsFalse()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var left = new ReadOnlyListSegment<int>(list, 1, 2);
            var right = new ReadOnlyListSegment<int>(list, 0, 2);

            // Act & Assert
            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_DifferentSourceSameContent_ReturnsFalse()
        {
            // Arrange
            var left = new ReadOnlyListSegment<int>(new List<int> { 1, 2, 3 });
            var right = new ReadOnlyListSegment<int>(new List<int> { 1, 2, 3 });

            // Act & Assert
            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
        }

        [Test]
        public void Equals_ObjectOverload_ComparesOnlySameSegmentType()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 });
            object copy = segment;
            object different = new ReadOnlyListSegment<long>(new long[] { 1, 2, 3 });

            // Act & Assert
            Assert.That(segment.Equals(copy), Is.True);
            Assert.That(segment.Equals(different), Is.False);
            Assert.That(segment.Equals((object?)null), Is.False);
        }

        [Test]
        public void DefaultSegment_EqualsEmptySegment()
        {
            // Arrange
            var empty = new ReadOnlyListSegment<int>(Array.Empty<int>());

            // Act & Assert
            Assert.That(default(ReadOnlyListSegment<int>).Equals(empty), Is.True);
            Assert.That(empty.Equals(ReadOnlyListSegment<int>.Empty), Is.True);
            Assert.That(
                default(ReadOnlyListSegment<int>).GetHashCode(),
                Is.EqualTo(empty.GetHashCode()));
        }

        [Test]
        public void Equals_CopyOfSegment_ReturnsTrue()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3 }, 1, 2);
            var copy = segment;

            // Act & Assert
            Assert.That(copy.Equals(segment), Is.True);
            Assert.That(copy == segment, Is.True);
        }

        #endregion

        #region IReadOnlyList Tests

        [Test]
        public void IReadOnlyList_ExposesCountAndItems()
        {
            // Arrange
            IReadOnlyList<int> list = new ReadOnlyListSegment<int>(
                new[] { 1, 2, 3, 4 },
                1,
                2);

            // Act & Assert
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(2));
            Assert.That(list[1], Is.EqualTo(3));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Linq_WorksOverSegment()
        {
            // Arrange
            var segment = new ReadOnlyListSegment<int>(new[] { 1, 2, 3, 4 }, 1, 2);

            // Act
            var result = segment.Select(x => x * 10).ToArray();

            // Assert
            Assert.That(result, Is.EqualTo(new[] { 20, 30 }));
        }

        #endregion

        static int SumByForeach(ReadOnlyListSegment<int> segment, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
            {
                foreach (int item in segment)
                    sum += item;
            }

            return sum;
        }

        static int SumSliceCounts(ReadOnlyListSegment<int> segment, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
                sum += segment.Slice(2, 3).Count;

            return sum;
        }

        sealed class BareReadOnlyList<T> : IReadOnlyList<T>
        {
            public BareReadOnlyList(params T[] items) => this.items = items;

            public T this[int index] => items[index];

            public int Count => items.Length;

            public IEnumerator<T> GetEnumerator() =>
                ((IEnumerable<T>)items).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

            readonly T[] items;
        }
    }
}
