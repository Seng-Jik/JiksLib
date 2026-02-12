using System;

namespace JiksLib
{
    /// <summary>
    /// 无论在什么样的机器上运行，均提供以小端字节序进行转换的BitConverter
    /// </summary>
    public static class LEBitConverter
    {
        public static bool ToBoolean(byte b) => b != 0;

        public static bool ToBoolean(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 1)
                throw new ArgumentException(
                    "ArraySegment length must be 1 for Boolean conversion.");

            return bytes.Array![bytes.Offset] != 0;
        }

        public static byte GetByte(bool value) => value ? (byte)1 : (byte)0;

        public static void GetBytes(bool value, ArraySegment<byte> output)
        {
            if (output.Count != 1)
                throw new ArgumentException(
                    "ArraySegment length must be 1 for Boolean conversion.");

            output.Array![output.Offset] = value ? (byte)1 : (byte)0;
        }

        public static char ToChar(ArraySegment<byte> bytes) =>
            (char)ToUInt16(bytes);

        public static void GetBytes(char value, ArraySegment<byte> output) =>
            GetBytes((ushort)value, output);

        public static byte[] GetBytes(char value) =>
            NormalGetBytes<ushort>(2, value, GetBytes);

        public static byte[] GetBytes(bool value) =>
            NormalGetBytes(1, value, GetBytes);

        public static short ToInt16(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for Int16 conversion.");

            return (short)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
        }

        public static void GetBytes(short value, ArraySegment<byte> output)
        {
            if (output.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for Int16 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        public static byte[] GetBytes(short value) =>
            NormalGetBytes(2, value, GetBytes);

        public static ushort ToUInt16(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for UInt16 or Char conversion.");

            return (ushort)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
        }

        public static void GetBytes(ushort value, ArraySegment<byte> output)
        {
            if (output.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for UInt16 or Char conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        public static byte[] GetBytes(ushort value) =>
            NormalGetBytes(2, value, GetBytes);

        public static int ToInt32(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 4)
                throw new ArgumentException(
                    "ArraySegment length must be 4 for Int32 conversion.");

            return bytes.Array![bytes.Offset] |
                   (bytes.Array[bytes.Offset + 1] << 8) |
                   (bytes.Array[bytes.Offset + 2] << 16) |
                   (bytes.Array[bytes.Offset + 3] << 24);
        }

        public static void GetBytes(int value, ArraySegment<byte> output)
        {
            if (output.Count != 4)
                throw new ArgumentException(
                    "ArraySegment length must be 4 for Int32 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
            output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        public static byte[] GetBytes(int value) =>
            NormalGetBytes(4, value, GetBytes);

        public static uint ToUInt32(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 4)
                throw new ArgumentException(
                    "ArraySegment length must be 4 for UInt32 conversion.");

            return (uint)(bytes.Array![bytes.Offset] |
                   (bytes.Array[bytes.Offset + 1] << 8) |
                   (bytes.Array[bytes.Offset + 2] << 16) |
                   (bytes.Array[bytes.Offset + 3] << 24));
        }

        public static void GetBytes(uint value, ArraySegment<byte> output)
        {
            if (output.Count != 4)
                throw new ArgumentException(
                    "ArraySegment length must be 4 for UInt32 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
            output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        public static byte[] GetBytes(uint value) =>
            NormalGetBytes(4, value, GetBytes);

        public static long ToInt64(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 8)
                throw new ArgumentException(
                    "ArraySegment length must be 8 for Int64 conversion.");

            return bytes.Array![bytes.Offset] |
                    ((long)bytes.Array[bytes.Offset + 1] << 8) |
                     ((long)bytes.Array[bytes.Offset + 2] << 16) |
                     ((long)bytes.Array[bytes.Offset + 3] << 24) |
                     ((long)bytes.Array[bytes.Offset + 4] << 32) |
                     ((long)bytes.Array[bytes.Offset + 5] << 40) |
                     ((long)bytes.Array[bytes.Offset + 6] << 48) |
                     ((long)bytes.Array[bytes.Offset + 7] << 56);
        }

        public static void GetBytes(long value, ArraySegment<byte> output)
        {
            if (output.Count != 8)
                throw new ArgumentException(
                    "ArraySegment length must be 8 for Int64 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
            output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            output.Array[output.Offset + 4] = (byte)((value >> 32) & 0xFF);
            output.Array[output.Offset + 5] = (byte)((value >> 40) & 0xFF);
            output.Array[output.Offset + 6] = (byte)((value >> 48) & 0xFF);
            output.Array[output.Offset + 7] = (byte)((value >> 56) & 0xFF);
        }

        public static byte[] GetBytes(long value) =>
            NormalGetBytes(8, value, GetBytes);

        public static ulong ToUInt64(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 8)
                throw new ArgumentException(
                    "ArraySegment length must be 8 for UInt64 conversion.");

            return bytes.Array![bytes.Offset] |
                    ((ulong)bytes.Array[bytes.Offset + 1] << 8) |
                     ((ulong)bytes.Array[bytes.Offset + 2] << 16) |
                     ((ulong)bytes.Array[bytes.Offset + 3] << 24) |
                     ((ulong)bytes.Array[bytes.Offset + 4] << 32) |
                     ((ulong)bytes.Array[bytes.Offset + 5] << 40) |
                     ((ulong)bytes.Array[bytes.Offset + 6] << 48) |
                     ((ulong)bytes.Array[bytes.Offset + 7] << 56);
        }

        public static void GetBytes(ulong value, ArraySegment<byte> output)
        {
            if (output.Count != 8)
                throw new ArgumentException(
                    "ArraySegment length must be 8 for UInt64 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
            output.Array[output.Offset + 2] = (byte)((value >> 16) & 0xFF);
            output.Array[output.Offset + 3] = (byte)((value >> 24) & 0xFF);
            output.Array[output.Offset + 4] = (byte)((value >> 32) & 0xFF);
            output.Array[output.Offset + 5] = (byte)((value >> 40) & 0xFF);
            output.Array[output.Offset + 6] = (byte)((value >> 48) & 0xFF);
            output.Array[output.Offset + 7] = (byte)((value >> 56) & 0xFF);
        }

        public static byte[] GetBytes(ulong value) =>
            NormalGetBytes(8, value, GetBytes);

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
    }
}
