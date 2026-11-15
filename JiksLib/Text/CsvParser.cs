using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JiksLib.Extensions;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 解析器
    /// 解析内存中的字符串或字符串片段，按 RFC 4180 处理引号、转义引号与引号内的换行；
    /// 分隔符可修改以读取 TSV / DSV。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 迭代器是结构体：<c>foreach (var row in parser)</c> 得到 <see cref="Row"/>，
    /// <c>foreach (var field in row)</c> 得到 <see cref="StringSegment"/>，全程不装箱、不分配行对象。
    /// 字段内容默认是对底层字符串的切片（零拷贝）；只有包含转义引号（<c>""</c>）的字段才需要新建字符串。
    /// </para>
    /// <para>
    /// 记录分隔符支持 CRLF、LF、CR；最后一条记录之后的终止符不会再产生一条空记录。
    /// 空记录按一个空字段处理。解析器不可重入，同一实例只应串行遍历一次。
    /// </para>
    /// </remarks>
    public sealed class CsvParser
    {
        /// <summary>
        /// 用整个字符串构造解析器
        /// </summary>
        /// <param name="source">要解析的 CSV 文本</param>
        /// <param name="separator">字段分隔符，默认为逗号</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="separator"/> 为引号或换行符时抛出
        /// </exception>
        public CsvParser(string source, char separator = ',')
            : this(new StringSegment(source.ThrowIfNull(nameof(source))), separator)
        {
        }

        /// <summary>
        /// 用字符串片段构造解析器
        /// </summary>
        /// <param name="source">要解析的 CSV 片段</param>
        /// <param name="separator">字段分隔符，默认为逗号</param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="separator"/> 为引号或换行符时抛出
        /// </exception>
        public CsvParser(StringSegment source, char separator = ',')
        {
            this.source = source;
            this.separator = ValidateSeparator(separator);
        }

        /// <summary>
        /// 获取行枚举器
        /// 结构体枚举器，配合 foreach 使用不产生分配
        /// </summary>
        public Enumerator GetEnumerator() => new(source, separator);

        /// <summary>
        /// 校验分隔符：引号与换行符会破坏 CSV 结构，不允许作为分隔符
        /// </summary>
        internal static char ValidateSeparator(char separator, string paramName = "separator")
        {
            if (separator == '"' || separator == '\r' || separator == '\n')
                throw new ArgumentException(
                    "Separator cannot be a double quote or a line break.",
                    paramName);

            return separator;
        }

        readonly StringSegment source;
        readonly char separator;

        /// <summary>
        /// 一行记录
        /// 本身是一个结构体枚举器，<c>foreach (var field in row)</c> 按顺序取出该行的所有字段
        /// </summary>
        public struct Row : IEnumerator<StringSegment>
        {
            /// <summary>
            /// 用记录片段构造行枚举器
            /// </summary>
            /// <param name="record">不包含记录终止符的原始记录文本</param>
            /// <param name="separator">字段分隔符</param>
            internal Row(StringSegment record, char separator)
            {
                this.record = record;
                this.separator = separator;
                position = 0;
                finished = false;
                current = StringSegment.Empty;
            }

            /// <summary>
            /// 当前字段
            /// </summary>
            public StringSegment Current => current;

            object? IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一个字段
            /// </summary>
            /// <returns>还有字段时返回 true</returns>
            public bool MoveNext()
            {
                if (finished)
                    return false;

                // 空记录（例如两个换行之间没有任何字符）按一个空字段处理
                if (record.Length == 0)
                {
                    current = StringSegment.Empty;
                    finished = true;
                    return true;
                }

                // 上一个字段以分隔符结尾：产生末尾空字段
                if (position == record.Length)
                {
                    current = StringSegment.Empty;
                    finished = true;
                    return true;
                }

                current = ParseField(out int next, out bool isLast);
                position = next;
                finished = isLast;
                return true;
            }

            /// <summary>
            /// 不支持重置：内部只保存了当前位置，重置成"开头"需要重新构造，请对 CsvParser 重新 GetEnumerator
            /// </summary>
            public void Reset() =>
                throw new NotSupportedException(
                    "Reset is not supported on CsvParser.Row. Use CsvParser.GetEnumerator() instead.");

            public void Dispose() { }

            /// <summary>
            /// 返回自身，使 Row 可以直接用于 foreach
            /// </summary>
            public Row GetEnumerator() => this;

            /// <summary>
            /// 解析 position 处的字段，并给出下一个字段的起始位置
            /// </summary>
            /// <param name="next">下一个字段的起始位置</param>
            /// <param name="isLast">当前字段之后没有分隔符（已到记录末尾）</param>
            StringSegment ParseField(out int next, out bool isLast)
            {
                int start = position;

                if (record[start] == '"')
                {
                    int index = start + 1;
                    bool hasEscapedQuote = false;

                    while (index < record.Length)
                    {
                        if (record[index] != '"')
                        {
                            index++;
                            continue;
                        }

                        // "" 表示一个转义引号，字段还没结束
                        if (index + 1 < record.Length && record[index + 1] == '"')
                        {
                            hasEscapedQuote = true;
                            index += 2;
                            continue;
                        }

                        int contentEnd = index;
                        index++;

                        // RFC 4180：结束引号之后只能是分隔符、记录结束或文件结束
                        if (index < record.Length && record[index] != separator)
                            throw new InvalidDataException(
                                $"Unexpected character after field closure: {record[index]}");

                        if (index >= record.Length)
                        {
                            next = record.Length;
                            isLast = true;
                        }
                        else
                        {
                            next = index + 1;
                            isLast = false;
                        }

                        var content = record.Slice(start + 1, contentEnd - start - 1);

                        return hasEscapedQuote ? Unescape(content) : content;
                    }

                    throw new InvalidDataException("Unclosed double quote in field.");
                }

                int end = start;

                while (end < record.Length && record[end] != separator)
                    end++;

                next = end < record.Length ? end + 1 : record.Length;
                isLast = end >= record.Length;

                return record.Slice(start, end - start);
            }

            /// <summary>
            /// 把字段内容中的 "" 还原为 "
            /// </summary>
            static StringSegment Unescape(StringSegment content) =>
                new(content.ToString().Replace("\"\"", "\""));

            readonly StringSegment record;
            readonly char separator;
            int position;
            bool finished;
            StringSegment current;
        }

        /// <summary>
        /// 行枚举器
        /// 结构体枚举器，配合 foreach 使用不产生分配
        /// </summary>
        public struct Enumerator : IEnumerator<Row>
        {
            internal Enumerator(StringSegment source, char separator)
            {
                this.source = source;
                this.separator = separator;
                position = 0;
                current = default;
                finished = false;
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
                if (finished)
                    return false;

                // 输入结束（或上一条记录正好以终止符结束）：不再产生空记录
                if (position >= source.Length)
                {
                    finished = true;
                    return false;
                }

                int start = position;
                int index = start;
                bool inQuotes = false;
                bool fieldStart = true;

                while (index < source.Length)
                {
                    char c = source[index];

                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            // 转义引号：跳过成对的两个引号；否则是结束引号
                            if (index + 1 < source.Length && source[index + 1] == '"')
                                index++;

                            else
                                inQuotes = false;
                        }

                        index++;
                        continue;
                    }

                    if (c == '"' && fieldStart)
                    {
                        inQuotes = true;
                        fieldStart = false;
                        index++;
                        continue;
                    }

                    if (c == separator)
                    {
                        fieldStart = true;
                        index++;
                        continue;
                    }

                    if (c == '\r' || c == '\n')
                    {
                        int recordEnd = index;
                        int next = index + 1;

                        if (c == '\r' && next < source.Length && source[next] == '\n')
                            next++;

                        current = new Row(
                            source.Slice(start, recordEnd - start),
                            separator);

                        position = next;
                        return true;
                    }

                    fieldStart = false;
                    index++;
                }

                if (inQuotes)
                    throw new InvalidDataException("Unclosed double quote in field.");

                // 最后一条记录没有终止符
                current = new Row(source.Slice(start, index - start), separator);
                position = index;
                finished = true;
                return true;
            }

            /// <summary>
            /// 不支持重置：请对 CsvParser 重新 GetEnumerator
            /// </summary>
            public void Reset() =>
                throw new NotSupportedException(
                    "Reset is not supported on CsvParser.Enumerator. Call CsvParser.GetEnumerator() instead.");

            public void Dispose() { }

            readonly StringSegment source;
            readonly char separator;
            int position;
            Row current;
            bool finished;
        }
    }
}
