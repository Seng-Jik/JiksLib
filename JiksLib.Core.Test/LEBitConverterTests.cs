using NUnit.Framework;
using JiksLib;
using System;

namespace JiksLib.Test
{
    [TestFixture]
    public class LEBitConverterTests
    {
        #region Int16 Tests

        [Test]
        public void ToInt16_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x34, 0x12 }; // 0x1234 = 4660 in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            short result = LEBitConverter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void GetBytes_Int16_WritesCorrectBytes()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x34));
            Assert.That(buffer[1], Is.EqualTo(0x12));
        }

        [Test]
        public void Int16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            short original = -12345;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            short result = LEBitConverter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToInt16_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 2);

            // Act
            short result = LEBitConverter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void GetBytes_Int16_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 2);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF)); // Should not be modified
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x12));
            Assert.That(buffer[3], Is.EqualTo(0xFF)); // Should not be modified
            Assert.That(buffer[4], Is.EqualTo(0xFF)); // Should not be modified
        }

        [Test]
        public void ToInt16_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[1];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToInt16(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void GetBytes_Int16_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void GetBytes_Int16_ReturnsByteArray()
        {
            // Arrange
            short value = 0x1234;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(2));
            Assert.That(bytes[0], Is.EqualTo(0x34));
            Assert.That(bytes[1], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_Int16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            short original = -12345;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            short result = LEBitConverter.ToInt16(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region UInt16 Tests

        [Test]
        public void ToUInt16_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x34, 0x12 }; // 0x1234 = 4660 in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            ushort result = LEBitConverter.ToUInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void GetBytes_UInt16_WritesCorrectBytes()
        {
            // Arrange
            ushort value = 0x1234;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x34));
            Assert.That(buffer[1], Is.EqualTo(0x12));
        }

        [Test]
        public void UInt16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            ushort original = 0xABCD;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            ushort result = LEBitConverter.ToUInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToUInt16_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 2);

            // Act
            ushort result = LEBitConverter.ToUInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void GetBytes_UInt16_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            ushort value = 0x1234;
            byte[] buffer = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 2);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x12));
            Assert.That(buffer[3], Is.EqualTo(0xFF));
            Assert.That(buffer[4], Is.EqualTo(0xFF));
        }

        [Test]
        public void ToUInt16_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[1];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToUInt16(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void GetBytes_UInt16_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            ushort value = 0x1234;
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void GetBytes_UInt16_ReturnsByteArray()
        {
            // Arrange
            ushort value = 0x1234;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(2));
            Assert.That(bytes[0], Is.EqualTo(0x34));
            Assert.That(bytes[1], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_UInt16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            ushort original = 0xABCD;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            ushort result = LEBitConverter.ToUInt16(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region Int32 Tests

        [Test]
        public void ToInt32_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 = 305419896 in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            int result = LEBitConverter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void GetBytes_Int32_WritesCorrectBytes()
        {
            // Arrange
            int value = 0x12345678;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x78));
            Assert.That(buffer[1], Is.EqualTo(0x56));
            Assert.That(buffer[2], Is.EqualTo(0x34));
            Assert.That(buffer[3], Is.EqualTo(0x12));
        }

        [Test]
        public void Int32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            int original = -123456789;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            int result = LEBitConverter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToInt32_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0xFF, 0x78, 0x56, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 3, 4);

            // Act
            int result = LEBitConverter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void GetBytes_Int32_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            int value = 0x12345678;
            byte[] buffer = new byte[8];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;
            var segment = new ArraySegment<byte>(buffer, 2, 4);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0xFF));
            Assert.That(buffer[2], Is.EqualTo(0x78));
            Assert.That(buffer[3], Is.EqualTo(0x56));
            Assert.That(buffer[4], Is.EqualTo(0x34));
            Assert.That(buffer[5], Is.EqualTo(0x12));
            Assert.That(buffer[6], Is.EqualTo(0xFF));
            Assert.That(buffer[7], Is.EqualTo(0xFF));
        }

        [Test]
        public void ToInt32_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[3];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToInt32(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 4"));
        }

        [Test]
        public void GetBytes_Int32_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            int value = 0x12345678;
            byte[] buffer = new byte[3];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 4"));
        }

        [Test]
        public void GetBytes_Int32_ReturnsByteArray()
        {
            // Arrange
            int value = 0x12345678;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(4));
            Assert.That(bytes[0], Is.EqualTo(0x78));
            Assert.That(bytes[1], Is.EqualTo(0x56));
            Assert.That(bytes[2], Is.EqualTo(0x34));
            Assert.That(bytes[3], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_Int32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            int original = -123456789;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            int result = LEBitConverter.ToInt32(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region UInt32 Tests

        [Test]
        public void ToUInt32_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 = 305419896 in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            uint result = LEBitConverter.ToUInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void GetBytes_UInt32_WritesCorrectBytes()
        {
            // Arrange
            uint value = 0x12345678;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x78));
            Assert.That(buffer[1], Is.EqualTo(0x56));
            Assert.That(buffer[2], Is.EqualTo(0x34));
            Assert.That(buffer[3], Is.EqualTo(0x12));
        }

        [Test]
        public void UInt32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            uint original = 0xABCDEF12;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            uint result = LEBitConverter.ToUInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToUInt32_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0xFF, 0x78, 0x56, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 3, 4);

            // Act
            uint result = LEBitConverter.ToUInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void GetBytes_UInt32_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            uint value = 0x12345678;
            byte[] buffer = new byte[8];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;
            var segment = new ArraySegment<byte>(buffer, 2, 4);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0xFF));
            Assert.That(buffer[2], Is.EqualTo(0x78));
            Assert.That(buffer[3], Is.EqualTo(0x56));
            Assert.That(buffer[4], Is.EqualTo(0x34));
            Assert.That(buffer[5], Is.EqualTo(0x12));
            Assert.That(buffer[6], Is.EqualTo(0xFF));
            Assert.That(buffer[7], Is.EqualTo(0xFF));
        }

        [Test]
        public void ToUInt32_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[3];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToUInt32(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 4"));
        }

        [Test]
        public void GetBytes_UInt32_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            uint value = 0x12345678;
            byte[] buffer = new byte[3];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 4"));
        }

        [Test]
        public void GetBytes_UInt32_ReturnsByteArray()
        {
            // Arrange
            uint value = 0x12345678;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(4));
            Assert.That(bytes[0], Is.EqualTo(0x78));
            Assert.That(bytes[1], Is.EqualTo(0x56));
            Assert.That(bytes[2], Is.EqualTo(0x34));
            Assert.That(bytes[3], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_UInt32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            uint original = 0xABCDEF12;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            uint result = LEBitConverter.ToUInt32(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region Int64 Tests

        [Test]
        public void ToInt64_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }; // 0x0123456789ABCDEF in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            long result = LEBitConverter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void GetBytes_Int64_WritesCorrectBytes()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xEF));
            Assert.That(buffer[1], Is.EqualTo(0xCD));
            Assert.That(buffer[2], Is.EqualTo(0xAB));
            Assert.That(buffer[3], Is.EqualTo(0x89));
            Assert.That(buffer[4], Is.EqualTo(0x67));
            Assert.That(buffer[5], Is.EqualTo(0x45));
            Assert.That(buffer[6], Is.EqualTo(0x23));
            Assert.That(buffer[7], Is.EqualTo(0x01));
        }

        [Test]
        public void Int64_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            long original = -1234567890123456789L;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            long result = LEBitConverter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToInt64_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0xFF, 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 3, 8);

            // Act
            long result = LEBitConverter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void GetBytes_Int64_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[12];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;
            var segment = new ArraySegment<byte>(buffer, 2, 8);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0xFF));
            Assert.That(buffer[2], Is.EqualTo(0xEF));
            Assert.That(buffer[3], Is.EqualTo(0xCD));
            Assert.That(buffer[4], Is.EqualTo(0xAB));
            Assert.That(buffer[5], Is.EqualTo(0x89));
            Assert.That(buffer[6], Is.EqualTo(0x67));
            Assert.That(buffer[7], Is.EqualTo(0x45));
            Assert.That(buffer[8], Is.EqualTo(0x23));
            Assert.That(buffer[9], Is.EqualTo(0x01));
            Assert.That(buffer[10], Is.EqualTo(0xFF));
            Assert.That(buffer[11], Is.EqualTo(0xFF));
        }

        [Test]
        public void ToInt64_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[7];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToInt64(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 8"));
        }

        [Test]
        public void GetBytes_Int64_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[7];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 8"));
        }

        [Test]
        public void GetBytes_Int64_ReturnsByteArray()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(8));
            Assert.That(bytes[0], Is.EqualTo(0xEF));
            Assert.That(bytes[1], Is.EqualTo(0xCD));
            Assert.That(bytes[2], Is.EqualTo(0xAB));
            Assert.That(bytes[3], Is.EqualTo(0x89));
            Assert.That(bytes[4], Is.EqualTo(0x67));
            Assert.That(bytes[5], Is.EqualTo(0x45));
            Assert.That(bytes[6], Is.EqualTo(0x23));
            Assert.That(bytes[7], Is.EqualTo(0x01));
        }

        [Test]
        public void GetBytes_Int64_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            long original = -1234567890123456789L;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            long result = LEBitConverter.ToInt64(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region UInt64 Tests

        [Test]
        public void ToUInt64_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }; // 0x0123456789ABCDEF in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            ulong result = LEBitConverter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void GetBytes_UInt64_WritesCorrectBytes()
        {
            // Arrange
            ulong value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xEF));
            Assert.That(buffer[1], Is.EqualTo(0xCD));
            Assert.That(buffer[2], Is.EqualTo(0xAB));
            Assert.That(buffer[3], Is.EqualTo(0x89));
            Assert.That(buffer[4], Is.EqualTo(0x67));
            Assert.That(buffer[5], Is.EqualTo(0x45));
            Assert.That(buffer[6], Is.EqualTo(0x23));
            Assert.That(buffer[7], Is.EqualTo(0x01));
        }

        [Test]
        public void UInt64_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            ulong original = 0xFEDCBA9876543210UL;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            ulong result = LEBitConverter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToUInt64_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0xFF, 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 3, 8);

            // Act
            ulong result = LEBitConverter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void GetBytes_UInt64_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            ulong value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[12];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;
            var segment = new ArraySegment<byte>(buffer, 2, 8);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0xFF));
            Assert.That(buffer[2], Is.EqualTo(0xEF));
            Assert.That(buffer[3], Is.EqualTo(0xCD));
            Assert.That(buffer[4], Is.EqualTo(0xAB));
            Assert.That(buffer[5], Is.EqualTo(0x89));
            Assert.That(buffer[6], Is.EqualTo(0x67));
            Assert.That(buffer[7], Is.EqualTo(0x45));
            Assert.That(buffer[8], Is.EqualTo(0x23));
            Assert.That(buffer[9], Is.EqualTo(0x01));
            Assert.That(buffer[10], Is.EqualTo(0xFF));
            Assert.That(buffer[11], Is.EqualTo(0xFF));
        }

        [Test]
        public void ToUInt64_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[7];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToUInt64(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 8"));
        }

        [Test]
        public void GetBytes_UInt64_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            ulong value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[7];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 8"));
        }

        [Test]
        public void GetBytes_UInt64_ReturnsByteArray()
        {
            // Arrange
            ulong value = 0x0123456789ABCDEF;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(8));
            Assert.That(bytes[0], Is.EqualTo(0xEF));
            Assert.That(bytes[1], Is.EqualTo(0xCD));
            Assert.That(bytes[2], Is.EqualTo(0xAB));
            Assert.That(bytes[3], Is.EqualTo(0x89));
            Assert.That(bytes[4], Is.EqualTo(0x67));
            Assert.That(bytes[5], Is.EqualTo(0x45));
            Assert.That(bytes[6], Is.EqualTo(0x23));
            Assert.That(bytes[7], Is.EqualTo(0x01));
        }

        [Test]
        public void GetBytes_UInt64_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            ulong original = 0xFEDCBA9876543210UL;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            ulong result = LEBitConverter.ToUInt64(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region Boolean Tests

        [Test]
        public void ToBoolean_Byte_ReturnsCorrectValue()
        {
            // Arrange & Act & Assert
            Assert.That(LEBitConverter.ToBoolean(0), Is.False);
            Assert.That(LEBitConverter.ToBoolean(1), Is.True);
            Assert.That(LEBitConverter.ToBoolean(255), Is.True);
        }

        [Test]
        public void ToBoolean_ArraySegment_ReturnsCorrectValue()
        {
            // Arrange
            byte[] trueBytes = { 1 };
            byte[] falseBytes = { 0 };
            byte[] nonZeroBytes = { 42 };
            var trueSegment = new ArraySegment<byte>(trueBytes);
            var falseSegment = new ArraySegment<byte>(falseBytes);
            var nonZeroSegment = new ArraySegment<byte>(nonZeroBytes);

            // Act & Assert
            Assert.That(LEBitConverter.ToBoolean(trueSegment), Is.True);
            Assert.That(LEBitConverter.ToBoolean(falseSegment), Is.False);
            Assert.That(LEBitConverter.ToBoolean(nonZeroSegment), Is.True);
        }

        [Test]
        public void ToBoolean_ArraySegment_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0x00, 0x01, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 1);

            // Act
            bool result = LEBitConverter.ToBoolean(segment);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ToBoolean_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[0];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToBoolean(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 1"));
        }

        [Test]
        public void GetByte_Bool_ReturnsCorrectByte()
        {
            // Arrange & Act & Assert
            Assert.That(LEBitConverter.GetByte(false), Is.EqualTo(0));
            Assert.That(LEBitConverter.GetByte(true), Is.EqualTo(1));
        }

        [Test]
        public void GetBytes_Bool_ArraySegment_WritesCorrectByte()
        {
            // Arrange
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);

            // Act - true
            LEBitConverter.GetBytes(true, segment);
            Assert.That(buffer[0], Is.EqualTo(1));

            // Act - false
            LEBitConverter.GetBytes(false, segment);
            Assert.That(buffer[0], Is.EqualTo(0));
        }

        [Test]
        public void GetBytes_Bool_ArraySegment_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            byte[] buffer = { 0xFF, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 1);

            // Act
            LEBitConverter.GetBytes(true, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(1));
            Assert.That(buffer[2], Is.EqualTo(0xFF));
        }

        [Test]
        public void GetBytes_Bool_ArraySegment_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] buffer = new byte[0];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(true, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 1"));
        }

        [Test]
        public void GetBytes_Bool_ReturnsByteArray()
        {
            // Arrange & Act
            byte[] trueBytes = LEBitConverter.GetBytes(true);
            byte[] falseBytes = LEBitConverter.GetBytes(false);

            // Assert
            Assert.That(trueBytes, Has.Length.EqualTo(1));
            Assert.That(trueBytes[0], Is.EqualTo(1));
            Assert.That(falseBytes, Has.Length.EqualTo(1));
            Assert.That(falseBytes[0], Is.EqualTo(0));
        }

        [Test]
        public void Boolean_RoundTrip_ReturnsOriginalValue()
        {
            // Test both true and false
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);

            // Test true
            LEBitConverter.GetBytes(true, segment);
            bool trueResult = LEBitConverter.ToBoolean(segment);
            Assert.That(trueResult, Is.True);

            // Test false
            LEBitConverter.GetBytes(false, segment);
            bool falseResult = LEBitConverter.ToBoolean(segment);
            Assert.That(falseResult, Is.False);
        }

        [Test]
        public void ToBoolean_Byte_AllNonZeroValuesReturnTrue()
        {
            // Test that all non-zero byte values return true
            for (int i = 1; i <= 255; i++)
            {
                Assert.That(LEBitConverter.ToBoolean((byte)i), Is.True,
                    $"Byte value {i} should return true");
            }
        }

        [Test]
        public void GetBytes_Bool_ArrayAlwaysLengthOne()
        {
            // Arrange & Act
            byte[] trueBytes = LEBitConverter.GetBytes(true);
            byte[] falseBytes = LEBitConverter.GetBytes(false);

            // Assert
            Assert.That(trueBytes.Length, Is.EqualTo(1));
            Assert.That(falseBytes.Length, Is.EqualTo(1));
        }

        #endregion

        #region Char Tests

        [Test]
        public void ToChar_ArraySegment_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x34, 0x12 }; // 'ሴ' (0x1234) in little-endian
            var segment = new ArraySegment<byte>(bytes);

            // Act
            char result = LEBitConverter.ToChar(segment);

            // Assert
            Assert.That(result, Is.EqualTo((char)0x1234));
        }

        [Test]
        public void ToChar_ArraySegment_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 2);

            // Act
            char result = LEBitConverter.ToChar(segment);

            // Assert
            Assert.That(result, Is.EqualTo((char)0x1234));
        }

        [Test]
        public void GetBytes_Char_ArraySegment_WritesCorrectBytes()
        {
            // Arrange
            char value = (char)0x1234;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x34));
            Assert.That(buffer[1], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_Char_ArraySegment_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            char value = (char)0x1234;
            byte[] buffer = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 2);

            // Act
            LEBitConverter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x12));
            Assert.That(buffer[3], Is.EqualTo(0xFF));
            Assert.That(buffer[4], Is.EqualTo(0xFF));
        }

        [Test]
        public void GetBytes_Char_ReturnsByteArray()
        {
            // Arrange
            char value = (char)0x1234;

            // Act
            byte[] bytes = LEBitConverter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(2));
            Assert.That(bytes[0], Is.EqualTo(0x34));
            Assert.That(bytes[1], Is.EqualTo(0x12));
        }

        [Test]
        public void GetBytes_Char_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            char original = 'A';

            // Act
            byte[] bytes = LEBitConverter.GetBytes(original);
            char result = LEBitConverter.ToChar(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void Char_RoundTrip_WithArraySegment_ReturnsOriginalValue()
        {
            // Arrange
            char original = 'Z';
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);

            // Act
            LEBitConverter.GetBytes(original, segment);
            char result = LEBitConverter.ToChar(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void ToChar_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            char minValue = char.MinValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            char result = LEBitConverter.ToChar(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToChar_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            char maxValue = char.MaxValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            char result = LEBitConverter.ToChar(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void GetBytes_Char_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            char value = 'A';
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);

            // Act & Assert
            Assert.That(() => LEBitConverter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void ToChar_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[1];
            var segment = new ArraySegment<byte>(bytes);

            // Act & Assert
            Assert.That(() => LEBitConverter.ToChar(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ToInt16_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            short minValue = short.MinValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            short result = LEBitConverter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToInt16_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            short maxValue = short.MaxValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            short result = LEBitConverter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ToInt32_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            int minValue = int.MinValue;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            int result = LEBitConverter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToInt32_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            int maxValue = int.MaxValue;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            int result = LEBitConverter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ToInt64_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            long minValue = long.MinValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            long result = LEBitConverter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToInt64_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            long maxValue = long.MaxValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            long result = LEBitConverter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ToUInt16_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            ushort minValue = ushort.MinValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            ushort result = LEBitConverter.ToUInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToUInt16_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            ushort maxValue = ushort.MaxValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            ushort result = LEBitConverter.ToUInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ToUInt32_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            uint minValue = uint.MinValue;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            uint result = LEBitConverter.ToUInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToUInt32_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            uint maxValue = uint.MaxValue;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            uint result = LEBitConverter.ToUInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ToUInt64_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            ulong minValue = ulong.MinValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(minValue, segment);

            // Act
            ulong result = LEBitConverter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void ToUInt64_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            ulong maxValue = ulong.MaxValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            LEBitConverter.GetBytes(maxValue, segment);

            // Act
            ulong result = LEBitConverter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void GetBytes_Zero_WritesAllZeroBytes()
        {
            // Test for each type
            byte[] buffer16 = new byte[2];
            LEBitConverter.GetBytes((short)0, new ArraySegment<byte>(buffer16));
            Assert.That(buffer16[0], Is.EqualTo(0));
            Assert.That(buffer16[1], Is.EqualTo(0));

            byte[] buffer32 = new byte[4];
            LEBitConverter.GetBytes(0, new ArraySegment<byte>(buffer32));
            Assert.That(buffer32[0], Is.EqualTo(0));
            Assert.That(buffer32[1], Is.EqualTo(0));
            Assert.That(buffer32[2], Is.EqualTo(0));
            Assert.That(buffer32[3], Is.EqualTo(0));

            byte[] buffer64 = new byte[8];
            LEBitConverter.GetBytes(0L, new ArraySegment<byte>(buffer64));
            Assert.That(buffer64[0], Is.EqualTo(0));
            Assert.That(buffer64[1], Is.EqualTo(0));
            Assert.That(buffer64[2], Is.EqualTo(0));
            Assert.That(buffer64[3], Is.EqualTo(0));
            Assert.That(buffer64[4], Is.EqualTo(0));
            Assert.That(buffer64[5], Is.EqualTo(0));
            Assert.That(buffer64[6], Is.EqualTo(0));
            Assert.That(buffer64[7], Is.EqualTo(0));
        }

        [Test]
        public void MixedOperations_OnSameBuffer_WorkCorrectly()
        {
            // Arrange: Create a buffer large enough for multiple values
            byte[] buffer = new byte[20]; // Enough for various types
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;

            // Act & Assert: Write different types at different positions
            // Write bool at position 0
            LEBitConverter.GetBytes(true, new ArraySegment<byte>(buffer, 0, 1));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(0xFF)); // Unchanged

            // Write char at position 1-2
            char charValue = (char)0x1234;
            LEBitConverter.GetBytes(charValue, new ArraySegment<byte>(buffer, 1, 2));
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x12));
            Assert.That(buffer[3], Is.EqualTo(0xFF)); // Unchanged

            // Write short at position 3-4
            short shortValue = 0x5678;
            LEBitConverter.GetBytes(shortValue, new ArraySegment<byte>(buffer, 3, 2));
            Assert.That(buffer[3], Is.EqualTo(0x78));
            Assert.That(buffer[4], Is.EqualTo(0x56));
            Assert.That(buffer[5], Is.EqualTo(0xFF)); // Unchanged

            // Write int at position 5-8
            int intValue = unchecked((int)0x9ABCDEF0);
            LEBitConverter.GetBytes(intValue, new ArraySegment<byte>(buffer, 5, 4));
            Assert.That(buffer[5], Is.EqualTo(0xF0));
            Assert.That(buffer[6], Is.EqualTo(0xDE));
            Assert.That(buffer[7], Is.EqualTo(0xBC));
            Assert.That(buffer[8], Is.EqualTo(0x9A));
            Assert.That(buffer[9], Is.EqualTo(0xFF)); // Unchanged

            // Write long at position 9-16
            long longValue = 0x1122334455667788L;
            LEBitConverter.GetBytes(longValue, new ArraySegment<byte>(buffer, 9, 8));
            Assert.That(buffer[9], Is.EqualTo(0x88));
            Assert.That(buffer[10], Is.EqualTo(0x77));
            Assert.That(buffer[11], Is.EqualTo(0x66));
            Assert.That(buffer[12], Is.EqualTo(0x55));
            Assert.That(buffer[13], Is.EqualTo(0x44));
            Assert.That(buffer[14], Is.EqualTo(0x33));
            Assert.That(buffer[15], Is.EqualTo(0x22));
            Assert.That(buffer[16], Is.EqualTo(0x11));

            // Verify reads work correctly
            Assert.That(LEBitConverter.ToBoolean(new ArraySegment<byte>(buffer, 0, 1)), Is.True);
            Assert.That(LEBitConverter.ToChar(new ArraySegment<byte>(buffer, 1, 2)), Is.EqualTo(charValue));
            Assert.That(LEBitConverter.ToInt16(new ArraySegment<byte>(buffer, 3, 2)), Is.EqualTo(shortValue));
            Assert.That(LEBitConverter.ToInt32(new ArraySegment<byte>(buffer, 5, 4)), Is.EqualTo(intValue));
            Assert.That(LEBitConverter.ToInt64(new ArraySegment<byte>(buffer, 9, 8)), Is.EqualTo(longValue));
        }

        #endregion
    }
}