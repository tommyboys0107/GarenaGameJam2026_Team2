using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// 場景載入工具，提供靜態方法供按鈕或腳本呼叫。
    /// </summary>
    public static class SceneLoader
    {
        public const string TitleScene = "Title";
        public const string CharacterSelectScene = "CharacterSelect";
        public const string GameScene = "Game";

        public static void LoadTitle()
        {
            SceneManager.LoadScene(TitleScene);
        }

        public static void LoadCharacterSelect()
        {
            SceneManager.LoadScene(CharacterSelectScene);
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(GameScene);
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
