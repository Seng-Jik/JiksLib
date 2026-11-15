using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 流式读取器
    /// 从 <see cref="Stream"/> 按 UTF-8（或指定编码）逐条读取 CSV 记录，字段以 <see cref="string"/> 返回；
    /// 解析规则与 <see cref="CsvParser"/> 完全一致（RFC 4180，支持引号转义与引号内换行）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 迭代器是结构体：<c>foreach (var row in reader)</c> 得到 <see cref="Row"/>，
    /// <c>foreach (var field in row)</c> 得到 <see cref="string"/>。
    /// 读取器只支持单遍遍历，多个枚举器会共享同一个底层流。
    /// </para>
    /// <para>
    /// 每条记录会先完整读入内存再解析字段，因此单条记录占用的内存与记录长度成正比；
    /// 记录之间不会缓存。
    /// </para>
    /// </remarks>
    public sealed class CsvReader : IDisposable
    {
        /// <summary>
        /// 从流构造 CSV 读取器
        /// </summary>
        /// <param name="stream">要读取的流，必须可读</param>
        /// <param name="encoding">文本编码，null 表示 UTF-8；默认会按 BOM 自动识别编码</param>
        /// <param name="leaveOpen">Dispose 时是否保留底层流</param>
        /// <param name="separator">字段分隔符，默认为逗号</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当流不可读，或 <paramref name="separator"/> 为引号/换行符时抛出
        /// </exception>
        public CsvReader(
            Stream stream,
            Encoding? encoding = null,
            bool leaveOpen = false,
            char separator = ',')
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("The stream must be readable.", nameof(stream));

            this.separator = CsvParser.ValidateSeparator(separator);

            textReader = new StreamReader(
                stream,
                encoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: leaveOpen);
        }

        /// <summary>
        /// 获取行枚举器
        /// 结构体枚举器，配合 foreach 使用不产生分配
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// 销毁读取器
        /// 构造时 <c>leaveOpen = false</c> 则同时销毁底层流
        /// </summary>
        public void Dispose()
        {
            textReader.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 读取下一条记录；没有更多记录时返回 null
        /// </summary>
        internal string? ReadRecord()
        {
            if (endOfStream)
                return null;

            var builder = recordBuilder;
            builder.Clear();

            bool hasContent = false;
            bool inQuotes = false;
            bool fieldStart = true;

            while (true)
            {
                int value = ReadChar();

                if (value < 0)
                {
                    if (inQuotes)
                        throw new InvalidDataException("Unclosed double quote in field.");

                    if (!hasContent)
                    {
                        endOfStream = true;
                        return null;
                    }

                    return builder.ToString();
                }

                char c = (char)value;
                hasContent = true;

                if (inQuotes)
                {
                    builder.Append(c);

                    if (c == '"')
                    {
                        // "" 是转义引号；否则是结束引号
                        if (PeekChar() == '"')
                            builder.Append((char)ReadChar());
                        else
                            inQuotes = false;
                    }

                    continue;
                }

                if (c == '"' && fieldStart)
                {
                    inQuotes = true;
                    fieldStart = false;
                    builder.Append(c);
                    continue;
                }

                if (c == separator)
                {
                    fieldStart = true;
                    builder.Append(c);
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && PeekChar() == '\n')
                        ReadChar();

                    return builder.ToString();
                }

                fieldStart = false;
                builder.Append(c);
            }
        }

        /// <summary>
        /// 读取下一个字符；没有字符时返回 -1
        /// 自带一字符 lookahead，避免依赖 <see cref="StreamReader.Peek"/> 在不同运行时的边界行为
        /// </summary>
        int ReadChar()
        {
            if (pendingChar != NoPendingChar)
            {
                int value = pendingChar;
                pendingChar = NoPendingChar;
                return value;
            }

            return textReader.Read();
        }

        /// <summary>
        /// 预读下一个字符但不消费；没有字符时返回 -1
        /// </summary>
        int PeekChar()
        {
            if (pendingChar == NoPendingChar)
                pendingChar = textReader.Read();

            return pendingChar;
        }

        const int NoPendingChar = -2;

        readonly StreamReader textReader;
        readonly char separator;
        readonly StringBuilder recordBuilder = new();
        int pendingChar = NoPendingChar;
        bool endOfStream;

        /// <summary>
        /// 一行记录
        /// 本身是一个结构体枚举器，<c>foreach (var field in row)</c> 按顺序取出该行的所有字段
        /// </summary>
        public struct Row : IEnumerator<string>
        {
            internal Row(string record, char separator)
            {
                inner = new CsvParser.Row(new StringSegment(record), separator);
            }

            /// <summary>
            /// 当前字段
            /// </summary>
            public string Current => inner.Current.ToString();

            object? IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一个字段
            /// </summary>
            /// <returns>还有字段时返回 true</returns>
            public bool MoveNext() => inner.MoveNext();

            /// <summary>
            /// 不支持重置：请对 CsvReader 重新 GetEnumerator
            /// </summary>
            public void Reset() =>
                throw new NotSupportedException(
                    "Reset is not supported on CsvReader.Row. Use CsvReader.GetEnumerator() instead.");

            public void Dispose() { }

            /// <summary>
            /// 返回自身，使 Row 可以直接用于 foreach
            /// </summary>
            public Row GetEnumerator() => this;

            CsvParser.Row inner;
        }

        /// <summary>
        /// 行枚举器
        /// 结构体枚举器，配合 foreach 使用不产生分配
        /// </summary>
        public struct Enumerator : IEnumerator<Row>
        {
            internal Enumerator(CsvReader reader)
            {
                this.reader = reader;
                current = default;
            }

            /// <summary>
            /// 当前行
            /// </summary>
            public Row Current => current;

            object? IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一条记录
            /// </summary>
            /// <returns>还有记录时返回 true</returns>
            public bool MoveNext()
            {
                var record = reader.ReadRecord();

                if (record == null)
                    return false;

                current = new Row(record, reader.separator);
                return true;
            }

            /// <summary>
            /// 不支持重置：请对 CsvReader 重新 GetEnumerator
            /// </summary>
            public void Reset() =>
                throw new NotSupportedException(
                    "Reset is not supported on CsvReader.Enumerator. Call CsvReader.GetEnumerator() instead.");

            public void Dispose() { }

            readonly CsvReader reader;
            Row current;
        }
    }
}
