using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test
{
    [TestFixture]
    public class StringSegmentComparerTests
    {
        [Test]
        public void EqualityComparer_Ordinal_UsesCaseSensitiveContent()
        {
            var comparer = StringSegment.EqualityComparer.Ordinal;
            var abc = new StringSegment("xxabcxx", 2, 3);
            var otherAbc = new StringSegment("--abc--", 2, 3);
            var upper = new StringSegment("ABC");

            Assert.That(comparer.Equals(abc, otherAbc), Is.True);
            Assert.That(comparer.Equals(abc, upper), Is.False);
            Assert.That(
                comparer.GetHashCode(abc),
                Is.EqualTo(comparer.GetHashCode(otherAbc)));
        }

        [Test]
        public void EqualityComparer_OrdinalIgnoreCase_UsesCaseInsensitiveContent()
        {
            var comparer = StringSegment.EqualityComparer.OrdinalIgnoreCase;
            var upper = new StringSegment("ABC");
            var lower = new StringSegment("abc");

            Assert.That(comparer.Equals(upper, lower), Is.True);
            Assert.That(
                comparer.GetHashCode(upper),
                Is.EqualTo(comparer.GetHashCode(lower)));
        }

        [Test]
        public void EqualityComparer_UnsupportedComparison_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new StringSegment.EqualityComparer(
                    StringComparison.CurrentCulture));
        }

        [Test]
        public void Comparer_Ordinal_OrdersContent()
        {
            var comparer = StringSegment.Comparer.Ordinal;

            Assert.That(
                comparer.Compare(new StringSegment("abc"), new StringSegment("abd")),
                Is.LessThan(0));

            Assert.That(
                comparer.Compare(new StringSegment("abc"), new StringSegment("abc")),
                Is.EqualTo(0));

            Assert.That(
                comparer.Compare(new StringSegment("abd"), new StringSegment("abc")),
                Is.GreaterThan(0));
        }

        [Test]
        public void Comparer_OrdinalIgnoreCase_IgnoresCase()
        {
            var comparer = StringSegment.Comparer.OrdinalIgnoreCase;

            Assert.That(
                comparer.Compare(new StringSegment("ABC"), new StringSegment("abc")),
                Is.EqualTo(0));

            Assert.That(
                comparer.Compare(new StringSegment("abc"), new StringSegment("ABD")),
                Is.LessThan(0));
        }

        [Test]
        public void Comparer_UnsupportedComparison_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new StringSegment.Comparer(StringComparison.CurrentCulture));
        }

        [Test]
        public void HashSet_WithIgnoreCaseComparer_Deduplicates()
        {
            var set = new HashSet<StringSegment>(
                StringSegment.EqualityComparer.OrdinalIgnoreCase)
            {
                new("abc"),
                new("ABC"),
                new("xxabcxx", 2, 3),
            };

            Assert.That(set.Count, Is.EqualTo(1));
        }

        [Test]
        public void Dictionary_WithIgnoreCaseComparer_FindsValue()
        {
            var dictionary = new Dictionary<StringSegment, int>(
                StringSegment.EqualityComparer.OrdinalIgnoreCase)
            {
                [new StringSegment("abc")] = 42,
            };

            Assert.That(dictionary.TryGetValue("ABC", out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Dictionary_DefaultComparer_UsesContentNotSourceOrOffset()
        {
            var dictionary = new Dictionary<StringSegment, int>
            {
                [new StringSegment("xxabcxx", 2, 3)] = 1,
                [new StringSegment("--abc--", 2, 3)] = 2,
            };

            Assert.That(dictionary.Count, Is.EqualTo(1));
            Assert.That(dictionary[new StringSegment("abc")], Is.EqualTo(2));
            Assert.That(dictionary.TryGetValue("abc", out int value), Is.True);
            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public void Dictionary_DefaultComparer_EmptyAndDefaultAreSameKey()
        {
            var dictionary = new Dictionary<StringSegment, int>
            {
                [default(StringSegment)] = 7,
            };

            Assert.That(dictionary.Count, Is.EqualTo(1));
            Assert.That(dictionary.ContainsKey(StringSegment.Empty), Is.True);
            Assert.That(dictionary.ContainsKey(new StringSegment(string.Empty)), Is.True);
            Assert.That(dictionary[new StringSegment(string.Empty)], Is.EqualTo(7));
        }

        [Test]
        public void HashSet_DefaultComparer_DeduplicatesContentEqualSegments()
        {
            var set = new HashSet<StringSegment>
            {
                new("abc"),
                new("xxabcxx", 2, 3),
                new("--abc--", 2, 3),
                default,
                new StringSegment(string.Empty),
            };

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Contains(new StringSegment("xxabcxx", 2, 3)), Is.True);
        }

        [Test]
        public void ArraySort_WithComparer_OrdersSegments()
        {
            var segments = new[]
            {
                new StringSegment("b"),
                new StringSegment("A"),
                new StringSegment("c"),
            };

            Array.Sort(segments, StringSegment.Comparer.Ordinal);

            Assert.That(
                segments.Select(s => s.ToString()).ToArray(),
                Is.EqualTo(new[] { "A", "b", "c" }));
        }

        [Test]
        public void ArraySort_WithIgnoreCaseComparer_OrdersSegments()
        {
            var segments = new[]
            {
                new StringSegment("b"),
                new StringSegment("A"),
                new StringSegment("C"),
                new StringSegment("d"),
            };

            Array.Sort(segments, StringSegment.Comparer.OrdinalIgnoreCase);

            Assert.That(
                segments.Select(s => s.ToString()).ToArray(),
                Is.EqualTo(new[] { "A", "b", "C", "d" }));
        }

        [Test]
        public void LinqOrderBy_WithComparer_OrdersSegments()
        {
            var segments = new[]
            {
                new StringSegment("bb"),
                new StringSegment("aa"),
                new StringSegment("ab"),
            };

            var ordered = segments
                .OrderBy(s => s, StringSegment.Comparer.Ordinal)
                .Select(s => s.ToString())
                .ToArray();

            Assert.That(ordered, Is.EqualTo(new[] { "aa", "ab", "bb" }));
        }

        [Test]
        public void Comparer_StaticInstances_AreAvailable()
        {
            Assert.That(StringSegment.Comparer.Ordinal, Is.Not.Null);
            Assert.That(StringSegment.Comparer.OrdinalIgnoreCase, Is.Not.Null);
            Assert.That(StringSegment.EqualityComparer.Ordinal, Is.Not.Null);
            Assert.That(
                StringSegment.EqualityComparer.OrdinalIgnoreCase,
                Is.Not.Null);
        }
    }
}
