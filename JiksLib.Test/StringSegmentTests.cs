using NUnit.Framework;
using JiksLib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test
{
    [TestFixture]
    public class StringSegmentTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithWholeString_CreatesFullSegment()
        {
            // Arrange
            const string text = "hello";

            // Act
            var segment = new StringSegment(text);

            // Assert
            Assert.That(segment.Source, Is.SameAs(text));
            Assert.That(segment.Offset, Is.EqualTo(0));
            Assert.That(segment.Length, Is.EqualTo(text.Length));
            Assert.That(segment.IsEmpty, Is.False);
            Assert.That(segment.ToString(), Is.EqualTo(text));
            Assert.That(segment.ToString(), Is.SameAs(text));
        }

        [Test]
        public void Constructor_WithOffsetAndLength_CreatesView()
        {
            // Arrange
            const string text = "0123456789";

            // Act
            var segment = new StringSegment(text, 2, 3);

            // Assert
            Assert.That(segment.Source, Is.SameAs(text));
            Assert.That(segment.Offset, Is.EqualTo(2));
            Assert.That(segment.Length, Is.EqualTo(3));
            Assert.That(segment.ToString(), Is.EqualTo("234"));
        }

        [Test]
        public void Constructor_WithEmptyRangeAtEnd_IsValid()
        {
            // Arrange
            const string text = "abc";

            // Act
            var segment = new StringSegment(text, 3, 0);

            // Assert
            Assert.That(segment.IsEmpty, Is.True);
            Assert.That(segment.Offset, Is.EqualTo(3));
            Assert.That(segment.Source, Is.SameAs(text));
            Assert.That(segment.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Constructor_WithNullString_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StringSegment(null!));
        }

        [Test]
        public void Constructor_WithNegativeOffset_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new StringSegment("abc", -1, 1));
        }

        [Test]
        public void Constructor_WithNegativeLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new StringSegment("abc", 0, -1));
        }

        [Test]
        public void Constructor_WithRangeOutsideString_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new StringSegment("abc", 1, 3));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new StringSegment("abc", 4, 0));
        }

        [Test]
        public void DefaultSegment_IsEmpty()
        {
            // Act
            var segment = default(StringSegment);

            // Assert
            Assert.That(segment.IsEmpty, Is.True);
            Assert.That(segment.Offset, Is.EqualTo(0));
            Assert.That(segment.Length, Is.EqualTo(0));
            Assert.That(segment.Source, Is.EqualTo(string.Empty));
            Assert.That(segment.ToString(), Is.EqualTo(string.Empty));
            Assert.That(segment, Is.EqualTo(StringSegment.Empty));
        }

        [Test]
        public void ImplicitConversion_FromString_CreatesFullSegment()
        {
            // Act
            StringSegment segment = "abc";

            // Assert
            Assert.That(segment.Length, Is.EqualTo(3));
            Assert.That(segment.ToString(), Is.EqualTo("abc"));
        }

        #endregion

        #region Indexer And Enumeration Tests

        [Test]
        public void Indexer_ReturnsCharacterRelativeToSegment()
        {
            // Arrange
            var segment = new StringSegment("xxabcxx", 2, 3);

            // Act & Assert
            Assert.That(segment[0], Is.EqualTo('a'));
            Assert.That(segment[1], Is.EqualTo('b'));
            Assert.That(segment[2], Is.EqualTo('c'));
        }

        [Test]
        public void Indexer_OutsideSegment_Throws()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<IndexOutOfRangeException>(() => { _ = segment[-1]; });
            Assert.Throws<IndexOutOfRangeException>(() => { _ = segment[3]; });
        }

        [Test]
        public void GetEnumerator_EnumeratesCharacters()
        {
            // Arrange
            var segment = new StringSegment("xxabcxx", 2, 3);
            var chars = new List<char>();

            // Act
            foreach (char c in segment)
                chars.Add(c);

            // Assert
            Assert.That(chars, Is.EqualTo(new[] { 'a', 'b', 'c' }));
        }

        [Test]
        public void Enumerator_CurrentBeforeMoveNext_Throws()
        {
            var segment = new StringSegment("abc");
            var enumerator = segment.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => { _ = enumerator.Current; });
        }

        [Test]
        public void Enumerator_CurrentAfterEnd_Throws()
        {
            var segment = new StringSegment("a");
            var enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo('a'));
            Assert.That(enumerator.MoveNext(), Is.False);
            Assert.Throws<InvalidOperationException>(() => { _ = enumerator.Current; });
        }

        [Test]
        public void Enumerator_Reset_RestartsEnumeration()
        {
            var segment = new StringSegment("ab");
            var enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);

            enumerator.Reset();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo('a'));
        }

        [Test]
        public void DefaultSegment_EnumeratesNothing()
        {
            var chars = new List<char>();

            foreach (char c in default(StringSegment))
                chars.Add(c);

            Assert.That(chars, Is.Empty);
        }

        [Test]
        public void IReadOnlyList_ExposesCountAndCharacters()
        {
            // Arrange
            IReadOnlyList<char> list = new StringSegment("xxabcxx", 2, 3);

            // Act & Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[1], Is.EqualTo('b'));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 'a', 'b', 'c' }));
        }

        #endregion

        #region Slice Tests

        [Test]
        public void Slice_WithStartIndex_ReturnsRestOfSegment()
        {
            // Arrange
            var segment = new StringSegment("xxabcxx", 2, 3);

            // Act
            var sub = segment.Slice(1);

            // Assert
            Assert.That(sub.ToString(), Is.EqualTo("bc"));
            Assert.That(sub.Offset, Is.EqualTo(3));
            Assert.That(sub.Length, Is.EqualTo(2));
        }

        [Test]
        public void Slice_WithStartIndexAndLength_ReturnsView()
        {
            // Arrange
            var segment = new StringSegment("0123456789", 2, 6);

            // Act
            var sub = segment.Slice(1, 2);

            // Assert
            Assert.That(sub.ToString(), Is.EqualTo("34"));
            Assert.That(sub.Offset, Is.EqualTo(3));
            Assert.That(sub.Length, Is.EqualTo(2));
        }

        [Test]
        public void Slice_AtEndOfSegment_IsEmpty()
        {
            var segment = new StringSegment("abc");

            var sub = segment.Slice(3);

            Assert.That(sub.IsEmpty, Is.True);
            Assert.That(sub.Source, Is.SameAs(segment.Source));
        }

        [Test]
        public void Slice_InvalidRange_Throws()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.Slice(-1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.Slice(4));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.Slice(2, 2));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.Slice(0, -1));
        }

        #endregion

        #region Conversion Tests

        [Test]
        public void ToString_WithStartIndex_ReturnsRestOfSegment()
        {
            var segment = new StringSegment("xxabcdxx", 2, 4);

            Assert.That(segment.ToString(1), Is.EqualTo("bcd"));
            Assert.That(segment.ToString(1, 2), Is.EqualTo("bc"));
        }

        [Test]
        public void ToCharArray_ReturnsSegmentCharacters()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);

            Assert.That(segment.ToCharArray(), Is.EqualTo(new[] { 'a', 'b', 'c' }));
            Assert.That(
                default(StringSegment).ToCharArray(),
                Is.SameAs(Array.Empty<char>()));
        }

        [Test]
        public void CopyTo_CopiesWholeSegment()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);
            var destination = new char[5];

            segment.CopyTo(destination, 1);

            Assert.That(new string(destination), Is.EqualTo("\0abc\0"));
        }

        [Test]
        public void CopyTo_CopiesPartOfSegment()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);
            var destination = new char[2];

            segment.CopyTo(1, destination, 0, 2);

            Assert.That(new string(destination), Is.EqualTo("bc"));
        }

        [Test]
        public void CopyTo_InvalidArguments_Throw()
        {
            var segment = new StringSegment("abc");
            var destination = new char[3];

            Assert.Throws<ArgumentNullException>(
                () => segment.CopyTo(null!));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(destination, -1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(new char[2]));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(4, destination, 0, 0));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(0, destination, 0, 4));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.CopyTo(0, destination, 1, 3));
        }

        #endregion

        #region Trim Tests

        [Test]
        public void Trim_RemovesWhitespaceFromBothEnds()
        {
            var segment = new StringSegment("[]  abc \t[]", 2, 7);

            Assert.That(segment.Trim().ToString(), Is.EqualTo("abc"));
        }

        [Test]
        public void Trim_EmptyOrWhitespaceOnly_BecomesEmpty()
        {
            Assert.That(default(StringSegment).Trim().IsEmpty, Is.True);
            Assert.That(new StringSegment("   ").Trim().IsEmpty, Is.True);
        }

        [Test]
        public void TrimChar_RemovesOnlySpecifiedCharacter()
        {
            var segment = new StringSegment("xxabcxy");

            Assert.That(segment.Trim('x').ToString(), Is.EqualTo("abcxy"));
        }

        [Test]
        public void TrimChars_RemovesAnySpecifiedCharacter()
        {
            var segment = new StringSegment("xyabc yx");

            Assert.That(segment.Trim('x', 'y').ToString(), Is.EqualTo("abc "));
        }

        [Test]
        public void Trim_WithEmptyChars_TrimsWhitespaceLikeBcl()
        {
            var segment = new StringSegment("  abc  ");

            // 与 string.Trim(char[]) 一致：空数组表示去掉空白字符
            Assert.That(segment.Trim(Array.Empty<char>()).ToString(), Is.EqualTo("abc"));
            Assert.That(segment.TrimStart(Array.Empty<char>()).ToString(), Is.EqualTo("abc  "));
            Assert.That(segment.TrimEnd(Array.Empty<char>()).ToString(), Is.EqualTo("  abc"));
        }

        [Test]
        public void Trim_WithNullChars_TrimsWhitespaceLikeBcl()
        {
            var segment = new StringSegment("  abc  ");

            // 与 string.Trim(null) 一致：null 表示去掉空白字符，而不是抛异常
            Assert.That(segment.Trim(null!).ToString(), Is.EqualTo("abc"));
            Assert.That(segment.TrimStart(null!).ToString(), Is.EqualTo("abc  "));
            Assert.That(segment.TrimEnd(null!).ToString(), Is.EqualTo("  abc"));
        }

        [Test]
        public void TrimStart_RemovesLeadingCharactersOnly()
        {
            var segment = new StringSegment("  abc  ");

            Assert.That(segment.TrimStart().ToString(), Is.EqualTo("abc  "));
            Assert.That(new StringSegment("xxabcxx").TrimStart('x').ToString(), Is.EqualTo("abcxx"));
            Assert.That(new StringSegment("xyabcxy").TrimStart('x', 'y').ToString(), Is.EqualTo("abcxy"));
        }

        [Test]
        public void TrimEnd_RemovesTrailingCharactersOnly()
        {
            var segment = new StringSegment("  abc  ");

            Assert.That(segment.TrimEnd().ToString(), Is.EqualTo("  abc"));
            Assert.That(new StringSegment("xxabcxx").TrimEnd('x').ToString(), Is.EqualTo("xxabc"));
            Assert.That(new StringSegment("xyabcxy").TrimEnd('x', 'y').ToString(), Is.EqualTo("xyabc"));
        }

        #endregion

        #region Search Tests

        [Test]
        public void IndexOf_Char_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);

            Assert.That(segment.IndexOf('a'), Is.EqualTo(0));
            Assert.That(segment.IndexOf('a', 1), Is.EqualTo(3));
            Assert.That(segment.IndexOf('z'), Is.EqualTo(-1));
            Assert.That(segment.IndexOf('a', segment.Length), Is.EqualTo(-1));
        }

        [Test]
        public void IndexOf_String_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);

            Assert.That(segment.IndexOf("bc"), Is.EqualTo(1));
            Assert.That(segment.IndexOf("bc", 2), Is.EqualTo(4));
            Assert.That(segment.IndexOf("abcd"), Is.EqualTo(-1));
            Assert.That(segment.IndexOf(string.Empty), Is.EqualTo(0));
            Assert.That(segment.IndexOf(string.Empty, 4), Is.EqualTo(4));
        }

        [Test]
        public void IndexOf_String_IgnoreCase()
        {
            var segment = new StringSegment("xxABCabcxx", 2, 6);

            Assert.That(
                segment.IndexOf("abc", 0, StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(0));

            Assert.That(
                segment.IndexOf("abc", 1, StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(3));
        }

        [Test]
        public void IndexOf_Segment_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);
            var value = new StringSegment("--bc--", 2, 2);

            Assert.That(segment.IndexOf(value), Is.EqualTo(1));
            Assert.That(segment.IndexOf(value, 2), Is.EqualTo(4));
            Assert.That(segment.IndexOf(new StringSegment("abcd")), Is.EqualTo(-1));
            Assert.That(segment.IndexOf(default(StringSegment)), Is.EqualTo(0));
        }

        [Test]
        public void IndexOf_Segment_IgnoreCase()
        {
            var segment = new StringSegment("xxABCabcxx", 2, 6);
            var value = new StringSegment("abc");

            Assert.That(
                segment.IndexOf(value, 0, StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(0));

            Assert.That(
                segment.IndexOf(value, 1, StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(3));
        }

        [Test]
        public void IndexOf_InvalidArguments_Throw()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<ArgumentNullException>(() => segment.IndexOf((string)null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf('a', -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf('a', 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf("a", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.IndexOf("a", 4));
        }

        [Test]
        public void LastIndexOf_Char_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);

            Assert.That(segment.LastIndexOf('a'), Is.EqualTo(3));
            Assert.That(segment.LastIndexOf('a', 2), Is.EqualTo(0));
            Assert.That(segment.LastIndexOf('z'), Is.EqualTo(-1));
        }

        [Test]
        public void LastIndexOf_String_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);

            Assert.That(segment.LastIndexOf("abc"), Is.EqualTo(3));
            Assert.That(segment.LastIndexOf("abc", 2), Is.EqualTo(0));
            Assert.That(segment.LastIndexOf("abc", 5), Is.EqualTo(3));
            Assert.That(segment.LastIndexOf("bc", 4), Is.EqualTo(1));
            Assert.That(segment.LastIndexOf("bc", 5), Is.EqualTo(4));
            Assert.That(segment.LastIndexOf("abcd"), Is.EqualTo(-1));
        }

        [Test]
        public void LastIndexOf_EmptyValue_FollowsStringSemantics()
        {
            var segment = new StringSegment("abc");

            Assert.That(segment.LastIndexOf(string.Empty), Is.EqualTo(3));
            Assert.That(segment.LastIndexOf(string.Empty, 0), Is.EqualTo(1));
            Assert.That(segment.LastIndexOf(string.Empty, 2), Is.EqualTo(3));
            Assert.That(default(StringSegment).LastIndexOf(string.Empty), Is.EqualTo(0));
        }

        [Test]
        public void LastIndexOf_Segment_FindsRelativePosition()
        {
            var segment = new StringSegment("xxabcabcxx", 2, 6);
            var value = new StringSegment("--bc--", 2, 2);

            Assert.That(segment.LastIndexOf(value), Is.EqualTo(4));
            Assert.That(segment.LastIndexOf(value, 3), Is.EqualTo(1));
        }

        [Test]
        public void LastIndexOf_IgnoreCase()
        {
            var segment = new StringSegment("xxABCabcxx", 2, 6);

            Assert.That(
                segment.LastIndexOf("abc", StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(3));

            Assert.That(
                segment.LastIndexOf("abc", 2, StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(0));
        }

        [Test]
        public void LastIndexOf_InvalidArguments_Throw()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<ArgumentNullException>(
                () => segment.LastIndexOf((string)null!));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.LastIndexOf('a', -1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.LastIndexOf('a', 3));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.LastIndexOf("a", -1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => segment.LastIndexOf("a", 4));
        }

        [Test]
        public void Contains_CharStringAndSegment()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);

            Assert.That(segment.Contains('b'), Is.True);
            Assert.That(segment.Contains('z'), Is.False);
            Assert.That(segment.Contains("bc"), Is.True);
            Assert.That(segment.Contains("BC", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(segment.Contains(new StringSegment("--bc--", 2, 2)), Is.True);
            Assert.That(segment.Contains(default(StringSegment)), Is.True);
        }

        [Test]
        public void StartsWith_CharStringAndSegment()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);

            Assert.That(segment.StartsWith('a'), Is.True);
            Assert.That(segment.StartsWith('b'), Is.False);
            Assert.That(segment.StartsWith("ab"), Is.True);
            Assert.That(segment.StartsWith("AB", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(segment.StartsWith("abc"), Is.True);
            Assert.That(segment.StartsWith("abcd"), Is.False);
            Assert.That(segment.StartsWith(string.Empty), Is.True);
            Assert.That(segment.StartsWith(default(StringSegment)), Is.True);
            Assert.That(
                segment.StartsWith(new StringSegment("--ab--", 2, 2)),
                Is.True);
        }

        [Test]
        public void EndsWith_CharStringAndSegment()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);

            Assert.That(segment.EndsWith('c'), Is.True);
            Assert.That(segment.EndsWith('b'), Is.False);
            Assert.That(segment.EndsWith("bc"), Is.True);
            Assert.That(segment.EndsWith("BC", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(segment.EndsWith("abc"), Is.True);
            Assert.That(segment.EndsWith("xabc"), Is.False);
            Assert.That(segment.EndsWith(string.Empty), Is.True);
            Assert.That(segment.EndsWith(default(StringSegment)), Is.True);
            Assert.That(
                segment.EndsWith(new StringSegment("--bc--", 2, 2)),
                Is.True);
        }

        [Test]
        public void StartsWithAndEndsWith_NullString_Throw()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<ArgumentNullException>(
                () => segment.StartsWith((string)null!));

            Assert.Throws<ArgumentNullException>(
                () => segment.EndsWith((string)null!));
        }

        #endregion

        #region Equality And Comparison Tests

        [Test]
        public void Equals_ContentBased_AcrossDifferentSources()
        {
            var left = new StringSegment("xxabcxx", 2, 3);
            var right = new StringSegment("--abc--", 2, 3);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentContent_IsFalse()
        {
            var left = new StringSegment("abc");
            var right = new StringSegment("abd");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
            Assert.That(left.Equals(default(StringSegment)), Is.False);
        }

        [Test]
        public void Equals_StringOverload()
        {
            var segment = new StringSegment("xxabcxx", 2, 3);

            Assert.That(segment.Equals("abc"), Is.True);
            Assert.That(segment.Equals("ABC"), Is.False);
            Assert.That(segment.Equals("ABC", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(segment.Equals((string?)null), Is.False);
            Assert.That(segment.Equals("abcd"), Is.False);
        }

        [Test]
        public void Equals_ObjectOverload_OnlyAcceptsSegment()
        {
            var segment = new StringSegment("abc");

            Assert.That(segment.Equals((object)new StringSegment("abc")), Is.True);
            Assert.That(segment.Equals((object)"abc"), Is.False);
            Assert.That(segment.Equals((object?)null), Is.False);
        }

        [Test]
        public void GetHashCode_IgnoreCase_MatchesIgnoreCaseEquality()
        {
            var upper = new StringSegment("ABC");
            var lower = new StringSegment("abc");

            Assert.That(upper.Equals(lower), Is.False);
            Assert.That(upper.Equals(lower, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(
                upper.GetHashCode(StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(lower.GetHashCode(StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void GetHashCode_WithUnsupportedComparison_Throws()
        {
            var segment = new StringSegment("abc");

            Assert.Throws<ArgumentException>(
                () => segment.GetHashCode(StringComparison.CurrentCulture));
        }

        [Test]
        public void CompareTo_OrdersByOrdinalRules()
        {
            Assert.That(
                new StringSegment("abc").CompareTo(new StringSegment("abd")),
                Is.LessThan(0));

            Assert.That(
                new StringSegment("abd").CompareTo(new StringSegment("abc")),
                Is.GreaterThan(0));

            Assert.That(
                new StringSegment("abc").CompareTo(new StringSegment("abcd")),
                Is.LessThan(0));

            Assert.That(
                new StringSegment("abc").CompareTo(new StringSegment("abc")),
                Is.EqualTo(0));

            Assert.That(
                new StringSegment("ABC").CompareTo(new StringSegment("abc")),
                Is.LessThan(0));
        }

        [Test]
        public void CompareTo_StringAndNull()
        {
            var segment = new StringSegment("abc");

            Assert.That(segment.CompareTo("abd"), Is.LessThan(0));
            Assert.That(segment.CompareTo("abc"), Is.EqualTo(0));
            Assert.That(segment.CompareTo((string?)null), Is.EqualTo(1));

            Assert.That(
                ((IComparable)segment).CompareTo((object?)null),
                Is.EqualTo(1));

            Assert.Throws<ArgumentException>(
                () => ((IComparable)segment).CompareTo((object)"abc"));
        }

        [Test]
        public void Compare_WithComparisonType()
        {
            Assert.That(
                StringSegment.Compare(
                    new StringSegment("ABC"),
                    new StringSegment("abc"),
                    StringComparison.OrdinalIgnoreCase),
                Is.EqualTo(0));

            Assert.That(
                StringSegment.Compare(
                    new StringSegment("abc"),
                    new StringSegment("abd"),
                    StringComparison.OrdinalIgnoreCase),
                Is.LessThan(0));

            Assert.That(
                StringSegment.Compare(
                    new StringSegment("abc"),
                    new StringSegment("abd")),
                Is.LessThan(0));
        }

        [Test]
        public void ComparisonOperators_Work()
        {
            var abc = new StringSegment("abc");
            var abcCopy = new StringSegment("abc");
            var abd = new StringSegment("abd");

            Assert.That(abc < abd, Is.True);
            Assert.That(abc <= abd, Is.True);
            Assert.That(abd > abc, Is.True);
            Assert.That(abd >= abc, Is.True);
            Assert.That(abc <= abcCopy, Is.True);
            Assert.That(abc >= abcCopy, Is.True);
        }

        #endregion

        #region Split Tests

        [Test]
        public void Split_MatchesStringSplitIncludingEmptyEntries()
        {
            const string text = "a,b,,c,";
            var segment = new StringSegment(text);

            var parts = segment.Split(',');

            Assert.That(parts.Length, Is.EqualTo(5));
            Assert.That(
                parts.Select(p => p.ToString()).ToArray(),
                Is.EqualTo(new[] { "a", "b", "", "c", "" }));
        }

        [Test]
        public void Split_EmptySegment_ReturnsSingleEmptySegment()
        {
            var parts = default(StringSegment).Split(',');

            Assert.That(parts.Length, Is.EqualTo(1));
            Assert.That(parts[0].IsEmpty, Is.True);
        }

        [Test]
        public void Split_NoSeparator_ReturnsWholeSegment()
        {
            var parts = new StringSegment("abc").Split(',');

            Assert.That(parts.Length, Is.EqualTo(1));
            Assert.That(parts[0].ToString(), Is.EqualTo("abc"));
        }

        [Test]
        public void Split_CustomSeparator_AndOffset()
        {
            var segment = new StringSegment("xxa|b|cxx", 2, 5);

            var parts = segment.Split('|');

            Assert.That(
                parts.Select(p => p.ToString()).ToArray(),
                Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Split0_EmptySegment_ReturnsEmptyArray()
        {
            var parts = default(StringSegment).Split0(',');

            Assert.That(parts, Is.Empty);
        }

        [Test]
        public void Split0_NonEmptySegment_MatchesSplit()
        {
            var segment = new StringSegment("xxa,b,,c,xx", 2, 7);

            var split = segment.Split(',');
            var split0 = segment.Split0(',');

            Assert.That(
                split0.Select(p => p.ToString()).ToArray(),
                Is.EqualTo(split.Select(p => p.ToString()).ToArray()));
        }

        #endregion

        #region Hash And Strip Tests

        [Test]
        public void BKDRHash_MatchesStringExtension()
        {
            const string text = "0123456789";
            var segment = new StringSegment(text, 3, 4);

            Assert.That(
                segment.BKDRHash(),
                Is.EqualTo("3456".BKDRHash()));

            Assert.That(
                segment.BKDRHash(12345),
                Is.EqualTo("3456".BKDRHash(12345)));
        }

        [Test]
        public void BKDRHash_EmptySegment_ReturnsInputHash()
        {
            Assert.That(
                default(StringSegment).BKDRHash(42),
                Is.EqualTo(42UL));
        }

        [Test]
        public void StripPrefix_ReturnsRemainingSegment()
        {
            var segment = new StringSegment("preHelloSuf");

            Assert.That(segment.StripPrefix("pre", out var remain), Is.True);
            Assert.That(remain.ToString(), Is.EqualTo("HelloSuf"));
            Assert.That(remain.Source, Is.SameAs(segment.Source));
        }

        [Test]
        public void StripPrefix_Failure_ReturnsEmptyRemain()
        {
            var segment = new StringSegment("Hello");

            Assert.That(segment.StripPrefix("pre", out var remain), Is.False);
            Assert.That(remain.IsEmpty, Is.True);
        }

        [Test]
        public void StripSuffix_ReturnsRemainingSegment()
        {
            var segment = new StringSegment("HelloSuf");

            Assert.That(segment.StripSuffix("Suf", out var remain), Is.True);
            Assert.That(remain.ToString(), Is.EqualTo("Hello"));
        }

        [Test]
        public void StripSuffix_EmptySuffix_ReturnsWholeSegment()
        {
            var segment = new StringSegment("Hello");

            Assert.That(segment.StripSuffix(default(StringSegment), out var remain), Is.True);
            Assert.That(remain.ToString(), Is.EqualTo("Hello"));
        }

        [Test]
        public void StripPrefixSuffix_ReturnsMiddleSegment()
        {
            var segment = new StringSegment("preHelloSuf");

            Assert.That(
                segment.StripPrefixSuffix("pre", "Suf", out var remain),
                Is.True);

            Assert.That(remain.ToString(), Is.EqualTo("Hello"));
        }

        [Test]
        public void StripPrefixSuffix_OverlappingAffixes_Fails()
        {
            var segment = new StringSegment("abc");

            Assert.That(
                segment.StripPrefixSuffix("abc", "abc", out var remain),
                Is.False);

            Assert.That(remain.IsEmpty, Is.True);
        }

        [Test]
        public void StripPrefixSuffix_ExactMatch_ReturnsEmptySegment()
        {
            var segment = new StringSegment("presuf");

            Assert.That(
                segment.StripPrefixSuffix("pre", "suf", out var remain),
                Is.True);

            Assert.That(remain.IsEmpty, Is.True);
        }

        #endregion
    }
}
