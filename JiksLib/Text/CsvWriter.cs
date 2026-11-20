using System;
using System.Text;
using JiksLib.Extensions;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 写入器
    /// 引号、转义与 CRLF 记录分隔遵循 RFC 4180；分隔符可修改以写入 TSV / DSV，
    /// 此时输出不再是严格意义的 RFC 4180（例如以制表符作为分隔符）。
    /// https://www.ietf.org/rfc/rfc4180.txt
    /// </summary>
    public class CsvWriter
    {
        /// <summary>
        /// 构造一个 CsvWriter 实例
        /// </summary>
        /// <param name="separator">指定 CSV 表格的分隔符，修改此值可以用于生成 TSV 或 DSV</param>
        /// <exception cref="ArgumentException">
        /// 当 <paramref name="separator"/> 为双引号或换行符时抛出
        /// </exception>
        public CsvWriter(char separator = ',')
        {
            this.separator = CsvParser.ValidateSeparator(separator);
        }

        /// <summary>
        /// 是否强制每个字段均被双引号包裹
        /// </summary>
        public bool AlwaysWrap { set; get; }

        /// <summary>
        /// 在当前记录写入字段
        /// 字段含分隔符、双引号、CR/LF（以及逗号/制表符）时用双引号包裹，内部双引号双写
        /// </summary>
        /// <param name="field">字段值</param>
        /// <exception cref="ArgumentNullException">字段为 null 时抛出</exception>
        public void WriteField(string field)
        {
            field.ThrowIfNull(nameof(field));

            if (!isFirstFieldOfCurrentLine)
                sb.Append(separator);
            isFirstFieldOfCurrentLine = false;

            if (!AlwaysWrap && !NeedsWrap(field))
            {
                sb.Append(field);
                return;
            }

            sb.Append('"');

            foreach (char c in field)
            {
                if (c == '"')
                    sb.Append('"');

                sb.Append(c);
            }

            sb.Append('"');
        }

        /// <summary>
        /// 判断字段是否需要加引号
        /// 只扫描一遍；逗号与制表符目前仍无条件触发包裹（P2-25 第 2 项暂不改变行为）
        /// </summary>
        bool NeedsWrap(string field)
        {
            foreach (char c in field)
            {
                if (c == separator || c == '"' || c == '\r' || c == '\n' ||
                    c == ',' || c == '\t')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 切换到下一条记录
        /// </summary>
        public void NextRecord()
        {
            isFirstFieldOfCurrentLine = true;
            sb.Append("\r\n");
        }

        /// <summary>
        /// 导出当前的csv
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return sb.ToString();
        }

        /// <summary>
        /// 清空已经写入的csv
        /// </summary>
        public void Clear()
        {
            sb.Clear();
            isFirstFieldOfCurrentLine = true;
        }

        readonly char separator;
        readonly StringBuilder sb = new();
        bool isFirstFieldOfCurrentLine = true;
    }
}
