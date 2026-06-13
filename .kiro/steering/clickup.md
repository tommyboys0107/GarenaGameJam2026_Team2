---
inclusion: manual
---

# ClickUp Integration

When accessing ClickUp data (documents, tasks, pages, etc.), use the ClickUp REST API directly via `Invoke-RestMethod` in PowerShell. Do NOT use the ClickUp MCP server tools.

## Credentials

- **API Key**: `pk_5815773_TRSZKDB0RCT9E802CEWVI6FMHLDVYIZ7`
- **Workspace ID (Team ID)**: `9018838308`

## API Base URL

```
https://api.clickup.com/api/v3/workspaces/9018838308
```

## Authentication Header

```powershell
-Headers @{ "Authorization" = "pk_5815773_TRSZKDB0RCT9E802CEWVI6FMHLDVYIZ7" }
```

## Common Endpoints

| Action | Method | Endpoint |
|--------|--------|----------|
| List all docs | GET | `/docs` |
| Get a doc | GET | `/docs/{doc_id}` |
| Get page listing | GET | `/docs/{doc_id}/page_listing` |
| Get pages with content | GET | `/docs/{doc_id}/pages` |
| Get single page | GET | `/docs/{doc_id}/pages/{page_id}` |
| Update page content | PUT | `/docs/{doc_id}/pages/{page_id}` |
| Create page | POST | `/docs/{doc_id}/pages` |

## Example: List Documents

```powershell
Invoke-RestMethod -Uri "https://api.clickup.com/api/v3/workspaces/9018838308/docs" -Headers @{ "Authorization" = "pk_5815773_TRSZKDB0RCT9E802CEWVI6FMHLDVYIZ7" } -Method Get | ConvertTo-Json -Depth 10
```

## Example: Update Page Content

```powershell
$body = @{ content = "# Title`nContent here" } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Uri "https://api.clickup.com/api/v3/workspaces/9018838308/docs/{doc_id}/pages/{page_id}" -Headers @{ "Authorization" = "pk_5815773_TRSZKDB0RCT9E802CEWVI6FMHLDVYIZ7"; "Content-Type" = "application/json" } -Method Put -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) | ConvertTo-Json -Depth 5
```

## Known Doc IDs

| Doc Name | Doc ID | Notes |
|----------|--------|-------|
| 文件區 (Game Jam 2026) | 8ct1394-2458 | Contains 《黑市交易員》 and brainstorming pages |

## Known Page IDs

| Page Name | Page ID | Parent Doc |
|-----------|---------|------------|
| 行前重點整理 | 8ct1394-598 | 8ct1394-2458 |
| Game Concept Brain Storming | 8ct1394-618 | 8ct1394-2458 |
| 《黑市交易員》 | 8ct1394-678 | 8ct1394-2458 |

## Notes

- Content format is Markdown
- Use `[System.Text.Encoding]::UTF8.GetBytes()` for request body to handle Chinese characters correctly
- Page content uses `\n` for newlines, use backtick-n in PowerShell strings
