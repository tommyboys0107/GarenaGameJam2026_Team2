using System;
using UnityEngine;
using Data;

namespace Gameplay
{
    /// <summary>
    /// 遊戲主流程管理器。
    /// 從 TimeData.csv 讀取遊戲時長，倒數計時，時間到後觸發結算。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>遊戲結束時觸發</summary>
        public event Action OnGameOver;

        /// <summary>目前剩餘時間</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>遊戲總時長</summary>
        public float GameDuration { get; private set; }

        /// <summary>遊戲是否正在進行</summary>
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            var timeConfig = CSVLoader.LoadTimeConfig();
            GameDuration = timeConfig.totalGameTime;
            StartGame();
        }

        public void StartGame()
        {
            TimeRemaining = GameDuration;
            IsPlaying = true;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsPlaying = false;
                OnGameOver?.Invoke();
            }
        }
    }
}
