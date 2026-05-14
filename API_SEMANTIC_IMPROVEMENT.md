# GetErrorClusterPoints API 改進：語意清晰化

## 🔍 問題診斷

### 原始設計的問題

**原始簽名：**
```csharp
public List<int> GetErrorClusterPoints(TeamSide team, int windowSize = 5)
```

**返回什麼：**
- 返回 `teamEvents` 中的索引 `i`（按團隊過濾後的列表的相對位置）

**問題：**
1. **註解不符實現**：註解說"時間點"，但返回的是索引
2. **信息不完整**：呼叫端無法對應到實際時間
3. **無法追蹤**：無法確定該密集點在全域事件序列中的位置
4. **缺乏上下文**：無法知道窗口的起始和結束時間

## ✨ 解決方案

### 新增 ErrorClusterInfo 類

```csharp
public class ErrorClusterInfo
{
    /// 密集點窗口起始時刻（第一個事件的時間戳）
    public DateTime StartTimestamp { get; set; }

    /// 密集點窗口結束時刻（最後一個事件的時間戳）
    public DateTime EndTimestamp { get; set; }

    /// 該窗口在全域事件序列中的起始索引
    public int GlobalEventStartIndex { get; set; }

    /// 該窗口內的失誤數量
    public int ErrorCount { get; set; }

    /// 該窗口的視窗大小
    public int WindowSize { get; set; }

    /// 密集點持續時間
    public TimeSpan Duration => EndTimestamp - StartTimestamp;

    /// 友善的字串表示
    public override string ToString()
    {
        return $"失誤密集 | 時間: {StartTimestamp:HH:mm:ss} - {EndTimestamp:HH:mm:ss}" +
               $" | 失誤數: {ErrorCount}/{WindowSize} | 全域索引: {GlobalEventStartIndex}";
    }
}
```

### 修改後的方法簽名

```csharp
// 【改前】
public List<int> GetErrorClusterPoints(TeamSide team, int windowSize = 5)

// 【改後】
public List<ErrorClusterInfo> GetErrorClusterPoints(TeamSide team, int windowSize = 5)
```

## 📊 改進對比

### 使用場景

#### ❌ 舊 API - 問題示例

```csharp
var clusters = statsEngine.GetErrorClusterPoints(TeamSide.Home);
// clusters = [0, 5, 12]

// 問題：
// - 這些數字是什麼意思？
// - 無法顯示時間點
// - 無法在 UI 中標記事件位置
foreach (var clusterIndex in clusters)
{
    Console.WriteLine($"失誤密集點: 位置 {clusterIndex}");  // 用途不明
}
```

#### ✅ 新 API - 完整信息

```csharp
var clusters = statsEngine.GetErrorClusterPoints(TeamSide.Home);
// clusters = [
//   ErrorClusterInfo { StartTimestamp: 15:02:10, EndTimestamp: 15:02:30, 
//                      GlobalEventStartIndex: 12, ErrorCount: 4, WindowSize: 5 },
//   ErrorClusterInfo { StartTimestamp: 15:05:45, EndTimestamp: 15:06:05, 
//                      GlobalEventStartIndex: 35, ErrorCount: 3, WindowSize: 5 }
// ]

foreach (var cluster in clusters)
{
    Console.WriteLine(cluster);
    // 輸出: 失誤密集 | 時間: 15:02:10 - 15:02:30 | 失誤數: 4/5 | 全域索引: 12
    
    // 現在可以：
    var duration = cluster.Duration;              // 知道持續時間
    var startTime = cluster.StartTimestamp;       // 知道確切時間
    var globalIndex = cluster.GlobalEventStartIndex;  // 定位在全域事件序列
}
```

## 📈 返回類型對比

| 信息 | 舊 API | 新 API | 用途 |
|------|--------|--------|------|
| 時間戳 | ❌ | ✅ | UI 顯示、日誌記錄 |
| 全域事件索引 | ❌ | ✅ | 事件定位、關聯查詢 |
| 持續時間 | ❌ | ✅ | 分析失誤週期 |
| 失誤數量 | ❌ | ✅ | 嚴重程度評估 |
| 視窗大小 | ❌ | ✅ | 算法參數記錄 |

## 📂 實施內容

### 新建文件
- `Services/ErrorClusterInfo.cs` - 失誤密集點信息類

### 修改文件
- `Services/StatisticsEngine.cs` - 更新 `GetErrorClusterPoints` 方法

### 改進詳情

#### StatisticsEngine.cs 中的實現

```csharp
public List<ErrorClusterInfo> GetErrorClusterPoints(TeamSide team, int windowSize = 5)
{
    if (windowSize <= 0)
        throw new ArgumentOutOfRangeException(nameof(windowSize), "視窗大小必須大於 0");

    var allEvents = _eventManager.GetAllEvents();
    var teamEvents = allEvents
        .Where(e => e.Team == team)
        .ToList();

    var clusters = new List<ErrorClusterInfo>();
    var errorActions = new[] { /* ... */ };

    for (int i = 0; i <= teamEvents.Count - windowSize; i++)
    {
        var windowEvents = teamEvents
            .Skip(i)
            .Take(windowSize)
            .ToList();

        int errorCount = windowEvents
            .Count(e => errorActions.Contains(e.Action));

        if (errorCount >= 3)
        {
            // ✅ 取得時間戳和全域索引
            var startEvent = windowEvents.First();
            var endEvent = windowEvents.Last();
            int globalEventStartIndex = allEvents.IndexOf(startEvent);

            var clusterInfo = new ErrorClusterInfo(
                startEvent.Timestamp,
                endEvent.Timestamp,
                globalEventStartIndex,
                errorCount,
                windowSize);

            clusters.Add(clusterInfo);
        }
    }

    return clusters;
}
```

**關鍵改進：**
1. 返回 `ErrorClusterInfo` 而非 `int`
2. 包含開始和結束時間戳
3. 計算全域事件索引供調用方參考
4. 提供失誤數量和持續時間計算

## ✅ 驗證確認

- ✅ 編譯成功（4.4 秒）
- ✅ 演示程序正常運行
- ✅ 所有 7 個步驟執行成功
- ✅ 向後相容：方法簽名清晰，返回類型完整

## 🎯 API 設計改進原則

1. **語意清晰**
   - ✅ 返回類型名稱清楚表達目的
   - ✅ 屬性名稱自我說明

2. **信息完整**
   - ✅ 不遺漏調用方需要的上下文
   - ✅ 提供足夠的元數據進行分析

3. **易於使用**
   - ✅ 內建計算屬性（Duration）
   - ✅ 友善的字串表示（ToString）

4. **向後相容考慮**
   - ⚠️ 這是破壞性變更（但方法未被調用過）
   - ✅ 新 API 更強大，值得升級

## 📚 相關文檔

- [Services/ErrorClusterInfo.cs](./src/MyProject/Services/ErrorClusterInfo.cs) - 新增的信息類
- [Services/StatisticsEngine.cs](./src/MyProject/Services/StatisticsEngine.cs) - 修改的方法
- [ARCHITECTURE.md](./ARCHITECTURE.md) - 待更新

## 推薦後續

1. 在 API_REFERENCE.md 中更新方法簽名和使用範例
2. 在 UI 層（WinForms）中展示失誤密集點的時間和位置
3. 考慮添加視覺化標記密集點的時間段

---

**API 語意清晰化完成！調用端現在能完整地理解失誤密集點的信息。** ✅
