using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Twitch
{
    /// <summary>
    /// 負責連接 Twitch IRC 並解析聊天訊息。
    /// 使用 TcpClient 在背景執行緒讀取，透過 ConcurrentQueue 傳回主執行緒。
    /// </summary>
    public class TwitchIRC : MonoBehaviour
    {
        [SerializeField] private TwitchConfig config;

        public event Action<string, string> OnChatMessage; // (username, message)
        public event Action OnConnected;
        public event Action OnDisconnected;

        private TcpClient _tcpClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private Thread _readThread;
        private readonly ConcurrentQueue<ChatMessage> _messageQueue = new();
        private bool _isConnected;
        private bool _shouldRun;

        private const string TwitchIrcUrl = "irc.chat.twitch.tv";

        /// <summary>
        /// 清空目前佇列中尚未處理的訊息。
        /// </summary>
        public void ClearMessageQueue()
        {
            while (_messageQueue.TryDequeue(out _)) { }
        }
        private const int TwitchIrcPort = 6667;

        private struct ChatMessage
        {
            public string Username;
            public string Message;
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("[TwitchIRC] 缺少 TwitchConfig，請在 Inspector 中指定。");
                return;
            }

            Connect();
        }

        private void Update()
        {
            // 將背景執行緒收到的訊息派發到主執行緒
            while (_messageQueue.TryDequeue(out var msg))
            {
                Debug.Log($"[TwitchIRC] 聊天: {msg.Username}: {msg.Message}");
                OnChatMessage?.Invoke(msg.Username, msg.Message);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (_isConnected) return;

            try
            {
                _tcpClient = new TcpClient(TwitchIrcUrl, TwitchIrcPort);
                var stream = _tcpClient.GetStream();
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream) { AutoFlush = true };

                // 認證
                if (config.IsAnonymous)
                {
                    // 匿名模式：使用 justinfan + 隨機數字
                    var anonUser = $"justinfan{UnityEngine.Random.Range(10000, 99999)}";
                    _writer.WriteLine("PASS SCHMOOPIIE");
                    _writer.WriteLine("NICK " + anonUser);
                }
                else
                {
                    _writer.WriteLine("PASS oauth:" + config.oauthToken);
                    _writer.WriteLine("NICK " + config.botUsername.ToLower());
                }

                // 加入頻道
                _writer.WriteLine("JOIN #" + config.channelName.ToLower());

                _isConnected = true;
                _shouldRun = true;

                // 開始背景讀取
                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();

                Debug.Log($"[TwitchIRC] 已連接到頻道: {config.channelName}");
                OnConnected?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TwitchIRC] 連線失敗: {e.Message}");
            }
        }

        public void Disconnect()
        {
            _shouldRun = false;
            _isConnected = false;

            try
            {
                _writer?.Close();
                _reader?.Close();
                _tcpClient?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TwitchIRC] 斷線時發生錯誤: {e.Message}");
            }

            _readThread = null;
            OnDisconnected?.Invoke();
            Debug.Log("[TwitchIRC] 已斷開連線。");
        }

        private void ReadLoop()
        {
            while (_shouldRun)
            {
                try
                {
                    if (_tcpClient == null || !_tcpClient.Connected) break;

                    var line = _reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;

                    // Debug: 顯示所有收到的 IRC 原始訊息
                    Debug.Log($"[TwitchIRC RAW] {line}");

                    // 回應 PING 以保持連線
                    if (line.StartsWith("PING"))
                    {
                        _writer.WriteLine("PONG :tmi.twitch.tv");
                        continue;
                    }

                    // 解析 PRIVMSG 格式: :username!username@username.tmi.twitch.tv PRIVMSG #channel :message
                    if (line.Contains("PRIVMSG"))
                    {
                        var parsed = ParseMessage(line);
                        if (parsed.HasValue)
                        {
                            _messageQueue.Enqueue(parsed.Value);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_shouldRun)
                    {
                        Debug.LogWarning($"[TwitchIRC] 讀取錯誤: {e.Message}");
                    }
                    break;
                }
            }
        }

        private ChatMessage? ParseMessage(string raw)
        {
            try
            {
                // 取得使用者名稱
                int exclamationIndex = raw.IndexOf('!');
                if (exclamationIndex <= 1) return null;

                string username = raw.Substring(1, exclamationIndex - 1);

                // 取得訊息內容
                int msgIndex = raw.IndexOf("PRIVMSG");
                if (msgIndex < 0) return null;

                int colonIndex = raw.IndexOf(':', msgIndex);
                if (colonIndex < 0) return null;

                string message = raw.Substring(colonIndex + 1);

                return new ChatMessage { Username = username, Message = message.Trim() };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 發送訊息到聊天室（需要 OAuth 認證，匿名模式不可用）
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (config.IsAnonymous)
            {
                Debug.LogWarning("[TwitchIRC] 匿名模式無法發送訊息。");
                return;
            }

            if (!_isConnected) return;

            try
            {
                _writer.WriteLine($"PRIVMSG #{config.channelName.ToLower()} :{message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TwitchIRC] 發送訊息失敗: {e.Message}");
            }
        }
    }
}
