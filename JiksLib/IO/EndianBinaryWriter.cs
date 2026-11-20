using System;
using System.IO;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// <see cref="Write(string)"/> / <see cref="WriteAsync(string)"/> 允许写入的字符串最大字节数，默认 64 MiB。
        /// 与 <see cref="EndianBinaryReader.MaxStringLength"/> 的默认值一致，
        /// 避免本库写出的字符串无法被默认配置的读取器读回；必须为非负数。
        /// </summary>
        public int MaxStringLength { get; set; } = 64 * 1024 * 1024;

        public EndianBinaryWriter(
            EndianBitConverter endianBitConverter,
            Stream stream,
            Encoding encoding,
            bool leaveOpen = false)
        {
            if (!stream.ThrowIfNull(nameof(stream)).CanWrite)
                throw new ArgumentException(
                    "The stream must be writable.", nameof(stream));

            BaseStream = stream;
            endian = endianBitConverter.ThrowIfNull(nameof(endianBitConverter));
            this.encoding = encoding.ThrowIfNull(nameof(encoding));
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
        /// 关闭该写入器
        /// 若 leaveOpen 为 false，则同时关闭基础流
        /// </summary>
        public void Close()
        {
            if (!leaveOpen)
                BaseStream.Close();
        }

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
        /// 释放该写入器，若 leaveOpen = false，则同时释放基础流
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
            var bufferArray = buffer.Array.ThrowIfNull(nameof(buffer));

            BaseStream.Write(bufferArray, buffer.Offset, buffer.Count);
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
            var bufferArray = buffer.Array.ThrowIfNull(nameof(buffer));

            return BaseStream.WriteAsync(
                bufferArray,
                buffer.Offset,
                buffer.Count,
                cancellationToken);
        }

        /// <summary>
        /// 写入一个字节数组
        /// </summary>
        public void Write(byte[] buffer)
        {
            buffer.ThrowIfNull(nameof(buffer));

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
            buffer.ThrowIfNull(nameof(buffer));

            return BaseStream.WriteAsync(
                buffer,
                0,
                buffer.Length,
                cancellationToken);
        }

        /// <summary>
        /// 写入一个字符
        /// </summary>
        public void Write(char c)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(2);
            endian.GetBytes(c, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个短整数
        /// </summary>
        public void Write(short value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(2);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个无符号短整数
        /// </summary>
        public void Write(ushort value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(2);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个整数
        /// </summary>
        public void Write(int value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(4);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个无符号整数
        /// </summary>
        public void Write(uint value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(4);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个长整数
        /// </summary>
        public void Write(long value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(8);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个无符号长整数
        /// </summary>
        public void Write(ulong value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(8);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个单精度浮点数
        /// </summary>
        public void Write(float value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(4);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个双精度浮点数
        /// </summary>
        public void Write(double value)
        {
            ArraySegment<byte> buffer = GetWriteBuffer(8);
            endian.GetBytes(value, buffer);
            Write(buffer);
        }

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        /// <param name="value">要写入的字符串</param>
        /// <exception cref="InvalidDataException">
        /// 编码后的字节长度超过 <see cref="MaxStringLength"/> 时抛出
        /// </exception>
        public void Write(string value)
        {
            value.ThrowIfNull(nameof(value));

            var bytes = EncodeString(value);
            Write(bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        /// <param name="value">要写入的字符串</param>
        public Task WriteAsync(string value) =>
            WriteAsync(value, CancellationToken.None);

        /// <summary>
        /// 写入一个字符串
        /// </summary>
        /// <param name="value">要写入的字符串</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <exception cref="InvalidDataException">
        /// 编码后的字节长度超过 <see cref="MaxStringLength"/> 时抛出
        /// </exception>
        public Task WriteAsync(string value, CancellationToken cancellationToken)
        {
            value.ThrowIfNull(nameof(value));

            var bytes = EncodeString(value);
            Write(bytes.Length);
            return WriteAsync(bytes, cancellationToken);
        }

        byte[]? writeBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ArraySegment<byte> GetWriteBuffer(int size)
        {
            writeBuffer ??= new byte[8];
            ArraySegment<byte> b = new(writeBuffer, 0, size);
            return b;
        }

        /// <summary>
        /// 按构造时的编码取得字符串字节，并校验长度上限
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// 编码后的字节长度超过 <see cref="MaxStringLength"/> 时抛出
        /// </exception>
        byte[] EncodeString(string value)
        {
            var bytes = encoding.GetBytes(value);

            if (bytes.Length > MaxStringLength)
                throw new InvalidDataException(
                    $"String length {bytes.Length} exceeds the maximum allowed length {MaxStringLength}.");

            return bytes;
        }

        readonly EndianBitConverter endian;
        readonly Encoding encoding;
        readonly bool leaveOpen;
    }
}
