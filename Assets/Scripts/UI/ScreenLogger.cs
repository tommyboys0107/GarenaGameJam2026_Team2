using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UI
{
    /// <summary>
    /// 將 Debug.Log 訊息顯示在畫面上的 TMP 文字元件中。
    /// 適用於 Build 後無法看到 Console 的情況。
    /// </summary>
    public class ScreenLogger : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI logText;

        [Header("設定")]
        [SerializeField] private int maxLines = 20;
        [SerializeField] private bool showTimestamp = true;
        [SerializeField] private bool showErrors = true;
        [SerializeField] private bool showWarnings = true;
        [SerializeField] private bool showLogs = true;

        private readonly Queue<string> _lines = new();

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error && !showErrors) return;
            if (type == LogType.Warning && !showWarnings) return;
            if (type == LogType.Log && !showLogs) return;

            string prefix = type switch
            {
                LogType.Error => "<color=red>[ERR]</color> ",
                LogType.Warning => "<color=yellow>[WARN]</color> ",
                _ => ""
            };

            string timestamp = showTimestamp ? $"<color=#888>[{Time.time:F1}]</color> " : "";
            string line = $"{timestamp}{prefix}{message}";

            _lines.Enqueue(line);
            while (_lines.Count > maxLines)
                _lines.Dequeue();

            if (logText != null)
                logText.text = string.Join("\n", _lines);
        }

        /// <summary>
        /// 手動清除畫面 log
        /// </summary>
        public void Clear()
        {
            _lines.Clear();
            if (logText != null)
                logText.text = "";
        }
    }
}
