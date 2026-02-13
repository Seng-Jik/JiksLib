using System;
using System.IO;
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
            if (!stream.ThrowIfNull().CanRead)
                throw new ArgumentException(
                    "The stream must be readable.", nameof(stream));

            BaseStream = stream;
            CanSeek = stream.CanSeek;
            endian = endianBitConverter.ThrowIfNull();
            this.encoding = encoding.ThrowIfNull();
            this.leaveOpen = leaveOpen;
        }

        public EndianBinaryReader(
            EndianBitConverter endianBitConverter,
            Stream stream,
            bool leaveOpen = false) :
            this(endianBitConverter, stream, Encoding.UTF8, leaveOpen)
        { }

        /// <summary>
        /// 关闭该读取器及基础流
        /// </summary>
        public void Close()
        {
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
        /// </summary>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public void ReadBytesExactly(ArraySegment<byte> buffer)
        {
            var read = ReadBytes(buffer);

            if (read < buffer.Count)
                throw new EndOfStreamException();
        }

        /// <summary>
        /// 读取指定数量的字节，并返回实际读取的字节数
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int ReadBytes(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
                throw new ArgumentException("Buffer array cannot be null.", nameof(buffer));

            return BaseStream.Read(buffer.Array, buffer.Offset, buffer.Count);
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
        public char ReadChar() => ReadInt(endian.ToChar, 2);

        /// <summary>
        /// 读取一个 Int16
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public short ReadInt16() => ReadInt(endian.ToInt16, 2);

        /// <summary>
        /// 读取一个 UInt16
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public ushort ReadUInt16() => ReadInt(endian.ToUInt16, 2);

        /// <summary>
        /// 读取一个 Int32
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public int ReadInt32() => ReadInt(endian.ToInt32, 4);

        /// <summary>
        /// 读取一个 UInt32
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public uint ReadUInt32() => ReadInt(endian.ToUInt32, 4);

        /// <summary>
        /// 读取一个 Int64
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public long ReadInt64() => ReadInt(endian.ToInt64, 8);

        /// <summary>
        /// 读取一个 UInt64
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        public ulong ReadUInt64() => ReadInt(endian.ToUInt64, 8);

        /// <summary>
        /// 以指定编码读取一个字符串，字符串的字节长度由前置的 Int32 指定
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EndOfStreamException">未读取到足够数量的字节时抛出</exception>
        /// <exception cref="InvalidDataException">字符串长度为负数时抛出</exception>
        public string ReadString()
        {
            int length = ReadInt32();

            if (length < 0)
                throw new InvalidDataException($"String length cannot be negative: {length}");

            if (length == 0)
                return string.Empty;

            var buf = GetReadBuffer(length);
            ReadBytesExactly(buf);
            return encoding.GetString(buf.Array!, buf.Offset, buf.Count);
        }

        T ReadInt<T>(Func<ArraySegment<byte>, T> converter, int size)
        {
            var buf = GetReadBuffer(size);
            ReadBytesExactly(buf);
            return converter(buf);
        }

        readonly EndianBitConverter endian;
        readonly Encoding encoding;
        readonly bool leaveOpen;
        byte[]? readBuffer = null;

        ArraySegment<byte> GetReadBuffer(int size)
        {
            if (readBuffer == null || readBuffer.Length < size)
                readBuffer = new byte[Math.Max(size, 8)];

            ArraySegment<byte> b = new(readBuffer, 0, size);
            return b;
        }
    }
}