using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    public class StockInfo
    {
        public string code;       // NARC, LOCK, BYTE
        public string name;       // 納克製藥
        public string attribute;  // 大型製藥公司
        public string blackMarketMeaning;
        public int initialPrice;  // 80, 100, 120
        public float currentPrice;
    }

    [Serializable]
    public class CharacterInfo
    {
        public string id;              // C01
        public string name;            // 維乃德·卡索
        public string gender;          // 男
        public string description;     // 西西里裁縫店老闆...
        public StockEffect[] specialty; // [NARC,+15],[BYTE,-10]
        public string selectQuote;     // In this business...
        public string selectImageId;   // C01_C
        public string unselectImageId; // C01
        public string portraitImageId; // C01_F
        public string audioId;         // C01_Intro
    }

    [Serializable]
    public class EventInfo
    {
        public string id;         // E01
        public string name;       // 臨床試驗報告外洩
        public string description;
        public string[] choiceIds; // E01-A, E01-B, E01-C
    }

    [Serializable]
    public class ChoiceInfo
    {
        public string id;         // E01-A
        public string name;       // 提前放空納克
        public StockEffect[] effects;
        public string resultText;
        public string resultEN;
        public string audioId;
        public string note;
    }

    [Serializable]
    public class AudienceEventInfo
    {
        public string id;         // A01
        public string name;       // 臨床試驗造假曝光
        public StockEffect[] effects;
        public string resultText;
        public string resultEN;
        public string audioId;
        public string imageId;
    }

    [Serializable]
    public class GoalInfo
    {
        public string id;              // G01
        public string nickname;        // 絕命毒師 (Il Farmacista)
        public string nicknameDesc;    // 掌握納克製藥的地下配方網路...
        public string unselectIconId;  // G01
        public string selectIconId;    // G01_L
        public string stockCode;       // NARC
        public int targetPercent;      // 40 or -40
    }

    [Serializable]
    public class TimeConfig
    {
        public int traderEventTime;      // 10
        public int traderEventCount;     // 6
        public int audienceEventTime;    // 20
        public int audienceEventCount;   // 3
        public int totalGameTime;        // 235
        public int trendAffectInterval;  // 5
        public int startDuration;        // 10
        public int eventInterval;        // 5
    }

    [Serializable]
    public struct StockEffect
    {
        public string stockCode; // NARC, LOCK, BYTE
        public int value;        // +20, -15, etc.
    }
}
