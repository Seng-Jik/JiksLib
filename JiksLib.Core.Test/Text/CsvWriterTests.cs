using NUnit.Framework;
using JiksLib.Text;
using System.Text;

namespace JiksLib.Test.Text
{
    [TestFixture]
    public class CsvWriterTests
    {
        [Test]
        public void Constructor_WithDefaultSeparator_CreatesInstance()
        {
            // Arrange & Act
            var writer = new CsvWriter();

            // Assert
            Assert.That(writer, Is.Not.Null);
            Assert.That(writer.AlwaysWrap, Is.False);
        }

        [Test]
        public void Constructor_WithCustomSeparator_CreatesInstance()
        {
            // Arrange & Act
            var writer = new CsvWriter('|');

            // Assert
            Assert.That(writer, Is.Not.Null);
        }

        [Test]
        public void WriteField_SimpleField_WritesWithoutQuotes()
        {
            // Arrange
            var writer = new CsvWriter();
            var field = "hello";

            // Act
            writer.WriteField(field);
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void WriteField_MultipleFields_SeparatesWithCommas()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a");
            writer.WriteField("b");
            writer.WriteField("c");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("a,b,c"));
        }

        [Test]
        public void WriteField_FieldWithComma_WrapsInQuotes()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a,b");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a,b\""));
        }

        [Test]
        public void WriteField_FieldWithQuote_EscapesQuotes()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a\"b");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a\"\"b\""));
        }

        [Test]
        public void WriteField_FieldWithNewLine_WrapsInQuotes()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a\nb");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a\nb\""));
        }

        [Test]
        public void WriteField_FieldWithCarriageReturn_WrapsInQuotes()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a\rb");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a\rb\""));
        }

        [Test]
        public void WriteField_FieldWithTab_WrapsInQuotes()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a\tb");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a\tb\""));
        }

        [Test]
        public void WriteField_WithCustomSeparatorFieldContainsSeparator_WrapsInQuotes()
        {
            // Arrange
            var writer = new CsvWriter('|');

            // Act
            writer.WriteField("a|b");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a|b\""));
        }

        [Test]
        public void AlwaysWrap_WhenTrue_WrapsAllFields()
        {
            // Arrange
            var writer = new CsvWriter { AlwaysWrap = true };

            // Act
            writer.WriteField("simple");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"simple\""));
        }

        [Test]
        public void NextRecord_AddsNewLine()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a");
            writer.WriteField("b");
            writer.NextRecord();
            writer.WriteField("c");
            writer.WriteField("d");
            var result = writer.ToString().TrimEnd('\n', '\r');

            // Assert
            Assert.That(result, Is.EqualTo("a,b\r\nc,d"));
        }

        [Test]
        public void NextRecord_ResetsFirstFieldFlag()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a");
            writer.NextRecord();
            writer.WriteField("b");
            var result = writer.ToString().TrimEnd('\n', '\r');

            // Assert
            Assert.That(result, Is.EqualTo("a\r\nb"));
        }

        [Test]
        public void ToString_ReturnsCurrentCsv()
        {
            // Arrange
            var writer = new CsvWriter();
            writer.WriteField("test");

            // Act
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Clear_ResetsWriter()
        {
            // Arrange
            var writer = new CsvWriter();
            writer.WriteField("a");
            writer.WriteField("b");

            // Act
            writer.Clear();
            writer.WriteField("c");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("c"));
        }

        [Test]
        public void Clear_ResetsFirstFieldFlag()
        {
            // Arrange
            var writer = new CsvWriter();
            writer.WriteField("a");
            writer.NextRecord();

            // Act
            writer.Clear();
            writer.WriteField("b");
            writer.WriteField("c");
            var result = writer.ToString().TrimEnd('\n', '\r');

            // Assert
            Assert.That(result, Is.EqualTo("b,c"));
        }

        [Test]
        public void WriteField_EmptyString_WritesEmptyField()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("");
            writer.WriteField("b");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo(",b"));
        }

        [Test]
        public void WriteField_AfterClear_WorksCorrectly()
        {
            // Arrange
            var writer = new CsvWriter();
            writer.WriteField("old");
            writer.Clear();

            // Act
            writer.WriteField("new");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("new"));
        }

        [Test]
        public void WriteField_WithAlwaysWrapCondition_FieldContainsCommaTriggersWrap()
        {
            // Arrange
            var writer = new CsvWriter();

            // Act
            writer.WriteField("a,b"); // Contains comma
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("\"a,b\""));
        }

        [Test]
        public void WriteField_WithCustomSeparator_WorksCorrectly()
        {
            // Arrange
            var writer = new CsvWriter(';');

            // Act
            writer.WriteField("a");
            writer.WriteField("b");
            var result = writer.ToString().TrimEnd();

            // Assert
            Assert.That(result, Is.EqualTo("a;b"));
        }
    }
}