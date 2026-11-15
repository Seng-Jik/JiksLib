using NUnit.Framework;
using JiksLib.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JiksLib.Test.Text
{
    [TestFixture]
    public class CsvReaderTests
    {
        static List<List<string>> ParseAll(
            string csv,
            char separator = ',',
            Encoding? encoding = null)
        {
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(csv);
            return ParseAll(new MemoryStream(bytes), separator, encoding);
        }

        static List<List<string>> ParseAll(
            Stream stream,
            char separator = ',',
            Encoding? encoding = null)
        {
            var rows = new List<List<string>>();

            using var reader = new CsvReader(stream, encoding, leaveOpen: false, separator);

            foreach (var row in reader)
            {
                var fields = new List<string>();

                foreach (var field in row)
                    fields.Add(field);

                rows.Add(fields);
            }

            return rows;
        }

        [Test]
        public void Read_SimpleCsv_ReturnsRows()
        {
            var rows = ParseAll("a,b,c\n1,2,3");

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "1", "2", "3" }));
        }

        [Test]
        public void Read_QuotedAndEscapedFields_UnquotesNormally()
        {
            var rows = ParseAll("\"a,b\",\"c\"\"d\"");

            Assert.That(rows[0], Is.EqualTo(new[] { "a,b", "c\"d" }));
        }

        [Test]
        public void Read_QuotedFieldWithNewline_StaysInOneRow()
        {
            var rows = ParseAll("\"line1\nline2\",x");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "line1\nline2", "x" }));
        }

        [Test]
        public void Read_CrLfRecords_AreSplit()
        {
            var rows = ParseAll("a,b\r\nc,d");

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[1], Is.EqualTo(new[] { "c", "d" }));
        }

        [Test]
        public void Read_EmptyStream_YieldsNoRows()
        {
            Assert.That(ParseAll(new MemoryStream()), Is.Empty);
        }

        [Test]
        public void Read_TrailingNewline_DoesNotProduceExtraRow()
        {
            var rows = ParseAll("a,b\n");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Read_BlankLines_ProduceEmptyRecords()
        {
            var rows = ParseAll("a\n\nb");

            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[1], Is.EqualTo(new[] { "" }));
        }

        [Test]
        public void Read_Utf8Bom_IsNotIncludedInFirstField()
        {
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes("a,b"))
                .ToArray();

            var rows = ParseAll(new MemoryStream(bytes));

            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Read_ChunkedStream_StillReadsWholeRecords()
        {
            var bytes = Encoding.UTF8.GetBytes("ab,\"c\r\nd\"\r\nxy,z");
            var rows = ParseAll(new OneByteStream(bytes));

            Assert.That(rows, Has.Count.EqualTo(2), Dump(rows));
            Assert.That(rows[0], Is.EqualTo(new[] { "ab", "c\r\nd" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "xy", "z" }));
        }

        static string Dump(List<List<string>> rows)
        {
            var parts = new List<string>();

            foreach (var row in rows)
            {
                var fields = new List<string>();

                foreach (var field in row)
                {
                    fields.Add(
                        field.Replace("\r", "\\r").Replace("\n", "\\n"));
                }

                parts.Add("[" + string.Join("|", fields) + "]");
            }

            return string.Join(" ", parts);
        }

        [Test]
        public void Read_CustomSeparator_ParsesTsv()
        {
            var rows = ParseAll("a|b|c", '|');

            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Read_UnclosedQuote_ThrowsInvalidDataException()
        {
            Assert.That(
                () => ParseAll("\"unclosed"),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Read_UnicodeEncoding_IsHonored()
        {
            var rows = ParseAll("中文,值", encoding: Encoding.Unicode);

            Assert.That(rows[0], Is.EqualTo(new[] { "中文", "值" }));
        }

        [Test]
        public void Read_MatchesCsvParserResults()
        {
            const string sample = "a,\"b,c\",\"d\"\"e\"\r\nx,\"y\nz\",\r\n\r\n";

            var expected = new List<List<string>>();

            foreach (var row in new CsvParser(sample))
            {
                var fields = new List<string>();

                foreach (var field in row)
                    fields.Add(field.ToString());

                expected.Add(fields);
            }

            Assert.That(ParseAll(sample), Is.EqualTo(expected));
        }

        [Test]
        public void Read_ManyRows_Works()
        {
            var builder = new StringBuilder();

            for (int i = 0; i < 1000; i++)
                builder.Append(i).Append(",x").Append('\n');

            var rows = ParseAll(builder.ToString());

            Assert.That(rows, Has.Count.EqualTo(1000));
            Assert.That(rows[999], Is.EqualTo(new[] { "999", "x" }));
        }

        [Test]
        public void Read_NestedForeach_YieldsRowsAndFields()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("a,b\nc,d"));
            using var reader = new CsvReader(stream);

            var fields = new List<string>();

            foreach (var row in reader)
            {
                foreach (var field in row)
                    fields.Add(field);
            }

            Assert.That(fields, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void Dispose_LeaveOpenFalse_DisposesStream()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("a"));
            var reader = new CsvReader(stream, leaveOpen: false);

            reader.Dispose();

            Assert.That(stream.CanRead, Is.False);
        }

        [Test]
        public void Dispose_LeaveOpenTrue_KeepsStreamOpen()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("a"));
            var reader = new CsvReader(stream, leaveOpen: true);

            reader.Dispose();

            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new CsvReader(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_NonReadableStream_ThrowsArgumentException()
        {
            Assert.That(
                () => new CsvReader(new WriteOnlyStream()),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase('"')]
        [TestCase('\r')]
        [TestCase('\n')]
        public void Constructor_InvalidSeparator_ThrowsArgumentException(char separator)
        {
            using var stream = new MemoryStream();

            Assert.That(
                () => new CsvReader(stream, separator: separator),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Reset_ThrowsNotSupportedException()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("a"));
            using var reader = new CsvReader(stream);

            var enumerator = reader.GetEnumerator();

            Assert.That(
                () => enumerator.Reset(),
                Throws.TypeOf<NotSupportedException>());
        }

        /// <summary>
        /// 每次 Read 最多返回 1 个字节，用于验证读取器能跨缓冲区边界正确拼记录
        /// </summary>
        private sealed class OneByteStream : Stream
        {
            readonly byte[] data;
            int position;

            public OneByteStream(byte[] data)
            {
                this.data = data;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => data.Length;

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (position >= data.Length)
                    return 0;

                buffer[offset] = data[position++];
                return 1;
            }

            public override void Flush() { }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }

        /// <summary>
        /// 只写流，用于验证可读性校验
        /// </summary>
        private sealed class WriteOnlyStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;

            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();

            public override void Flush() { }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) { }
        }
    }
}
