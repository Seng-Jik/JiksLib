#nullable enable

using System.Text;
using JiksLib.Extensions;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 写入器
    /// 其中 CSV 格式遵守 IETF RFC 4180 中所描述的格式，同时也允许修改分隔符以写入TSV和DSV
    /// https://www.ietf.org/rfc/rfc4180.txt
    /// </summary>
    public class CsvWriter
    {
        /// <summary>
        /// 构造一个 CsvWriter 实例
        /// </summary>
        /// <param name="separator">指定 CSV 表格的分隔符，修改此值可以用于生成 TSV 或 DSV</param>
        public CsvWriter(char separator = ',')
        {
            this.separator = separator;
            separatorString = separator.ToString();
        }

        /// <summary>
        /// 是否强制每个字段均被双引号包裹
        /// </summary>
        public bool AlwaysWrap { set; get; } = false;

        /// <summary>
        /// 在当前记录写入字段
        /// </summary>
        /// <param name="field">字段值</param>
        public void WriteField(string field)
        {
            field.ThrowIfNull();

            if (!isFirstFieldOfCurrentLine)
                sb.Append(separator);
            isFirstFieldOfCurrentLine = false;

            bool wrap =
                AlwaysWrap ||
                field.Contains(separatorString) ||
                field.Contains("\r") ||
                field.Contains("\"") ||
                field.Contains("\'") ||
                field.Contains("\n") ||
                field.Contains(",") ||
                field.Contains("\t");

            if (wrap) sb.Append('\"');
            sb.Append(wrap ? field.Replace("\"", "\"\"") : field);
            if (wrap) sb.Append('\"');
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
        readonly string separatorString;
        readonly StringBuilder sb = new();
        bool isFirstFieldOfCurrentLine = true;

    }
}
