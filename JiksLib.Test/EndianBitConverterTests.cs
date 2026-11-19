using NUnit.Framework;
using JiksLib;
using System;

namespace JiksLib.Test
{
    [TestFixture]
    public class EndianBitConverterTests
    {
        #region LittleEndian Property Tests

        [Test]
        public void LittleEndian_Property_ReturnsLEBitConverterInstance()
        {
            // Arrange & Act
            var converter = EndianBitConverter.LittleEndian;

            // Assert
            Assert.That(converter, Is.Not.Null);
            Assert.That(converter, Is.InstanceOf<EndianBitConverter>());
        }

        [Test]
        public void LittleEndian_Property_IsSingleton()
        {
            // Arrange & Act
            var converter1 = EndianBitConverter.LittleEndian;
            var converter2 = EndianBitConverter.LittleEndian;

            // Assert
            Assert.That(converter1, Is.SameAs(converter2));
        }

        #endregion

        #region BigEndian Property Tests

        [Test]
        public void BigEndian_Property_ReturnsBEBitConverterInstance()
        {
            // Arrange & Act
            var converter = EndianBitConverter.BigEndian;

            // Assert
            Assert.That(converter, Is.Not.Null);
            Assert.That(converter, Is.InstanceOf<EndianBitConverter>());
        }

        [Test]
        public void BigEndian_Property_IsSingleton()
        {
            // Arrange & Act
            var converter1 = EndianBitConverter.BigEndian;
            var converter2 = EndianBitConverter.BigEndian;

            // Assert
            Assert.That(converter1, Is.SameAs(converter2));
        }

        #endregion

        #region SystemEndian Property Tests

        [Test]
        public void SystemEndian_Property_ReturnsCorrectConverterBasedOnSystem()
        {
            // Arrange & Act
            var converter = EndianBitConverter.SystemEndian;

            // Assert
            Assert.That(converter, Is.Not.Null);
            if (BitConverter.IsLittleEndian)
            {
                Assert.That(converter, Is.SameAs(EndianBitConverter.LittleEndian));
            }
            else
            {
                Assert.That(converter, Is.SameAs(EndianBitConverter.BigEndian));
            }
        }

        [Test]
        public void SystemEndian_Property_IsSingleton()
        {
            // Arrange & Act
            var converter1 = EndianBitConverter.SystemEndian;
            var converter2 = EndianBitConverter.SystemEndian;

            // Assert
            Assert.That(converter1, Is.SameAs(converter2));
        }

        #endregion

        #region LittleEndian Functional Tests

        [Test]
        public void LittleEndian_ToInt16_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x34, 0x12 }; // 0x1234 = 4660 in little-endian
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void LittleEndian_GetBytes_Int16_WritesCorrectBytes()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x34));
            Assert.That(buffer[1], Is.EqualTo(0x12));
        }

        [Test]
        public void LittleEndian_Int16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            short original = -12345;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            converter.GetBytes(original, segment);
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region BigEndian Functional Tests

        [Test]
        public void BigEndian_ToInt16_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x12, 0x34 }; // 0x1234 = 4660 in big-endian
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.BigEndian;

            // Act
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void BigEndian_GetBytes_Int16_WritesCorrectBytes()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x12));
            Assert.That(buffer[1], Is.EqualTo(0x34));
        }

        [Test]
        public void BigEndian_Int16_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            short original = -12345;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(original, segment);
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void BigEndian_ToInt32_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x12, 0x34, 0x56, 0x78 }; // 0x12345678 in big-endian
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.BigEndian;

            // Act
            int result = converter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x12345678));
        }

        [Test]
        public void BigEndian_GetBytes_Int32_WritesCorrectBytes()
        {
            // Arrange
            int value = 0x12345678;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x12));
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x56));
            Assert.That(buffer[3], Is.EqualTo(0x78));
        }

        [Test]
        public void BigEndian_Int32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            int original = -123456789;
            byte[] buffer = new byte[4];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(original, segment);
            int result = converter.ToInt32(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void BigEndian_ToInt64_ValidBytes_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }; // 0x0123456789ABCDEF in big-endian
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.BigEndian;

            // Act
            long result = converter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x0123456789ABCDEF));
        }

        [Test]
        public void BigEndian_GetBytes_Int64_WritesCorrectBytes()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x01));
            Assert.That(buffer[1], Is.EqualTo(0x23));
            Assert.That(buffer[2], Is.EqualTo(0x45));
            Assert.That(buffer[3], Is.EqualTo(0x67));
            Assert.That(buffer[4], Is.EqualTo(0x89));
            Assert.That(buffer[5], Is.EqualTo(0xAB));
            Assert.That(buffer[6], Is.EqualTo(0xCD));
            Assert.That(buffer[7], Is.EqualTo(0xEF));
        }

        [Test]
        public void BigEndian_Int64_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            long original = -1234567890123456789L;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(original, segment);
            long result = converter.ToInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region Common Type Tests (Boolean, SByte, Char)

        [Test]
        public void ToBoolean_Byte_ReturnsCorrectValue()
        {
            // Arrange & Act & Assert
            var converter = EndianBitConverter.LittleEndian; // Endianness doesn't matter for single byte
            Assert.That(converter.ToBoolean(0), Is.False);
            Assert.That(converter.ToBoolean(1), Is.True);
            Assert.That(converter.ToBoolean(255), Is.True);
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
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            Assert.That(converter.ToBoolean(trueSegment), Is.True);
            Assert.That(converter.ToBoolean(falseSegment), Is.False);
            Assert.That(converter.ToBoolean(nonZeroSegment), Is.True);
        }

        [Test]
        public void GetBytes_Bool_ArraySegment_WritesCorrectByte()
        {
            // Arrange
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.LittleEndian;

            // Act - true
            converter.GetBytes(true, segment);
            Assert.That(buffer[0], Is.EqualTo(1));

            // Act - false
            converter.GetBytes(false, segment);
            Assert.That(buffer[0], Is.EqualTo(0));
        }

        [Test]
        public void ToSByte_ArraySegment_ReturnsCorrectValue()
        {
            // Arrange
            byte[] positiveBytes = { 42 };
            byte[] negativeBytes = { 0x86 }; // -122 in two's complement
            var positiveSegment = new ArraySegment<byte>(positiveBytes);
            var negativeSegment = new ArraySegment<byte>(negativeBytes);
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            Assert.That(converter.ToSByte(positiveSegment), Is.EqualTo((sbyte)42));
            Assert.That(converter.ToSByte(negativeSegment), Is.EqualTo((sbyte)-122));
        }

        [Test]
        public void GetBytes_SByte_ArraySegment_WritesCorrectByte()
        {
            // Arrange
            sbyte value = -100;
            byte[] buffer = new byte[1];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo((byte)value));
        }

        [Test]
        public void ToChar_ArraySegment_ReturnsCorrectValue()
        {
            // Arrange
            byte[] bytes = { 0x34, 0x12 }; // 'ሴ' (0x1234) in little-endian
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            char result = converter.ToChar(segment);

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
            var converter = EndianBitConverter.LittleEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0x34));
            Assert.That(buffer[1], Is.EqualTo(0x12));
        }

        #endregion

        #region Offset Tests

        [Test]
        public void ToInt16_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0x34, 0x12, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 2);
            var converter = EndianBitConverter.LittleEndian;

            // Act
            short result = converter.ToInt16(segment);

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
            var converter = EndianBitConverter.LittleEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0x34));
            Assert.That(buffer[2], Is.EqualTo(0x12));
            Assert.That(buffer[3], Is.EqualTo(0xFF));
            Assert.That(buffer[4], Is.EqualTo(0xFF));
        }

        [Test]
        public void BigEndian_ToInt16_WithOffset_ReadsFromCorrectPosition()
        {
            // Arrange
            byte[] bytes = { 0xFF, 0xFF, 0x12, 0x34, 0xFF };
            var segment = new ArraySegment<byte>(bytes, 2, 2);
            var converter = EndianBitConverter.BigEndian;

            // Act
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(0x1234));
        }

        [Test]
        public void BigEndian_GetBytes_Int16_WithOffset_WritesToCorrectPosition()
        {
            // Arrange
            short value = 0x1234;
            byte[] buffer = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            var segment = new ArraySegment<byte>(buffer, 1, 2);
            var converter = EndianBitConverter.BigEndian;

            // Act
            converter.GetBytes(value, segment);

            // Assert
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0x12));
            Assert.That(buffer[2], Is.EqualTo(0x34));
            Assert.That(buffer[3], Is.EqualTo(0xFF));
            Assert.That(buffer[4], Is.EqualTo(0xFF));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void ToInt16_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[1];
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            Assert.That(() => converter.ToInt16(segment),
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
            var converter = EndianBitConverter.LittleEndian;

            // Act & Assert
            Assert.That(() => converter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 2"));
        }

        [Test]
        public void BigEndian_ToInt32_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] bytes = new byte[3];
            var segment = new ArraySegment<byte>(bytes);
            var converter = EndianBitConverter.BigEndian;

            // Act & Assert
            Assert.That(() => converter.ToInt32(segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 4"));
        }

        [Test]
        public void BigEndian_GetBytes_Int64_InsufficientLength_ThrowsArgumentException()
        {
            // Arrange
            long value = 0x0123456789ABCDEF;
            byte[] buffer = new byte[7];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;

            // Act & Assert
            Assert.That(() => converter.GetBytes(value, segment),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains("ArraySegment length must be 8"));
        }

        #endregion

        #region GetBytes Returns Byte Array Tests

        [Test]
        public void GetBytes_Int16_ReturnsByteArray()
        {
            // Arrange
            short value = 0x1234;
            var converter = EndianBitConverter.LittleEndian;

            // Act
            byte[] bytes = converter.GetBytes(value);

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
            var converter = EndianBitConverter.LittleEndian;

            // Act
            byte[] bytes = converter.GetBytes(original);
            short result = converter.ToInt16(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void BigEndian_GetBytes_Int32_ReturnsByteArray()
        {
            // Arrange
            int value = 0x12345678;
            var converter = EndianBitConverter.BigEndian;

            // Act
            byte[] bytes = converter.GetBytes(value);

            // Assert
            Assert.That(bytes, Has.Length.EqualTo(4));
            Assert.That(bytes[0], Is.EqualTo(0x12));
            Assert.That(bytes[1], Is.EqualTo(0x34));
            Assert.That(bytes[2], Is.EqualTo(0x56));
            Assert.That(bytes[3], Is.EqualTo(0x78));
        }

        [Test]
        public void BigEndian_GetBytes_Int32_RoundTrip_ReturnsOriginalValue()
        {
            // Arrange
            int original = -123456789;
            var converter = EndianBitConverter.BigEndian;

            // Act
            byte[] bytes = converter.GetBytes(original);
            int result = converter.ToInt32(new ArraySegment<byte>(bytes));

            // Assert
            Assert.That(result, Is.EqualTo(original));
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Test]
        public void ToInt16_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            short minValue = short.MinValue;
            byte[] buffer = new byte[2];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.LittleEndian;
            converter.GetBytes(minValue, segment);

            // Act
            short result = converter.ToInt16(segment);

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
            var converter = EndianBitConverter.LittleEndian;
            converter.GetBytes(maxValue, segment);

            // Act
            short result = converter.ToInt16(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void BigEndian_ToUInt64_MinValue_ReturnsCorrectValue()
        {
            // Arrange
            ulong minValue = ulong.MinValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;
            converter.GetBytes(minValue, segment);

            // Act
            ulong result = converter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(minValue));
        }

        [Test]
        public void BigEndian_ToUInt64_MaxValue_ReturnsCorrectValue()
        {
            // Arrange
            ulong maxValue = ulong.MaxValue;
            byte[] buffer = new byte[8];
            var segment = new ArraySegment<byte>(buffer);
            var converter = EndianBitConverter.BigEndian;
            converter.GetBytes(maxValue, segment);

            // Act
            ulong result = converter.ToUInt64(segment);

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void GetBytes_Zero_WritesAllZeroBytes()
        {
            // Test for each type
            var converter = EndianBitConverter.LittleEndian;

            byte[] buffer16 = new byte[2];
            converter.GetBytes((short)0, new ArraySegment<byte>(buffer16));
            Assert.That(buffer16[0], Is.EqualTo(0));
            Assert.That(buffer16[1], Is.EqualTo(0));

            byte[] buffer32 = new byte[4];
            converter.GetBytes(0, new ArraySegment<byte>(buffer32));
            Assert.That(buffer32[0], Is.EqualTo(0));
            Assert.That(buffer32[1], Is.EqualTo(0));
            Assert.That(buffer32[2], Is.EqualTo(0));
            Assert.That(buffer32[3], Is.EqualTo(0));

            byte[] buffer64 = new byte[8];
            converter.GetBytes(0L, new ArraySegment<byte>(buffer64));
            Assert.That(buffer64[0], Is.EqualTo(0));
            Assert.That(buffer64[1], Is.EqualTo(0));
            Assert.That(buffer64[2], Is.EqualTo(0));
            Assert.That(buffer64[3], Is.EqualTo(0));
            Assert.That(buffer64[4], Is.EqualTo(0));
            Assert.That(buffer64[5], Is.EqualTo(0));
            Assert.That(buffer64[6], Is.EqualTo(0));
            Assert.That(buffer64[7], Is.EqualTo(0));
        }

        #endregion

        #region 端序 x 类型矩阵（补充 LittleEndian 覆盖）

        /// <summary>
        /// 同一数值在大小端下必须产出不同的字节序列，且能原样读回。
        /// 原测试只覆盖 BigEndian，这里补齐 LittleEndian。
        /// </summary>
        [TestCase(0x12345678, new byte[] { 0x12, 0x34, 0x56, 0x78 }, new byte[] { 0x78, 0x56, 0x34, 0x12 })]
        [TestCase(-1, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
        [TestCase(1, new byte[] { 0x00, 0x00, 0x00, 0x01 }, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
        public void Int32_Endianness_ProducesExpectedBytesAndRoundTrips(
            int value,
            byte[] bigEndian,
            byte[] littleEndian)
        {
            var buffer = new byte[4];

            EndianBitConverter.BigEndian.GetBytes(value, new ArraySegment<byte>(buffer));
            Assert.That(buffer, Is.EqualTo(bigEndian));

            EndianBitConverter.LittleEndian.GetBytes(value, new ArraySegment<byte>(buffer));
            Assert.That(buffer, Is.EqualTo(littleEndian));

            Assert.That(
                EndianBitConverter.BigEndian.ToInt32(new ArraySegment<byte>(bigEndian)),
                Is.EqualTo(value));
            Assert.That(
                EndianBitConverter.LittleEndian.ToInt32(new ArraySegment<byte>(littleEndian)),
                Is.EqualTo(value));
        }

        [Test]
        public void Single_RoundTrip_PreservesValue()
        {
            const float value = 3.1415927f;
            var buffer = new byte[4];

            foreach (var converter in new[] { EndianBitConverter.LittleEndian, EndianBitConverter.BigEndian })
            {
                converter.GetBytes(value, new ArraySegment<byte>(buffer));
                Assert.That(
                    converter.ToSingle(new ArraySegment<byte>(buffer)),
                    Is.EqualTo(value));
            }
        }

        [Test]
        public void Double_RoundTrip_PreservesValue()
        {
            const double value = -2.718281828459045;
            var buffer = new byte[8];

            foreach (var converter in new[] { EndianBitConverter.LittleEndian, EndianBitConverter.BigEndian })
            {
                converter.GetBytes(value, new ArraySegment<byte>(buffer));
                Assert.That(
                    converter.ToDouble(new ArraySegment<byte>(buffer)),
                    Is.EqualTo(value));
            }
        }

        #endregion

        #region 缓冲区校验的参数名测试

        [Test]
        public void ToInt32_BufferTooSmall_ReportsPublicParameterName()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => EndianBitConverter.LittleEndian.ToInt32(
                    new ArraySegment<byte>(new byte[2])));

            Assert.That(ex!.ParamName, Is.EqualTo("bytes"));
        }

        [Test]
        public void GetBytes_BufferTooSmall_ReportsPublicParameterName()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => EndianBitConverter.LittleEndian.GetBytes(
                    1,
                    new ArraySegment<byte>(new byte[2])));

            Assert.That(ex!.ParamName, Is.EqualTo("output"));
        }

        [Test]
        public void ToInt32_DefaultSegment_ReportsPublicParameterName()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => EndianBitConverter.LittleEndian.ToInt32(default));

            Assert.That(ex!.ParamName, Is.EqualTo("bytes"));
        }

        #endregion
    }
}
