using System;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 遊戲主流程管理器。
    /// 負責 60 秒倒數計時，時間到後觸發結算。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("遊戲設定")]
        [SerializeField] private float gameDuration = 60f;

        /// <summary>遊戲結束時觸發</summary>
        public event Action OnGameOver;

        /// <summary>目前剩餘時間</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>遊戲是否正在進行</summary>
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            TimeRemaining = gameDuration;
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
