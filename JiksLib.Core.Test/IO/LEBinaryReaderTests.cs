using NUnit.Framework;
using JiksLib.IO;
using System;
using System.IO;
using System.Text;

namespace JiksLib.Test.IO
{
    [TestFixture]
    public class LEBinaryReaderTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithNullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.That(() => new LEBinaryReader(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_WithNullEncoding_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new LEBinaryReader(stream, null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_WithNonReadableStream_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            stream.Close(); // Make stream non-readable

            // Act & Assert
            Assert.That(() => new LEBinaryReader(stream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("readable"));
        }

        [Test]
        public void Constructor_SetsCanSeekPropertyCorrectly()
        {
            // Arrange
            using var seekableStream = new MemoryStream(new byte[10]);
            using var nonSeekableStream = new NonSeekableStream(new byte[10]);

            // Act
            using var reader1 = new LEBinaryReader(seekableStream, leaveOpen: true);
            using var reader2 = new LEBinaryReader(nonSeekableStream, leaveOpen: true);

            // Assert
            Assert.That(reader1.CanSeek, Is.True);
            Assert.That(reader2.CanSeek, Is.False);
        }

        [Test]
        public void Constructor_WithLeaveOpen_DoesNotCloseStreamOnDispose()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);
            var reader = new LEBinaryReader(stream, leaveOpen: true);

            // Act
            reader.Dispose();

            // Assert
            Assert.That(stream.CanRead, Is.True, "Stream should still be readable after dispose with leaveOpen=true");
        }

        [Test]
        public void Constructor_WithoutLeaveOpen_ClosesStreamOnDispose()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);
            var reader = new LEBinaryReader(stream, leaveOpen: false);

            // Act
            reader.Dispose();

            // Assert
            Assert.That(stream.CanRead, Is.False, "Stream should be closed after dispose with leaveOpen=false");
        }

        #endregion

        #region Position Tests

        [Test]
        public void Position_Get_OnSeekableStream_ReturnsCorrectPosition()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            reader.ReadByte(); // Read first byte

            // Assert
            Assert.That(reader.Position, Is.EqualTo(1));
        }

        [Test]
        public void Position_Set_OnSeekableStream_MovesStreamPosition()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            reader.Position = 2;
            var byteRead = reader.ReadByte();

            // Assert
            Assert.That(byteRead, Is.EqualTo(0x03));
        }

        [Test]
        public void Position_Get_OnNonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableStream(new byte[10]);
            using var reader = new LEBinaryReader(stream, leaveOpen: true);

            // Act & Assert
            Assert.That(() => { var _ = reader.Position; },
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.Contains("does not support seeking"));
        }

        [Test]
        public void Position_Set_OnNonSeekableStream_ThrowsNotSupportedException()
        {
            // Arrange
            using var stream = new NonSeekableStream(new byte[10]);
            using var reader = new LEBinaryReader(stream, leaveOpen: true);

            // Act & Assert
            Assert.That(() => reader.Position = 5,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.Contains("does not support seeking"));
        }

        #endregion

        #region ReadByte Tests

        [Test]
        public void ReadByte_WithData_ReturnsCorrectByte()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(reader.ReadByte(), Is.EqualTo(0x01));
            Assert.That(reader.ReadByte(), Is.EqualTo(0x02));
            Assert.That(reader.ReadByte(), Is.EqualTo(0x03));
        }

        [Test]
        public void ReadByte_AtEndOfStream_ReturnsMinusOne()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadByte();

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        #endregion

        #region ReadBytes Tests

        [Test]
        public void ReadBytes_WithValidBuffer_ReadsCorrectBytes()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[3];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            var bytesRead = reader.ReadBytes(segment);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(0x01));
            Assert.That(buffer[1], Is.EqualTo(0x02));
            Assert.That(buffer[2], Is.EqualTo(0x03));
        }

        [Test]
        public void ReadBytes_WithOffset_ReadsToCorrectPosition()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[6];
            var segment = new ArraySegment<byte>(buffer, 2, 3);

            // Act
            var bytesRead = reader.ReadBytes(segment);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(0));
            Assert.That(buffer[1], Is.EqualTo(0));
            Assert.That(buffer[2], Is.EqualTo(0x01));
            Assert.That(buffer[3], Is.EqualTo(0x02));
            Assert.That(buffer[4], Is.EqualTo(0x03));
            Assert.That(buffer[5], Is.EqualTo(0));
        }

        [Test]
        public void ReadBytes_WithPartialData_ReturnsPartialReadCount()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[5];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            var bytesRead = reader.ReadBytes(segment);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(0x01));
            Assert.That(buffer[1], Is.EqualTo(0x02));
        }

        [Test]
        public void ReadBytes_WithEmptyBuffer_ReturnsZero()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[0];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            var bytesRead = reader.ReadBytes(segment);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(0));
        }

        [Test]
        public void ReadBytes_WithNullArray_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[10]);
            using var reader = new LEBinaryReader(stream);
            var segment = default(ArraySegment<byte>); // Array is null

            // Act & Assert
            Assert.That(() => reader.ReadBytes(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("cannot be null"));
        }

        [Test]
        public void ReadBytesExactly_WithEnoughData_ReadsAllBytes()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            reader.ReadBytesExactly(segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x01));
            Assert.That(buffer[1], Is.EqualTo(0x02));
            Assert.That(buffer[2], Is.EqualTo(0x03));
            Assert.That(buffer[3], Is.EqualTo(0x04));
        }

        [Test]
        public void ReadBytesExactly_WithInsufficientData_ThrowsEndOfStreamException()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x02 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);
            var buffer = new byte[5];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => reader.ReadBytesExactly(segment),
                Throws.TypeOf<EndOfStreamException>());
        }

        #endregion

        #region Boolean and SByte Tests

        [Test]
        public void ReadBoolean_WithZeroByte_ReturnsFalse()
        {
            // Arrange
            var data = new byte[] { 0x00 };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadBoolean();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ReadBoolean_WithNonZeroByte_ReturnsTrue()
        {
            // Arrange
            var data = new byte[] { 0x01, 0x42, 0xFF };
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadBoolean(), Is.True);
        }

        [Test]
        public void ReadBoolean_AtEndOfStream_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadBoolean(),
                Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void ReadSByte_WithPositiveValue_ReturnsCorrectSByte()
        {
            // Arrange
            var data = new byte[] { 0x2A, 0x7F }; // 42, 127
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)42));
            Assert.That(reader.ReadSByte(), Is.EqualTo(sbyte.MaxValue));
        }

        [Test]
        public void ReadSByte_WithNegativeValue_ReturnsCorrectSByte()
        {
            // Arrange
            var data = new byte[] { 0x86, 0x80 }; // -122, -128
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)-122));
            Assert.That(reader.ReadSByte(), Is.EqualTo(sbyte.MinValue));
        }

        [Test]
        public void ReadSByte_AtEndOfStream_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadSByte(),
                Throws.TypeOf<EndOfStreamException>());
        }

        #endregion

        #region Numeric Type Tests (Int16, UInt16, Int32, UInt32, Int64, UInt64, Char)

        [Test]
        public void ReadInt16_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x34, 0x12 }; // 0x1234 = 4660
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadInt16();

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void ReadUInt16_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x34, 0x12 }; // 0x1234 = 4660
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadUInt16();

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void ReadInt32_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadInt32();

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void ReadUInt32_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadUInt32();

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void ReadInt64_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }; // 0x0123456789ABCDEF
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadInt64();

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void ReadUInt64_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }; // 0x0123456789ABCDEF
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadUInt64();

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void ReadChar_ReturnsCorrectValue()
        {
            // Arrange
            var data = new byte[] { 0x34, 0x12 }; // 'ሴ' (0x1234)
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadChar();

            // Assert
            Assert.That(result, Is.EqualTo((char)0x1234));
        }

        [Test]
        public void ReadChar_WithMultipleChars_ReturnsCorrectValues()
        {
            // Arrange
            var data = new byte[] { 0x41, 0x00, 0x42, 0x00, 0x43, 0x00 }; // 'A', 'B', 'C'
            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(reader.ReadChar(), Is.EqualTo('A'));
            Assert.That(reader.ReadChar(), Is.EqualTo('B'));
            Assert.That(reader.ReadChar(), Is.EqualTo('C'));
        }

        [Test]
        public void ReadNumericTypes_AtEndOfStream_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[1]); // Not enough for any numeric type
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadInt16(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadUInt16(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadInt32(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadUInt32(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadInt64(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadUInt64(),
                Throws.TypeOf<EndOfStreamException>());
            Assert.That(() => reader.ReadChar(),
                Throws.TypeOf<EndOfStreamException>());
        }

        #endregion

        #region String Tests

        [Test]
        public void ReadString_WithUTF8Encoding_ReturnsCorrectString()
        {
            // Arrange
            var text = "Hello, 世界!";
            var bytes = Encoding.UTF8.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            var data = new byte[lengthBytes.Length + bytes.Length];
            Array.Copy(lengthBytes, 0, data, 0, lengthBytes.Length);
            Array.Copy(bytes, 0, data, lengthBytes.Length, bytes.Length);

            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream, Encoding.UTF8);

            // Act
            var result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(text));
        }

        [Test]
        public void ReadString_WithUTF32Encoding_ReturnsCorrectString()
        {
            // Arrange
            var text = "Hello, 世界!";
            var encoding = Encoding.UTF32;
            var bytes = encoding.GetBytes(text);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            var data = new byte[lengthBytes.Length + bytes.Length];
            Array.Copy(lengthBytes, 0, data, 0, lengthBytes.Length);
            Array.Copy(bytes, 0, data, lengthBytes.Length, bytes.Length);

            using var stream = new MemoryStream(data);
            using var reader = new LEBinaryReader(stream, encoding);

            // Act
            var result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(text));
        }

        [Test]
        public void ReadString_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var lengthBytes = BitConverter.GetBytes(0);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            using var stream = new MemoryStream(lengthBytes);
            using var reader = new LEBinaryReader(stream);

            // Act
            var result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ReadString_WithInsufficientDataForLength_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[3]); // Not enough for Int32 length
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadString(),
                Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void ReadString_WithInsufficientDataForString_ThrowsEndOfStreamException()
        {
            // Arrange
            var lengthBytes = BitConverter.GetBytes(10); // Claim 10 bytes of string data
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            using var stream = new MemoryStream(lengthBytes); // But only provide length, no string data
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadString(),
                Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void ReadString_WithNegativeLength_ThrowsInvalidDataException()
        {
            // Arrange
            var lengthBytes = BitConverter.GetBytes(-1); // Negative length
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            using var stream = new MemoryStream(lengthBytes);
            using var reader = new LEBinaryReader(stream);

            // Act & Assert
            Assert.That(() => reader.ReadString(),
                Throws.TypeOf<InvalidDataException>());
        }

        #endregion

        #region Close Tests

        [Test]
        public void Close_ClosesUnderlyingStream()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);
            var reader = new LEBinaryReader(stream);

            // Act
            reader.Close();

            // Assert
            Assert.That(stream.CanRead, Is.False);
        }

        #endregion

        #region Helper Classes

        private class NonSeekableStream : Stream
        {
            private readonly MemoryStream _innerStream;

            public NonSeekableStream(byte[] data)
            {
                _innerStream = new MemoryStream(data);
            }

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => _innerStream.Length;
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _innerStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _innerStream.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}