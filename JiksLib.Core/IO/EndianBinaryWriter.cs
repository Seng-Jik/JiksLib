using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JiksLib.Extensions;

namespace JiksLib.IO
{
    /// <summary>
    /// 以指定端序读取数据的二进制流读取器
    /// </summary>
    public sealed class EndianBinaryWriter : IDisposable
    {
        /// <summary>
        /// 基本流
        /// </summary>
        public Stream BaseStream { get; private set; }

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

        /// <summary>
        /// 该流是否支持查找
        /// </summary>
        public bool CanSeek { get; private set; }

        public EndianBinaryWriter(
            EndianBitConverter endianBitConverter,
            Stream stream,
            Encoding encoding,
            bool leaveOpen = false)
        {
            if (!stream.ThrowIfNull().CanWrite)
                throw new ArgumentException(
                    "The stream must be writable.", nameof(stream));

            BaseStream = stream;
            endian = endianBitConverter.ThrowIfNull();
            this.encoding = encoding.ThrowIfNull();
            this.leaveOpen = leaveOpen;
            CanSeek = stream.CanSeek;
        }

        public EndianBinaryWriter(
            EndianBitConverter endianBitConverter,
            Stream stream,
            bool leaveOpen = false) :
            this(endianBitConverter, stream, Encoding.UTF8, leaveOpen)
        { }

        /// <summary>
        /// 关闭流和二进制写入器
        /// </summary>
        public void Close() => BaseStream.Close();

        /// <summary>
        /// 清除缓冲区并将所有数据写入基础流
        /// </summary>
        public void Flush() => BaseStream.Flush();

        /// <summary>
        /// 异步清除缓冲区并将所有数据写入基础流
        /// </summary>
        public Task FlushAsync(CancellationToken cancellationToken) =>
            BaseStream.FlushAsync(cancellationToken);

        /// <summary>
        /// 异步清除缓冲区并将所有数据写入基础流
        /// </summary>
        public Task FlushAsync() =>
            BaseStream.FlushAsync();

        /// <summary>
        /// 释放该写入器，若 leaveOpen = true，则同时释放基础流
        /// </summary>
        public void Dispose()
        {
            if (!leaveOpen)
                BaseStream.Dispose();
        }

        /// <summary>
        /// 移动流位置
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="origin">起始位置</param>
        /// <exception cref="NotSupportedException">当基础流不支持查找时抛出该异常</exception>
        public void Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException(
                    "The underlying stream does not support seeking.");

            BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// 写入一个字节
        /// </summary>
        public void Write(byte b) => BaseStream.WriteByte(b);

        /// <summary>
        /// 写入一个有符号字节
        /// </summary>
        public void Write(sbyte b) => BaseStream.WriteByte((byte)b);

        /// <summary>
        /// 写入一个布尔值
        /// </summary>
        public void Write(bool b) => BaseStream.WriteByte(endian.GetByte(b));

        /// <summary>
        /// 写入一个字节数组片段
        /// </summary>
        public void Write(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
                throw new ArgumentException("Buffer array cannot be null.", nameof(buffer));

            BaseStream.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        /// <summary>
        /// 异步写入一个字节数组片段
        /// </summary>
        public Task WriteAsync(ArraySegment<byte> buffer) =>
            WriteAsync(buffer, CancellationToken.None);

        /// <summary>
        /// 异步写入一个字节数组片段
        /// </summary>
        public Task WriteAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (buffer.Array == null)
                throw new ArgumentException("Buffer array cannot be null.", nameof(buffer));

            return BaseStream.WriteAsync(
                buffer.Array,
                buffer.Offset,
                buffer.Count,
                cancellationToken);
        }

        /// <summary>
        /// 写入一个字节数组
        /// </summary>
        public void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            BaseStream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 异步写入一个字节数组
        /// </summary>
        public Task WriteAsync(byte[] buffer) =>
            WriteAsync(buffer, CancellationToken.None);

        /// <summary>
        /// 异步写入一个字节数组
        /// </summary>
        public Task WriteAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return BaseStream.WriteAsync(
                buffer,
                0,
                buffer.Length,
                cancellationToken);
        }

        /// <summary>
        /// 写入一个字符
        /// </summary>
        public void Write(char c) =>
            WriteInt(c, 2, endian.GetBytes);

        /// <summary>
        /// 写入一个短整数
        /// </summary>
        public void Write(short value) =>
            WriteInt(value, 2, endian.GetBytes);

        /// <summary>
        /// 写入一个无符号短整数
        /// </summary>
        public void Write(ushort value) =>
            WriteInt(value, 2, endian.GetBytes);

        /// <summary>
        /// 写入一个整数
        /// </summary>
        public void Write(int value) =>
            WriteInt(value, 4, endian.GetBytes);

        /// <summary>
        /// 写入一个无符号整数
        /// </summary>
        public void Write(uint value) =>
            WriteInt(value, 4, endian.GetBytes);

        /// <summary>
        /// 写入一个长整数
        /// </summary>
        public void Write(long value) =>
            WriteInt(value, 8, endian.GetBytes);

        /// <summary>
        /// 写入一个无符号长整数
        /// </summary>
        public void Write(ulong value) =>
            WriteInt(value, 8, endian.GetBytes);

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        public void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var bytes = encoding.GetBytes(value);
            Write(bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        public Task WriteAsync(string value) =>
            WriteAsync(value, CancellationToken.None);

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        public Task WriteAsync(string value, CancellationToken cancellationToken)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var bytes = encoding.GetBytes(value);
            Write(bytes.Length);
            return WriteAsync(bytes, cancellationToken);
        }

        void WriteInt<T>(
            T value,
            int size,
            Action<T, ArraySegment<byte>> bitConverter)
        {
            var buf = GetWriteBuffer(size);
            bitConverter(value, buf);
            Write(buf);
        }

        byte[]? writeBuffer = null;

        ArraySegment<byte> GetWriteBuffer(int size)
        {
            if (writeBuffer == null)
                writeBuffer = new byte[8];

            ArraySegment<byte> b = new(writeBuffer, 0, size);
            return b;
        }

        readonly EndianBitConverter endian;
        readonly Encoding encoding;
        readonly bool leaveOpen;
    }
}