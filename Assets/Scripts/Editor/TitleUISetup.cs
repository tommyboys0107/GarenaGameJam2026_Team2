using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;

/// <summary>
/// 一次性 Editor 工具：自動設定 TitleUI 的 AudioSource 和 AudioClip。
/// 使用方式：Unity Editor 選單 → Tools → Setup Title UI Audio
/// </summary>
public static class TitleUISetup
{
    [MenuItem("Tools/Setup Title UI Audio")]
    public static void Setup()
    {
        // 確保 Title 場景已開啟
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Title")
        {
            // 嘗試開啟 Title 場景
            var titleScene = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", OpenSceneMode.Single);
            if (!titleScene.IsValid())
            {
                Debug.LogError("[TitleUISetup] 無法開啟 Title 場景！");
                return;
            }
        }

        // 找到 TitleManager 上的 TitleUI
        var titleUI = Object.FindFirstObjectByType<TitleUI>();
        if (titleUI == null)
        {
            Debug.LogError("[TitleUISetup] 找不到 TitleUI 組件！");
            return;
        }

        var go = titleUI.gameObject;

        // 加上 AudioSource（如果還沒有的話）
        var audioSource = go.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 音效
        }

        // 載入音效檔
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sounds/Sfx/a_button_click_sound_#4-1781354721102.mp3");

        if (clip == null)
        {
            Debug.LogError("[TitleUISetup] 找不到音效檔！請確認路徑：Assets/Sounds/Sfx/a_button_click_sound_#4-1781354721102.mp3");
            return;
        }

        // 用 SerializedObject 設定 private [SerializeField] 欄位
        var so = new SerializedObject(titleUI);
        so.FindProperty("sfxSource").objectReferenceValue = audioSource;
        so.FindProperty("buttonClickSound").objectReferenceValue = clip;
        so.ApplyModifiedProperties();

        // 標記場景已修改並儲存
        EditorUtility.SetDirty(titleUI);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[TitleUISetup] ✅ 完成！已設定 sfxSource 和 buttonClickSound。場景已儲存。");
    }
}
