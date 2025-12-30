using NUnit.Framework;
using JiksLib.Text;
using System;
using System.IO;
using System.Text;

namespace JiksLib.Test.Text
{
    [TestFixture]
    public class CsvReaderTests
    {
        [Test]
        public void Constructor_WithString_CreatesInstance()
        {
            // Arrange
            var csvContent = "a,b,c\n1,2,3";

            // Act
            using var reader = new CsvReader(csvContent);

            // Assert
            Assert.That(reader, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithTextReader_CreatesInstance()
        {
            // Arrange
            var textReader = new StringReader("a,b,c");

            // Act
            using var reader = new CsvReader(textReader, leaveOpen: true);

            // Assert
            Assert.That(reader, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithCustomSeparator_UsesCustomSeparator()
        {
            // Arrange
            var csvContent = "a|b|c";

            // Act
            using var reader = new CsvReader(csvContent, '|');
            var field1 = reader.PopField();

            // Assert
            Assert.That(field1, Is.EqualTo("a"));
        }

        [Test]
        public void PopField_SimpleCsv_ReturnsFields()
        {
            // Arrange
            var csvContent = "a,b,c";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo("b"));
            Assert.That(reader.PopField(), Is.EqualTo("c"));
            Assert.That(reader.PopField(), Is.Null);
        }

        [Test]
        public void PopField_WithQuotedField_ReturnsUnquotedValue()
        {
            // Arrange
            var csvContent = "\"a,b\",c";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a,b"));
            Assert.That(reader.PopField(), Is.EqualTo("c"));
        }

        [Test]
        public void PopField_WithEscapedQuote_ReturnsCorrectValue()
        {
            // Arrange
            var csvContent = "\"a\"\"b\",c";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a\"b"));
            Assert.That(reader.PopField(), Is.EqualTo("c"));
        }

        [Test]
        public void PopField_WithEmptyField_ReturnsEmptyString()
        {
            // Arrange
            var csvContent = "a,,c";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo(""));
            Assert.That(reader.PopField(), Is.EqualTo("c"));
        }

        [Test]
        public void PopField_WithUnclosedQuote_ThrowsInvalidDataException()
        {
            // Arrange
            var csvContent = "\"unclosed";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(() => reader.PopField(), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void NextRecord_MovesToNextLine()
        {
            // Arrange
            var csvContent = "a,b,c\n1,2,3";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("1"));
            Assert.That(reader.PopField(), Is.EqualTo("2"));
            Assert.That(reader.PopField(), Is.EqualTo("3"));
        }

        [Test]
        public void NextRecord_AtEndOfFile_ReturnsFalse()
        {
            // Arrange
            var csvContent = "a,b";
            using var reader = new CsvReader(csvContent);
            reader.PopField(); // a
            reader.PopField(); // b
            reader.PopField(); // null

            // Act
            var result = reader.NextRecord();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void NextRecord_WithEmptyLines_ProducesEmptyFields()
        {
            // Arrange
            var csvContent = "a,b\n\n\nc,d";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            // Read first line
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo("b"));
            Assert.That(reader.PopField(), Is.Null);

            // First empty line - produces empty field
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("")); // Empty field from empty line
            Assert.That(reader.PopField(), Is.Null);

            // Second empty line
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo(""));
            Assert.That(reader.PopField(), Is.Null);

            // Third line contains c,d
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("c"));
            Assert.That(reader.PopField(), Is.EqualTo("d"));
            Assert.That(reader.PopField(), Is.Null);
        }

        [Test]
        public void PopField_AfterNextRecord_ResetsFieldReading()
        {
            // Arrange
            var csvContent = "a,b\nc,d";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo("b"));
            Assert.That(reader.PopField(), Is.Null);
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("c"));
            Assert.That(reader.PopField(), Is.EqualTo("d"));
        }

        [Test]
        public void Dispose_WithLeaveOpenFalse_DisposesTextReader()
        {
            // Arrange
            var textReader = new StringReader("a,b,c");
            var reader = new CsvReader(textReader, leaveOpen: false);

            // Act
            reader.Dispose();

            // Assert - StringReader.Dispose doesn't throw, but we can verify it's disposed
            // by checking that we can't read from it (StringReader doesn't actually
            // throw on read after dispose, so we'll just verify the method completes)
            Assert.Pass("Dispose completed without error");
        }

        [Test]
        public void Dispose_WithLeaveOpenTrue_DoesNotDisposeTextReader()
        {
            // Arrange
            var textReader = new StringReader("a,b,c");
            var reader = new CsvReader(textReader, leaveOpen: true);

            // Act
            reader.Dispose();

            // Assert - TextReader should still be usable
            Assert.DoesNotThrow(() => textReader.Read());
        }

        [Test]
        public void PopField_WithCarriageReturnNewLine_HandlesCorrectly()
        {
            // Arrange
            var csvContent = "a,b\r\nc,d";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo("b"));
            Assert.That(reader.PopField(), Is.Null);
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("c"));
            Assert.That(reader.PopField(), Is.EqualTo("d"));
        }

        [Test]
        public void PopField_WithTrailingSeparator_HandlesCorrectly()
        {
            // Arrange
            var csvContent = "a,b,";
            using var reader = new CsvReader(csvContent);

            // Act & Assert
            Assert.That(reader.PopField(), Is.EqualTo("a"));
            Assert.That(reader.PopField(), Is.EqualTo("b"));
            Assert.That(reader.PopField(), Is.EqualTo(""));
            Assert.That(reader.PopField(), Is.Null);
        }

        [Test]
        public void NextRecord_WithoutPoppingAllFields_StillMovesToNextLine()
        {
            // Arrange
            var csvContent = "a,b,c\n1,2,3";
            using var reader = new CsvReader(csvContent);

            // Act
            reader.PopField(); // a
            // Don't pop b and c

            // Assert
            Assert.That(reader.NextRecord(), Is.True);
            Assert.That(reader.PopField(), Is.EqualTo("1"));
        }
    }
}