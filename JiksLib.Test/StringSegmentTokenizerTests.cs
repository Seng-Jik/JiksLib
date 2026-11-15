using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace JiksLib.Test
{
    [TestFixture]
    public class StringSegmentTokenizerTests
    {
        [Test]
        public void Tokenize_ReturnsAllTokens()
        {
            var segment = new StringSegment("a,b,c");

            var tokens = Tokenize(segment, ',');

            Assert.That(tokens.Count, Is.EqualTo(3));
            Assert.That(tokens[0].ToString(), Is.EqualTo("a"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("b"));
            Assert.That(tokens[2].ToString(), Is.EqualTo("c"));
        }

        [Test]
        public void Tokenize_KeepsEmptyEntriesAndTrailingEmpty()
        {
            var segment = new StringSegment("a,b,,c,");

            var tokens = Tokenize(segment, ',');

            Assert.That(tokens.Count, Is.EqualTo(5));
            Assert.That(tokens[0].ToString(), Is.EqualTo("a"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("b"));
            Assert.That(tokens[2].IsEmpty, Is.True);
            Assert.That(tokens[3].ToString(), Is.EqualTo("c"));
            Assert.That(tokens[4].IsEmpty, Is.True);
        }

        [Test]
        public void Tokenize_EmptySegment_ReturnsOneEmptyToken()
        {
            var tokens = Tokenize(default(StringSegment), ',');

            Assert.That(tokens.Count, Is.EqualTo(1));
            Assert.That(tokens[0].IsEmpty, Is.True);
        }

        [Test]
        public void Tokenize_NoSeparator_ReturnsWholeSegment()
        {
            var segment = new StringSegment("abc");

            var tokens = Tokenize(segment, ',');

            Assert.That(tokens.Count, Is.EqualTo(1));
            Assert.That(tokens[0].ToString(), Is.EqualTo("abc"));
        }

        [Test]
        public void Tokenize_OffsetSegment_ReturnsRelativeTokens()
        {
            var segment = new StringSegment("xxa,bxx", 2, 3);

            var tokens = Tokenize(segment, ',');

            Assert.That(tokens.Count, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("a"));
            Assert.That(tokens[0].Offset, Is.EqualTo(2));
            Assert.That(tokens[1].ToString(), Is.EqualTo("b"));
            Assert.That(tokens[1].Offset, Is.EqualTo(4));
        }

        [Test]
        public void Tokenize_CustomSeparator()
        {
            var segment = new StringSegment("a|b|c");

            var tokens = Tokenize(segment, '|');

            Assert.That(tokens.Count, Is.EqualTo(3));
            Assert.That(tokens[2].ToString(), Is.EqualTo("c"));
        }

        [Test]
        public void Tokenize_DefaultSeparator_IsComma()
        {
            var segment = new StringSegment("a,b");

            var tokens = new List<StringSegment>();
            var tokenizer = segment.Tokenize();

            while (tokenizer.MoveNext())
                tokens.Add(tokenizer.Current);

            Assert.That(tokens.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tokenizer_PublicConstructor_Works()
        {
            var segment = new StringSegment("a,b");
            var tokenizer = new StringSegment.Tokenizer(segment, ',');

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.ToString(), Is.EqualTo("a"));
            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.ToString(), Is.EqualTo("b"));
            Assert.That(tokenizer.MoveNext(), Is.False);
        }

        [Test]
        public void Tokenizer_CurrentBeforeMoveNext_Throws()
        {
            var tokenizer = new StringSegment("a,b").Tokenize(',');

            Assert.Throws<InvalidOperationException>(
                () => { _ = tokenizer.Current; });
        }

        [Test]
        public void Tokenizer_CurrentAfterEnd_Throws()
        {
            var tokenizer = new StringSegment("a").Tokenize(',');

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.ToString(), Is.EqualTo("a"));
            Assert.That(tokenizer.MoveNext(), Is.False);

            Assert.Throws<InvalidOperationException>(
                () => { _ = tokenizer.Current; });
        }

        [Test]
        public void Tokenizer_Reset_RestartsFromFirstToken()
        {
            var tokenizer = new StringSegment("a,b").Tokenize(',');

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.MoveNext(), Is.False);

            tokenizer.Reset();

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void Tokenizer_AsIEnumerator_Works()
        {
            IEnumerator<StringSegment> enumerator =
                new StringSegment("a,b").Tokenize(',');

            var tokens = new List<string>();

            while (enumerator.MoveNext())
                tokens.Add(enumerator.Current.ToString());

            Assert.That(tokens, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Tokenizer_TokensShareSourceString()
        {
            const string text = "xxa,bxx";
            var segment = new StringSegment(text, 2, 3);

            var tokenizer = segment.Tokenize(',');

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.Source, Is.SameAs(text));

            Assert.That(tokenizer.MoveNext(), Is.True);
            Assert.That(tokenizer.Current.Source, Is.SameAs(text));
        }

        [Test]
        public void Tokenizer_Dispose_IsNoOp()
        {
            var tokenizer = new StringSegment("a,b").Tokenize(',');

            Assert.DoesNotThrow(tokenizer.Dispose);
            Assert.That(tokenizer.MoveNext(), Is.True);
        }

        static List<StringSegment> Tokenize(
            StringSegment segment,
            char separator)
        {
            var result = new List<StringSegment>();
            var tokenizer = segment.Tokenize(separator);

            while (tokenizer.MoveNext())
                result.Add(tokenizer.Current);

            return result;
        }

#if NET5_0_OR_GREATER
        [Test]
        public void Tokenizer_DoesNotAllocate()
        {
            var segment = new StringSegment("a,bb,ccc,dddd,eeeee,ffffff");

            // 预热：用与测量相同的工作量把 JIT / 分层编译路径跑热。
            Assert.That(SumTokenLengths(segment, 1000), Is.EqualTo(21000));

            // 分层编译与 GC 记账偶尔会在单次测量里产生一次性分配，
            // 因此取多轮中的最小增量：稳定分配会在每轮出现，一次性噪声不会。
            long best = long.MaxValue;

            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                int sum = SumTokenLengths(segment, 1000);
                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(sum, Is.EqualTo(21000));
                best = Math.Min(best, after - before);
            }

            Assert.That(best, Is.EqualTo(0));
        }

        [Test]
        public void SegmentEnumerator_DoesNotAllocate()
        {
            var segment = new StringSegment("abcdefghij");
            const int expectedPerRound = (97 + 98 + 99 + 100 + 101 + 102 + 103 + 104 + 105 + 106) * 1000;

            // 预热
            Assert.That(SumChars(segment, 1000), Is.EqualTo(expectedPerRound));

            long best = long.MaxValue;

            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                int sum = SumChars(segment, 1000);
                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(sum, Is.EqualTo(expectedPerRound));
                best = Math.Min(best, after - before);
            }

            Assert.That(best, Is.EqualTo(0));
        }

        static int SumTokenLengths(StringSegment segment, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
            {
                var tokenizer = segment.Tokenize(',');

                while (tokenizer.MoveNext())
                    sum += tokenizer.Current.Length;
            }

            return sum;
        }

        static int SumChars(StringSegment segment, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
            {
                foreach (char c in segment)
                    sum += c;
            }

            return sum;
        }
#endif
    }
}
