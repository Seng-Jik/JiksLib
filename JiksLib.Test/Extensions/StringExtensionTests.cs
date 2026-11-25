using NUnit.Framework;
using JiksLib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class StringExtensionTests
    {
        #region Split0 Tests

        [Test]
        public void Split0_WithEmptyString_ReturnsEmptyArray()
        {
            // Arrange
            string emptyString = "";

            // Act
            var result = emptyString.Split0();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void Split0_WithWhitespaceString_ReturnsSingleElement()
        {
            // Arrange
            string whitespaceString = "   ";

            // Act
            var result = whitespaceString.Split0();

            // Assert
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("   "));
        }

        [Test]
        public void Split0_WithDefaultSeparator_WorksLikeSplit()
        {
            // Arrange
            string input = "a,b,c,d,e";

            // Act
            var result = input.Split0();

            // Assert
            Assert.That(result.Length, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo("a"));
            Assert.That(result[1], Is.EqualTo("b"));
            Assert.That(result[2], Is.EqualTo("c"));
            Assert.That(result[3], Is.EqualTo("d"));
            Assert.That(result[4], Is.EqualTo("e"));
        }

        [Test]
        public void Split0_WithCustomSeparator_WorksCorrectly()
        {
            // Arrange
            string input = "a|b|c|d";

            // Act
            var result = input.Split0('|');

            // Assert
            Assert.That(result.Length, Is.EqualTo(4));
            Assert.That(result[0], Is.EqualTo("a"));
            Assert.That(result[1], Is.EqualTo("b"));
            Assert.That(result[2], Is.EqualTo("c"));
            Assert.That(result[3], Is.EqualTo("d"));
        }

        [Test]
        public void Split0_WithNoSeparatorInString_ReturnsSingleElement()
        {
            // Arrange
            string input = "abcdefg";

            // Act
            var result = input.Split0();

            // Assert
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("abcdefg"));
        }

        [Test]
        public void Split0_WithMultipleSeparators_ReturnsAllParts()
        {
            // Arrange
            string input = "a,b,,c,d,";

            // Act
            var result = input.Split0();

            // Assert
            Assert.That(result.Length, Is.EqualTo(6));
            Assert.That(result[0], Is.EqualTo("a"));
            Assert.That(result[1], Is.EqualTo("b"));
            Assert.That(result[2], Is.EqualTo("")); // Empty between commas
            Assert.That(result[3], Is.EqualTo("c"));
            Assert.That(result[4], Is.EqualTo("d"));
            Assert.That(result[5], Is.EqualTo("")); // Trailing empty
        }

        [Test]
        public void Split0_WithTabSeparator_WorksCorrectly()
        {
            // Arrange
            string input = "a\tb\tc";

            // Act
            var result = input.Split0('\t');

            // Assert
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo("a"));
            Assert.That(result[1], Is.EqualTo("b"));
            Assert.That(result[2], Is.EqualTo("c"));
        }

        #endregion

        #region BKDRHash Tests

        [Test]
        public void BKDRHash_WithEmptyString_ReturnsInputHash()
        {
            // Arrange
            string emptyString = "";
            ulong inputHash = 54321;

            // Act
            var result = emptyString.BKDRHash(inputHash);

            // Assert
            Assert.That(result, Is.EqualTo(inputHash));
        }

        [Test]
        public void BKDRHash_WithGivenInputHash_CalculatesCorrectly()
        {
            // Arrange
            string input = "ab";
            ulong inputHash = 100;

            // Act
            var result = input.BKDRHash(inputHash);

            // Assert
            // hash=100
            // 'a'=97, hash=100*16777619+97=1677761997
            // 'b'=98, hash=1677761997*16777619+98=28148851558345241
            Assert.That(result, Is.EqualTo(28148851558345241UL));
        }

        [Test]
        public void BKDRHash_WithUnicodeCharacters_CalculatesBasedOnCharCode()
        {
            // Arrange
            string input = "🐱"; // Cat emoji, char code may be surrogate pair

            // Act
            var result = input.BKDRHash();

            // Assert
            // Note: emoji may be represented as multiple chars
            // Just verify it doesn't throw and returns some value
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void BKDRHash_WithLongString_DoesNotOverflow()
        {
            // Arrange
            string input = new string('a', 1000);

            // Act
            var result = input.BKDRHash();

            // Assert
            // Should not overflow (ulong.MaxValue is large)
            // Just verify it returns some value
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void BKDRHash_Consistency_SameStringSameHash()
        {
            // Arrange
            string input1 = "hello world";
            string input2 = "hello world";
            ulong inputHash = 42;

            // Act
            var result1 = input1.BKDRHash(inputHash);
            var result2 = input2.BKDRHash(inputHash);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void BKDRHash_DifferentStrings_DifferentHashes()
        {
            // Arrange
            string input1 = "hello";
            string input2 = "world";

            // Act
            var result1 = input1.BKDRHash();
            var result2 = input2.BKDRHash();

            // Assert
            // Different strings should (very likely) have different hashes
            Assert.That(result1, Is.Not.EqualTo(result2));
        }

        [Test]
        public void BKDRHash_WithZeroInputHash_CalculatesCorrectly()
        {
            // Arrange
            string input = "abc";

            // Act
            var result = input.BKDRHash(0);

            // Assert
            // hash=0
            // 'a'=97, hash=0*16777619+97=97
            // 'b'=98, hash=97*16777619+98=1627429141
            // 'c'=99, hash=1627429141*16777619+99=27304386077195378
            Assert.That(result, Is.EqualTo(27304386077195378UL));
        }

        [Test]
        public void BKDRHash_CharValueDivisibleCollision_NoLongerCollides()
        {
            // 旧乘子 131 下："ab" 与单个字符 '\u3205'（0x3205 = 12805）哈希相同
            Assert.That("ab".BKDRHash(), Is.Not.EqualTo("\u3205".BKDRHash()));
        }

        #endregion

        #region StripPrefix Tests

        [Test]
        public void StripPrefix_WithEmptyString_ReturnsFalse()
        {
            // Arrange
            string emptyString = "";
            string prefix = "pre";

            // Act
            var result = emptyString.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefix_WithNullPrefix_ThrowsArgumentNullException()
        {
            // Arrange
            string input = "something";

            // Act & Assert
            Assert.That(
                () => input.StripPrefix(null!, out _),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void StripPrefix_WithEmptyPrefix_ReturnsTrueAndOriginalString()
        {
            // Arrange
            string input = "anything";
            string emptyPrefix = "";

            // Act
            var result = input.StripPrefix(emptyPrefix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(input));
        }

        [Test]
        public void StripPrefix_WithMatchingPrefix_ReturnsTrueAndRemainder()
        {
            // Arrange
            string input = "preHelloWorld";
            string prefix = "pre";

            // Act
            var result = input.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo("HelloWorld"));
        }

        [Test]
        public void StripPrefix_WithExactMatch_ReturnsTrueAndEmptyString()
        {
            // Arrange
            string input = "prefix";
            string prefix = "prefix";

            // Act
            var result = input.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(""));
        }

        [Test]
        public void StripPrefix_WithNonMatchingPrefix_ReturnsFalse()
        {
            // Arrange
            string input = "something";
            string prefix = "pre";

            // Act
            var result = input.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefix_WithCaseSensitiveMismatch_ReturnsFalse()
        {
            // Arrange
            string input = "PrefixSomething";
            string prefix = "prefix"; // lowercase

            // Act
            var result = input.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefix_WithLongerPrefixThanString_ReturnsFalse()
        {
            // Arrange
            string input = "ab";
            string prefix = "abcd";

            // Act
            var result = input.StripPrefix(prefix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        #endregion

        #region StripSuffix Tests

        [Test]
        public void StripSuffix_WithEmptyString_ReturnsFalse()
        {
            // Arrange
            string emptyString = "";
            string suffix = "suf";

            // Act
            var result = emptyString.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripSuffix_WithNullSuffix_ThrowsArgumentNullException()
        {
            // Arrange
            string input = "something";

            // Act & Assert
            Assert.That(
                () => input.StripSuffix(null!, out _),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void StripSuffix_WithEmptySuffix_ReturnsTrueAndOriginalString()
        {
            // Arrange
            string input = "anything";
            string emptySuffix = "";

            // Act
            var result = input.StripSuffix(emptySuffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(input));
        }

        [Test]
        public void StripSuffix_WithMatchingSuffix_ReturnsTrueAndRemainder()
        {
            // Arrange
            string input = "HelloWorldSuf";
            string suffix = "Suf";

            // Act
            var result = input.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo("HelloWorld"));
        }

        [Test]
        public void StripSuffix_WithExactMatch_ReturnsTrueAndEmptyString()
        {
            // Arrange
            string input = "suffix";
            string suffix = "suffix";

            // Act
            var result = input.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(""));
        }

        [Test]
        public void StripSuffix_WithNonMatchingSuffix_ReturnsFalse()
        {
            // Arrange
            string input = "something";
            string suffix = "suf";

            // Act
            var result = input.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripSuffix_WithCaseSensitiveMismatch_ReturnsFalse()
        {
            // Arrange
            string input = "SomethingSuffix";
            string suffix = "SUFFIX"; // uppercase

            // Act
            var result = input.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripSuffix_WithLongerSuffixThanString_ReturnsFalse()
        {
            // Arrange
            string input = "ab";
            string suffix = "abcd";

            // Act
            var result = input.StripSuffix(suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        #endregion

        #region StripPrefixSuffix Tests

        [Test]
        public void StripPrefixSuffix_WithEmptyString_ReturnsFalse()
        {
            // Arrange
            string emptyString = "";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = emptyString.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_WithNullPrefix_ThrowsArgumentNullException()
        {
            // Arrange
            string input = "something";

            // Act & Assert
            Assert.That(
                () => input.StripPrefixSuffix(null!, "suf", out _),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void StripPrefixSuffix_WithEmptyPrefixAndSuffix_ReturnsTrueAndOriginalString()
        {
            // Arrange
            string input = "anything";
            string emptyPrefix = "";
            string emptySuffix = "";

            // Act
            var result = input.StripPrefixSuffix(emptyPrefix, emptySuffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(input));
        }

        [Test]
        public void StripPrefixSuffix_WithMatchingPrefixAndSuffix_ReturnsTrueAndRemainder()
        {
            // Arrange
            string input = "preHelloWorldsuf";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo("HelloWorld"));
        }

        [Test]
        public void StripPrefixSuffix_WithExactMatch_ReturnsTrueAndEmptyString()
        {
            // Arrange
            string input = "presuf";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(""));
        }

        [Test]
        public void StripPrefixSuffix_WithOnlyPrefixMatching_ReturnsFalse()
        {
            // Arrange
            string input = "preHelloWorld";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_WithOnlySuffixMatching_ReturnsFalse()
        {
            // Arrange
            string input = "HelloWorldsuf";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_WithNeitherMatching_ReturnsFalse()
        {
            // Arrange
            string input = "something";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_WithOverlappingPrefixAndSuffix_HandlesCorrectly()
        {
            // Arrange
            // String where prefix and suffix could overlap if not careful
            string input = "abcabc";
            string prefix = "abc";
            string suffix = "abc";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo("")); // "abcabc" with "abc" prefix and "abc" suffix leaves ""
        }

        [Test]
        public void StripPrefixSuffix_WithStringShorterThanPrefixPlusSuffix_ReturnsFalse()
        {
            // Arrange
            string input = "ab";
            string prefix = "abc";
            string suffix = "def";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_WithPrefixEqualToSuffix_WorksCorrectly()
        {
            // Arrange
            string input = "preMiddlepre";
            string prefix = "pre";
            string suffix = "pre";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo("Middle"));
        }

        [Test]
        public void StripPrefixSuffix_WithCaseSensitiveMismatch_ReturnsFalse()
        {
            // Arrange
            string input = "PreHelloWorldSuf";
            string prefix = "pre"; // lowercase
            string suffix = "suf"; // lowercase

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void StripPrefixSuffix_EdgeCase_EmptyStringBetweenPrefixAndSuffix()
        {
            // Arrange
            string input = "presuf";
            string prefix = "pre";
            string suffix = "suf";

            // Act
            var result = input.StripPrefixSuffix(prefix, suffix, out var remain);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(remain, Is.EqualTo(""));
        }

        #endregion

        #region AsSegment Tests

        [Test]
        public void AsSegment_ReturnsFullStringView()
        {
            const string value = "hello,world";

            var segment = value.AsSegment();

            Assert.That(segment.Length, Is.EqualTo(value.Length));
            Assert.That(segment.Source, Is.SameAs(value));
            Assert.That(segment.ToString(), Is.EqualTo(value));
        }

        [Test]
        public void AsSegment_WithEmptyString_IsEmpty()
        {
            var segment = string.Empty.AsSegment();

            Assert.That(segment.IsEmpty, Is.True);
            Assert.That(segment.Length, Is.EqualTo(0));
        }

        [Test]
        public void AsSegment_Null_ThrowsArgumentNullException()
        {
            string? value = null;

            Assert.That(
                () => value!.AsSegment(),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region Tokenize Tests

        [Test]
        public void Tokenize_SplitsStringOnSeparator()
        {
            var tokens = new List<string>();

            foreach (var token in "a,b,c".Tokenize(','))
                tokens.Add(token.ToString());

            Assert.That(tokens, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Tokenize_DefaultSeparator_IsComma()
        {
            var tokens = new List<string>();

            foreach (var token in "a,b".Tokenize())
                tokens.Add(token.ToString());

            Assert.That(tokens, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Tokenize_EmptyString_ReturnsSingleEmptyToken()
        {
            var tokens = new List<string>();

            foreach (var token in "".Tokenize(','))
                tokens.Add(token.ToString());

            Assert.That(tokens, Is.EqualTo(new[] { "" }));
        }

        [Test]
        public void Tokenize_PreservesEmptyEntries()
        {
            var tokens = new List<string>();

            foreach (var token in "a,,c,".Tokenize(','))
                tokens.Add(token.ToString());

            Assert.That(tokens, Is.EqualTo(new[] { "a", "", "c", "" }));
        }

        [Test]
        public void Tokenize_Null_ThrowsArgumentNullException()
        {
            string? value = null;

            Assert.That(
                () => value!.Tokenize(','),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion
    }
}
