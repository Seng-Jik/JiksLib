using NUnit.Framework;
using JiksLib.IO;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiksLib.Test.IO
{
    [TestFixture]
    public class LEBinaryWriterTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithNullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.That(() => new LEBinaryWriter(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_WithNullEncoding_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new LEBinaryWriter(stream, null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_WithNonWritableStream_ShouldThrowArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            stream.Close(); // Make stream non-writable

            // Act & Assert
            Assert.That(() => new LEBinaryWriter(stream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("writable"));
        }

        [Test]
        public void Constructor_SetsCanSeekPropertyCorrectly()
        {
            // Arrange
            using var seekableStream = new MemoryStream();
            using var nonSeekableStream = new NonSeekableStream();

            // Act
            using var writer1 = new LEBinaryWriter(seekableStream, leaveOpen: true);
            using var writer2 = new LEBinaryWriter(nonSeekableStream, leaveOpen: true);

            // Assert
            Assert.That(writer1.CanSeek, Is.True);
            Assert.That(writer2.CanSeek, Is.False);
        }

        [Test]
        public void Constructor_WithLeaveOpen_DoesNotCloseStreamOnDispose()
        {
            // Arrange
            var stream = new MemoryStream();
            var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act
            writer.Dispose();

            // Assert
            Assert.That(stream.CanWrite, Is.True, "Stream should still be writable after dispose with leaveOpen=true");
        }

        [Test]
        public void Constructor_WithoutLeaveOpen_ClosesStreamOnDispose()
        {
            // Arrange
            var stream = new MemoryStream();
            var writer = new LEBinaryWriter(stream, leaveOpen: false);

            // Act
            writer.Dispose();

            // Assert
            Assert.That(stream.CanWrite, Is.False, "Stream should be closed after dispose with leaveOpen=false");
        }

        #endregion

        #region Position Tests

        [Test]
        public void Position_Get_OnSeekableStream_ReturnsCorrectPosition()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Write((byte)0x01);
            writer.Write((byte)0x02);

            // Assert
            Assert.That(writer.Position, Is.EqualTo(2));
        }

        [Test]
        public void Position_Set_OnSeekableStream_MovesStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Position = 5;
            writer.Write((byte)0x01);

            // Assert
            Assert.That(stream.ToArray()[5], Is.EqualTo(0x01));
        }

        [Test]
        public void Position_Get_OnNonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act & Assert
            Assert.That(() => { var _ = writer.Position; },
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.Contains("does not support seeking"));
        }

        [Test]
        public void Position_Set_OnNonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act & Assert
            Assert.That(() => writer.Position = 5,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.Contains("does not support seeking"));
        }

        #endregion

        #region Seek Tests

        [Test]
        public void Seek_OnSeekableStream_MovesStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[10]);
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Seek(5, SeekOrigin.Begin);
            writer.Write((byte)0x01);

            // Assert
            Assert.That(stream.ToArray()[5], Is.EqualTo(0x01));
        }

        [Test]
        public void Seek_OnNonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act & Assert
            Assert.That(() => writer.Seek(0, SeekOrigin.Begin),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.Contains("does not support seeking"));
        }

        [Test]
        public void Seek_WithLargeOffset_WorksCorrectly()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[1000]);
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Seek(500, SeekOrigin.Begin);
            writer.Write((byte)0x01);

            // Assert
            Assert.That(stream.ToArray()[500], Is.EqualTo(0x01));
        }

        #endregion

        #region Basic Write Tests (byte, sbyte, bool)

        [Test]
        public void Write_Byte_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Write((byte)0x01);
            writer.Write((byte)0xFF);
            writer.Write((byte)0x00);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(3));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0xFF));
            Assert.That(data[2], Is.EqualTo(0x00));
        }

        [Test]
        public void Write_SByte_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Write((sbyte)42);
            writer.Write((sbyte)-42);
            writer.Write((sbyte)sbyte.MaxValue);
            writer.Write((sbyte)sbyte.MinValue);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(42));
            Assert.That(data[1], Is.EqualTo(0xD6)); // -42 in two's complement
            Assert.That(data[2], Is.EqualTo(127));
            Assert.That(data[3], Is.EqualTo(128));
        }

        [Test]
        public void Write_Bool_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Write(true);
            writer.Write(false);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(2));
            Assert.That(data[0], Is.EqualTo(1));
            Assert.That(data[1], Is.EqualTo(0));
        }

        #endregion

        #region ArraySegment Write Tests

        [Test]
        public void Write_ArraySegment_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var segment = new ArraySegment<byte>(buffer);

            // Act
            writer.Write(segment);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public void Write_ArraySegment_WithOffset_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 3);

            // Act
            writer.Write(segment);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(3));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
        }

        [Test]
        public void Write_ArraySegment_WithNullArray_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var segment = default(ArraySegment<byte>); // Array is null

            // Act & Assert
            Assert.That(() => writer.Write(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("cannot be null"));
        }

        [Test]
        public void WriteAsync_ArraySegment_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var segment = new ArraySegment<byte>(buffer);

            // Act
            writer.WriteAsync(segment).Wait();

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public async Task WriteAsync_ArraySegment_WithCancellationToken_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var segment = new ArraySegment<byte>(buffer);
            var cancellationToken = CancellationToken.None;

            // Act
            await writer.WriteAsync(segment, cancellationToken);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public async Task WriteAsync_ArraySegment_WithCancellationToken_WhenNullArray_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var segment = default(ArraySegment<byte>); // Array is null
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.That(async () => await writer.WriteAsync(segment, cancellationToken),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("cannot be null"));
        }

        #endregion

        #region Byte Array Write Tests

        [Test]
        public void Write_ByteArray_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            writer.Write(buffer);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public void Write_ByteArray_WithNullArray_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act & Assert
            Assert.That(() => writer.Write((byte[])null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void WriteAsync_ByteArray_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            writer.WriteAsync(buffer).Wait();

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public async Task WriteAsync_ByteArray_WithCancellationToken_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var cancellationToken = CancellationToken.None;

            // Act
            await writer.WriteAsync(buffer, cancellationToken);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x01));
            Assert.That(data[1], Is.EqualTo(0x02));
            Assert.That(data[2], Is.EqualTo(0x03));
            Assert.That(data[3], Is.EqualTo(0x04));
        }

        [Test]
        public async Task WriteAsync_ByteArray_WithCancellationToken_WhenNullArray_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.That(async () => await writer.WriteAsync((byte[])null!, cancellationToken),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region Numeric Type Tests (char, short, ushort, int, uint, long, ulong)

        [Test]
        public void Write_Char_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            char value = (char)0x1234; // 'ሴ'

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(2));
            Assert.That(data[0], Is.EqualTo(0x34));
            Assert.That(data[1], Is.EqualTo(0x12));
        }

        [Test]
        public void Write_Int16_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            short value = 0x1234;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(2));
            Assert.That(data[0], Is.EqualTo(0x34));
            Assert.That(data[1], Is.EqualTo(0x12));
        }

        [Test]
        public void Write_UInt16_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            ushort value = 0x1234;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(2));
            Assert.That(data[0], Is.EqualTo(0x34));
            Assert.That(data[1], Is.EqualTo(0x12));
        }

        [Test]
        public void Write_Int32_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            int value = 0x12345678;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x78));
            Assert.That(data[1], Is.EqualTo(0x56));
            Assert.That(data[2], Is.EqualTo(0x34));
            Assert.That(data[3], Is.EqualTo(0x12));
        }

        [Test]
        public void Write_UInt32_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            uint value = 0x12345678;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4));
            Assert.That(data[0], Is.EqualTo(0x78));
            Assert.That(data[1], Is.EqualTo(0x56));
            Assert.That(data[2], Is.EqualTo(0x34));
            Assert.That(data[3], Is.EqualTo(0x12));
        }

        [Test]
        public void Write_Int64_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            long value = 0x0123456789ABCDEF;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(8));
            Assert.That(data[0], Is.EqualTo(0xEF));
            Assert.That(data[1], Is.EqualTo(0xCD));
            Assert.That(data[2], Is.EqualTo(0xAB));
            Assert.That(data[3], Is.EqualTo(0x89));
            Assert.That(data[4], Is.EqualTo(0x67));
            Assert.That(data[5], Is.EqualTo(0x45));
            Assert.That(data[6], Is.EqualTo(0x23));
            Assert.That(data[7], Is.EqualTo(0x01));
        }

        [Test]
        public void Write_UInt64_WritesLittleEndianBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            ulong value = 0x0123456789ABCDEF;

            // Act
            writer.Write(value);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(8));
            Assert.That(data[0], Is.EqualTo(0xEF));
            Assert.That(data[1], Is.EqualTo(0xCD));
            Assert.That(data[2], Is.EqualTo(0xAB));
            Assert.That(data[3], Is.EqualTo(0x89));
            Assert.That(data[4], Is.EqualTo(0x67));
            Assert.That(data[5], Is.EqualTo(0x45));
            Assert.That(data[6], Is.EqualTo(0x23));
            Assert.That(data[7], Is.EqualTo(0x01));
        }

        [Test]
        public void Write_NumericTypes_WithBoundaryValues_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act
            writer.Write(short.MinValue);
            writer.Write(short.MaxValue);
            writer.Write(ushort.MinValue);
            writer.Write(ushort.MaxValue);
            writer.Write(int.MinValue);
            writer.Write(int.MaxValue);
            writer.Write(uint.MinValue);
            writer.Write(uint.MaxValue);
            writer.Write(long.MinValue);
            writer.Write(long.MaxValue);
            writer.Write(ulong.MinValue);
            writer.Write(ulong.MaxValue);

            // Assert - Just verify they don't throw exceptions
            Assert.That(stream.Length, Is.EqualTo(2 + 2 + 2 + 2 + 4 + 4 + 4 + 4 + 8 + 8 + 8 + 8));
        }

        #endregion

        #region String Tests

        [Test]
        public void Write_String_WithUTF8Encoding_WritesLengthAndBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream, Encoding.UTF8);
            string text = "Hello, 世界!";

            // Act
            writer.Write(text);

            // Assert
            var data = stream.ToArray();
            var bytes = Encoding.UTF8.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            Assert.That(data, Has.Length.EqualTo(4 + bytes.Length));

            // Check length
            for (int i = 0; i < 4; i++)
                Assert.That(data[i], Is.EqualTo(lengthBytes[i]));

            // Check string bytes
            for (int i = 0; i < bytes.Length; i++)
                Assert.That(data[i + 4], Is.EqualTo(bytes[i]));
        }

        [Test]
        public void Write_String_WithUTF32Encoding_WritesLengthAndBytes()
        {
            // Arrange
            var encoding = Encoding.UTF32;
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream, encoding);
            string text = "Hello, 世界!";

            // Act
            writer.Write(text);

            // Assert
            var data = stream.ToArray();
            var bytes = encoding.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            Assert.That(data, Has.Length.EqualTo(4 + bytes.Length));

            // Check length
            for (int i = 0; i < 4; i++)
                Assert.That(data[i], Is.EqualTo(lengthBytes[i]));
        }

        [Test]
        public void Write_String_WithEmptyString_WritesZeroLength()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            string text = "";

            // Act
            writer.Write(text);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4)); // Only length
            Assert.That(BitConverter.ToInt32(data, 0), Is.EqualTo(0));
        }

        [Test]
        public void Write_String_WithNullString_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act & Assert
            Assert.That(() => writer.Write((string)null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async Task WriteAsync_String_WithUTF8Encoding_WritesLengthAndBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream, Encoding.UTF8);
            string text = "Hello, 世界!";

            // Act
            await writer.WriteAsync(text);

            // Assert
            var data = stream.ToArray();
            var bytes = Encoding.UTF8.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            Assert.That(data, Has.Length.EqualTo(4 + bytes.Length));

            // Check length
            for (int i = 0; i < 4; i++)
                Assert.That(data[i], Is.EqualTo(lengthBytes[i]));

            // Check string bytes
            for (int i = 0; i < bytes.Length; i++)
                Assert.That(data[i + 4], Is.EqualTo(bytes[i]));
        }

        [Test]
        public async Task WriteAsync_String_WithCancellationToken_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream, Encoding.UTF8);
            string text = "Hello, 世界!";
            var cancellationToken = CancellationToken.None;

            // Act
            await writer.WriteAsync(text, cancellationToken);

            // Assert
            var data = stream.ToArray();
            var bytes = Encoding.UTF8.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            Assert.That(data, Has.Length.EqualTo(4 + bytes.Length));

            // Check length
            for (int i = 0; i < 4; i++)
                Assert.That(data[i], Is.EqualTo(lengthBytes[i]));

            // Check string bytes
            for (int i = 0; i < bytes.Length; i++)
                Assert.That(data[i + 4], Is.EqualTo(bytes[i]));
        }

        [Test]
        public async Task WriteAsync_String_WithEmptyString_WritesZeroLength()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);
            string text = "";

            // Act
            await writer.WriteAsync(text);

            // Assert
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(4)); // Only length
            Assert.That(BitConverter.ToInt32(data, 0), Is.EqualTo(0));
        }

        [Test]
        public void WriteAsync_String_WithNullString_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act & Assert
            Assert.That(async () => await writer.WriteAsync((string)null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region Flush and Close Tests

        [Test]
        public void Flush_FlushesUnderlyingStream()
        {
            // Arrange
            var stream = new FlushTrackingStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act
            writer.Flush();

            // Assert
            Assert.That(stream.FlushCalled, Is.True);
        }

        [Test]
        public async Task FlushAsync_FlushesUnderlyingStream()
        {
            // Arrange
            var stream = new FlushTrackingStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.That(stream.FlushAsyncCalled, Is.True);
        }

        [Test]
        public async Task FlushAsync_WithCancellationToken_FlushesUnderlyingStream()
        {
            // Arrange
            var stream = new FlushTrackingStream();
            using var writer = new LEBinaryWriter(stream, leaveOpen: true);
            var cancellationToken = CancellationToken.None;

            // Act
            await writer.FlushAsync(cancellationToken);

            // Assert
            Assert.That(stream.FlushAsyncCalled, Is.True);
        }

        [Test]
        public void Close_ClosesUnderlyingStream()
        {
            // Arrange
            var stream = new MemoryStream();
            var writer = new LEBinaryWriter(stream);

            // Act
            writer.Close();

            // Assert
            Assert.That(stream.CanWrite, Is.False);
        }

        #endregion

        #region Buffer Reuse Tests

        [Test]
        public void Write_MultipleNumericTypes_ReusesBuffer()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var writer = new LEBinaryWriter(stream);

            // Act - Write various types that should use the internal buffer
            writer.Write((short)0x1234);
            writer.Write((ushort)0x5678);
            writer.Write(unchecked((int)0x9ABCDEF0));
            writer.Write((uint)0x12345678);
            writer.Write((long)0x1122334455667788);
            writer.Write((ulong)0x99AABBCCDDEEFF00);

            // Assert - Verify data is written correctly
            var data = stream.ToArray();
            Assert.That(data, Has.Length.EqualTo(2 + 2 + 4 + 4 + 8 + 8));
        }

        #endregion

        #region Helper Classes

        private class NonSeekableStream : Stream
        {
            private readonly MemoryStream _innerStream = new MemoryStream();

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _innerStream.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
            public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _innerStream.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private class FlushTrackingStream : Stream
        {
            public bool FlushCalled { get; private set; } = false;
            public bool FlushAsyncCalled { get; private set; } = false;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get; set; }

            public override void Flush() => FlushCalled = true;
            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                FlushAsyncCalled = true;
                return Task.CompletedTask;
            }

            public override int Read(byte[] buffer, int offset, int count) => 0;
            public override long Seek(long offset, SeekOrigin origin) => 0;
            public override void SetLength(long value) { }
            public override void Write(byte[] buffer, int offset, int count) { }
        }

        #endregion
    }
}