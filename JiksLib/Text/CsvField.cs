using System;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 字段
    /// 保存字段的原始切片与"是否包含 <c>""</c> 转义"的标记，解转义是惰性的。
    /// </summary>
    /// <remarks>
    /// 解析阶段只做切片、不分配：枚举行与字段全程零分配。
    /// 只有读取包含转义引号的字段的 <see cref="Value"/> / <see cref="ToString"/> 时，
    /// 才会新建一次字符串——原始内容无法用"连续切片"表示去掉转义后的值。
    /// 不含转义的字段（绝大多数）取 <see cref="Value"/> 不会分配。
    /// </remarks>
    public readonly struct CsvField
    {
        internal CsvField(StringSegment raw, bool hasEscapedQuotes)
        {
            Raw = raw;
            HasEscapedQuotes = hasEscapedQuotes;
        }

        /// <summary>
        /// 空字段
        /// </summary>
        public static readonly CsvField Empty = new(StringSegment.Empty, false);

        /// <summary>
        /// 原始内容（保留 <c>""</c> 转义，零拷贝）
        /// </summary>
        public StringSegment Raw { get; }

        /// <summary>
        /// 是否包含 <c>""</c> 转义
        /// </summary>
        public bool HasEscapedQuotes { get; }

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
        public static implicit operator StringSegment(CsvField field) => field.Value;

        /// <summary>
        /// 把字段内容中的 <c>""</c> 还原为 <c>"</c>
        /// </summary>
        static StringSegment Unescape(StringSegment content) =>
            new(content.ToString().Replace("\"\"", "\""));
    }
}
