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

            while (row.MoveNext())
                fields.Add(row.Current.ToString());

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
        public void Row_IsEnumerableOfStringSegment_SupportsLinq()
        {
            var parser = new CsvParser("a,b,c");
            var enumerator = parser.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);

            var row = enumerator.Current;
            var fields = ((IEnumerable<StringSegment>)row)
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
                ((IEnumerable<StringSegment>)row).Select(f => f.ToString()),
                Is.EqualTo(new[] { "a", "b" }));
            Assert.That(
                ((IEnumerable<StringSegment>)row).Select(f => f.ToString()),
                Is.EqualTo(new[] { "a", "b" }));
        }
    }
}
