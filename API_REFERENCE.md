# API 參考

本文檔對應目前 `src/MyProject` 的實作，重點整理事件管理、計分、統計與導出工具的公開 API。

## 命名空間

- `MyProject.Models`
- `MyProject.Services`
- `MyProject.Utilities`

## 快速總覽

| 類別 | 角色 |
|------|------|
| `EventManager` | 管理事件清單、撤銷、重做與事件通知 |
| `ScoringService` | 根據事件自動更新比分與局數 |
| `StatisticsEngine` | 計算個人、隊伍、趨勢與失誤集群統計 |
| `CsvExporter` | 匯出事件與統計報告為 CSV |
| `GameEvent` / `Player` / `Team` / `Match` | 核心資料模型 |

## EventManager

負責維護比賽事件，並用事件通知讓 UI 或其他服務層同步狀態。

### 建構子

```csharp
var eventManager = new EventManager();
```

### 事件

- `EventAdded`
- `EventUndone`
- `EventRedone`
- `EventsCleared`

### 方法

```csharp
void AddEvent(GameEvent gameEvent)
bool Undo()
bool Redo()
List<GameEvent> GetAllEvents()
List<GameEvent> GetEventsByPlayer(int playerId)
List<GameEvent> GetEventsByAction(ActionType action)
List<GameEvent> GetEventsBySet(int setNumber)
void ClearAllEvents()
int GetEventCount()
bool CanUndo()
bool CanRedo()
```

### 行為重點

- `AddEvent` 會清空 redo 棧。
- `GetAllEvents` 回傳副本，不直接暴露內部集合。
- `ClearAllEvents` 會觸發 `EventsCleared`，適合讓 `ScoringService` 重算。

## ScoringService

負責把事件轉成比分變化，並處理雙軌制局分與三先勝制比賽結束條件。

### 建構子

```csharp
var scoringService = new ScoringService(match, eventManager);
```

### 事件

- `ScoreUpdated`：比分更新時觸發，回傳新的比分字串。
- `SetFinished`：單局結束時觸發，回傳獲勝方編號，`0` 代表主隊，`1` 代表客隊。
- `MatchFinished`：比賽結束時觸發，回傳獲勝隊伍 `TeamSide`。

### 方法

```csharp
void ProcessGameEvent(GameEvent gameEvent)
void SetTeamScore(TeamSide team, int score)
string GetCurrentScore()
string GetDetailedScore()
```

### 規則

- `AttackSuccess`、`BlockSuccess`、`ServeSuccess` 視為得分事件。
- `AttackFault`、`BlockFault`、`ServeFault`、`ReceiveFault`、`TossFault`、`AttackOutOfBounds` 視為對方得分。
- `TeamScore` 直接為指定隊伍加分。
- `Timeout`、`Substitution`、`TechnicalFault`、`Other` 不直接改變比分。
- 單局採 25 分先勝制，且需領先 2 分。
- 比賽採先贏 2 局者獲勝。

### 同步機制

`ScoringService` 會訂閱 `EventManager` 的 `EventUndone`、`EventRedone`、`EventsCleared`，在事件歷史改變時重新計算比分。

## StatisticsEngine

負責以事件清單與比賽資料產生統計結果。

### 建構子

```csharp
var statistics = new StatisticsEngine(eventManager, match);
```

### 方法

```csharp
double GetPlayerAttackSuccessRate(int playerId, TeamSide team)
double GetPlayerServeSuccessRate(int playerId, TeamSide team)
double GetTeamAttackSuccessRate(TeamSide team)
double GetTeamServeSuccessRate(TeamSide team)
Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
Dictionary<ActionType, int> GetTeamErrorBreakdown(TeamSide team)
int GetPlayerScoresTotals(int playerId, TeamSide team)
int GetPlayerErrorCount(int playerId, TeamSide team)
List<(int Time, int HomeScore, int AwayScore)> GetScoreTrendData()
List<ErrorClusterInfo> GetErrorClusterPoints(TeamSide team, int windowSize = 5)
string GenerateStatisticsReport()
```

### 設計重點

- 球員統計都需要同時指定 `playerId` 與 `TeamSide`，避免兩隊同背號造成混淆。
- `GetScoreTrendData` 會按局數與時間排序，回傳比分進度序列。
- `GetErrorClusterPoints` 會找出指定隊伍在滑動窗口內的失誤密集點，`windowSize` 必須大於 0。

### `ErrorClusterInfo`

```csharp
DateTime StartTimestamp
DateTime EndTimestamp
int GlobalEventStartIndex
int ErrorCount
int WindowSize
TimeSpan Duration
```

## CsvExporter

提供 CSV 匯出功能，適合做後續報表或外部分析。

### 方法

```csharp
bool ExportEventsToCSV(EventManager eventManager, Match match, string filePath)
bool ExportStatisticsToCSV(StatisticsEngine statistics, Match match, EventManager eventManager, string filePath)
```

### 輸出內容

- 事件 CSV 會輸出時間、球員背號、動作、隊伍、局數、比分與備註。
- 統計 CSV 會輸出基本比賽資訊、隊伍統計、得分來源與球員統計。

### 轉義規則

- 會自動處理含逗號、引號與換行的欄位。
- 匯出失敗時會回傳 `false` 並寫出錯誤訊息。

## Models

### ActionType

主要動作類型如下：

- `ServeSuccess`, `ServeFault`
- `AttackSuccess`, `AttackFault`, `AttackBlocked`, `AttackOutOfBounds`
- `BlockSuccess`, `BlockFault`
- `ReceiveSuccess`, `ReceiveFault`
- `TossSuccess`, `TossFault`
- `TeamScore`, `Substitution`, `Timeout`, `TechnicalFault`, `Other`

### MatchStatus

- `NotStarted`
- `InProgress`
- `Paused`
- `Finished`

### TeamSide

- `Home`
- `Away`

### GameEvent

```csharp
public class GameEvent
{
    public DateTime Timestamp { get; set; }
    public int PlayerId { get; set; }
    public ActionType Action { get; set; }
    public string Score { get; set; }
    public TeamSide Team { get; set; }
    public int SetNumber { get; set; }
    public string Notes { get; set; }
}
```

建構子：

```csharp
GameEvent()
GameEvent(int playerId, ActionType action, string score, TeamSide team, int setNumber = 1)
```

### Player

```csharp
public class Player
{
    public int JerseyNumber { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public int Height { get; set; }
    public bool IsActive { get; set; }
    public TeamSide Team { get; set; }
}
```

建構子：

```csharp
Player(int jerseyNumber, string name, string position, TeamSide team)
Player()
```

### Team

```csharp
public class Team
{
    public TeamSide Side { get; set; }
    public string TeamName { get; set; }
    public List<Player> Players { get; set; }
    public Dictionary<int, int> SetScores { get; set; }
    public int SetsWon { get; set; }
}
```

建構子與方法：

```csharp
Team(TeamSide side, string teamName)
void AddPlayer(Player player)
Player? GetPlayerByNumber(int jerseyNumber)
int GetCurrentSetScore(int currentSetNumber)
void UpdateCurrentSetScore(int currentSetNumber, int newScore)
void StartNewSet(int nextSetNumber)
List<Player> GetActivePlayers()
```

### Match

```csharp
public class Match
{
    public Team HomeTeam { get; set; }
    public Team AwayTeam { get; set; }
    public DateTime StartTime { get; set; }
    public MatchStatus Status { get; set; }
    public int CurrentSetNumber { get; set; }
    public string Venue { get; set; }
}
```

建構子與方法：

```csharp
Match(Team homeTeam, Team awayTeam, string venue = "")
void StartMatch()
void PauseMatch()
void ResumeMatch()
void FinishMatch()
string GetCurrentScore()
```

## 建議使用流程

```csharp
var homeTeam = new Team(TeamSide.Home, "主隊");
var awayTeam = new Team(TeamSide.Away, "客隊");
var match = new Match(homeTeam, awayTeam, "中正紀念堂排球館");
var eventManager = new EventManager();
var scoringService = new ScoringService(match, eventManager);
var statistics = new StatisticsEngine(eventManager, match);

match.StartMatch();

var gameEvent = new GameEvent(1, ActionType.ServeSuccess, match.GetCurrentScore(), TeamSide.Home, match.CurrentSetNumber);
eventManager.AddEvent(gameEvent);
scoringService.ProcessGameEvent(gameEvent);

var report = statistics.GenerateStatisticsReport();
```

## 備註

- 目前核心邏輯已完成，WinForms UI 仍屬後續整合目標。
- 專案不依賴外部套件，主要依靠 .NET 標準庫。
scoring.ProcessGameEvent(evt1);  // 比分: 1-0

var evt2 = new GameEvent(2, ActionType.AttackSuccess, "1-0", TeamSide.Away, 1);
eventManager.AddEvent(evt2);
scoring.ProcessGameEvent(evt2);  // 比分: 1-1

// 4. 查詢統計
Console.WriteLine($"主隊攻擊成功率: {stats.GetTeamAttackSuccessRate(TeamSide.Home):F2}%");

// 球員個人統計（注意現在需要指定 TeamSide）
Console.WriteLine($"球員 #1 (主隊) 攻擊成功率: {stats.GetPlayerAttackSuccessRate(1, TeamSide.Home):F2}%");
Console.WriteLine($"球員 #2 (客隊) 發球成功率: {stats.GetPlayerServeSuccessRate(2, TeamSide.Away):F2}%");

// 5. 導出數據
CsvExporter.ExportEventsToCSV(eventManager, match, "events.csv");
CsvExporter.ExportStatisticsToCSV(stats, match, eventManager, "stats.csv");
```
