using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 一次性 Editor 工具：在 Title 場景建立 Twitch ID InputField 和 Warning Panel。
/// 用完後可以刪除這個腳本。
/// </summary>
public static class TitleUISetupTwitch
{
    [MenuItem("Tools/Setup Twitch UI in Title")]
    public static void Setup()
    {
        var canvas = GameObject.Find("TitleCanvas");
        if (canvas == null)
        {
            Debug.LogError("找不到 TitleCanvas，請確認 Title 場景已開啟");
            return;
        }

        var canvasRect = canvas.GetComponent<RectTransform>();

        // === InputField ===
        var inputGO = new GameObject("TwitchIdInput");
        Undo.RegisterCreatedObjectUndo(inputGO, "Create TwitchIdInput");
        inputGO.transform.SetParent(canvasRect, false);
        var inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(0, -220);
        inputRect.sizeDelta = new Vector2(400, 50);
        var inputBg = inputGO.AddComponent<Image>();
        inputBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGO.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 6); taRect.offsetMax = new Vector2(-10, -7);
        textArea.AddComponent<RectMask2D>();

        var placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        var phRect = placeholder.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phTmp = placeholder.AddComponent<TextMeshProUGUI>();
        phTmp.text = "\u8f38\u5165 Twitch \u983b\u9053 ID...";
        phTmp.fontSize = 20;
        phTmp.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var txRect = textGO.AddComponent<RectTransform>();
        txRect.anchorMin = Vector2.zero; txRect.anchorMax = Vector2.one;
        txRect.offsetMin = Vector2.zero; txRect.offsetMax = Vector2.zero;
        var txTmp = textGO.AddComponent<TextMeshProUGUI>();
        txTmp.fontSize = 20; txTmp.color = Color.white;
        txTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = txTmp;
        inputField.placeholder = phTmp;
        inputField.characterLimit = 30;

        // === Warning Panel ===
        var warningGO = new GameObject("WarningPanel");
        Undo.RegisterCreatedObjectUndo(warningGO, "Create WarningPanel");
        warningGO.transform.SetParent(canvasRect, false);
        var wRect = warningGO.AddComponent<RectTransform>();
        wRect.anchorMin = Vector2.zero; wRect.anchorMax = Vector2.one;
        wRect.offsetMin = Vector2.zero; wRect.offsetMax = Vector2.zero;
        var wImg = warningGO.AddComponent<Image>();
        wImg.color = new Color(0, 0, 0, 0.7f);

        var content = new GameObject("Content");
        content.transform.SetParent(warningGO.transform, false);
        var cRect = content.AddComponent<RectTransform>();
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(500, 200);
        var cImg = content.AddComponent<Image>();
        cImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var warnText = new GameObject("WarningText");
        warnText.transform.SetParent(content.transform, false);
        var wtRect = warnText.AddComponent<RectTransform>();
        wtRect.anchoredPosition = new Vector2(0, 30);
        wtRect.sizeDelta = new Vector2(450, 80);
        var wtTmp = warnText.AddComponent<TextMeshProUGUI>();
        wtTmp.text = "\u5c1a\u672a\u8f38\u5165 Twitch \u983b\u9053 ID\n\u89c0\u773e\u6295\u7968\u529f\u80fd\u5c07\u7121\u6cd5\u4f7f\u7528";
        wtTmp.fontSize = 22; wtTmp.color = Color.white;
        wtTmp.alignment = TextAlignmentOptions.Center;

        // Back Button
        var backBtn = CreateButton(content.transform, "BackButton", "\u8fd4\u56de",
            new Vector2(-80, -60), new Color(0.4f, 0.4f, 0.4f, 1f));

        // Start Anyway Button
        var startBtn = CreateButton(content.transform, "StartAnywayButton", "\u76f4\u63a5\u958b\u59cb",
            new Vector2(80, -60), new Color(0.8f, 0.3f, 0.3f, 1f));

        warningGO.SetActive(false);

        // === Set TitleUI references ===
        var titleUI = Object.FindAnyObjectByType<UI.TitleUI>();
        if (titleUI != null)
        {
            var so = new SerializedObject(titleUI);
            so.FindProperty("twitchIdInput").objectReferenceValue = inputField;
            so.FindProperty("warningPanel").objectReferenceValue = warningGO;
            so.FindProperty("warningBackButton").objectReferenceValue = backBtn;
            so.FindProperty("warningStartButton").objectReferenceValue = startBtn;

            // TwitchConfig
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Data/TwitchConfig.asset");
            if (config != null)
                so.FindProperty("twitchConfig").objectReferenceValue = config;

            so.ApplyModifiedProperties();
            Debug.Log("[Setup] TitleUI references set!");
        }

        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Setup] Twitch UI created in Title scene. Run Tools > Setup Twitch UI in Title");
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Color bgColor)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(130, 40);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = bgColor;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 20;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
