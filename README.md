# 排球戰術數據記錄系統 (Volleyball Scouting & Trend Analyzer)

## 🏐 項目概述

這是一個為排球比賽設計的 **專業數據記錄與分析系統**。系統用於即時記錄比賽中的所有關鍵事件，自動計分，並提供深入的統計分析和趨勢預測功能。

**技術棧：** C# .NET 10.0 WinForms  
**開發階段：** 核心邏輯層完成 ✅

---

## 🎯 核心功能

### ✅ 事件管理系統
- 記錄比賽中的所有動作（發球、攻擊、攔網、防守等）
- 支持 **撤銷/重做** 操作
- 按球員、動作、局數查詢事件
- 與 UI 層完全解耦（基於事件驅動架構）

### ✅ 自動評分系統
- 根據事件類型自動計算比分
- 支持 **雙軌制計分** (25 分先勝制，需差 2 分)
- 三先勝制判定 (先贏 2 局者獲勝)
- 實時比分更新通知

### ✅ 統計引擎
- **個人統計：** 攻擊成功率、發球成功率、得分貢獻、失誤計數
- **隊伍統計：** 隊伍整體表現分析
- **得分來源分析：** 攻擊、攔網、發球各類型得分統計
- **趨勢分析：** 比分進度曲線、失誤密集點檢測
- **統計報告：** 自動生成詳細文字報告

### ✅ 資料導出
- CSV 格式導出事件清單
- CSV 格式導出統計報告
- 完整的 CSV 轉義處理

---

## 📁 項目結構

```
src/MyProject/
├── Models/                      # 數據模型層
│   ├── ActionType.cs           # 動作類型 (18 種)
│   ├── Player.cs               # 球員
│   ├── GameEvent.cs            # 比賽事件
│   ├── Team.cs                 # 隊伍
│   └── Match.cs                # 比賽
│
├── Services/                    # 業務邏輯層
│   ├── EventManager.cs         # 事件管理 (新增/撤銷/重做)
│   ├── ScoringService.cs       # 評分邏輯 (自動計分)
│   └── StatisticsEngine.cs     # 統計分析
│
├── Utilities/                   # 工具層
│   └── CsvExporter.cs          # CSV 導出
│
└── Program.cs                  # 演示程序

文檔:
├── ARCHITECTURE.md             # 完整架構設計
├── API_REFERENCE.md            # API 快速參考
├── PROJECT_STATUS.md           # 項目狀態報告
└── README.md                   # 本文件
```

---

## 🚀 快速開始

### 1. 編譯項目

```bash
cd src\MyProject
dotnet build
```

### 2. 運行演示程序

```bash
dotnet run
```

**演示輸出：**
```
=== 排球戰術數據記錄系統 ===

[第 1 步] 建立隊伍和球員...
✓ 主隊: 台北虎隊 (6 名球員)
✓ 客隊: 高雄鷹隊 (6 名球員)

[第 2 步] 建立比賽...
✓ 比賽開始: 台北虎隊 vs 高雄鷹隊 - 局: 1 - 狀態: InProgress

[第 3 步] 初始化服務層...
✓ 服務層初始化完成

[第 4 步] 記錄比賽事件 (模擬前 10 個回合)...
  → 事件已記錄: [10:59:24] 球員 #1 - ServeSuccess - 比分: 0-0
  ✓ 比分更新: 1-0
  → 事件已記錄: [10:59:24] 球員 #2 - AttackSuccess - 比分: 1-0
  ✓ 比分更新: 1-1
  ...

[第 5 步] 統計分析結果...
主隊 (台北虎隊):
  攻擊成功率: 66.67%
  發球成功率: 100.00%

[第 6 步] 導出數據...
✓ 事件資料已導出至: C:\Users\User\Desktop\volleyball_events.csv
✓ 統計資料已導出至: C:\Users\User\Desktop\volleyball_statistics.csv
```

---

## 📚 使用示例

### 基本使用

```csharp
// 1. 建立隊伍和比賽
var homeTeam = new Team(TeamSide.Home, "主隊");
homeTeam.AddPlayer(new Player(1, "球員1", "主攻", TeamSide.Home));

var match = new Match(homeTeam, awayTeam);
var eventManager = new EventManager();
var scoring = new ScoringService(match, eventManager);

// 2. 訂閱事件通知
scoring.ScoreUpdated += (_, score) => 
    Console.WriteLine($"比分: {score}");

// 3. 記錄事件並自動計分
var evt = new GameEvent(
    playerId: 1,
    action: ActionType.AttackSuccess,
    score: "0-0",
    team: TeamSide.Home,
    setNumber: 1
);
eventManager.AddEvent(evt);
scoring.ProcessGameEvent(evt);  // 比分自動更新為 1-0

// 4. 撤銷操作
if (eventManager.CanUndo()) {
    eventManager.Undo();  // 比分恢復為 0-0
}

// 5. 統計分析
var stats = new StatisticsEngine(eventManager, match);
double attackRate = stats.GetTeamAttackSuccessRate(TeamSide.Home);
var trendData = stats.GetScoreTrendData();

// 6. 導出數據
CsvExporter.ExportEventsToCSV(eventManager, match, "events.csv");
CsvExporter.ExportStatisticsToCSV(stats, match, eventManager, "stats.csv");
```

### 在 WinForms 中集成

```csharp
public partial class MainForm : Form {
    private ScoringService _scoring;
    
    public MainForm() {
        InitializeComponent();
        
        _scoring = new ScoringService(match, eventManager);
        _scoring.ScoreUpdated += UpdateUI;
    }
    
    private void UpdateUI(object? sender, string score) {
        scoreLabel.Text = score;
    }
}
```

---

## 🏆 設計特點

### 1. **完全解耦的架構**
- 邏輯層無須知道 UI 實現細節
- 通過委派和事件實現通信
- 支援多種 UI 框架（WinForms、WPF、Web 等）

### 2. **強型別安全**
- 所有動作類型使用 `ActionType` 枚舉
- 編譯時類型檢查，減少運行時錯誤

### 3. **單一職責原則**
- `EventManager` 只管理事件
- `ScoringService` 只處理計分邏輯
- `StatisticsEngine` 只提供統計功能

### 4. **完整的撤銷/重做**
- 基於棧結構實現
- 支援查詢是否可操作

### 5. **無外部依賴**
- 純 .NET 實現
- 便於測試和部署

---

## 📖 文檔

| 文檔 | 說明 |
|------|------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | 完整的架構設計、設計原則、工作流程詳解 |
| [API_REFERENCE.md](./API_REFERENCE.md) | API 快速參考、代碼示例、數據模型文檔 |
| [PROJECT_STATUS.md](./PROJECT_STATUS.md) | 項目狀態、功能清單、開發建議 |

---

## 💡 支援的動作類型

| 類別 | 動作 |
|------|------|
| **發球** | ServeSuccess, ServeFault |
| **攻擊** | AttackSuccess, AttackFault, AttackBlocked, AttackOutOfBounds |
| **攔網** | BlockSuccess, BlockFault |
| **防守** | ReceiveSuccess, ReceiveFault |
| **傳球** | TossSuccess, TossFault |
| **其他** | TeamScore, Substitution, Timeout, TechnicalFault, Other |

---

## 📊 計分規則

### 得分條件
- ✅ 攻擊成功 → 得分
- ✅ 攔網成功 → 得分  
- ✅ 發球成功 → 得分
- ✅ 對方失誤 → 得分

### 局制規則
- ✅ 25 分先勝制
- ✅ 需領先 2 分才能結束本局
- ✅ 三先勝制（先贏 2 局者獲勝）

---

## ✨ 後續功能規劃

- [ ] WinForms UI 界面
- [ ] 實時數據可視化圖表
- [ ] 熱力圖分析（場地位置分析）
- [ ] 對手數據比較
- [ ] 資料庫集成
- [ ] 雲端備份

---

## 🛠️ 技術要求

- **.NET 版本:** .NET 10.0
- **C# 版本:** 12.0
- **平台:** Windows / Linux / macOS
- **依賴項:** 無外部依賴

---

## 📞 支持

**問題反饋：** 請查閱 [ARCHITECTURE.md](./ARCHITECTURE.md) 中的 API 文檔或 [API_REFERENCE.md](./API_REFERENCE.md) 中的快速參考。

---

**版本：** 1.0.0 (核心邏輯層)  
**最後更新：** 2026-05-14  
**狀態：** ✅ 完成並測試

---

**核心邏輯層開發完成，等待 UI 層集成！** 🚀