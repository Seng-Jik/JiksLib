using System;
using System.Runtime.CompilerServices;

namespace JiksLib
{
    /// <summary>
    /// 指定端序的 BitConverter
    /// </summary>
    /// <remarks>
    /// 所有接收 <see cref="ArraySegment{T}"/> 的读写方法都要求片段长度不小于目标类型的大小，
    /// 否则抛出 <see cref="ArgumentException"/>；<c>ArraySegment.Array</c> 为 null 时抛出
    /// <see cref="ArgumentNullException"/>。返回数组的重载会比写入片段的重载多分配一个数组。
    /// </remarks>
    public abstract class EndianBitConverter
    {
        #region Boolean

        /// <summary>
        /// 把单字节解释为布尔值（0 为 false，非 0 为 true）
        /// </summary>
        /// <param name="b">要解释的字节</param>
        /// <returns>解释后的布尔值</returns>
        public bool ToBoolean(byte b) => b != 0;

        /// <summary>
        /// 从 1 字节读取布尔值（0 为 false，非 0 为 true）
        /// </summary>
        /// <param name="bytes">包含 1 字节的缓冲区</param>
        /// <returns>读取到的布尔值</returns>
        public bool ToBoolean(ArraySegment<byte> bytes)
        {
            CheckBuffer(bytes, 1, "Boolean", nameof(bytes));
            return bytes.Array![bytes.Offset] != 0;
        }

        /// <summary>
        /// 把布尔值转为 0/1 字节
        /// </summary>
        /// <param name="value">要转换的布尔值</param>
        /// <returns>true 为 1，false 为 0</returns>
        public byte GetByte(bool value) => value ? (byte)1 : (byte)0;

        /// <summary>
        /// 把布尔值写入 1 字节（true 为 1，false 为 0）
        /// </summary>
        /// <param name="value">要写入的布尔值</param>
        /// <param name="output">目标缓冲区，长度至少 1 字节</param>
        public void GetBytes(bool value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "Boolean", nameof(output));
            output.Array![output.Offset] = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// 返回按实例端序编码的表示布尔值的 1 字节数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 1 的字节数组</returns>
        public byte[] GetBytes(bool value)
        {
            byte[] bytes = new byte[1];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region SByte

        /// <summary>
        /// 读取 1 字节有符号整数
        /// </summary>
        /// <param name="bytes">包含 1 字节的缓冲区</param>
        /// <returns>读取到的有符号字节</returns>
        public sbyte ToSByte(ArraySegment<byte> bytes)
        {
            CheckBuffer(bytes, 1, "SByte", nameof(bytes));
            return (sbyte)bytes.Array![bytes.Offset];
        }

        /// <summary>
        /// 写入 1 字节有符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 1 字节</param>
        public void GetBytes(sbyte value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "SByte", nameof(output));
            output.Array![output.Offset] = (byte)value;
        }

        /// <summary>
        /// 返回按实例端序编码的1 字节有符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 1 的字节数组</returns>
        public byte[] GetBytes(sbyte value)
        {
            byte[] bytes = new byte[1];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion SByte

        #region Char

        /// <summary>
        /// 按实例端序读取 2 字节 UTF-16 字符
        /// </summary>
        /// <param name="bytes">包含 2 字节的缓冲区</param>
        /// <returns>读取到的字符</returns>
        public char ToChar(ArraySegment<byte> bytes) =>
            (char)ToUInt16(bytes);

        /// <summary>
        /// 按实例端序写入 2 字节 UTF-16 字符
        /// </summary>
        /// <param name="value">要写入的字符</param>
        /// <param name="output">目标缓冲区，长度至少 2 字节</param>
        public void GetBytes(char value, ArraySegment<byte> output) =>
            GetBytes((ushort)value, output);

        /// <summary>
        /// 返回按实例端序编码的 2 字节 UTF-16 字符数组
        /// </summary>
        /// <param name="value">要转换的字符</param>
        /// <returns>长度为 2 的字节数组</returns>
        public byte[] GetBytes(char value) =>
            GetBytes((ushort)value);

        #endregion

        #region Int16

        /// <summary>
        /// 按实例端序读取 2 字节有符号整数
        /// </summary>
        /// <param name="bytes">包含 2 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract short ToInt16(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 2 字节有符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 2 字节</param>
        public abstract void GetBytes(short value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的2 字节有符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 2 的字节数组</returns>
        public byte[] GetBytes(short value)
        {
            byte[] bytes = new byte[2];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt16

        /// <summary>
        /// 按实例端序读取 2 字节无符号整数
        /// </summary>
        /// <param name="bytes">包含 2 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract ushort ToUInt16(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 2 字节无符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 2 字节</param>
        public abstract void GetBytes(ushort value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的2 字节无符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 2 的字节数组</returns>
        public byte[] GetBytes(ushort value)
        {
            byte[] bytes = new byte[2];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Int32

        /// <summary>
        /// 按实例端序读取 4 字节有符号整数
        /// </summary>
        /// <param name="bytes">包含 4 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract int ToInt32(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 4 字节有符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 4 字节</param>
        public abstract void GetBytes(int value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的4 字节有符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 4 的字节数组</returns>
        public byte[] GetBytes(int value)
        {
            byte[] bytes = new byte[4];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt32
        /// <summary>
        /// 按实例端序读取 4 字节无符号整数
        /// </summary>
        /// <param name="bytes">包含 4 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract uint ToUInt32(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 4 字节无符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 4 字节</param>
        public abstract void GetBytes(uint value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的4 字节无符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 4 的字节数组</returns>
        public byte[] GetBytes(uint value)
        {
            byte[] bytes = new byte[4];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Int64

        /// <summary>
        /// 按实例端序读取 8 字节有符号整数
        /// </summary>
        /// <param name="bytes">包含 8 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract long ToInt64(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 8 字节有符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 8 字节</param>
        public abstract void GetBytes(long value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的8 字节有符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 8 的字节数组</returns>
        public byte[] GetBytes(long value)
        {
            byte[] bytes = new byte[8];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt64

        /// <summary>
        /// 按实例端序读取 8 字节无符号整数
        /// </summary>
        /// <param name="bytes">包含 8 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public abstract ulong ToUInt64(ArraySegment<byte> bytes);
        /// <summary>
        /// 按实例端序写入 8 字节无符号整数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 8 字节</param>
        public abstract void GetBytes(ulong value, ArraySegment<byte> output);
        /// <summary>
        /// 返回按实例端序编码的8 字节无符号整数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 8 的字节数组</returns>
        public byte[] GetBytes(ulong value)
        {
            byte[] bytes = new byte[8];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Single

        /// <summary>
        /// 按实例端序读取 4 字节 IEEE 754 单精度浮点数
        /// </summary>
        /// <param name="bytes">包含 4 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public float ToSingle(ArraySegment<byte> bytes) =>
            Int32BitsToSingle(ToInt32(bytes));

        /// <summary>
        /// 返回按实例端序编码的 4 字节 IEEE 754 单精度浮点数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 4 的字节数组</returns>
        public byte[] GetBytes(float value) =>
            GetBytes(SingleToInt32Bits(value));

        /// <summary>
        /// 按实例端序写入 4 字节 IEEE 754 单精度浮点数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 4 字节</param>
        public void GetBytes(float value, ArraySegment<byte> output) =>
            GetBytes(SingleToInt32Bits(value), output);

        #endregion

        #region Double

        /// <summary>
        /// 按实例端序读取 8 字节 IEEE 754 双精度浮点数
        /// </summary>
        /// <param name="bytes">包含 8 字节的缓冲区</param>
        /// <returns>读取到的值</returns>
        public double ToDouble(ArraySegment<byte> bytes) =>
            BitConverter.Int64BitsToDouble(ToInt64(bytes));

        /// <summary>
        /// 返回按实例端序编码的 8 字节 IEEE 754 双精度浮点数数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>长度为 8 的字节数组</returns>
        public byte[] GetBytes(double value) =>
            GetBytes(BitConverter.DoubleToInt64Bits(value));

        /// <summary>
        /// 按实例端序写入 8 字节 IEEE 754 双精度浮点数
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="output">目标缓冲区，长度至少 8 字节</param>
        public void GetBytes(double value, ArraySegment<byte> output) =>
            GetBytes(BitConverter.DoubleToInt64Bits(value), output);

        #endregion

        private sealed class LEBitConverter : EndianBitConverter
        {
            /// <inheritdoc/>
            public override void GetBytes(short value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "Int16", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(long value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "Int64", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 4] = (byte)((value >> 32) & 0xFF);
                output.Array[output.Offset + 5] = (byte)((value >> 40) & 0xFF);
                output.Array[output.Offset + 6] = (byte)((value >> 48) & 0xFF);
                output.Array[output.Offset + 7] = (byte)((value >> 56) & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(ulong value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "UInt64", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 4] = (byte)((value >> 32) & 0xFF);
                output.Array[output.Offset + 5] = (byte)((value >> 40) & 0xFF);
                output.Array[output.Offset + 6] = (byte)((value >> 48) & 0xFF);
                output.Array[output.Offset + 7] = (byte)((value >> 56) & 0xFF);
            }

            /// <inheritdoc/>
            public override short ToInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "Int16", nameof(bytes));
                return (short)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            /// <inheritdoc/>
            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32", nameof(bytes));
                return bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24);
            }

            /// <inheritdoc/>
            public override long ToInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "Int64", nameof(bytes));
                return bytes.Array![bytes.Offset] |
                        ((long)bytes.Array[bytes.Offset + 1] << 8) |
                        ((long)bytes.Array[bytes.Offset + 2] << 16) |
                        ((long)bytes.Array[bytes.Offset + 3] << 24) |
                        ((long)bytes.Array[bytes.Offset + 4] << 32) |
                        ((long)bytes.Array[bytes.Offset + 5] << 40) |
                        ((long)bytes.Array[bytes.Offset + 6] << 48) |
                        ((long)bytes.Array[bytes.Offset + 7] << 56);
            }

            /// <inheritdoc/>
            public override ushort ToUInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "UInt16", nameof(bytes));
                return (ushort)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            /// <inheritdoc/>
            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32", nameof(bytes));
                return (uint)(bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24));
            }

            /// <inheritdoc/>
            public override ulong ToUInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "UInt64", nameof(bytes));
                return bytes.Array![bytes.Offset] |
                        ((ulong)bytes.Array[bytes.Offset + 1] << 8) |
                        ((ulong)bytes.Array[bytes.Offset + 2] << 16) |
                        ((ulong)bytes.Array[bytes.Offset + 3] << 24) |
                        ((ulong)bytes.Array[bytes.Offset + 4] << 32) |
                        ((ulong)bytes.Array[bytes.Offset + 5] << 40) |
                        ((ulong)bytes.Array[bytes.Offset + 6] << 48) |
                        ((ulong)bytes.Array[bytes.Offset + 7] << 56);
            }
        }

        private sealed class BEBitConverter : EndianBitConverter
        {
            /// <inheritdoc/>
            public override void GetBytes(short value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "Int16", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(long value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "Int64", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 56) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 48) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 40) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 32) & 0xFF);
                output.Array[output.Offset + 4] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 5] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 6] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 7] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override void GetBytes(ulong value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "UInt64", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 56) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 48) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 40) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 32) & 0xFF);
                output.Array[output.Offset + 4] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 5] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 6] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 7] = (byte)(value & 0xFF);
            }

            /// <inheritdoc/>
            public override short ToInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "Int16", nameof(bytes));
                return (short)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            /// <inheritdoc/>
            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32", nameof(bytes));
                return (bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3];
            }

            /// <inheritdoc/>
            public override long ToInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "Int64", nameof(bytes));
                return ((long)bytes.Array![bytes.Offset] << 56) |
                       ((long)bytes.Array[bytes.Offset + 1] << 48) |
                       ((long)bytes.Array[bytes.Offset + 2] << 40) |
                       ((long)bytes.Array[bytes.Offset + 3] << 32) |
                       ((long)bytes.Array[bytes.Offset + 4] << 24) |
                       ((long)bytes.Array[bytes.Offset + 5] << 16) |
                       ((long)bytes.Array[bytes.Offset + 6] << 8) |
                       bytes.Array[bytes.Offset + 7];
            }

            /// <inheritdoc/>
            public override ushort ToUInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "UInt16", nameof(bytes));
                return (ushort)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            /// <inheritdoc/>
            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32", nameof(bytes));
                return (uint)((bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3]);
            }

            /// <inheritdoc/>
            public override ulong ToUInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "UInt64", nameof(bytes));
                return ((ulong)bytes.Array![bytes.Offset] << 56) |
                       ((ulong)bytes.Array[bytes.Offset + 1] << 48) |
                       ((ulong)bytes.Array[bytes.Offset + 2] << 40) |
                       ((ulong)bytes.Array[bytes.Offset + 3] << 32) |
                       ((ulong)bytes.Array[bytes.Offset + 4] << 24) |
                       ((ulong)bytes.Array[bytes.Offset + 5] << 16) |
                       ((ulong)bytes.Array[bytes.Offset + 6] << 8) |
                       bytes.Array[bytes.Offset + 7];
            }
        }

        /// <summary>
        /// 小端转换器实例
        /// </summary>
        public static EndianBitConverter LittleEndian { get; private set; } =
            new LEBitConverter();

        /// <summary>
        /// 大端转换器实例
        /// </summary>
        public static EndianBitConverter BigEndian { get; private set; } =
            new BEBitConverter();

        /// <summary>
        /// 当前系统端序对应的转换器（<see cref="LittleEndian"/> 或 <see cref="BigEndian"/>）
        /// </summary>
        public static EndianBitConverter SystemEndian { get; private set; } =
            BitConverter.IsLittleEndian ? LittleEndian : BigEndian;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckBuffer(
            ArraySegment<byte> buf,
            int size,
            string typeName,
            string paramName)
        {
            if (buf.Array == null)
                throw new ArgumentNullException(
                    paramName,
                    "ArraySegment.Array must not be null.");

            if (buf.Count < size)
                throw new ArgumentException(
                    $"ArraySegment length must be {size} for {typeName} conversion, " +
                    $"but was {buf.Count}.",
                    paramName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Int32BitsToSingle(int bits)
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            return BitConverter.Int32BitsToSingle(bits);
#else
            // net48 等旧目标框架没有位模式转换 API，
            // 先得到本机端序的字节，再交给 BitConverter 按本机端序解释。
            return BitConverter.ToSingle(SystemEndian.GetBytes(bits), 0);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SingleToInt32Bits(float value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            return BitConverter.SingleToInt32Bits(value);
#else
            // net48 等旧目标框架没有位模式转换 API，
            // 先得到本机端序的字节，再按本机端序读回位模式。
            return SystemEndian.ToInt32(
                new ArraySegment<byte>(BitConverter.GetBytes(value)));
#endif
        }

        internal EndianBitConverter() { }
    }
}
