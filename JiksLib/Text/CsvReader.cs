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
    /// 解析规则与 <see cref="CsvParser"/> 一致：引号、<c>""</c> 转义与引号内换行遵循 RFC 4180；
    /// 记录分隔兼容 CRLF / LF / CR，未加引号字段中的引号按字面字符处理（比 RFC 4180 更宽松）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 字段是<b>按需读取</b>的：<see cref="Enumerator.MoveNext"/> 只准备一行，不读取记录内容；
    /// 只有枚举 <see cref="Row"/> 的字段时才真正解析。因此：
    /// </para>
    /// <list type="bullet">
    /// <item>跳过整行（不枚举字段）不会构造任何字符串，只做一次扫描；</item>
    /// <item>只读前几个字段（例如主键）时，剩余字段会在下一轮 MoveNext 时被直接跳过，不会物化。</item>
    /// </list>
    /// <para>
    /// <see cref="Row"/> 是当前记录在流上的<b>单遍视图</b>：必须在同一次循环内枚举字段；
    /// 推进到下一行后旧 Row 失效，再次枚举会抛 <see cref="InvalidOperationException"/>。
    /// 读取器本身不实现 <see cref="IEnumerable{T}"/>：底层是单遍流。
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
        /// 当前行版本：推进到新行时递增，用于让过期 Row 报错
        /// </summary>
        internal int Version => version;

        /// <summary>
        /// 准备下一行；当前行还有未读字段时先跳过
        /// </summary>
        internal bool PrepareNextRow()
        {
            // 用户只读了部分字段（或整行没读字段）：直接跳过剩余内容，不物化
            if (rowStarted && !rowFinished)
                SkipCurrentRow();

            rowStarted = false;
            rowFinished = false;
            fieldIndex = 0;

            if (PeekChar() < 0)
                return false;

            rowStarted = true;
            version++;
            return true;
        }

        /// <summary>
        /// 读取当前行的下一个字段；当前行已结束时返回 false
        /// 只物化这个字段本身，不会读取整条记录
        /// </summary>
        internal bool TryReadField(out string field)
        {
            field = string.Empty;

            if (rowFinished)
                return false;

            int first = PeekChar();

            if (first < 0)
            {
                // 分隔符后紧跟 EOF：补一个末尾空字段
                if (fieldIndex == 0)
                {
                    rowFinished = true;
                    return false;
                }

                rowFinished = true;
                fieldIndex++;
                return true;
            }

            if (first == '"')
            {
                ReadChar();
                field = ReadQuotedField();
            }
            else
            {
                field = ReadUnquotedField((char)first);
            }

            fieldIndex++;
            return true;
        }

        /// <summary>
        /// 跳过当前行剩余内容（尊重引号：引号内的换行属于字段内容）
        /// </summary>
        void SkipCurrentRow()
        {
            bool atFieldStart = true;

            while (true)
            {
                int value = ReadChar();

                if (value < 0)
                {
                    rowFinished = true;
                    return;
                }

                char c = (char)value;

                if (c == '"' && atFieldStart)
                {
                    SkipQuotedField();

                    int after = PeekChar();

                    if (after < 0)
                    {
                        rowFinished = true;
                        return;
                    }

                    char a = (char)after;

                    if (a == separator)
                    {
                        ReadChar();
                        atFieldStart = true;
                        continue;
                    }

                    if (a == '\r' || a == '\n')
                    {
                        ConsumeTerminator(a);
                        rowFinished = true;
                        return;
                    }

                    throw new InvalidDataException(
                        $"Unexpected character after field closure: {a}");
                }

                if (c == separator)
                {
                    atFieldStart = true;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && PeekChar() == '\n')
                        ReadChar();

                    rowFinished = true;
                    return;
                }

                atFieldStart = false;
            }
        }

        /// <summary>
        /// 跳过引号区；未闭合时抛异常
        /// </summary>
        void SkipQuotedField()
        {
            while (true)
            {
                int value = ReadChar();

                if (value < 0)
                    throw new InvalidDataException("Unclosed double quote in field.");

                if (value != '"')
                    continue;

                if (PeekChar() == '"')
                {
                    ReadChar();
                    continue;
                }

                return;
            }
        }

        /// <summary>
        /// 读取一个无引号字段
        /// </summary>
        string ReadUnquotedField(char first)
        {
            if (first == separator)
            {
                ReadChar();
                return string.Empty;
            }

            if (first == '\r' || first == '\n')
            {
                ConsumeTerminator(first);
                rowFinished = true;
                return string.Empty;
            }

            fieldBuilder.Clear();

            while (true)
            {
                int value = PeekChar();

                if (value < 0)
                {
                    rowFinished = true;
                    break;
                }

                char c = (char)value;

                if (c == separator)
                {
                    ReadChar();
                    break;
                }

                if (c == '\r' || c == '\n')
                {
                    ConsumeTerminator(c);
                    rowFinished = true;
                    break;
                }

                fieldBuilder.Append(c);
                ReadChar();
            }

            return fieldBuilder.ToString();
        }

        /// <summary>
        /// 读取一个带引号的字段（开引号已被消费）
        /// </summary>
        string ReadQuotedField()
        {
            fieldBuilder.Clear();

            while (true)
            {
                int value = ReadChar();

                if (value < 0)
                    throw new InvalidDataException("Unclosed double quote in field.");

                char c = (char)value;

                if (c != '"')
                {
                    fieldBuilder.Append(c);
                    continue;
                }

                // "" 是转义引号；否则是结束引号
                if (PeekChar() == '"')
                {
                    fieldBuilder.Append('"');
                    ReadChar();
                    continue;
                }

                break;
            }

            int after = PeekChar();

            if (after < 0)
            {
                rowFinished = true;
            }
            else
            {
                char a = (char)after;

                if (a == separator)
                {
                    ReadChar();
                }
                else if (a == '\r' || a == '\n')
                {
                    ConsumeTerminator(a);
                    rowFinished = true;
                }
                else
                {
                    throw new InvalidDataException(
                        $"Unexpected character after field closure: {a}");
                }
            }

            return fieldBuilder.ToString();
        }

        /// <summary>
        /// 消费记录终止符；<paramref name="c"/> 是 CR 时一并消费紧随的 LF
        /// </summary>
        void ConsumeTerminator(char c)
        {
            ReadChar();

            if (c == '\r' && PeekChar() == '\n')
                ReadChar();
        }

        /// <summary>
        /// 预读下一个字符但不消费；没有字符时返回 -1
        /// </summary>
        int PeekChar()
        {
            if (bufferIndex < bufferLength)
                return charBuffer[bufferIndex];

            if (!FillBuffer())
                return -1;

            return charBuffer[bufferIndex];
        }

        /// <summary>
        /// 读取下一个字符；没有字符时返回 -1
        /// </summary>
        int ReadChar()
        {
            if (bufferIndex < bufferLength)
                return charBuffer[bufferIndex++];

            if (!FillBuffer())
                return -1;

            return charBuffer[bufferIndex++];
        }

        /// <summary>
        /// 从底层流补充缓冲区；已到末尾时返回 false
        /// </summary>
        bool FillBuffer()
        {
            if (endOfStream)
                return false;

            bufferLength = textReader.Read(charBuffer, 0, charBuffer.Length);
            bufferIndex = 0;

            if (bufferLength <= 0)
            {
                bufferLength = 0;
                endOfStream = true;
                return false;
            }

            return true;
        }

        const int BufferSize = 4096;

        readonly StreamReader textReader;
        readonly char separator;
        readonly StringBuilder fieldBuilder = new();
        readonly char[] charBuffer = new char[BufferSize];

        int bufferIndex;
        int bufferLength;
        int version;
        int fieldIndex;
        bool rowStarted;
        bool rowFinished;
        bool endOfStream;

        /// <summary>
        /// 一行记录
        /// 当前记录在流上的单遍视图，<c>foreach (var field in row)</c> 按顺序读取字段。
        /// 实现 <see cref="IEnumerable{T}"/>（元素为 <see cref="string"/>），枚举器是嵌套的
        /// <see cref="Row.Enumerator"/>。
        /// </summary>
        public readonly struct Row : IEnumerable<string>
        {
            internal Row(CsvReader reader, int version)
            {
                this.reader = reader;
                this.version = version;
            }

            /// <summary>
            /// 获取字段枚举器
            /// 结构体枚举器，配合 foreach 使用不产生分配
            /// </summary>
            public Enumerator GetEnumerator() => new(reader, version);

            /// <summary>
            /// 通过 IEnumerable 遍历（会装箱结构体枚举器，仅用于 LINQ 等通用场景）
            /// </summary>
            IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            readonly CsvReader reader;
            readonly int version;

            /// <summary>
            /// 字段枚举器
            /// 逐字段从流读取；结构体枚举器，配合 foreach 使用不产生分配。
            /// 只物化被读取到的字段，跳过未读字段时不分配。
            /// </summary>
            public struct Enumerator : IEnumerator<string>
            {
                internal Enumerator(CsvReader reader, int version)
                {
                    this.reader = reader;
                    this.version = version;
                    current = string.Empty;
                    finished = false;
                }

                /// <summary>
                /// 当前字段
                /// </summary>
                public string Current => current;

                object? IEnumerator.Current => Current;

                /// <summary>
                /// 移动到下一个字段
                /// </summary>
                /// <returns>还有字段时返回 true</returns>
                public bool MoveNext()
                {
                    if (finished)
                        return false;

                    if (reader.Version != version)
                        throw new InvalidOperationException(
                            "The row is no longer current. Enumerate its fields before advancing the reader.");

                    if (!reader.TryReadField(out current))
                    {
                        finished = true;
                        return false;
                    }

                    return true;
                }

                /// <summary>
                /// 不支持重置：请重新调用 GetEnumerator
                /// </summary>
                public void Reset() =>
                    throw new NotSupportedException(
                        "Reset is not supported on CsvReader.Row.Enumerator. Call GetEnumerator() again instead.");

                public void Dispose() { }

                readonly CsvReader reader;
                readonly int version;
                string current;
                bool finished;
            }
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
                if (!reader.PrepareNextRow())
                    return false;

                current = new Row(reader, reader.Version);
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
