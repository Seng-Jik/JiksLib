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
    /// <c>foreach (var field in row)</c> 得到 <see cref="Field"/>，全程不装箱、不分配。
    /// 字段保存的是底层字符串的切片（零拷贝）；解转义是惰性的，只有读取
    /// <see cref="Field.Value"/> / <see cref="Field.ToString"/> 且字段确实含
    /// <c>""</c> 转义时才会新建一次字符串。
    /// </para>
    /// <para>
    /// 记录分隔符支持 CRLF、LF、CR；最后一条记录之后的终止符不会再产生一条空记录。
    /// 空记录按一个空字段处理。解析器不可重入，同一实例只应串行遍历一次。
    /// </para>
    /// <para>
    /// 类型同时实现 <see cref="IEnumerable{T}"/>（元素为 <see cref="Row"/>），因此可以直接用 LINQ；
    /// 但通过接口遍历会装箱结构体枚举器，热路径请仍使用 <c>foreach</c> 或直接调用
    /// <see cref="GetEnumerator"/>。
    /// </para>
    /// </remarks>
    public sealed class CsvParser : IEnumerable<CsvParser.Row>
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
        /// 通过 IEnumerable 遍历（会装箱结构体枚举器，仅用于 LINQ 等通用场景）
        /// </summary>
        IEnumerator<CsvParser.Row> IEnumerable<CsvParser.Row>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
        /// 记录边界扫描关心的特殊字符：引号与换行
        /// </summary>
        static readonly char[] RecordSpecialChars = { '"', '\r', '\n' };

        /// <summary>
        /// 从 <paramref name="startIndex"/> 开始查找记录边界相关的特殊字符（引号 / CR / LF）
        /// 借助底层字符串的 <see cref="string.IndexOfAny(char[], int, int)"/> 做向量化跳跃，
        /// 避免逐字符扫描
        /// </summary>
        /// <returns>相对片段的下标；找不到时返回 -1</returns>
        static int IndexOfRecordSpecial(StringSegment segment, int startIndex)
        {
            if (startIndex >= segment.Length)
                return -1;

            int index = segment.Source.IndexOfAny(
                RecordSpecialChars,
                segment.Offset + startIndex,
                segment.Length - startIndex);

            return index < 0 ? -1 : index - segment.Offset;
        }

        /// <summary>
        /// 跳过引号区：从开引号之后开始扫描，返回结束引号的位置
        /// <c>""</c> 是转义引号，单个 <c>"</c> 是结束引号；找不到结束引号时抛异常
        /// </summary>
        static int SkipQuotedSection(
            StringSegment record,
            int startIndex,
            out bool hasEscapedQuote)
        {
            hasEscapedQuote = false;

            while (true)
            {
                int quote = record.IndexOf('"', startIndex);

                if (quote < 0)
                    throw new InvalidDataException("Unclosed double quote in field.");

                if (quote + 1 < record.Length && record[quote + 1] == '"')
                {
                    hasEscapedQuote = true;
                    startIndex = quote + 2;
                    continue;
                }

                return quote;
            }
        }

        /// <summary>
        /// CSV 字段
        /// 保存字段的原始切片与"是否包含 <c>""</c> 转义"的标记，解转义是惰性的。
        /// 默认值（<c>default</c>）表示空字段：<see cref="Raw"/> 为默认 <see cref="StringSegment"/>，长度为 0。
        /// </summary>
        /// <remarks>
        /// 解析阶段只做切片、不分配：枚举行与字段全程零分配。
        /// 只有读取包含转义引号的字段的 <see cref="Value"/> / <see cref="ToString"/> 时，
        /// 才会新建一次字符串——原始内容无法用"连续切片"表示去掉转义后的值。
        /// 不含转义的字段（绝大多数）取 <see cref="Value"/> 不会分配。
        /// </remarks>
        public readonly struct Field
        {
            internal Field(StringSegment raw, bool hasEscapedQuotes)
            {
                Raw = raw;
                HasEscapedQuotes = hasEscapedQuotes;
            }

            /// <summary>
            /// 原始内容（保留 <c>""</c> 转义，零拷贝）
            /// </summary>
            public readonly StringSegment Raw;

            /// <summary>
            /// 是否包含 <c>""</c> 转义
            /// </summary>
            public readonly bool HasEscapedQuotes;

            /// <summary>
            /// 解转义后的值
            /// 不含转义时直接返回 <see cref="Raw"/>（零分配）；含转义时新建一次字符串
            /// </summary>
            public StringSegment Value => HasEscapedQuotes ? Unescape(Raw) : Raw;

            /// <summary>
            /// 返回解转义后的字符串
            /// </summary>
            public override string ToString() => Value.ToString();

            /// <summary>
            /// 隐式转换为解转义后的片段
            /// 便于沿用 <c>foreach (StringSegment field in row)</c> 的写法
            /// </summary>
            public static implicit operator StringSegment(Field field) => field.Value;

            /// <summary>
            /// 把字段内容中的 <c>""</c> 还原为 <c>"</c>
            /// </summary>
            static StringSegment Unescape(StringSegment content) =>
                new(content.ToString().Replace("\"\"", "\""));
        }

        /// <summary>
        /// 一行记录
        /// 保存记录的原始切片，<c>foreach (var field in row)</c> 按顺序取出该行的所有字段。
        /// 实现 <see cref="IEnumerable{T}"/>（元素为 <see cref="Field"/>），枚举器是嵌套的
        /// <see cref="Row.Enumerator"/>；同一行可以重复枚举。
        /// </summary>
        public readonly struct Row : IEnumerable<Field>
        {
            /// <summary>
            /// 用记录片段构造行
            /// </summary>
            /// <param name="record">不包含记录终止符的原始记录文本</param>
            /// <param name="separator">字段分隔符</param>
            internal Row(StringSegment record, char separator)
            {
                this.record = record;
                this.separator = separator;
            }

            /// <summary>
            /// 获取字段枚举器
            /// 结构体枚举器，配合 foreach 使用不产生分配
            /// </summary>
            public Enumerator GetEnumerator() => new(record, separator);

            /// <summary>
            /// 通过 IEnumerable 遍历（会装箱结构体枚举器，仅用于 LINQ 等通用场景）
            /// </summary>
            IEnumerator<Field> IEnumerable<Field>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            readonly StringSegment record;
            readonly char separator;

            /// <summary>
            /// 字段枚举器
            /// 逐字段解析一条记录；结构体枚举器，配合 foreach 使用不产生分配。
            /// </summary>
            public struct Enumerator : IEnumerator<Field>
            {
                internal Enumerator(StringSegment record, char separator)
                {
                    this.record = record;
                    this.separator = separator;
                    position = 0;
                    finished = false;
                    current = default;
                }

                /// <summary>
                /// 当前字段
                /// </summary>
                public Field Current => current;

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
                        current = default;
                        finished = true;
                        return true;
                    }

                    // 上一个字段以分隔符结尾：产生末尾空字段
                    if (position == record.Length)
                    {
                        current = default;
                        finished = true;
                        return true;
                    }

                    current = ParseField(out int next, out bool isLast);
                    position = next;
                    finished = isLast;
                    return true;
                }

                /// <summary>
                /// 不支持重置：请重新调用 GetEnumerator
                /// </summary>
                public void Reset() =>
                    throw new NotSupportedException(
                        "Reset is not supported on CsvParser.Row.Enumerator. Call GetEnumerator() again instead.");

                public void Dispose() { }

                /// <summary>
                /// 解析 position 处的字段，并给出下一个字段的起始位置
                /// 只做切片与标记，不分配；解转义推迟到 <see cref="Field.Value"/>
                /// </summary>
                /// <param name="next">下一个字段的起始位置</param>
                /// <param name="isLast">当前字段之后没有分隔符（已到记录末尾）</param>
                Field ParseField(out int next, out bool isLast)
                {
                    int start = position;

                    if (record[start] == '"')
                    {
                        int closing = SkipQuotedSection(record, start + 1, out bool hasEscapedQuote);
                        int after = closing + 1;

                        // RFC 4180：结束引号之后只能是分隔符、记录结束或文件结束
                        if (after < record.Length && record[after] != separator)
                            throw new InvalidDataException(
                                $"Unexpected character after field closure: {record[after]}");

                        if (after >= record.Length)
                        {
                            next = record.Length;
                            isLast = true;
                        }
                        else
                        {
                            next = after + 1;
                            isLast = false;
                        }

                        return new Field(
                            record.Slice(start + 1, closing - start - 1),
                            hasEscapedQuote);
                    }

                    // 无引号字段：直接跳到分隔符
                    int end = record.IndexOf(separator, start);

                    if (end < 0)
                    {
                        next = record.Length;
                        isLast = true;
                        end = record.Length;
                    }
                    else
                    {
                        next = end + 1;
                        isLast = false;
                    }

                    return new Field(record.Slice(start, end - start), false);
                }

                readonly StringSegment record;
                readonly char separator;
                int position;
                bool finished;
                Field current;
            }
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

                while (true)
                {
                    int special = IndexOfRecordSpecial(source, index);

                    // 没有终止符：最后一条记录
                    if (special < 0)
                    {
                        current = new Row(
                            source.Slice(start, source.Length - start),
                            separator);

                        position = source.Length;
                        finished = true;
                        return true;
                    }

                    char c = source[special];

                    // 引号外的换行就是记录边界
                    if (c == '\r' || c == '\n')
                    {
                        int next = special + 1;

                        if (c == '\r' && next < source.Length && source[next] == '\n')
                            next++;

                        current = new Row(
                            source.Slice(start, special - start),
                            separator);

                        position = next;
                        return true;
                    }

                    // 引号：只有出现在字段开头才开启引号区，否则按字面字符处理
                    bool atFieldStart = special == start || source[special - 1] == separator;

                    if (!atFieldStart)
                    {
                        index = special + 1;
                        continue;
                    }

                    // 跳过整个引号区：引号内的换行与分隔符都是内容
                    index = SkipQuotedSection(source, special + 1, out _) + 1;
                }
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
