# 性能優化：EventManager.GetAllEvents() 重複調用問題

## 問題診斷

在 `StatisticsEngine` 中的兩個方法存在性能問題：

### `GetTeamScoringBreakdown` (原始實現)
```csharp
public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
{
    var scoringActions = new[] 
    { 
        ActionType.AttackSuccess, 
        ActionType.BlockSuccess, 
        ActionType.ServeSuccess,
        ActionType.TeamScore
    };

    var breakdown = new Dictionary<ActionType, int>();
    foreach (var action in scoringActions)
    {
        breakdown[action] = _eventManager.GetAllEvents()  // ❌ 迴圈內每次都複製!
            .Count(e => e.Team == team && e.Action == action);
    }
    return breakdown;
}
```

### `GetTeamErrorBreakdown` (原始實現)
```csharp
public Dictionary<ActionType, int> GetTeamErrorBreakdown(TeamSide team)
{
    var errorActions = new[]
    {
        ActionType.AttackFault,
        ActionType.ServeFault,
        ActionType.BlockFault,
        ActionType.ReceiveFault,
        ActionType.TossFault
    };

    var breakdown = new Dictionary<ActionType, int>();
    foreach (var action in errorActions)
    {
        breakdown[action] = _eventManager.GetAllEvents()  // ❌ 迴圈內每次都複製!
            .Count(e => e.Team == team && e.Action == action);
    }
    return breakdown;
}
```

### 性能成本分析

**GetAllEvents() 實現：**
```csharp
public List<GameEvent> GetAllEvents()
{
    return new List<GameEvent>(_events);  // 完整副本
}
```

**不必要的複製：**
- `GetTeamScoringBreakdown`：4 種得分動作 × N 個事件 = 4N 個物件複製
- `GetTeamErrorBreakdown`：5 種失誤動作 × N 個事件 = 5N 個物件複製

**示例場景（100 個事件）：**
```
GetTeamScoringBreakdown:  4 次副本操作 = 400 個事件物件複製
GetTeamErrorBreakdown:    5 次副本操作 = 500 個事件物件複製
```

---

## 解決方案已實施

### ✅ 第 1 階 - 快速修復（已完成）

在方法內部先調用一次 `GetAllEvents()`，然後在迴圈中重用：

**GetTeamScoringBreakdown - 優化版本：**
```csharp
public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
{
    var scoringActions = new[] 
    { 
        ActionType.AttackSuccess, 
        ActionType.BlockSuccess, 
        ActionType.ServeSuccess,
        ActionType.TeamScore
    };

    var allEvents = _eventManager.GetAllEvents();  // ✅ 只複製一次
    var breakdown = new Dictionary<ActionType, int>();
    foreach (var action in scoringActions)
    {
        breakdown[action] = allEvents
            .Count(e => e.Team == team && e.Action == action);
    }
    return breakdown;
}
```

**GetTeamErrorBreakdown - 優化版本：**
```csharp
public Dictionary<ActionType, int> GetTeamErrorBreakdown(TeamSide team)
{
    var errorActions = new[]
    {
        ActionType.AttackFault,
        ActionType.ServeFault,
        ActionType.BlockFault,
        ActionType.ReceiveFault,
        ActionType.TossFault
    };

    var allEvents = _eventManager.GetAllEvents();  // ✅ 只複製一次
    var breakdown = new Dictionary<ActionType, int>();
    foreach (var action in errorActions)
    {
        breakdown[action] = allEvents
            .Count(e => e.Team == team && e.Action == action);
    }
    return breakdown;
}
```

**改進效果：**
- `GetTeamScoringBreakdown`：4N 減少到 N
- `GetTeamErrorBreakdown`：5N 減少到 N
- **內存分配減少：75% 和 80%**

---

## 進階優化建議（未來可實施）

### 🔄 第 2 階 - 提供唯讀枚舉（推薦）

在 `EventManager` 中添加唯讀枚舉，完全避免副本：

```csharp
public class EventManager
{
    private List<GameEvent> _events;

    // ✅ 新增：唯讀枚舉介面
    public IEnumerable<GameEvent> GetAllEventsReadOnly()
    {
        return _events; // 直接返回，無複製
    }

    // 保持現有方法以向後相容
    public List<GameEvent> GetAllEvents()
    {
        return new List<GameEvent>(_events);
    }
}
```

**在 StatisticsEngine 中使用：**
```csharp
public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
{
    var scoringActions = new[] { /* ... */ };
    var allEvents = _eventManager.GetAllEventsReadOnly();  // 無複製
    var breakdown = new Dictionary<ActionType, int>();
    foreach (var action in scoringActions)
    {
        breakdown[action] = allEvents
            .Count(e => e.Team == team && e.Action == action);
    }
    return breakdown;
}
```

**優勢：**
- 完全避免內存複製
- 保證內部狀態安全性
- 向後相容

### 💾 第 3 階 - 緩存計算結果（高級）

若該方法被頻繁調用：

```csharp
private Dictionary<TeamSide, Dictionary<ActionType, int>> _scoringBreakdownCache;
private int _lastEventCount;

public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
{
    // 檢查緩存是否有效
    int currentEventCount = _eventManager.GetEventCount();
    if (_lastEventCount != currentEventCount)
    {
        // 事件清單已變化，重新計算並緩存
        _scoringBreakdownCache = ComputeAllBreakdowns();
        _lastEventCount = currentEventCount;
    }
    
    return _scoringBreakdownCache[team];
}

private Dictionary<TeamSide, Dictionary<ActionType, int>> ComputeAllBreakdowns()
{
    // 計算並返回所有隊伍的得分統計
    // ...
}
```

---

## 驗證與測試

### ✅ 編譯狀態
```
在 4.5 秒內建置 成功
MyProject net10.0 成功 → bin\Debug\net10.0\MyProject.dll
```

### 性能基準

**假設比賽中有 200 個事件：**

| 操作 | 原始實現 | 優化後 | 改進 |
|------|---------|--------|------|
| GetTeamScoringBreakdown | 800 個物件複製 | 200 個 | **75%** |
| GetTeamErrorBreakdown | 1000 個物件複製 | 200 個 | **80%** |
| 總內存節省 | 1800 物件 | 400 物件 | **78%** |

---

## 受影響的代碼位置

| 檔案 | 方法 | 狀態 | 變更 |
|------|------|------|------|
| `StatisticsEngine.cs` | `GetTeamScoringBreakdown` | ✅ 已優化 | 加入 `var allEvents` 快取 |
| `StatisticsEngine.cs` | `GetTeamErrorBreakdown` | ✅ 已優化 | 加入 `var allEvents` 快取 |

---

## 建議後續行動

### 立即可行（已完成）
- ✅ 優化 `GetTeamScoringBreakdown` 和 `GetTeamErrorBreakdown`

### 短期建議（1-2 周）
- [ ] 在 `EventManager` 中實現 `GetAllEventsReadOnly()`
- [ ] 更新 `StatisticsEngine` 使用新方法
- [ ] 性能基準測試

### 中期建議（1 個月）
- [ ] 評估緩存策略的必要性
- [ ] 考慮其他高頻查詢的優化

---

## 設計原則

### 現有優化遵循的原則
1. **最小化副本** - 只在必要時複製
2. **遵守 SOLID**
   - S (Single Responsibility) - EventManager 管理事件存儲
   - O (Open/Closed) - 支援擴展而無需修改核心
   - D (Dependency Inversion) - 透過介面依賴
3. **向後相容** - 現有 API 保持不變

### 推薦進階優化遵循的原則
1. **唯讀保護** - 提供安全的無複製存取
2. **漸進式強化** - 先優化，後緩存
3. **性能可測** - 提供基準點進行測量

---

## 相關文檔

- [ARCHITECTURE.md](./ARCHITECTURE.md) - 架構設計
- [API_REFERENCE.md](./API_REFERENCE.md) - API 參考
- [IMPROVEMENT_SUMMARY.md](./IMPROVEMENT_SUMMARY.md) - 先前的 API 改進

---

**優化完成並驗證編譯成功！建議評估第 2 階段的唯讀枚舉實現。** ✅
