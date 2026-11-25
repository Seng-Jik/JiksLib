using System;
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
    /// 读取是<b>单遍、按需</b>的：<see cref="ReadRow"/> 只准备一行，不读取记录内容；
    /// 只有调用 <see cref="ReadField"/> / <see cref="TryReadField"/> 时才真正解析字段。因此：
    /// </para>
    /// <list type="bullet">
    /// <item>跳过整行（不枚举字段）不会构造任何字符串，只做一次扫描；</item>
    /// <item>只读前几个字段（例如主键）时，剩余字段会在下一轮 MoveNext 时被直接跳过，不会物化。</item>
    /// </list>
    /// <para>
    /// 读取器本身不实现 <see cref="IEnumerable{T}"/>：底层是单遍流。
    /// 每行必须在 <see cref="ReadRow"/> 之后读取字段；未读字段会在下一行开始时被直接跳过。
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
        /// 销毁读取器
        /// 构造时 <c>leaveOpen = false</c> 则同时销毁底层流
        /// </summary>
        public void Dispose()
        {
            disposed = true;
            textReader.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 校验读取器尚未被释放
        /// </summary>
        void ThrowIfDisposed()
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(CsvReader));
#endif
        }

        /// <summary>
        /// 推进到下一行
        /// 当前行还有未读字段时先跳过，不物化被跳过的内容。
        /// </summary>
        /// <returns>成功推进到一行时返回 true；流结束时返回 false</returns>
        /// <exception cref="ObjectDisposedException">读取器已经被释放时抛出</exception>
        public bool ReadRow()
        {
            ThrowIfDisposed();
            return PrepareNextRow();
        }

        /// <summary>
        /// 读取当前行的下一个字段
        /// </summary>
        /// <returns>字段内容</returns>
        /// <exception cref="InvalidOperationException">
        /// 尚未调用 <see cref="ReadRow"/>，或当前行的字段已经读完时抛出
        /// </exception>
        /// <exception cref="ObjectDisposedException">读取器已经被释放时抛出</exception>
        public string ReadField()
        {
            if (!TryReadField(out var field))
                throw new InvalidOperationException(
                    "No more fields in the current row. Call ReadRow() first.");

            return field;
        }

        /// <summary>
        /// 尝试读取当前行的下一个字段
        /// </summary>
        /// <param name="field">字段内容；当前行已经没有字段时为 <see cref="string.Empty"/></param>
        /// <returns>读取到字段时返回 true；当前行已经没有字段时返回 false</returns>
        /// <exception cref="InvalidOperationException">尚未调用 <see cref="ReadRow"/> 时抛出</exception>
        /// <exception cref="ObjectDisposedException">读取器已经被释放时抛出</exception>
        public bool TryReadField(out string field)
        {
            ThrowIfDisposed();

            if (!rowStarted)
                throw new InvalidOperationException(
                    "Call ReadRow() before reading fields.");

            return TryReadFieldCore(out field);
        }

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
            return true;
        }

        /// <summary>
        /// 读取当前行的下一个字段；当前行已结束时返回 false
        /// 只物化这个字段本身，不会读取整条记录
        /// </summary>
        bool TryReadFieldCore(out string field)
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
        bool disposed;
        int fieldIndex;
        bool rowStarted;
        bool rowFinished;
        bool endOfStream;

    }
}
