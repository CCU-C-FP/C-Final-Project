# API 設計改進：球員背號重複問題修正

## 問題描述

在原始設計中，`StatisticsEngine` 中的個人統計 API 只按 `playerId` 進行篩選，而沒有考慮 `TeamSide`：

```csharp
// 原始實現 - 有問題！
public double GetPlayerAttackSuccessRate(int playerId)
{
    var attacks = _eventManager.GetEventsByPlayer(playerId)
        .Where(e => e.Action == ActionType.AttackSuccess || ...)
        .ToList();
    // ... 返回成功率
}
```

### 風險場景

當兩隊球員背號相同時（如演示程式的 1-6 都重複），統計會混淆：

```
主隊球員 #1 (陳明昊) 的統計
+ 客隊球員 #1 (吳昆遠) 的統計
= 混淆的結果！
```

---

## 解決方案

### 受影響的方法

1. **`GetPlayerAttackSuccessRate`**
   ```csharp
   // 舊簽名
   public double GetPlayerAttackSuccessRate(int playerId)
   
   // 新簽名
   public double GetPlayerAttackSuccessRate(int playerId, TeamSide team)
   ```

2. **`GetPlayerServeSuccessRate`**
   ```csharp
   // 舊簽名
   public double GetPlayerServeSuccessRate(int playerId)
   
   // 新簽名
   public double GetPlayerServeSuccessRate(int playerId, TeamSide team)
   ```

3. **`GetPlayerErrorCount`**
   ```csharp
   // 舊簽名
   public int GetPlayerErrorCount(int playerId)
   
   // 新簽名
   public int GetPlayerErrorCount(int playerId, TeamSide team)
   ```

### 實現改進

所有方法現在都添加了 `Team` 過濾：

```csharp
public double GetPlayerAttackSuccessRate(int playerId, TeamSide team)
{
    var attacks = _eventManager.GetEventsByPlayer(playerId)
        .Where(e => e.Team == team &&  // ← 關鍵改進
                   (e.Action == ActionType.AttackSuccess || 
                    e.Action == ActionType.AttackFault ||
                    e.Action == ActionType.AttackBlocked))
        .ToList();

    if (attacks.Count == 0)
        return 0;

    int successful = attacks.Count(e => e.Action == ActionType.AttackSuccess);
    return (double)successful / attacks.Count * 100;
}
```

---

## 影響範圍

### 代碼變動

| 文件 | 變動內容 | 狀態 |
|------|---------|------|
| `StatisticsEngine.cs` | 3 個方法簽名變更、實現修改 | ✅ 完成 |
| `CsvExporter.cs` | 4 個調用點更新 | ✅ 完成 |
| `Program.cs` | 演示代碼更新、新增個人統計演示 | ✅ 完成 |
| `ARCHITECTURE.md` | API 文檔更新 | ✅ 完成 |
| `API_REFERENCE.md` | API 簽名更新、示例代碼更新 | ✅ 完成 |
| `PROJECT_STATUS.md` | 改進記錄、版本日誌 | ✅ 完成 |

### 向後相容性

⚠️ **破壞性變更**：任何調用這三個方法的代碼都需要更新。

**遷移指南：**

```csharp
// 舊代碼
double rate = stats.GetPlayerAttackSuccessRate(1);

// 新代碼
double rate = stats.GetPlayerAttackSuccessRate(1, TeamSide.Home);
```

---

## 驗證

### CSV 導出驗證

球員統計現在正確分離了兩隊的數據：

```csv
隊伍,球員背號,球員名稱,攻擊成功率(%),發球成功率(%),得分貢獻,失誤次數
台北虎隊,1,陳明昊,50.00,100.00,1,1
...
高雄鷹隊,1,吳昆遠,0.00,0.00,0,0
```

### 演示程序輸出

```
── 球員個人統計（前 3 名主要球員）──
主隊 - 陳明昊 (#1):
  攻擊成功率: 50.00%
  發球成功率: 100.00%
  失誤次數: 1

客隊 - 張駿昊 (#2):
  攻擊成功率: 100.00%
  發球成功率: 100.00%
  失誤次數: 0
```

✅ 統計數據正確隔離

---

## 設計考慮

### 為什麼不使用全域唯一 PlayerId？

1. **現實排球規則**：球員背號在隊伍內唯一，跨隊可重複
2. **資料建模**：`Player` 物件已正確包含 `TeamSide` 屬性
3. **向後相容**：改進 API 簽名比改變數據模型影響更小

### 最佳實踐

1. **始終指定上下文**
   - ✅ 個人統計必須指定 `TeamSide`
   - ✅ 隊伍統計已內置 `TeamSide`

2. **一致的 API 設計**
   - ✅ `GetPlayerScoresTotals` 已包含 `TeamSide`（保持不變）
   - ✅ 所有個人統計方法現已統一

3. **防禦性編程**
   - ✅ 杜絕歧義
   - ✅ 編譯時檢查

---

## 相關資源

- [ARCHITECTURE.md](./ARCHITECTURE.md) - 完整架構文檔
- [API_REFERENCE.md](./API_REFERENCE.md) - API 快速參考
- [PROJECT_STATUS.md](./PROJECT_STATUS.md) - 項目狀態報告

---

**感謝指出此設計缺陷！改進已完成並通過測試。** ✅
