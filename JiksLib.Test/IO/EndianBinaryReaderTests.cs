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
    public class EndianBinaryReaderTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithNullEndianBitConverter_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new EndianBinaryReader(null!, stream, Encoding.UTF8),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(() => new EndianBinaryReader(EndianBitConverter.LittleEndian, null!, Encoding.UTF8),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullEncoding_ThrowsArgumentNullException()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act & Assert
            Assert.That(() => new EndianBinaryReader(EndianBitConverter.LittleEndian, stream, null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNonReadableStream_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new NonReadableStream();

            // Act & Assert
            Assert.That(() => new EndianBinaryReader(EndianBitConverter.LittleEndian, stream, Encoding.UTF8),
                Throws.ArgumentException.With.Message.Contains("readable"));
        }

        [Test]
        public void Constructor_WithLeaveOpen_DoesNotDisposeStream()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using (var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8, leaveOpen: true))
            {
                reader.ReadByte();
            }

            // Assert
            Assert.That(stream.CanRead, Is.True); // Stream should still be open
            stream.Dispose(); // Clean up
        }

        [Test]
        public void Constructor_WithoutLeaveOpen_DisposesStream()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using (var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8, leaveOpen: false))
            {
                reader.ReadByte();
            }

            // Assert
            Assert.That(stream.CanRead, Is.False); // Stream should be disposed
        }

        [Test]
        public void Constructor_DefaultUsesUTF8Encoding()
        {
            // Arrange
            var bytes = Encoding.UTF8.GetBytes("Hello");
            using var stream = new MemoryStream();
            // Write length (little-endian) and string bytes
            stream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            var converter = EndianBitConverter.LittleEndian;

            // Act
            using var reader = new EndianBinaryReader(converter, stream);
            string result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo("Hello"));
        }

        #endregion

        #region Property Tests

        [Test]
        public void BaseStream_ReturnsProvidedStream()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.BaseStream, Is.SameAs(stream));
        }

        [Test]
        public void CanSeek_ReflectsUnderlyingStreamCapability()
        {
            // Arrange
            using var seekableStream = new MemoryStream(new byte[10]);
            using var nonSeekableStream = new NonSeekableStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            using (var reader1 = new EndianBinaryReader(converter, seekableStream, Encoding.UTF8))
            {
                Assert.That(reader1.CanSeek, Is.True);
            }

            using (var reader2 = new EndianBinaryReader(converter, nonSeekableStream, Encoding.UTF8))
            {
                Assert.That(reader2.CanSeek, Is.False);
            }
        }

        [Test]
        public void Position_Get_WhenCanSeekIsTrue_ReturnsStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[20]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            reader.ReadByte();
            reader.ReadByte();
            long expectedPosition = 2;

            // Act
            long position = reader.Position;

            // Assert
            Assert.That(position, Is.EqualTo(expectedPosition));
        }

        [Test]
        public void Position_Get_WhenCanSeekIsFalse_ThrowsNotSupportedException()
        {
            // Arrange
            using var nonSeekableStream = new NonSeekableStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, nonSeekableStream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.Position, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Position_Set_WhenCanSeekIsTrue_UpdatesStreamPosition()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[20]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            long newPosition = 5;

            // Act
            reader.Position = newPosition;

            // Assert
            Assert.That(reader.Position, Is.EqualTo(newPosition));
            Assert.That(stream.Position, Is.EqualTo(newPosition));
        }

        [Test]
        public void Position_Set_WhenCanSeekIsFalse_ThrowsNotSupportedException()
        {
            // Arrange
            using var nonSeekableStream = new NonSeekableStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, nonSeekableStream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.Position = 0, Throws.TypeOf<NotSupportedException>());
        }

        #endregion

        #region Read Primitive Types Tests (Little Endian)

        [Test]
        public void ReadByte_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x01, 0x02, 0x03 };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

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
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            int result = reader.ReadByte();

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void ReadBoolean_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x00, 0x01, 0xFF };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.False); // 0x00
            Assert.That(reader.ReadBoolean(), Is.True);  // 0x01
            Assert.That(reader.ReadBoolean(), Is.True);  // 0xFF
        }

        [Test]
        public void ReadBoolean_AtEndOfStream_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.ReadBoolean(), Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void ReadSByte_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x00, 0x2A, 0x86, 0x7F, 0x80 };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)0x00));
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)0x2A));
            Assert.That(reader.ReadSByte(), Is.EqualTo(unchecked((sbyte)0x86))); // -122
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)0x7F)); // 127
            Assert.That(reader.ReadSByte(), Is.EqualTo(unchecked((sbyte)0x80))); // -128
        }

        [Test]
        public void ReadSByte_AtEndOfStream_ThrowsEndOfStreamException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.ReadSByte(), Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void ReadInt16_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x34, 0x12 }; // 0x1234 in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            short result = reader.ReadInt16();

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void ReadUInt16_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0xCD, 0xAB }; // 0xABCD in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            ushort result = reader.ReadUInt16();

            // Assert
            Assert.That(result, Is.EqualTo(0xABCD));
        }

        [Test]
        public void ReadInt32_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            int result = reader.ReadInt32();

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void ReadUInt32_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0xF0, 0xDE, 0xBC, 0x9A }; // 0x9ABCDEF0 in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            uint result = reader.ReadUInt32();

            // Assert
            Assert.That(result, Is.EqualTo(0x9ABCDEF0));
        }

        [Test]
        public void ReadInt64_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }; // 0x0123456789ABCDEF in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            long result = reader.ReadInt64();

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void ReadUInt64_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE }; // 0xFEDCBA9876543210 in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            ulong result = reader.ReadUInt64();

            // Assert
            Assert.That(result, Is.EqualTo(0xFEDCBA9876543210));
        }

        [Test]
        public void ReadChar_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x41, 0x00, 0x34, 0x12 }; // 'A' (0x0041) and 0x1234 in little-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadChar(), Is.EqualTo('A'));
            Assert.That(reader.ReadChar(), Is.EqualTo((char)0x1234));
        }

        #endregion

        #region Read Primitive Types Tests (Big Endian)

        [Test]
        public void ReadInt16_BigEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x12, 0x34 }; // 0x1234 in big-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.BigEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            short result = reader.ReadInt16();

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void ReadInt32_BigEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x12, 0x34, 0x56, 0x78 }; // 0x12345678 in big-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.BigEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            int result = reader.ReadInt32();

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void ReadInt64_BigEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }; // 0x0123456789ABCDEF in big-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.BigEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            long result = reader.ReadInt64();

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void ReadChar_BigEndian_ReturnsCorrectValue()
        {
            // Arrange
            byte[] data = { 0x00, 0x41, 0x12, 0x34 }; // 'A' (0x0041) and 0x1234 in big-endian
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.BigEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadChar(), Is.EqualTo('A'));
            Assert.That(reader.ReadChar(), Is.EqualTo((char)0x1234));
        }

        #endregion

        #region Read String Tests

        [Test]
        public void ReadString_LittleEndian_ReturnsCorrectValue()
        {
            // Arrange
            string testString = "Hello, World!";
            byte[] lengthBytes = BitConverter.GetBytes(testString.Length); // Little-endian
            byte[] stringBytes = Encoding.UTF8.GetBytes(testString);
            using var stream = new MemoryStream();
            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(stringBytes, 0, stringBytes.Length);
            stream.Position = 0;
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            string result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(testString));
        }

        [Test]
        public void ReadString_BigEndian_ReturnsCorrectValue()
        {
            // Arrange
            string testString = "Hello, World!";
            byte[] lengthBytes = new byte[4];
            int length = testString.Length;
            // Big-endian length
            lengthBytes[0] = (byte)((length >> 24) & 0xFF);
            lengthBytes[1] = (byte)((length >> 16) & 0xFF);
            lengthBytes[2] = (byte)((length >> 8) & 0xFF);
            lengthBytes[3] = (byte)(length & 0xFF);
            byte[] stringBytes = Encoding.UTF8.GetBytes(testString);
            using var stream = new MemoryStream();
            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(stringBytes, 0, stringBytes.Length);
            stream.Position = 0;
            var converter = EndianBitConverter.BigEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            string result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(testString));
        }

        [Test]
        public void ReadString_WithDifferentEncodings_ReturnsCorrectValue()
        {
            // Arrange
            string testString = "Hello, 世界!";
            var encodings = new[] { Encoding.UTF8, Encoding.Unicode, Encoding.ASCII };

            foreach (var encoding in encodings)
            {
                byte[] stringBytes = encoding.GetBytes(testString);
                byte[] lengthBytes = BitConverter.GetBytes(stringBytes.Length); // Little-endian
                using var stream = new MemoryStream();
                stream.Write(lengthBytes, 0, lengthBytes.Length);
                stream.Write(stringBytes, 0, stringBytes.Length);
                stream.Position = 0;
                var converter = EndianBitConverter.LittleEndian;
                using var reader = new EndianBinaryReader(converter, stream, encoding);

                // Act
                string result = reader.ReadString();

                // Assert
                Assert.That(result, Is.EqualTo(encoding.GetString(stringBytes)));
            }
        }

        [Test]
        public void ReadString_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            byte[] lengthBytes = BitConverter.GetBytes(0); // Length = 0
            using var stream = new MemoryStream(lengthBytes);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act
            string result = reader.ReadString();

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ReadString_NegativeLength_ThrowsInvalidDataException()
        {
            // Arrange
            byte[] lengthBytes = BitConverter.GetBytes(-1); // Negative length
            using var stream = new MemoryStream(lengthBytes);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.ReadString(), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void ReadString_InsufficientData_ThrowsEndOfStreamException()
        {
            // Arrange
            byte[] lengthBytes = BitConverter.GetBytes(10); // Length = 10
            using var stream = new MemoryStream(lengthBytes); // Only length, no string data
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(() => reader.ReadString(), Throws.TypeOf<EndOfStreamException>());
        }

        #endregion

        #region Read Array and Buffer Tests

        [Test]
        public void ReadBytes_ReturnsCorrectBytes()
        {
            // Arrange
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            var buffer = new ArraySegment<byte>(new byte[3]);

            // Act
            int bytesRead = reader.ReadBytes(buffer);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(3));
            Assert.That(buffer.Array![0], Is.EqualTo(0x01));
            Assert.That(buffer.Array[1], Is.EqualTo(0x02));
            Assert.That(buffer.Array[2], Is.EqualTo(0x03));
        }

        [Test]
        public void ReadBytes_WithNullArray_ThrowsArgumentException()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[10]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            var segment = new ArraySegment<byte>();

            // Act & Assert
            Assert.That(() => reader.ReadBytes(segment), Throws.ArgumentException.With.Message.Contains("null"));
        }

        [Test]
        public void ReadBytes_AtEndOfStream_ReturnsZero()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[0]);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            var buffer = new ArraySegment<byte>(new byte[10]);

            // Act
            int bytesRead = reader.ReadBytes(buffer);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(0));
        }

        [Test]
        public void ReadBytesExactly_ReadsExactNumberOfBytes()
        {
            // Arrange
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            var buffer = new ArraySegment<byte>(new byte[3]);

            // Act
            reader.ReadBytesExactly(buffer);

            // Assert
            Assert.That(buffer.Array![0], Is.EqualTo(0x01));
            Assert.That(buffer.Array[1], Is.EqualTo(0x02));
            Assert.That(buffer.Array[2], Is.EqualTo(0x03));
        }

        [Test]
        public void ReadBytesExactly_InsufficientData_ThrowsEndOfStreamException()
        {
            // Arrange
            byte[] data = { 0x01, 0x02 };
            using var stream = new MemoryStream(data);
            var converter = EndianBitConverter.LittleEndian;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);
            var buffer = new ArraySegment<byte>(new byte[5]); // Request 5 bytes but only 2 available

            // Act & Assert
            Assert.That(() => reader.ReadBytesExactly(buffer), Throws.TypeOf<EndOfStreamException>());
        }

        #endregion

        #region Integration Tests (Reader + Writer)

        [Test]
        public void ReadWrite_RoundTrip_LittleEndian_AllTypes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Write test data
            writer.Write(true);
            writer.Write((byte)42);
            writer.Write((sbyte)-100);
            writer.Write((short)0x1234);
            writer.Write((ushort)0xABCD);
            writer.Write(0x12345678);
            writer.Write(0x9ABCDEF0U);
            writer.Write(0x0123456789ABCDEFL);
            writer.Write(0xFEDCBA9876543210UL);
            writer.Write('X');
            writer.Write("Test String");

            stream.Position = 0;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadByte(), Is.EqualTo(42));
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)-100));
            Assert.That(reader.ReadInt16(), Is.EqualTo((short)0x1234));
            Assert.That(reader.ReadUInt16(), Is.EqualTo((ushort)0xABCD));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0x12345678));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x9ABCDEF0U));
            Assert.That(reader.ReadInt64(), Is.EqualTo(0x0123456789ABCDEFL));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(0xFEDCBA9876543210UL));
            Assert.That(reader.ReadChar(), Is.EqualTo('X'));
            Assert.That(reader.ReadString(), Is.EqualTo("Test String"));
        }

        [Test]
        public void ReadWrite_RoundTrip_BigEndian_AllTypes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.BigEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Write test data
            writer.Write(true);
            writer.Write((byte)42);
            writer.Write((sbyte)-100);
            writer.Write((short)0x1234);
            writer.Write((ushort)0xABCD);
            writer.Write(0x12345678);
            writer.Write(0x9ABCDEF0U);
            writer.Write(0x0123456789ABCDEFL);
            writer.Write(0xFEDCBA9876543210UL);
            writer.Write('X');
            writer.Write("Test String");

            stream.Position = 0;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadByte(), Is.EqualTo(42));
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)-100));
            Assert.That(reader.ReadInt16(), Is.EqualTo((short)0x1234));
            Assert.That(reader.ReadUInt16(), Is.EqualTo((ushort)0xABCD));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0x12345678));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x9ABCDEF0U));
            Assert.That(reader.ReadInt64(), Is.EqualTo(0x0123456789ABCDEFL));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(0xFEDCBA9876543210UL));
            Assert.That(reader.ReadChar(), Is.EqualTo('X'));
            Assert.That(reader.ReadString(), Is.EqualTo("Test String"));
        }

        [Test]
        public void ReadWrite_RoundTrip_SystemEndian_AllTypes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.SystemEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Write test data
            writer.Write(true);
            writer.Write((byte)42);
            writer.Write((sbyte)-100);
            writer.Write((short)0x1234);
            writer.Write((ushort)0xABCD);
            writer.Write(0x12345678);
            writer.Write(0x9ABCDEF0U);
            writer.Write(0x0123456789ABCDEFL);
            writer.Write(0xFEDCBA9876543210UL);
            writer.Write('X');
            writer.Write("Test String");

            stream.Position = 0;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadByte(), Is.EqualTo(42));
            Assert.That(reader.ReadSByte(), Is.EqualTo((sbyte)-100));
            Assert.That(reader.ReadInt16(), Is.EqualTo((short)0x1234));
            Assert.That(reader.ReadUInt16(), Is.EqualTo((ushort)0xABCD));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0x12345678));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x9ABCDEF0U));
            Assert.That(reader.ReadInt64(), Is.EqualTo(0x0123456789ABCDEFL));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(0xFEDCBA9876543210UL));
            Assert.That(reader.ReadChar(), Is.EqualTo('X'));
            Assert.That(reader.ReadString(), Is.EqualTo("Test String"));
        }

        #endregion

        #region Edge Cases and Error Tests

        [Test]
        public void Read_AllMinMaxValues_ReturnsCorrectValues()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            // Write all min/max values
            writer.Write(true);
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

            stream.Position = 0;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadByte(), Is.EqualTo(byte.MinValue));
            Assert.That(reader.ReadByte(), Is.EqualTo(byte.MaxValue));
            Assert.That(reader.ReadSByte(), Is.EqualTo(sbyte.MinValue));
            Assert.That(reader.ReadSByte(), Is.EqualTo(sbyte.MaxValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MinValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MaxValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MinValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MaxValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MaxValue));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MinValue));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MaxValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MinValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadChar(), Is.EqualTo(char.MinValue));
            Assert.That(reader.ReadChar(), Is.EqualTo(char.MaxValue));
        }

        [Test]
        public void Read_InsufficientDataForPrimitiveType_ThrowsEndOfStreamException()
        {
            // Test each primitive type with insufficient data
            var testCases = new[]
            {
                new { Type = "Boolean", Data = new byte[0] },
                new { Type = "Int16", Data = new byte[1] },
                new { Type = "UInt16", Data = new byte[1] },
                new { Type = "Int32", Data = new byte[3] },
                new { Type = "UInt32", Data = new byte[3] },
                new { Type = "Int64", Data = new byte[7] },
                new { Type = "UInt64", Data = new byte[7] },
                new { Type = "Char", Data = new byte[1] },
            };

            foreach (var testCase in testCases)
            {
                using var stream = new MemoryStream(testCase.Data);
                var converter = EndianBitConverter.LittleEndian;
                using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

                // Act & Assert
                Assert.That(() =>
                {
                    switch (testCase.Type)
                    {
                        case "Boolean": reader.ReadBoolean(); break;
                        case "Int16": reader.ReadInt16(); break;
                        case "UInt16": reader.ReadUInt16(); break;
                        case "Int32": reader.ReadInt32(); break;
                        case "UInt32": reader.ReadUInt32(); break;
                        case "Int64": reader.ReadInt64(); break;
                        case "UInt64": reader.ReadUInt64(); break;
                        case "Char": reader.ReadChar(); break;
                    }
                }, Throws.TypeOf<EndOfStreamException>(), $"Failed for type {testCase.Type}");
            }
        }

        [Test]
        public void Read_MixedTypesWithSeeking_CorrectlyReadsData()
        {
            // Arrange
            using var stream = new MemoryStream();
            var converter = EndianBitConverter.LittleEndian;
            using var writer = new EndianBinaryWriter(converter, stream, Encoding.UTF8);

            writer.Write(0x12345678);
            writer.Write(0x9ABCDEF0U);
            writer.Write("First String");
            writer.Write(0x1122334455667788L);
            writer.Write("Second String");

            stream.Position = 0;
            using var reader = new EndianBinaryReader(converter, stream, Encoding.UTF8);

            // Act & Assert
            Assert.That(reader.ReadInt32(), Is.EqualTo(0x12345678));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x9ABCDEF0U));
            Assert.That(reader.ReadString(), Is.EqualTo("First String"));

            // Seek back to beginning and read again
            reader.Position = 0;
            Assert.That(reader.ReadInt32(), Is.EqualTo(0x12345678));

            // Seek to position of second string
            reader.Position = sizeof(int) + sizeof(uint) + (sizeof(int) + "First String".Length);
            Assert.That(reader.ReadInt64(), Is.EqualTo(0x1122334455667788L));
            Assert.That(reader.ReadString(), Is.EqualTo("Second String"));
        }

        #endregion

        #region Helper Classes

        private class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) { }
        }

        private class NonSeekableStream : Stream
        {
            private readonly MemoryStream _memoryStream;

            public NonSeekableStream(byte[] data)
            {
                _memoryStream = new MemoryStream(data);
            }

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