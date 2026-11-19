using System;
using System.Runtime.CompilerServices;

namespace JiksLib
{
    /// <summary>
    /// 指定端序的 BitConverter
    /// </summary>
    public abstract class EndianBitConverter
    {
        #region Boolean

        public bool ToBoolean(byte b) => b != 0;

        public bool ToBoolean(ArraySegment<byte> bytes)
        {
            CheckBuffer(bytes, 1, "Boolean", nameof(bytes));
            return bytes.Array![bytes.Offset] != 0;
        }

        public byte GetByte(bool value) => value ? (byte)1 : (byte)0;

        public void GetBytes(bool value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "Boolean", nameof(output));
            output.Array![output.Offset] = value ? (byte)1 : (byte)0;
        }

        public byte[] GetBytes(bool value)
        {
            byte[] bytes = new byte[1];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region SByte

        public sbyte ToSByte(ArraySegment<byte> bytes)
        {
            CheckBuffer(bytes, 1, "SByte", nameof(bytes));
            return (sbyte)bytes.Array![bytes.Offset];
        }

        public void GetBytes(sbyte value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "SByte", nameof(output));
            output.Array![output.Offset] = (byte)value;
        }

        public byte[] GetBytes(sbyte value)
        {
            byte[] bytes = new byte[1];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion SByte

        #region Char

        public char ToChar(ArraySegment<byte> bytes) =>
            (char)ToUInt16(bytes);

        public void GetBytes(char value, ArraySegment<byte> output) =>
            GetBytes((ushort)value, output);

        public byte[] GetBytes(char value) =>
            GetBytes((ushort)value);

        #endregion

        #region Int16

        public abstract short ToInt16(ArraySegment<byte> bytes);
        public abstract void GetBytes(short value, ArraySegment<byte> output);
        public byte[] GetBytes(short value)
        {
            byte[] bytes = new byte[2];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt16

        public abstract ushort ToUInt16(ArraySegment<byte> bytes);
        public abstract void GetBytes(ushort value, ArraySegment<byte> output);
        public byte[] GetBytes(ushort value)
        {
            byte[] bytes = new byte[2];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Int32

        public abstract int ToInt32(ArraySegment<byte> bytes);
        public abstract void GetBytes(int value, ArraySegment<byte> output);
        public byte[] GetBytes(int value)
        {
            byte[] bytes = new byte[4];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt32
        public abstract uint ToUInt32(ArraySegment<byte> bytes);
        public abstract void GetBytes(uint value, ArraySegment<byte> output);
        public byte[] GetBytes(uint value)
        {
            byte[] bytes = new byte[4];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Int64

        public abstract long ToInt64(ArraySegment<byte> bytes);
        public abstract void GetBytes(long value, ArraySegment<byte> output);
        public byte[] GetBytes(long value)
        {
            byte[] bytes = new byte[8];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region UInt64

        public abstract ulong ToUInt64(ArraySegment<byte> bytes);
        public abstract void GetBytes(ulong value, ArraySegment<byte> output);
        public byte[] GetBytes(ulong value)
        {
            byte[] bytes = new byte[8];
            GetBytes(value, new ArraySegment<byte>(bytes));
            return bytes;
        }

        #endregion

        #region Single

        public float ToSingle(ArraySegment<byte> bytes) =>
            Int32BitsToSingle(ToInt32(bytes));

        public byte[] GetBytes(float value) =>
            GetBytes(SingleToInt32Bits(value));

        public void GetBytes(float value, ArraySegment<byte> output) =>
            GetBytes(SingleToInt32Bits(value), output);

        #endregion

        #region Double

        public double ToDouble(ArraySegment<byte> bytes) =>
            BitConverter.Int64BitsToDouble(ToInt64(bytes));

        public byte[] GetBytes(double value) =>
            GetBytes(BitConverter.DoubleToInt64Bits(value));

        public void GetBytes(double value, ArraySegment<byte> output) =>
            GetBytes(BitConverter.DoubleToInt64Bits(value), output);

        #endregion

        private sealed class LEBitConverter : EndianBitConverter
        {
            public override void GetBytes(short value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "Int16", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32", nameof(output));
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

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

            public override short ToInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "Int16", nameof(bytes));
                return (short)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32", nameof(bytes));
                return bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24);
            }

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

            public override ushort ToUInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "UInt16", nameof(bytes));
                return (ushort)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32", nameof(bytes));
                return (uint)(bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24));
            }

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
            public override void GetBytes(short value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "Int16", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32", nameof(output));
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

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

            public override short ToInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "Int16", nameof(bytes));
                return (short)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32", nameof(bytes));
                return (bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3];
            }

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

            public override ushort ToUInt16(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 2, "UInt16", nameof(bytes));
                return (ushort)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32", nameof(bytes));
                return (uint)((bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3]);
            }

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

        public static EndianBitConverter LittleEndian { get; private set; } =
            new LEBitConverter();

        public static EndianBitConverter BigEndian { get; private set; } =
            new BEBitConverter();

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
