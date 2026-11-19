using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using JiksLib.Extensions;

namespace JiksLib.IO
{
    /// <summary>
    /// 以指定端序读取数据的二进制流读取器
    /// </summary>
    public sealed class EndianBinaryReader : IDisposable
    {
        /// <summary>
        /// 基础流
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// 该流是否支持查找
        /// </summary>
        public bool CanSeek { get; private set; }

        /// <summary>
        /// <see cref="ReadString"/> 允许的最大字节长度，默认 64 MiB。
        /// 用于避免损坏或恶意的长度前缀导致超大分配。
        /// </summary>
        public int MaxStringLength { get; set; } = 64 * 1024 * 1024;

        /// <summary>
        /// 当前流位置
        /// 可以设置该属性以移动流位置
        /// 仅当 CanSeek = true 时可用
        /// </summary>
        public long Position
        {
            get
            {
                if (!CanSeek)
                    throw new NotSupportedException(
                        "The underlying stream does not support seeking.");

                return BaseStream.Position;
            }
            set
            {
                if (!CanSeek)
                    throw new NotSupportedException(
                        "The underlying stream does not support seeking.");

                BaseStream.Position = value;
            }
        }

        public EndianBinaryReader(
            EndianBitConverter endianBitConverter,
            Stream stream,
            Encoding encoding,
            bool leaveOpen = false)
        {
            if (!stream.ThrowIfNull(nameof(stream)).CanRead)
                throw new ArgumentException(
                    "The stream must be readable.", nameof(stream));

            BaseStream = stream;
            CanSeek = stream.CanSeek;
            endian = endianBitConverter.ThrowIfNull(nameof(endianBitConverter));
            this.encoding = encoding.ThrowIfNull(nameof(encoding));
            this.leaveOpen = leaveOpen;
        }

        public EndianBinaryReader(
            EndianBitConverter endianBitConverter,
            Stream stream,
            bool leaveOpen = false) :
            this(endianBitConverter, stream, Encoding.UTF8, leaveOpen)
        { }

        /// <summary>
        /// 关闭该读取器
        /// 若 leaveOpen 为 false，则同时关闭基础流
        /// </summary>
        public void Close()
        {
            if (!leaveOpen)
                BaseStream.Close();
        }

        /// <summary>
        /// 销毁该读取器
        /// 若 leaveOpen 为 false，则同时销毁基础流
        /// </summary>
        public void Dispose()
        {
            if (!leaveOpen)
                BaseStream.Dispose();
        }

        /// <summary>
        /// 读取一个字节，当到达末尾时返回 -1
        /// </summary>
        /// <returns></returns>
        public int ReadByte() =>
            BaseStream.ReadByte();

        /// <summary>
        /// 读取指定数量的字节，若未能读取足够数量的字节则抛出 EndOfStreamException
        /// 会循环读取直到读满给定片段，以兼容一次读取可能少于请求数量的流
        /// </summary>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public void ReadBytesExactly(ArraySegment<byte> buffer)
        {
            var bufferArray = buffer.Array.ThrowIfNull(nameof(buffer));
            var offset = buffer.Offset;
            var remaining = buffer.Count;

            while (remaining > 0)
            {
                var read = BaseStream.Read(bufferArray, offset, remaining);

                if (read <= 0)
                    throw new EndOfStreamException();

                offset += read;
                remaining -= read;
            }
        }

        /// <summary>
        /// 读取指定数量的字节，并返回实际读取的字节数
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int ReadBytes(ArraySegment<byte> buffer)
        {
            var bufferArray = buffer.Array.ThrowIfNull(nameof(buffer));

            return BaseStream.Read(bufferArray, buffer.Offset, buffer.Count);
        }


        /// <summary>
        /// 读取一个布尔值
        /// </summary>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>

        public bool ReadBoolean() =>
            ReadByte() switch
            {
                -1 => throw new EndOfStreamException(),
                var x => endian.ToBoolean((byte)x),
            };

        /// <summary>
        /// 读取一个有符号字节
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public sbyte ReadSByte() =>
            ReadByte() switch
            {
                -1 => throw new EndOfStreamException(),
                var b => (sbyte)b,
            };

        /// <summary>
        /// 读取一个字符
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public char ReadChar()
        {
            var buf = GetReadBuffer(2);
            ReadBytesExactly(buf);
            return endian.ToChar(buf);
        }

        /// <summary>
        /// 读取一个 Int16
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public short ReadInt16()
        {
            var buf = GetReadBuffer(2);
            ReadBytesExactly(buf);
            return endian.ToInt16(buf);
        }

        /// <summary>
        /// 读取一个 UInt16
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public ushort ReadUInt16()
        {
            var buf = GetReadBuffer(2);
            ReadBytesExactly(buf);
            return endian.ToUInt16(buf);
        }

        /// <summary>
        /// 读取一个 Int32
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public int ReadInt32()
        {
            var buf = GetReadBuffer(4);
            ReadBytesExactly(buf);
            return endian.ToInt32(buf);
        }

        /// <summary>
        /// 读取一个 UInt32
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public uint ReadUInt32()
        {
            var buf = GetReadBuffer(4);
            ReadBytesExactly(buf);
            return endian.ToUInt32(buf);
        }

        /// <summary>
        /// 读取一个 Int64
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public long ReadInt64()
        {
            var buf = GetReadBuffer(8);
            ReadBytesExactly(buf);
            return endian.ToInt64(buf);
        }

        /// <summary>
        /// 读取一个 UInt64
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public ulong ReadUInt64()
        {
            var buf = GetReadBuffer(8);
            ReadBytesExactly(buf);
            return endian.ToUInt64(buf);
        }

        /// <summary>
        /// 读取一个单精度浮点数
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public float ReadSingle()
        {
            var buf = GetReadBuffer(4);
            ReadBytesExactly(buf);
            return endian.ToSingle(buf);
        }

        /// <summary>
        /// 读取一个双精度浮点数
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public double ReadDouble()
        {
            var buf = GetReadBuffer(8);
            ReadBytesExactly(buf);
            return endian.ToDouble(buf);
        }

        /// <summary>
        /// 以指定编码读取一个字符串，字符串的字节长度由前置的 Int32 指定
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        /// <exception cref="InvalidDataException">
        /// 字符串长度为负数，或超过 <see cref="MaxStringLength"/> 时抛出
        /// </exception>
        public string ReadString()
        {
            int length = ReadInt32();

            if (length < 0)
                throw new InvalidDataException($"String length cannot be negative: {length}");

            if (length > MaxStringLength)
                throw new InvalidDataException(
                    $"String length {length} exceeds the maximum allowed length {MaxStringLength}.");

            if (length == 0)
                return string.Empty;

            // 可 seek 时先确认剩余长度，避免为截断的流分配大缓冲区
            if (CanSeek && length > BaseStream.Length - BaseStream.Position)
                throw new EndOfStreamException();

            // 大块字符串使用临时缓冲区，避免把实例的读缓冲区永久撑大
            if (length > MaxCachedStringBuffer)
            {
                var largeBuffer = new byte[length];
                ReadBytesExactly(new ArraySegment<byte>(largeBuffer));
                return encoding.GetString(largeBuffer, 0, length);
            }

            var buf = GetReadBuffer(length);
            ReadBytesExactly(buf);
            return encoding.GetString(buf.Array!, buf.Offset, buf.Count);
        }

        readonly EndianBitConverter endian;
        readonly Encoding encoding;
        readonly bool leaveOpen;
        byte[]? readBuffer = null;

        /// <summary>
        /// 读缓冲区缓存的长度上限；超过该长度的字符串改用临时缓冲区，读后即可回收。
        /// </summary>
        const int MaxCachedStringBuffer = 4096;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ArraySegment<byte> GetReadBuffer(int size)
        {
            if (readBuffer == null || readBuffer.Length < size)
                readBuffer = new byte[Math.Max(size, 8)];

            ArraySegment<byte> b = new(readBuffer, 0, size);
            return b;
        }
    }
}
