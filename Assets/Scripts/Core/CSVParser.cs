using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Core
{
    public static class CSVParser
    {
        // 正規表達式：以逗號分割，但忽略引號內的逗號
        private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        // 正規表達式：處理多種換行符號
        private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        private static readonly char[] TRIM_CHARS = { '\"' };

        /// <summary>
        /// 解析 CSV 文本，回傳以 Dictionary 組成的列表。
        /// 第一列將作為 Dictionary 的 Key。
        /// </summary>
        public static List<Dictionary<string, string>> Parse(string csvText)
        {
            var list = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(csvText)) return list;

            var lines = Regex.Split(csvText, LINE_SPLIT_RE);
            if (lines.Length <= 1) return list;

            // 取得第一列作為標題 (Header)
            var header = Regex.Split(lines[0], SPLIT_RE);
            for (var i = 0; i < header.Length; i++)
            {
                header[i] = header[i].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
            }

            // 解析後續資料列
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || values[0] == "") continue;

                var entry = new Dictionary<string, string>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j];
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                    // 處理雙引號跳脫
                    value = value.Replace("\"\"", "\"");
                    entry[header[j]] = value;
                }
                list.Add(entry);
            }
            return list;
        }
    }
}
