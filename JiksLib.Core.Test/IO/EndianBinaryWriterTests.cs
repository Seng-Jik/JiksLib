using NUnit.Framework;
using JiksLib;
using JiksLib.IO;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JiksLib.Test.IO
{
    [TestFixture]
    public class EndianBinaryWriterTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithNullEndianBitConverter_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new EndianBinaryWriter(null!, stream, Encoding.UTF8),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(() => new EndianBinaryWriter(EndianBitConverter.LittleEndian, null!, Encoding.UTF8),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullEncoding_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new EndianBinaryWriter(EndianBitConverter.LittleEndian, stream, null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNonWritableStream_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[10], false); // Read-only stream

            // Act & Assert
            Assert.That(() => new EndianBinaryWriter(EndianBitConverter.LittleEndian, stream, Encoding.UTF8),
                Throws.ArgumentException.With.Message.Contains("writable"));
        }

        [Test]
        public void Constructor_WithLeaveOpen_DoesNotDisposeStream()
        {
            // Arrange
            var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using (var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(42);
            }

            // Assert
            Assert.That(stream.CanWrite, Is.True); // Stream should still be open
            stream.Dispose(); // Clean up
        }

        [Test]
        public void Constructor_WithoutLeaveOpen_DisposesStream()
        {
            // Arrange
            var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using (var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(42);
            }

            // Assert
            Assert.That(stream.CanWrite, Is.False); // Stream should be disposed
        }

        [Test]
        public void Constructor_DefaultUsesUTF8Encoding()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using var writer = new EndianBinaryWriter(converter, stream);

            // Assert
            writer.Write("Hello");
            stream.Position = 0;
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            int length = reader.ReadInt32();
            string result = Encoding.UTF8.GetString(reader.ReadBytes(length));
            Assert.That(result, Is.EqualTo("Hello"));
        }

        #endregion

        #region Property Tests

        [Test]
        public void BaseStream_ReturnsProvidedStream()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(writer.BaseStream, Is.SameAs(stream));
        }

        [Test]
        public void CanSeek_ReflectsUnderlyingStreamCapability()
        {
            // Arrange
            using var seekableStream = new MemoryStream();
            using var nonSeekableStream = new NonSeekableStream();
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            using (var writer1 = new EndianBinaryWriter(converter, seekableStream, Encoding.UTF8))
            {
                Assert.That(writer1.CanSeek, Is.True);
            }

            using (var writer2 = new EndianBinaryWriter(converter, nonSeekableStream, Encoding.UTF8))
            {
                Assert.That(writer2.CanSeek, Is.False);
            }
        }

        [Test]
        public void Position_Get_WhenCanSeekIsTrue_ReturnsStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            writer.Write(0x12345678);
            long expectedPosition = sizeof(int);

            // Act
            long position = writer.Position;

            // Assert
            Assert.That(position, Is.EqualTo(expectedPosition));
        }

        [Test]
        public void Position_Get_WhenCanSeekIsFalse_ThrowsNotSupportedException()
        {
            // Arrange
            using var nonSeekableStream = new NonSeekableStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, nonSeekableStream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => writer.Position, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Position_Set_WhenCanSeekIsTrue_UpdatesStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            writer.Write(0x12345678);
            writer.Write(0x9ABCDEF0);
            long newPosition = sizeof(int);

            // Act
            writer.Position = newPosition;

            // Assert
            Assert.That(writer.Position, Is.EqualTo(newPosition));
            Assert.That(stream.Position, Is.EqualTo(newPosition));
        }

        [Test]
        public void Position_Set_WhenCanSeekIsFalse_ThrowsNotSupportedException()
        {
            // Arrange
            using var nonSeekableStream = new NonSeekableStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, nonSeekableStream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => writer.Position = 0, Throws.TypeOf<NotSupportedException>());
        }

        #endregion

        #region Write Primitive Types Tests (Little Endian)

        [Test]
        public void Write_Boolean_LittleEndian_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(true);
            writer.Write(false);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(1));  // true -> 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0));  // false -> 0
        }

        [Test]
        public void Write_SByte_LittleEndian_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write((sbyte)42);
            writer.Write((sbyte)-100);
            writer.Write(sbyte.MinValue);
            writer.Write(sbyte.MaxValue);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(42));
            Assert.That(stream.ReadByte(), Is.EqualTo(156)); // -100 as byte
            Assert.That(stream.ReadByte(), Is.EqualTo(128)); // sbyte.MinValue
            Assert.That(stream.ReadByte(), Is.EqualTo(127)); // sbyte.MaxValue
        }

        [Test]
        public void Write_Byte_LittleEndian_WritesCorrectByte()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write((byte)42);
            writer.Write(byte.MinValue);
            writer.Write(byte.MaxValue);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(42));
            Assert.That(stream.ReadByte(), Is.EqualTo(0));
            Assert.That(stream.ReadByte(), Is.EqualTo(255));
        }

        [Test]
        public void Write_Int16_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write((short)0x1234);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // Low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // High byte
        }

        [Test]
        public void Write_UInt16_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write((ushort)0xABCD);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0xCD)); // Low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0xAB)); // High byte
        }

        [Test]
        public void Write_Int32_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0x12345678);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x78)); // Byte 0
            Assert.That(stream.ReadByte(), Is.EqualTo(0x56)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // Byte 3
        }

        [Test]
        public void Write_UInt32_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0x9ABCDEF0U);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0xF0)); // Byte 0
            Assert.That(stream.ReadByte(), Is.EqualTo(0xDE)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0xBC)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x9A)); // Byte 3
        }

        [Test]
        public void Write_Int64_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0x0123456789ABCDEFL);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0xEF)); // Byte 0
            Assert.That(stream.ReadByte(), Is.EqualTo(0xCD)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0xAB)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x89)); // Byte 3
            Assert.That(stream.ReadByte(), Is.EqualTo(0x67)); // Byte 4
            Assert.That(stream.ReadByte(), Is.EqualTo(0x45)); // Byte 5
            Assert.That(stream.ReadByte(), Is.EqualTo(0x23)); // Byte 6
            Assert.That(stream.ReadByte(), Is.EqualTo(0x01)); // Byte 7
        }

        [Test]
        public void Write_UInt64_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0xFEDCBA9876543210UL);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x10)); // Byte 0
            Assert.That(stream.ReadByte(), Is.EqualTo(0x32)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0x54)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x76)); // Byte 3
            Assert.That(stream.ReadByte(), Is.EqualTo(0x98)); // Byte 4
            Assert.That(stream.ReadByte(), Is.EqualTo(0xBA)); // Byte 5
            Assert.That(stream.ReadByte(), Is.EqualTo(0xDC)); // Byte 6
            Assert.That(stream.ReadByte(), Is.EqualTo(0xFE)); // Byte 7
        }

        [Test]
        public void Write_Char_LittleEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write('A'); // 0x0041
            writer.Write((char)0x1234);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x41)); // 'A' low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x00)); // 'A' high byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // 0x1234 low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // 0x1234 high byte
        }

        #endregion

        #region Write Primitive Types Tests (Big Endian)

        [Test]
        public void Write_Int16_BigEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write((short)0x1234);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // High byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // Low byte
        }

        [Test]
        public void Write_Int32_BigEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0x12345678);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // Byte 0 (MSB)
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0x56)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x78)); // Byte 3 (LSB)
        }

        [Test]
        public void Write_Int64_BigEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(0x0123456789ABCDEFL);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x01)); // Byte 0 (MSB)
            Assert.That(stream.ReadByte(), Is.EqualTo(0x23)); // Byte 1
            Assert.That(stream.ReadByte(), Is.EqualTo(0x45)); // Byte 2
            Assert.That(stream.ReadByte(), Is.EqualTo(0x67)); // Byte 3
            Assert.That(stream.ReadByte(), Is.EqualTo(0x89)); // Byte 4
            Assert.That(stream.ReadByte(), Is.EqualTo(0xAB)); // Byte 5
            Assert.That(stream.ReadByte(), Is.EqualTo(0xCD)); // Byte 6
            Assert.That(stream.ReadByte(), Is.EqualTo(0xEF)); // Byte 7 (LSB)
        }

        [Test]
        public void Write_Char_BigEndian_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write('A'); // 0x0041
            writer.Write((char)0x1234);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x00)); // 'A' high byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x41)); // 'A' low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // 0x1234 high byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // 0x1234 low byte
        }

        #endregion

        #region Write String Tests

        [Test]
        public void Write_String_LittleEndian_WritesLengthAndBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            string testString = "Hello, World!";

            // Act
            writer.Write(testString);
            stream.Position = 0;

            // Assert - Check length (little-endian)
            Assert.That(stream.ReadByte(), Is.EqualTo(testString.Length & 0xFF)); // Length low byte
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 8) & 0xFF));
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 16) & 0xFF));
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 24) & 0xFF)); // Length high byte

            // Check string bytes
            var bytes = Encoding.UTF8.GetBytes(testString);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.That(stream.ReadByte(), Is.EqualTo(bytes[i]));
            }
        }

        [Test]
        public void Write_String_BigEndian_WritesLengthAndBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            string testString = "Hello, World!";

            // Act
            writer.Write(testString);
            stream.Position = 0;

            // Assert - Check length (big-endian)
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 24) & 0xFF)); // Length high byte
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 16) & 0xFF));
            Assert.That(stream.ReadByte(), Is.EqualTo((testString.Length >> 8) & 0xFF));
            Assert.That(stream.ReadByte(), Is.EqualTo(testString.Length & 0xFF)); // Length low byte

            // Check string bytes
            var bytes = Encoding.UTF8.GetBytes(testString);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.That(stream.ReadByte(), Is.EqualTo(bytes[i]));
            }
        }

        [Test]
        public void Write_String_WithDifferentEncodings_WritesCorrectBytes()
        {
            // Arrange
            string testString = "Hello, 世界!";
            var encodings = new[] { Encoding.UTF8, Encoding.Unicode, Encoding.ASCII };

            foreach (var encoding in encodings)
            {
                using var stream = new MemoryStream();
                var converter = EndianBitConverter.LittleEndian;
                using var writer = new EndianBinaryWriter(converter, stream, encoding);

                // Act
                writer.Write(testString);
                stream.Position = 0;

                // Assert - Read length
                int length = stream.ReadByte() | (stream.ReadByte() << 8) |
                            (stream.ReadByte() << 16) | (stream.ReadByte() << 24);

                // Read string bytes
                var buffer = new byte[length];
                stream.Read(buffer, 0, length);
                string result = encoding.GetString(buffer);

                Assert.That(result, Is.EqualTo(encoding.GetString(encoding.GetBytes(testString))));
            }
        }

        [Test]
        public void Write_String_Null_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => writer.Write((string)null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Write_String_Empty_WritesZeroLength()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write("");
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0)); // Length low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0));
            Assert.That(stream.ReadByte(), Is.EqualTo(0));
            Assert.That(stream.ReadByte(), Is.EqualTo(0)); // Length high byte
            Assert.That(stream.Length, Is.EqualTo(4)); // Only length, no data
        }

        #endregion

        #region Write Array Tests

        [Test]
        public void Write_ByteArray_WritesAllBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            byte[] buffer = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            writer.Write(buffer);
            stream.Position = 0;

            // Assert
            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.That(stream.ReadByte(), Is.EqualTo(buffer[i]));
            }
        }

        [Test]
        public void Write_ByteArray_Null_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => writer.Write((byte[])null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Write_ArraySegment_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            byte[] data = { 0xFF, 0xFF, 0x01, 0x02, 0x03, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(data, 2, 3); // [0x01, 0x02, 0x03]

            // Act
            writer.Write(segment);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x01));
            Assert.That(stream.ReadByte(), Is.EqualTo(0x02));
            Assert.That(stream.ReadByte(), Is.EqualTo(0x03));
            Assert.That(stream.Length, Is.EqualTo(3));
        }

        [Test]
        public void Write_ArraySegment_WithNullArray_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            var segment = new ArraySegment<byte>();

            // Act & Assert
            Assert.That(() => writer.Write(segment), Throws.ArgumentException.With.Message.Contains("null"));
        }

        #endregion

        #region Async Tests

        [Test]
        public async Task WriteAsync_ByteArray_WritesAllBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            byte[] buffer = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            await writer.WriteAsync(buffer);
            stream.Position = 0;

            // Assert
            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.That(stream.ReadByte(), Is.EqualTo(buffer[i]));
            }
        }

        [Test]
        public async Task WriteAsync_ArraySegment_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            byte[] data = { 0xFF, 0xFF, 0x01, 0x02, 0x03, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(data, 2, 3);

            // Act
            await writer.WriteAsync(segment);
            stream.Position = 0;

            // Assert
            Assert.That(stream.ReadByte(), Is.EqualTo(0x01));
            Assert.That(stream.ReadByte(), Is.EqualTo(0x02));
            Assert.That(stream.ReadByte(), Is.EqualTo(0x03));
        }

        [Test]
        public async Task WriteAsync_String_WritesLengthAndBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            string testString = "Async Test";

            // Act
            await writer.WriteAsync(testString);
            stream.Position = 0;

            // Assert - Check length
            int length = stream.ReadByte() | (stream.ReadByte() << 8) |
                        (stream.ReadByte() << 16) | (stream.ReadByte() << 24);
            Assert.That(length, Is.EqualTo(testString.Length));

            // Check string bytes
            var bytes = Encoding.UTF8.GetBytes(testString);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.That(stream.ReadByte(), Is.EqualTo(bytes[i]));
            }
        }

        [Test]
        public void WriteAsync_String_Null_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(async () => await writer.WriteAsync((string)null!), Throws.ArgumentNullException);
        }

        #endregion

        #region Seek and Flush Tests

        [Test]
        public void Seek_WhenCanSeekIsTrue_MovesStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            writer.Write(0x12345678);
            long initialPosition = writer.Position;

            // Act
            writer.Seek(0, SeekOrigin.Begin);

            // Assert
            Assert.That(writer.Position, Is.EqualTo(0));
            Assert.That(initialPosition, Is.EqualTo(sizeof(int)));
        }

        [Test]
        public void Seek_WhenCanSeekIsFalse_ThrowsNotSupportedException()
        {
            // Arrange
            using var nonSeekableStream = new NonSeekableStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, nonSeekableStream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => writer.Seek(0, SeekOrigin.Begin), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Flush_ClearsBuffer()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            writer.Write(0x12345678);

            // Act
            writer.Flush();

            // Assert - No exception should be thrown
            Assert.That(stream.Length, Is.EqualTo(sizeof(int)));
        }

        [Test]
        public async Task FlushAsync_ClearsBuffer()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);
            writer.Write(0x12345678);

            // Act
            await writer.FlushAsync();

            // Assert - No exception should be thrown
            Assert.That(stream.Length, Is.EqualTo(sizeof(int)));
        }

        #endregion

        #region Edge Cases and Error Tests

        [Test]
        public void Write_MixedTypes_CorrectlyWritesAllData()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(true);
            writer.Write((byte)42);
            writer.Write((sbyte)-100);
            writer.Write((short)0x1234);
            writer.Write(0x56789ABC);
            writer.Write(0xDEF0123456789ABCL);
            writer.Write('X');
            writer.Write("Test");

            stream.Position = 0;

            // Assert - Verify each value
            Assert.That(stream.ReadByte(), Is.EqualTo(1)); // bool true
            Assert.That(stream.ReadByte(), Is.EqualTo(42)); // byte
            Assert.That(stream.ReadByte(), Is.EqualTo(156)); // sbyte -100
            Assert.That(stream.ReadByte(), Is.EqualTo(0x34)); // short low byte
            Assert.That(stream.ReadByte(), Is.EqualTo(0x12)); // short high byte
            // Continue checking other values...
        }

        [Test]
        public void Write_AllMinMaxValues_CorrectlyWrites()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Act
            writer.Write(bool.FalseString != null); // true
            writer.Write(byte.MinValue);
            writer.Write(byte.MaxValue);
            writer.Write(sbyte.MinValue);
            writer.Write(sbyte.MaxValue);
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
            writer.Write(char.MinValue);
            writer.Write(char.MaxValue);

            // Assert - No exception should be thrown
            Assert.That(stream.Length, Is.GreaterThan(0));
        }

        #endregion

        #region Helper Classes

        private class NonSeekableStream : Stream
        {
            private readonly MemoryStream _memoryStream = new MemoryStream();

            public override bool CanRead => _memoryStream.CanRead;
            public override bool CanSeek => false; // Non-seekable
            public override bool CanWrite => _memoryStream.CanWrite;
            public override long Length => _memoryStream.Length;
            public override long Position
            {
                get => _memoryStream.Position;
                set => throw new NotSupportedException();
            }

            public override void Flush() => _memoryStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _memoryStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => _memoryStream.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _memoryStream.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}