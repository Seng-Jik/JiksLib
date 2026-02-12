using System;

namespace JiksLib
{
    public static class LEBitConverter
    {
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

        public static ushort ToUInt16(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for UInt16 conversion.");

            return (ushort)(bytes.Array![bytes.Offset] | (bytes.Array[bytes.Offset + 1] << 8));
        }


        public static void GetBytes(ushort value, ArraySegment<byte> output)
        {
            if (output.Count != 2)
                throw new ArgumentException(
                    "ArraySegment length must be 2 for UInt16 conversion.");

            output.Array![output.Offset] = (byte)(value & 0xFF);
            output.Array[output.Offset + 1] = (byte)((value >> 8) & 0xFF);
        }
        public static int ToInt32(ArraySegment<byte> bytes)
        {
            if (bytes.Count != 4)
                throw new ArgumentException(
                    "ArraySegment length must be 4 for Int32 conversion.");

            return (ushort)(bytes.Array![bytes.Offset] |
                   (bytes.Array[bytes.Offset + 1] << 8) |
                   (bytes.Array[bytes.Offset + 2] << 16) |
                   (bytes.Array[bytes.Offset + 3] << 24));
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
    }
}
