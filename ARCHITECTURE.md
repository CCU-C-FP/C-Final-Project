# 排球戰術數據記錄系統 - 架構文檔

## 項目概述

「排球戰術數據記錄系統 (Volleyball Scouting & Trend Analyzer)」是一個 WinForms 應用程式，用於記錄和分析排球比賽中的所有關鍵事件。本文檔描述核心邏輯層的架構設計。

---

## 核心設計原則

### 1. **解耦設計（Separation of Concerns）**
- **模型層（Models）**：純資料定義，不包含業務邏輯
- **服務層（Services）**：業務邏輯和規則引擎
- **工具層（Utilities）**：資料導出和轉換

### 2. **強型別安全**
- 使用 `Enum` 替代字串（如 `ActionType`、`TeamSide`）
- 編譯時類型檢查，減少運行時錯誤

### 3. **事件驅動 & 解耦通知**
- 使用委派和事件機制，與 UI 層完全解耦
- UI 層通過訂閱事件獲得通知，無需直接依賴業務邏輯

### 4. **不可逆操作支持**
- 撤銷/重做功能通過棧結構實現
- 確保所有操作可追蹤

---

## 項目結構

```
MyProject/
├── Models/                    # 資料模型層
│   ├── ActionType.cs         # 動作類型枚舉
│   ├── Player.cs             # 球員類別
│   ├── GameEvent.cs          # 比賽事件
│   ├── Team.cs               # 隊伍類別
│   └── Match.cs              # 比賽類別
│
├── Services/                  # 服務層（業務邏輯）
│   ├── EventManager.cs       # 事件管理器（新增、撤銷、重做）
│   ├── ScoringService.cs     # 評分服務（自動計分、局結束判定）
│   └── StatisticsEngine.cs   # 統計引擎（成功率、趨勢分析）
│
├── Utilities/                 # 工具層
│   └── CsvExporter.cs        # CSV 資料導出
│
└── Program.cs                # 主程式（演示示例）
```

---

## 核心類別詳解

### Models 層

#### **ActionType（動作類型）**
強型別定義所有排球動作：
- 發球相關：`ServeSuccess`、`ServeFault`
- 攻擊相關：`AttackSuccess`、`AttackFault`、`AttackBlocked`
- 攔網相關：`BlockSuccess`、`BlockFault`
- 防守相關：`ReceiveSuccess`、`ReceiveFault`
- 傳球相關：`TossSuccess`、`TossFault`

#### **Player（球員）**
```csharp
public class Player {
    public int JerseyNumber { get; set; }      // 背號（唯一識別符）
    public string Name { get; set; }           // 姓名
    public string Position { get; set; }       // 位置
    public int Height { get; set; }            // 身高
    public bool IsActive { get; set; }         // 是否在場
    public TeamSide Team { get; set; }         // 所屬隊伍
}
```

#### **GameEvent（比賽事件）**
```csharp
public class GameEvent {
    public DateTime Timestamp { get; set; }    // 事件時刻
    public int PlayerId { get; set; }          // 執行者背號（0 = 團隊事件）
    public ActionType Action { get; set; }     // 動作類型
    public string Score { get; set; }          // 事件時的比分
    public TeamSide Team { get; set; }         // 所屬隊伍
    public int SetNumber { get; set; }         // 局數
    public string Notes { get; set; }          // 備註
}
```

#### **Team（隊伍）**
- 管理隊伍的所有球員
- 維護雙軌制計分（每局一個得分）
- 追蹤贏得的局數

#### **Match（比賽）**
- 管理兩支隊伍和比賽狀態
- 追蹤目前局數
- 提供比分查詢接口

---

### Services 層

#### **EventManager（事件管理器）**
**職責：**
- 維護事件清單（`List<GameEvent>`）
- 支援撤銷/重做操作（使用 Stack）
- 提供事件查詢接口

**主要方法：**
```csharp
public void AddEvent(GameEvent gameEvent)              // 新增事件
public bool Undo()                                      // 撤銷
public bool Redo()                                      // 重做
public List<GameEvent> GetEventsByPlayer(int playerId) // 按球員查詢
public List<GameEvent> GetEventsByAction(ActionType)   // 按動作查詢
public List<GameEvent> GetEventsBySet(int setNumber)   // 按局數查詢
```

**事件通知：**
```csharp
public event EventHandler<GameEvent>? EventAdded;      // 事件已新增
public event EventHandler<GameEvent>? EventUndone;     // 事件已撤銷
public event EventHandler<GameEvent>? EventRedone;     // 事件已重做
```

**使用示例：**
```csharp
var manager = new EventManager();

// 訂閱事件
manager.EventAdded += (sender, evt) => Console.WriteLine($"事件: {evt}");

// 新增事件
manager.AddEvent(new GameEvent(...));

// 撤銷
manager.Undo();
```

---

#### **ScoringService（評分服務）**
**職責：**
- 根據事件自動計算比分
- 判定局/比賽結束
- 實現雙軌制計分規則（25 分先勝制，需差 2 分）

**主要方法：**
```csharp
public void ProcessGameEvent(GameEvent gameEvent)      // 處理事件並更新比分
public void SetTeamScore(TeamSide team, int score)     // 手動設定比分
public string GetCurrentScore()                        // 取得當前比分
public string GetDetailedScore()                       // 取得詳細局數比分
```

**事件通知：**
```csharp
public event EventHandler<int>? SetFinished;           // 局已結束
public event EventHandler<TeamSide>? MatchFinished;    // 比賽已結束
public event EventHandler<string>? ScoreUpdated;       // 比分已更新
```

**計分規則：**
- 攻擊得分、攔網得分、發球成功 → 該隊得 1 分
- 攻擊失誤、攔網失誤、發球失誤 → 對方隊得 1 分
- 每局 25 分先勝制，需領先 2 分
- 三先勝制（先贏 2 局者獲勝）

**使用示例：**
```csharp
var scoring = new ScoringService(match, eventManager);

// 訂閱事件
scoring.ScoreUpdated += (sender, score) => 
    Console.WriteLine($"比分: {score}");

// 處理比賽事件
var evt = new GameEvent(1, ActionType.AttackSuccess, ...);
scoring.ProcessGameEvent(evt);
```

---

#### **StatisticsEngine（統計引擎）**
**職責：**
- 計算各類成功率指標
- 分析得分和失誤來源
- 提供趨勢分析數據

**主要方法：**
```csharp
// 個人統計
public double GetPlayerAttackSuccessRate(int playerId, TeamSide team)
public double GetPlayerServeSuccessRate(int playerId, TeamSide team)
public int GetPlayerScoresTotals(int playerId, TeamSide team)
public int GetPlayerErrorCount(int playerId, TeamSide team)

// 隊伍統計
public double GetTeamAttackSuccessRate(TeamSide team)
public double GetTeamServeSuccessRate(TeamSide team)
public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
public Dictionary<ActionType, int> GetTeamErrorBreakdown(TeamSide team)

// 趨勢分析
public List<(int Time, int HomeScore, int AwayScore)> GetScoreTrendData()
public List<int> GetErrorClusterPoints(TeamSide team, int windowSize = 5)

// 報告
public string GenerateStatisticsReport()
```

**使用示例：**
```csharp
var stats = new StatisticsEngine(eventManager, match);

// 取得攻擊成功率
double rate = stats.GetTeamAttackSuccessRate(TeamSide.Home);

// 取得得分來源
var breakdown = stats.GetTeamScoringBreakdown(TeamSide.Home);
Console.WriteLine($"攻擊得分: {breakdown[ActionType.AttackSuccess]}");
```

---

### Utilities 層

#### **CsvExporter（CSV 導出工具）**
**職責：**
- 將事件清單導出為 CSV
- 將統計報告導出為 CSV

**主要方法：**
```csharp
public static bool ExportEventsToCSV(
    EventManager eventManager, 
    Match match, 
    string filePath)

public static bool ExportStatisticsToCSV(
    StatisticsEngine statistics, 
    Match match, 
    EventManager eventManager, 
    string filePath)
```

**CSV 格式：**

事件資料：
```
時刻,球員背號,動作類型,隊伍,局數,比分,備註
2026-05-14 10:59:24,1,ServeSuccess,Home,1,1-0,
2026-05-14 10:59:24,2,AttackSuccess,Away,1,1-1,
```

統計資料：
```
隊伍,攻擊成功率(%),發球成功率(%)
台北虎隊,66.67,100.00
高雄鷹隊,100.00,100.00
```

---

## 工作流程示例

### 1. 初始化比賽
```csharp
// 建立隊伍和球員
var homeTeam = new Team(TeamSide.Home, "主隊名稱");
homeTeam.AddPlayer(new Player(1, "球員1", "主攻", TeamSide.Home));

var awayTeam = new Team(TeamSide.Away, "客隊名稱");
awayTeam.AddPlayer(new Player(1, "球員2", "主攻", TeamSide.Away));

// 建立比賽
var match = new Match(homeTeam, awayTeam);
match.StartMatch();
```

### 2. 記錄事件並自動計分
```csharp
var eventManager = new EventManager();
var scoring = new ScoringService(match, eventManager);

// 記錄球員 #1 發球成功
var evt = new GameEvent(1, ActionType.ServeSuccess, "0-0", TeamSide.Home, 1);
eventManager.AddEvent(evt);
scoring.ProcessGameEvent(evt);
// 比分自動更新為 1-0
```

### 3. 統計和分析
```csharp
var stats = new StatisticsEngine(eventManager, match);

// 取得統計數據
var report = stats.GenerateStatisticsReport();
var trendData = stats.GetScoreTrendData();
```

### 4. 導出數據
```csharp
CsvExporter.ExportEventsToCSV(eventManager, match, "events.csv");
CsvExporter.ExportStatisticsToCSV(stats, match, eventManager, "stats.csv");
```

---

## UI 集成指南

### 與 WinForms 解耦

**不推薦的做法：**
```csharp
//  UI 直接調用服務
class MainForm : Form {
    private ScoringService _scoring;
    
    void OnButtonClick() {
        // UI 直接修改業務邏輯 - 耦合度高
        _scoring.SetTeamScore(TeamSide.Home, 25);
    }
}
```

**推薦的做法：**
```csharp
// ✓ UI 通過事件訂閱
class MainForm : Form {
    private ScoringService _scoring;
    
    public MainForm() {
        // 訂閱事件
        _scoring.ScoreUpdated += UpdateScoreDisplay;
    }
    
    private void UpdateScoreDisplay(object? sender, string score) {
        // 在 UI 線程更新顯示
        if (InvokeRequired) {
            Invoke(() => scoreLabel.Text = score);
        } else {
            scoreLabel.Text = score;
        }
    }
}
```

### 事件訂閱模式

```csharp
// 在 WinForms 中訂閱事件
eventManager.EventAdded += (sender, evt) => {
    if (this.InvokeRequired) {
        this.Invoke(() => {
            eventListBox.Items.Add(evt.ToString());
        });
    }
};

scoring.SetFinished += (sender, setWinner) => {
    MessageBox.Show(
        setWinner == 0 ? "主隊贏得本局" : "客隊贏得本局"
    );
};
```

---

## 擴展建議

### 1. 新增業務規則
在 `ScoringService` 中擴展 `ProcessGameEvent` 方法：
```csharp
// 例如：新增排球規則（快速進攻得分等）
case ActionType.QuickAttackSuccess:
    // 特殊計分邏輯
    break;
```

### 2. 新增統計指標
在 `StatisticsEngine` 中新增方法：
```csharp
public double GetTeamBlockSuccessRate(TeamSide team) {
    // 計算攔網成功率
}
```

### 3. 新增資料源
建立新的 Exporter 類別（如 JSON、XML）：
```csharp
public class JsonExporter {
    public static bool ExportEventsToJson(...) { }
}
```

### 4. 實時更新 UI
使用 `BackgroundWorker` 或 `async/await` 處理事件：
```csharp
private async void RecordEventAsync() {
    var evt = new GameEvent(...);
    await Task.Run(() => {
        eventManager.AddEvent(evt);
        scoring.ProcessGameEvent(evt);
    });
}
```

---

## 最佳實踐

1. **始終使用 Enum 而非字串**
   - 用`ActionType.AttackSuccess`
   - 不用`"AttackSuccess"`

2. **使用委派通知而非直接回調**
   - 保持邏輯層和 UI 層的解耦

3. **每個類別單一職責**
   - `EventManager` 只管理事件
   - `ScoringService` 只處理計分邏輯
   - `StatisticsEngine` 只提供統計

4. **驗證輸入數據**
   ```csharp
   public void AddEvent(GameEvent gameEvent) {
       if (gameEvent == null)
           throw new ArgumentNullException(nameof(gameEvent));
       // ...
   }
   ```

5. **記錄充分的文檔註釋**
   - 每個公開方法都應有 XML 文檔註釋

---

## 總結

本系統采用 **分層架構設計**，確保：
- ✓ 邏輯層與 UI 層完全解耦
- ✓ 強型別安全的代碼
- ✓ 易於測試和維護
- ✓ 支援撤銷/重做等復雜操作
- ✓ 靈活的統計和分析功能

所有核心功能已實現，可直接集成到 WinForms UI 層。
