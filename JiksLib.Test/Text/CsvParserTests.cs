using NUnit.Framework;
using JiksLib;
using JiksLib.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JiksLib.Test.Text
{
    [TestFixture]
    public class CsvParserTests
    {
        static List<List<string>> ParseAll(string csv, char separator = ',')
        {
            var rows = new List<List<string>>();

            foreach (var row in new CsvParser(csv, separator))
            {
                var fields = new List<string>();

                foreach (var field in row)
                    fields.Add(field.ToString());

                rows.Add(fields);
            }

            return rows;
        }

        [Test]
        public void Parse_SimpleFields_ReturnsOneRow()
        {
            var rows = ParseAll("a,b,c");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Parse_QuotedFieldWithSeparator_KeepsSeparator()
        {
            var rows = ParseAll("\"a,b\",c");

            Assert.That(rows[0], Is.EqualTo(new[] { "a,b", "c" }));
        }

        [Test]
        public void Parse_EscapedQuotes_UnescapesValue()
        {
            var rows = ParseAll("\"a\"\"b\",c");

            Assert.That(rows[0], Is.EqualTo(new[] { "a\"b", "c" }));
        }

        [Test]
        public void Parse_QuotedFieldWithLf_KeepsNewlineInsideField()
        {
            var rows = ParseAll("\"line1\nline2\",x");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "line1\nline2", "x" }));
        }

        [Test]
        public void Parse_QuotedFieldWithCrLf_KeepsNewlineInsideField()
        {
            var rows = ParseAll("\"line1\r\nline2\",x");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "line1\r\nline2", "x" }));
        }

        [Test]
        public void Parse_EmptyFields_ReturnsEmptyStrings()
        {
            var rows = ParseAll("a,,c");

            Assert.That(rows[0], Is.EqualTo(new[] { "a", "", "c" }));
        }

        [Test]
        public void Parse_TrailingSeparator_ProducesTrailingEmptyField()
        {
            var rows = ParseAll("a,b,");

            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b", "" }));
        }

        [Test]
        public void Parse_OnlySeparator_ProducesTwoEmptyFields()
        {
            var rows = ParseAll(",");

            Assert.That(rows[0], Is.EqualTo(new[] { "", "" }));
        }

        [Test]
        public void Parse_QuotedEmptyField_ReturnsEmptyString()
        {
            var rows = ParseAll("\"\",a");

            Assert.That(rows[0], Is.EqualTo(new[] { "", "a" }));
        }

        [Test]
        public void Parse_UnquotedQuote_IsLiteral()
        {
            var rows = ParseAll("a\"b,c");

            Assert.That(rows[0], Is.EqualTo(new[] { "a\"b", "c" }));
        }

        [Test]
        public void Parse_EmptyInput_YieldsNoRows()
        {
            Assert.That(ParseAll(""), Is.Empty);
        }

        [Test]
        public void Parse_SingleField_YieldsOneRow()
        {
            var rows = ParseAll("a");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void Parse_BlankLines_ProduceEmptyRecords()
        {
            var rows = ParseAll("a\n\nb");

            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[0], Is.EqualTo(new[] { "a" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "" }));
            Assert.That(rows[2], Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void Parse_TrailingTerminator_DoesNotProduceExtraRow()
        {
            var rows = ParseAll("a,b\n");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Parse_CrLfLfAndCr_AllActAsRecordSeparators()
        {
            var rows = ParseAll("a\r\nb\nc\rd");

            Assert.That(rows, Has.Count.EqualTo(4));
            Assert.That(rows[0], Is.EqualTo(new[] { "a" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "b" }));
            Assert.That(rows[2], Is.EqualTo(new[] { "c" }));
            Assert.That(rows[3], Is.EqualTo(new[] { "d" }));
        }

        [Test]
        public void Parse_UnclosedQuote_ThrowsInvalidDataException()
        {
            Assert.That(
                () => ParseAll("\"unclosed"),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Parse_UnexpectedCharacterAfterClosingQuote_ThrowsInvalidDataException()
        {
            Assert.That(
                () => ParseAll("\"a\"b,c"),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Parse_CustomSeparator_ParsesTsv()
        {
            var rows = ParseAll("a|b|c", '|');

            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Parse_CustomSeparator_CommaIsLiteral()
        {
            var rows = ParseAll("a,b|c", '|');

            Assert.That(rows[0], Is.EqualTo(new[] { "a,b", "c" }));
        }

        [Test]
        public void Parse_FromStringSegmentOffset_ParsesOnlyTheSlice()
        {
            var source = new StringSegment("xxa,b", 2, 3);
            var parser = new CsvParser(source);

            var rows = new List<string>();

            foreach (var row in parser)
            {
                foreach (var field in row)
                    rows.Add(field.ToString());
            }

            Assert.That(rows, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Parse_RowEnumerator_YieldsFieldsInOrder()
        {
            var parser = new CsvParser("a,b,\"c,d\"");
            var enumerator = parser.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);

            var row = enumerator.Current;
            var fields = new List<string>();
            var rowEnumerator = row.GetEnumerator();

            while (rowEnumerator.MoveNext())
                fields.Add(rowEnumerator.Current.ToString());

            Assert.That(fields, Is.EqualTo(new[] { "a", "b", "c,d" }));
        }

        [Test]
        public void Parse_NullSource_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new CsvParser((string)null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase('"')]
        [TestCase('\r')]
        [TestCase('\n')]
        public void Parse_InvalidSeparator_ThrowsArgumentException(char separator)
        {
            Assert.That(
                () => new CsvParser("a", separator),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Parse_Reset_ThrowsNotSupportedException()
        {
            var enumerator = new CsvParser("a").GetEnumerator();

            Assert.That(
                () => enumerator.Reset(),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Parser_IsEnumerableOfRow_SupportsLinq()
        {
            IEnumerable<CsvParser.Row> rows = new CsvParser("a,b\nc,d");

            var fields = rows
                .SelectMany(row => row.Select(field => field.ToString()))
                .ToArray();

            Assert.That(fields, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void Row_IsEnumerableOfField_SupportsLinq()
        {
            var parser = new CsvParser("a,b,c");
            var enumerator = parser.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);

            var row = enumerator.Current;
            var fields = ((IEnumerable<CsvParser.Field>)row)
                .Select(field => field.ToString())
                .ToArray();

            Assert.That(fields, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Row_CanBeEnumeratedRepeatedly()
        {
            var parser = new CsvParser("a,b");
            var enumerator = parser.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);

            var row = enumerator.Current;

            Assert.That(
                ((IEnumerable<CsvParser.Field>)row).Select(f => f.ToString()),
                Is.EqualTo(new[] { "a", "b" }));
            Assert.That(
                ((IEnumerable<CsvParser.Field>)row).Select(f => f.ToString()),
                Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Field_Default_IsEmptyField()
        {
            var field = default(CsvParser.Field);

            Assert.That(field.Raw.Length, Is.EqualTo(0));
            Assert.That(field.HasEscapedQuotes, Is.False);
            Assert.That(field.Value.Length, Is.EqualTo(0));
            Assert.That(field.ToString(), Is.Empty);
        }

        [Test]
        public void Field_ExposesRawContentAndLazyValue()
        {
            var parser = new CsvParser("\"a\"\"b\",c");
            var enumerator = parser.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);

            var row = enumerator.Current;
            var rowEnumerator = row.GetEnumerator();

            Assert.That(rowEnumerator.MoveNext(), Is.True);
            var escaped = rowEnumerator.Current;

            Assert.That(escaped.HasEscapedQuotes, Is.True);
            Assert.That(escaped.Raw.ToString(), Is.EqualTo("a\"\"b"));
            Assert.That(escaped.Value.ToString(), Is.EqualTo("a\"b"));
            Assert.That(escaped.ToString(), Is.EqualTo("a\"b"));

            // 隐式转换等价于 Value，便于 foreach (StringSegment ...) 写法
            StringSegment implicitValue = escaped;
            Assert.That(implicitValue.ToString(), Is.EqualTo("a\"b"));

            Assert.That(rowEnumerator.MoveNext(), Is.True);
            var plain = rowEnumerator.Current;

            Assert.That(plain.HasEscapedQuotes, Is.False);
            Assert.That(plain.Raw, Is.EqualTo(plain.Value));
            Assert.That(plain.ToString(), Is.EqualTo("c"));
        }

#if NET5_0_OR_GREATER
        [Test]
        public void Parse_Enumeration_DoesNotAllocate()
        {
            // 覆盖：普通字段、引号内含逗号、引号内含换行（这些都不需要解转义）
            var parser = new CsvParser("a,bb,ccc,\"d,e\",\"f\nf\"\r\nx,y");

            // 预热：把 JIT 与首次调用路径跑热
            Assert.That(SumRawLengths(parser, 1), Is.EqualTo(14));

            // 分层编译/GC 记账偶尔会在单次测量里产生一次性分配，取多轮最小增量
            long best = long.MaxValue;

            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                int sum = SumRawLengths(parser, 100);
                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(sum, Is.EqualTo(14 * 100));
                best = Math.Min(best, after - before);
            }

            Assert.That(best, Is.EqualTo(0));
        }

        [Test]
        public void Parse_EnumerationWithEscapedQuotes_DoesNotAllocate()
        {
            // 只看 Raw：即使字段含 "" 转义，枚举也不分配
            var parser = new CsvParser("\"a\"\"b\",c,\"d\"\"e\"");

            Assert.That(SumRawLengths(parser, 1), Is.EqualTo(9));

            long best = long.MaxValue;

            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                int sum = SumRawLengths(parser, 100);
                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(sum, Is.EqualTo(9 * 100));
                best = Math.Min(best, after - before);
            }

            Assert.That(best, Is.EqualTo(0));
        }

        [Test]
        public void Field_Value_DoesNotAllocateForPlainField()
        {
            var parser = new CsvParser("a,bb,ccc");

            Assert.That(SumValueLengths(parser, 1), Is.EqualTo(6));

            long best = long.MaxValue;

            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                int sum = SumValueLengths(parser, 100);
                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(sum, Is.EqualTo(6 * 100));
                best = Math.Min(best, after - before);
            }

            Assert.That(best, Is.EqualTo(0));
        }

        [Test]
        public void Field_Value_AllocatesForEscapedField()
        {
            var field = GetFirstField(new CsvParser("\"a\"\"b\""));

            // 预热
            Assert.That(field.Value.Length, Is.EqualTo(3));

            long before = GC.GetAllocatedBytesForCurrentThread();
            var value = field.Value;
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(value.ToString(), Is.EqualTo("a\"b"));
            Assert.That(after, Is.GreaterThan(before));
        }

        static CsvParser.Field GetFirstField(CsvParser parser)
        {
            var enumerator = parser.GetEnumerator();
            enumerator.MoveNext();
            var row = enumerator.Current;
            var rowEnumerator = row.GetEnumerator();
            rowEnumerator.MoveNext();
            return rowEnumerator.Current;
        }

        static int SumRawLengths(CsvParser parser, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
            {
                foreach (var row in parser)
                {
                    foreach (var field in row)
                        sum += field.Raw.Length;
                }
            }

            return sum;
        }

        static int SumValueLengths(CsvParser parser, int times)
        {
            int sum = 0;

            for (int i = 0; i < times; i++)
            {
                foreach (var row in parser)
                {
                    foreach (var field in row)
                        sum += field.Value.Length;
                }
            }

            return sum;
        }
#endif
    }
}
