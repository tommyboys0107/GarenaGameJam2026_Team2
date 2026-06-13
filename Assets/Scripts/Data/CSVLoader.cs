using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// 從 Resources 載入 CSV 並解析為對應資料結構。
    /// </summary>
    public static class CSVLoader
    {
        public static List<StockInfo> LoadStocks()
        {
            var lines = ReadCSV("StockData");
            var list = new List<StockInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 5) continue;
                list.Add(new StockInfo
                {
                    code = cols[0].Trim(),
                    name = cols[1].Trim(),
                    attribute = cols[2].Trim(),
                    blackMarketMeaning = cols[3].Trim(),
                    initialPrice = ParsePrice(cols[4]),
                    currentPrice = ParsePrice(cols[4])
                });
            }

            return list;
        }

        public static List<CharacterInfo> LoadCharacters()
        {
            var lines = ReadCSV("CharacterData");
            var list = new List<CharacterInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 4) continue;
                list.Add(new CharacterInfo
                {
                    id = cols[0].Trim(),
                    name = cols[1].Trim(),
                    nickname = cols[2].Trim(),
                    description = cols[3].Trim()
                });
            }

            return list;
        }

        public static List<EventInfo> LoadEvents()
        {
            var lines = ReadCSV("EventData");
            var list = new List<EventInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 4) continue;
                var choiceStr = cols[3].Trim().Trim('"');
                var choiceIds = choiceStr.Split(',');
                for (int i = 0; i < choiceIds.Length; i++)
                    choiceIds[i] = choiceIds[i].Trim();

                list.Add(new EventInfo
                {
                    id = cols[0].Trim(),
                    name = cols[1].Trim(),
                    description = cols[2].Trim(),
                    choiceIds = choiceIds
                });
            }

            return list;
        }

        public static Dictionary<string, ChoiceInfo> LoadChoices()
        {
            var lines = ReadCSV("ChooseData");
            var dict = new Dictionary<string, ChoiceInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 5) continue;
                var choice = new ChoiceInfo
                {
                    id = cols[0].Trim(),
                    name = cols[1].Trim(),
                    effects = ParseEffects(cols[2]),
                    resultText = cols[3].Trim(),
                    resultEN = cols.Length > 4 ? cols[4].Trim() : "",
                    audioId = cols.Length > 5 ? cols[5].Trim() : "",
                    note = cols.Length > 6 ? cols[6].Trim() : ""
                };
                dict[choice.id] = choice;
            }

            return dict;
        }

        public static List<AudienceEventInfo> LoadAudienceEvents()
        {
            var lines = ReadCSV("AudienceData");
            var list = new List<AudienceEventInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 5) continue;
                list.Add(new AudienceEventInfo
                {
                    id = cols[0].Trim(),
                    name = cols[1].Trim(),
                    effects = ParseEffects(cols[2]),
                    resultText = cols[3].Trim(),
                    resultEN = cols.Length > 4 ? cols[4].Trim() : "",
                    audioId = cols.Length > 5 ? cols[5].Trim() : "",
                    imageId = cols.Length > 6 ? cols[6].Trim() : ""
                });
            }

            return list;
        }

        public static List<GoalInfo> LoadGoals()
        {
            var lines = ReadCSV("GoalData");
            var list = new List<GoalInfo>();

            foreach (var cols in lines)
            {
                if (cols.Length < 3) continue;
                list.Add(new GoalInfo
                {
                    id = cols[0].Trim(),
                    stockCode = cols[1].Trim(),
                    targetPercent = int.Parse(cols[2].Trim())
                });
            }

            return list;
        }

        public static TimeConfig LoadTimeConfig()
        {
            var lines = ReadCSV("TimeData");
            var config = new TimeConfig();

            foreach (var cols in lines)
            {
                if (cols.Length < 2) continue;
                string key = cols[0].Trim();
                int val = int.Parse(cols[1].Trim());

                switch (key)
                {
                    case "traderEventTime": config.traderEventTime = val; break;
                    case "traderEventCount": config.traderEventCount = val; break;
                    case "audienceEventTime": config.audienceEventTime = val; break;
                    case "audienceEventCount": config.audienceEventCount = val; break;
                    case "totalGameTime": config.totalGameTime = val; break;
                    case "trendAffectInterval": config.trendAffectInterval = val; break;
                }
            }

            return config;
        }

        // === 工具方法 ===

        private static List<string[]> ReadCSV(string fileName)
        {
            var textAsset = Resources.Load<TextAsset>(fileName);
            if (textAsset == null)
            {
                Debug.LogError($"[CSVLoader] 找不到 Resources/{fileName}.csv");
                return new List<string[]>();
            }

            var result = new List<string[]>();
            var lines = textAsset.text.Split('\n');

            // 跳過第一行（標題）
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var cols = ParseCSVLine(line);
                result.Add(cols);
            }

            return result;
        }

        /// <summary>
        /// 支援引號內逗號的 CSV 行解析
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);
            return result.ToArray();
        }

        /// <summary>
        /// 解析 "[NARC,-20],[BYTE,+8]" 格式
        /// </summary>
        private static StockEffect[] ParseEffects(string raw)
        {
            var effects = new List<StockEffect>();
            raw = raw.Trim().Trim('"');

            var matches = Regex.Matches(raw, @"\[(\w+),([\+\-]?\d+)\]");
            foreach (Match m in matches)
            {
                effects.Add(new StockEffect
                {
                    stockCode = m.Groups[1].Value,
                    value = int.Parse(m.Groups[2].Value)
                });
            }

            return effects.ToArray();
        }

        private static int ParsePrice(string raw)
        {
            raw = raw.Trim().Replace("$", "").Replace(",", "");
            return int.TryParse(raw, out int val) ? val : 0;
        }
    }
}
