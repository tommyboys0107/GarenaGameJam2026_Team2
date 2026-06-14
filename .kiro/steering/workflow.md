# Workflow 規範

## MCP 優先原則

所有可以透過 Unity MCP 完成的操作，都**必須**直接用 MCP 執行，不要留下「你需要手動做」的步驟給使用者。

具體包含：

### 場景物件操作
- 建立 GameObject → 用 `manage_gameobject`
- 加入元件 → 用 `manage_components`
- 設定 SerializedField 參考 → 用 `manage_components` 的 `set_property`（透過 instanceID 綁定）
- 儲存場景 → 用 `manage_scene(action: "save")`

### 完整流程
當新增或修改腳本的 SerializedField 後，必須接著：
1. 用 MCP `find_gameobjects` 找到場景中對應的 GameObject
2. 用 MCP `manage_components` 設定 property 綁定參考
3. 用 MCP `manage_scene` 儲存場景

**永遠不要只寫程式碼然後告訴使用者「去 Inspector 拖」。**

### 編譯驗證
修改腳本後：
1. 用 `refresh_unity(compile: "request")` 觸發編譯
2. 用 `read_console` 確認沒有錯誤

### 例外情況
以下情況可以請使用者手動處理：
- 需要拖入尚未存在的美術資源（圖片、音效等）
- 需要調整視覺位置（因為只有使用者能看到 Game View）
- 需要填入機密資訊（如 OAuth Token）
