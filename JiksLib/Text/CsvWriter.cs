using System.Text;

namespace CosmosPrelude.Misc
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
        }

        /// <summary>
        /// 是否强制每个字段均被双引号包裹
        /// </summary>
        public bool AlwaysWrap { private set; get; } = false;

        /// <summary>
        /// 在当前记录写入字段
        /// </summary>
        /// <param name="field">字段值</param>
        public void WriteField(string field)
        {
            if (!isFirstFieldOfCurrentLine)
                sb.Append(separator);
            isFirstFieldOfCurrentLine = false;

            bool wrap =
                AlwaysWrap ||
                field.Contains(separator.ToString()) ||
                field.Contains("\r") ||
                field.Contains("\"") ||
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
            sb.AppendLine();
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


        #region 实现细节

        readonly char separator;
        readonly StringBuilder sb = new();
        bool isFirstFieldOfCurrentLine = true;

        #endregion

    }
}
