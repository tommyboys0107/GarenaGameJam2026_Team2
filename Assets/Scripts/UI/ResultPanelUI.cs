using UnityEngine;
using UnityEngine.UIElements;
using Gameplay;
using Core;
using Data;

namespace UI
{
    /// <summary>
    /// 結算畫面 UI Toolkit 控制器。
    /// 顯示成功/失敗、目標股票+價格、重玩/返回首頁按鈕。
    /// </summary>
    public class ResultPanelUI : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WinConditionChecker winConditionChecker;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _container;
        private Label _titleLabel;
        private Label _goalLabel;
        private Label _resultLabel;
        private Button _replayButton;
        private Button _titleButton;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            // 開始時隱藏整個 UI Document
            if (_uiDocument != null)
                _uiDocument.enabled = false;
        }

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.OnGameEnd += ShowResult;
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.OnGameEnd -= ShowResult;
        }

        private void ShowResult(bool isWin)
        {
            // 啟用 UIDocument
            if (_uiDocument != null)
                _uiDocument.enabled = true;

            // 等一幀讓 UIDocument 建立 visual tree
            StartCoroutine(SetupUINextFrame(isWin));
        }

        private System.Collections.IEnumerator SetupUINextFrame(bool isWin)
        {
            yield return null;

            _root = _uiDocument.rootVisualElement;
            _container = _root.Q<VisualElement>("result-container");
            _titleLabel = _root.Q<Label>("result-title");
            _goalLabel = _root.Q<Label>("goal-info");
            _resultLabel = _root.Q<Label>("result-detail");
            _replayButton = _root.Q<Button>("replay-button");
            _titleButton = _root.Q<Button>("title-button");

            // 設定內容
            if (_titleLabel != null)
                _titleLabel.text = isWin ? "任務完成！" : "任務失敗";

            if (winConditionChecker != null && winConditionChecker.CurrentGoal != null)
            {
                var goal = winConditionChecker.CurrentGoal;
                string direction = goal.targetPercent > 0 ? "漲幅" : "跌幅";
                string targetStr = $"{(goal.targetPercent > 0 ? "+" : "")}{goal.targetPercent}%";

                if (_goalLabel != null)
                    _goalLabel.text = $"目標：{goal.stockCode} {direction} {targetStr}";

                float actualPercent = winConditionChecker.CurrentPercent;
                float currentPrice = winConditionChecker.CurrentPrice;
                float initialPrice = winConditionChecker.InitialPrice;

                if (_resultLabel != null)
                    _resultLabel.text = $"實際：{goal.stockCode} {actualPercent:+0.0;-0.0}%\n(${initialPrice:F0} \u2192 ${currentPrice:F0})";
            }

            // 綁定按鈕
            if (_replayButton != null)
                _replayButton.clicked += OnReplayClicked;

            if (_titleButton != null)
                _titleButton.clicked += OnTitleClicked;

            // 顯示動畫
            if (_container != null)
                _container.AddToClassList("visible");
        }

        private void OnReplayClicked()
        {
            SceneLoader.ReloadGame();
        }

        private void OnTitleClicked()
        {
            GameData.Reset();
            SceneLoader.LoadTitle();
        }
    }
}
