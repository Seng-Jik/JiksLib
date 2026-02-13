using System;

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
            CheckBuffer(bytes, 1, "Boolean");
            return bytes.Array![bytes.Offset] != 0;
        }

        public byte GetByte(bool value) => value ? (byte)1 : (byte)0;

        public void GetBytes(bool value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "Boolean");
            output.Array![output.Offset] = value ? (byte)1 : (byte)0;
        }

        public byte[] GetBytes(bool value) =>
            NormalGetBytes(1, value, GetBytes);

        #endregion

        #region SByte

        public sbyte ToSByte(ArraySegment<byte> bytes)
        {
            CheckBuffer(bytes, 1, "SByte");
            return (sbyte)bytes.Array![bytes.Offset];
        }

        public void GetBytes(sbyte value, ArraySegment<byte> output)
        {
            CheckBuffer(output, 1, "SByte");
            output.Array![output.Offset] = (byte)value;
        }

        public byte[] GetBytes(sbyte value) =>
            NormalGetBytes(1, value, GetBytes);

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
        public byte[] GetBytes(short value) => NormalGetBytes(2, value, GetBytes);

        #endregion

        #region UInt16

        public abstract ushort ToUInt16(ArraySegment<byte> bytes);
        public abstract void GetBytes(ushort value, ArraySegment<byte> output);
        public byte[] GetBytes(ushort value) => NormalGetBytes(2, value, GetBytes);

        #endregion

        #region Int32

        public abstract int ToInt32(ArraySegment<byte> bytes);
        public abstract void GetBytes(int value, ArraySegment<byte> output);
        public byte[] GetBytes(int value) => NormalGetBytes(4, value, GetBytes);

        #endregion

        #region UInt32
        public abstract uint ToUInt32(ArraySegment<byte> bytes);
        public abstract void GetBytes(uint value, ArraySegment<byte> output);
        public byte[] GetBytes(uint value) => NormalGetBytes(4, value, GetBytes);

        #endregion

        #region Int64

        public abstract long ToInt64(ArraySegment<byte> bytes);
        public abstract void GetBytes(long value, ArraySegment<byte> output);
        public byte[] GetBytes(long value) => NormalGetBytes(8, value, GetBytes);

        #endregion

        #region UInt64

        public abstract ulong ToUInt64(ArraySegment<byte> bytes);
        public abstract void GetBytes(ulong value, ArraySegment<byte> output);
        public byte[] GetBytes(ulong value) => NormalGetBytes(8, value, GetBytes);

        #endregion

        private sealed class LEBitConverter : EndianBitConverter
        {
            public override void GetBytes(short value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "Int16");
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16");
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32");
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32");
                output.Array![output.Offset] = (byte)(value & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            public override void GetBytes(long value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "Int64");
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
                CheckBuffer(output, 8, "UInt64");
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
                CheckBuffer(bytes, 2, "Int16");
                return (short)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32");
                return bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24);
            }

            public override long ToInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "Int64");
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
                CheckBuffer(bytes, 2, "UInt16");
                return (ushort)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
            }

            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32");
                return (uint)(bytes.Array![bytes.Offset] |
                        (bytes.Array[bytes.Offset + 1] << 8) |
                        (bytes.Array[bytes.Offset + 2] << 16) |
                        (bytes.Array[bytes.Offset + 3] << 24));
            }

            public override ulong ToUInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "UInt64");
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
                CheckBuffer(output, 2, "Int16");
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            public override void GetBytes(ushort value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 2, "UInt16");
                output.Array![output.Offset] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 1] = (byte)(value & 0xFF);
            }

            public override void GetBytes(int value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "Int32");
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

            public override void GetBytes(uint value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 4, "UInt32");
                output.Array![output.Offset] = (byte)((value >> 24) & 0xFF);
                output.Array[output.Offset + 1] = (byte)((value >> 16) & 0xFF);
                output.Array[output.Offset + 2] = (byte)((value >> 8) & 0xFF);
                output.Array[output.Offset + 3] = (byte)(value & 0xFF);
            }

            public override void GetBytes(long value, ArraySegment<byte> output)
            {
                CheckBuffer(output, 8, "Int64");
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
                CheckBuffer(output, 8, "UInt64");
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
                CheckBuffer(bytes, 2, "Int16");
                return (short)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            public override int ToInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "Int32");
                return (bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3];
            }

            public override long ToInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "Int64");
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
                CheckBuffer(bytes, 2, "UInt16");
                return (ushort)((bytes.Array![bytes.Offset] << 8) | bytes.Array[bytes.Offset + 1]);
            }

            public override uint ToUInt32(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 4, "UInt32");
                return (uint)((bytes.Array![bytes.Offset] << 24) |
                       (bytes.Array[bytes.Offset + 1] << 16) |
                       (bytes.Array[bytes.Offset + 2] << 8) |
                       bytes.Array[bytes.Offset + 3]);
            }

            public override ulong ToUInt64(ArraySegment<byte> bytes)
            {
                CheckBuffer(bytes, 8, "UInt64");
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

        static void CheckBuffer(
            ArraySegment<byte> buf,
            int size,
            string typeName)
        {
            if (buf.Array == null)
                throw new ArgumentException(
                    $"ArraySegment.Array must not be null for {typeName} conversion.");
            if (buf.Count != size)
                throw new ArgumentException(
                    $"ArraySegment length must be {size} for {typeName} conversion.");
        }

        static byte[] NormalGetBytes<T>(
            int size,
            T arg,
            Action<T, ArraySegment<byte>> originalFunc)
        {
            byte[] bytes = new byte[size];
            ArraySegment<byte> segment = new(bytes, 0, size);
            originalFunc(arg, segment);
            return bytes;
        }

        internal EndianBitConverter() { }
    }
}