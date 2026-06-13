using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Core;

namespace UI
{
    /// <summary>
    /// 角色選擇場景的 UI 控制器。
    /// 六個角色以 2 排 × 3 列排列，對應鍵盤 Q/W/E（上排）、A/S/D（下排）。
    /// 選擇後自動進入遊戲場景。
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("角色選項的外框 (依序 Q/W/E/A/S/D)")]
        [SerializeField] private Image[] characterFrames = new Image[6];

        [Header("角色名稱文字 (選填)")]
        [SerializeField] private TextMeshProUGUI[] characterNames = new TextMeshProUGUI[6];

        [Header("目前選擇的提示文字")]
        [SerializeField] private TextMeshProUGUI selectionText;

        [Header("視覺設定")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color selectedColor = Color.yellow;

        [Header("按下選擇鍵後延遲幾秒進入遊戲")]
        [SerializeField] private float confirmDelay = 0.5f;

        private int _currentSelection = -1;
        private bool _confirmed;

        // 對應按鍵：Q=0, W=1, E=2, A=3, S=4, D=5
        private readonly Key[] _keys = { Key.Q, Key.W, Key.E, Key.A, Key.S, Key.D };
        private readonly string[] _keyLabels = { "Q", "W", "E", "A", "S", "D" };

        private void Start()
        {
            // 初始化所有框為預設顏色
            for (int i = 0; i < characterFrames.Length; i++)
            {
                if (characterFrames[i] != null)
                    characterFrames[i].color = normalColor;
            }

            if (selectionText != null)
                selectionText.text = "按 Q/W/E/A/S/D 選擇角色";
        }

        private void Update()
        {
            if (_confirmed) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int i = 0; i < _keys.Length; i++)
            {
                if (keyboard[_keys[i]].wasPressedThisFrame)
                {
                    SelectCharacter(i);
                    break;
                }
            }
        }

        private void SelectCharacter(int index)
        {
            // 更新視覺
            if (_currentSelection >= 0 && _currentSelection < characterFrames.Length)
            {
                if (characterFrames[_currentSelection] != null)
                    characterFrames[_currentSelection].color = normalColor;
            }

            _currentSelection = index;

            if (characterFrames[index] != null)
                characterFrames[index].color = selectedColor;

            if (selectionText != null)
                selectionText.text = $"已選擇角色 {_keyLabels[index]}（{index + 1}號）";

            // 儲存選擇並進入遊戲
            _confirmed = true;
            GameData.SelectedCharacterIndex = index;
            Invoke(nameof(GoToGame), confirmDelay);
        }

        private void GoToGame()
        {
            SceneLoader.LoadGame();
        }
    }
}
