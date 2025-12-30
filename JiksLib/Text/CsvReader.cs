using System;
using System.IO;
using System.Text;
using JiksLib.Extensions;

namespace JiksLib.Text
{
    /// <summary>
    /// CSV 阅读器
    /// 其中 CSV 格式遵守 IETF RFC 4180 中所描述的格式，同时也允许修改分隔符以读取 TSV 和 DSV
    /// https://www.ietf.org/rfc/rfc4180.txt
    /// </summary>
    public class CsvReader : IDisposable
    {
        /// <summary>
        /// 从 TextReader 读取一个 CSV 表格
        /// </summary>
        /// <param name="csv">正在读取 CSV 文件的 TextReader</param>
        /// <param name="leaveOpen">销毁该对象时是否不销毁 TextReader</param>
        /// <param name="separator">分隔符，修改此值可以读取 TSV 或 DSV</param>
        public CsvReader(
            TextReader csv,
            bool leaveOpen = false,
            char separator = ',')
        {
            this.csv = csv.ThrowIfNull();
            this.leaveOpen = leaveOpen;
            this.separator = separator;
        }

        /// <summary>
        /// 从字符串读取一个 CSV 表格
        /// </summary>
        /// <param name="csv">CSV 字符串</param>
        /// <param name="separator">分隔符，修改此值可以读取 TSV 或 DSV</param>
        public CsvReader(string csv, char separator = ',') :
            this(new StringReader(csv.ThrowIfNull()), false, separator)
        {
        }

        public void Dispose()
        {
            if (!leaveOpen)
                csv.Dispose();
        }


        /// <summary>
        /// 从当前行弹出一个字段
        /// </summary>
        /// <returns>当前字段，若不能弹出字段则返回null</returns>
        public string? PopField()
        {
            if (noMoreFields) return null;

            bool isQuoted = csv.Peek() == '\"';
            if (isQuoted) csv.Read();

            while (true)
            {
                var peek = csv.Peek();

                if (isQuoted)
                {
                    if (peek == -1)
                        throw new InvalidDataException("字段双引号未闭合");

                    if (peek == '\"')
                    {
                        csv.Read();
                        var afterDoubleQuote = csv.Peek();

                        if (afterDoubleQuote == separator ||
                            afterDoubleQuote == -1 ||
                            afterDoubleQuote == '\r' ||
                            afterDoubleQuote == '\n')
                        {
                            isQuoted = false;
                            break;
                        }
                        else if (afterDoubleQuote == '\"')
                            csv.Read();
                        else
                        {
                            csv.Read();
                            throw new InvalidDataException(
                                $"在字段闭合后发现意外字符{(char)afterDoubleQuote}");
                        }
                    }
                    else csv.Read();
                }
                else if (
                    peek == separator ||
                    peek == '\n' ||
                    peek == '\r' ||
                    peek == -1)
                    break;
                else csv.Read();

                fieldSb.Append((char)peek);
            }

            var field = fieldSb.ToString();
            fieldSb.Clear();

            var peek2 = csv.Peek();
            if (peek2 == '\n' || peek2 == '\r' || peek2 == -1)
                noMoreFields = true;
            else if (peek2 == separator)
                csv.Read();
            else throw new InvalidDataException();

            return field;
        }

        /// <summary>
        /// 将光标移动到下一行
        /// </summary>
        /// <returns>如果文件结束，则返回false，否则返回true</returns>
        public bool NextRecord()
        {
            while (!noMoreFields) PopField();

            if (csv.Peek() == -1) return false;

            if (csv.Peek() == '\r') csv.Read();
            if (csv.Peek() == '\n') csv.Read();
            if (csv.Peek() == -1) return false;
            noMoreFields = false;
            return true;
        }

        #region 实现细节

        readonly TextReader csv;
        readonly StringBuilder fieldSb = new();
        readonly bool leaveOpen;
        readonly char separator;
        bool noMoreFields = false;

        #endregion
    }
}
