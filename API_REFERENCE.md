# API 參考

本文檔對應目前 `src/MyProject` 的實作（來源碼位於 `src/MyProject`），重點整理事件管理、計分、統計與導出工具的公開 API 與使用範例。

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
- `EventAdded` : `EventHandler<GameEvent>?` — 新增事件時通知，傳遞該 `GameEvent`。
- `EventUndone` : `EventHandler<GameEvent>?` — 撤銷事件時通知，傳遞被撤銷的 `GameEvent`。
- `EventRedone` : `EventHandler<GameEvent>?` — 重做事件時通知，傳遞被重做的 `GameEvent`。
- `EventsCleared` : `EventHandler?` — 所有事件被清空時通知（例如讓 `ScoringService` 重置比分）。

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

 - `ScoreUpdated` : `EventHandler<string>?` — 比分更新時觸發，參數為新的比分字串（透過 `GetCurrentScore()` 取得）。
 - `SetFinished` : `EventHandler<int>?` — 單局結束時觸發，參數為獲勝方識別（`0` 表示主隊，`1` 表示客隊）。
 - `MatchFinished` : `EventHandler<TeamSide>?` — 比賽結束時觸發，參數為獲勝隊伍的 `TeamSide`。

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

## 參數與回傳值範本

以下為每個主要類別常用方法的參數與回傳值說明範本，可作為文件擴充或自動產生說明的模板。

**EventManager**
- `AddEvent(GameEvent gameEvent)`
    - 參數: `gameEvent` — 要新增的事件物件。
    - 回傳: `void`。
- `Undo()`
    - 參數: 無。
    - 回傳: `bool` — 成功撤銷返回 `true`，否則 `false`。
- `Redo()`
    - 參數: 無。
    - 回傳: `bool` — 成功重做返回 `true`，否則 `false`。
- `GetAllEvents()`
    - 參數: 無。
    - 回傳: `List<GameEvent>` — 事件副本清單。
- `GetEventsByPlayer(int playerId)`
    - 參數: `playerId` — 球員背號。
    - 回傳: `List<GameEvent>` — 該球員相關事件。

**ScoringService**
- `ScoringService(Match match, EventManager eventManager)`
    - 參數: `match` — 目標比賽；`eventManager` — 事件來源（供重算用）。
    - 回傳: 建構子。
- `ProcessGameEvent(GameEvent gameEvent)`
    - 參數: `gameEvent` — 要處理的事件。
    - 回傳: `void`。
- `SetTeamScore(TeamSide team, int score)`
    - 參數: `team` — 要設定的隊伍；`score` — 目標分數。
    - 回傳: `void`。
- `GetCurrentScore()` / `GetDetailedScore()`
    - 參數: 無。
    - 回傳: `string` — 簡易或詳細比分字串。
    - 事件通知: `ScoreUpdated` (`EventHandler<string>`)、`SetFinished` (`EventHandler<int>`)、`MatchFinished` (`EventHandler<TeamSide>`)。

**StatisticsEngine**
- `GetPlayerAttackSuccessRate(int playerId, TeamSide team)` / `GetPlayerServeSuccessRate(int playerId, TeamSide team)`
    - 參數: `playerId` — 球員背號；`team` — 所屬隊伍。
    - 回傳: `double` — 百分比（0-100）。
- `GetTeamAttackSuccessRate(TeamSide team)` / `GetTeamServeSuccessRate(TeamSide team)`
    - 參數: `team` — 隊伍。
    - 回傳: `double` — 百分比（0-100）。
- `GetTeamScoringBreakdown(TeamSide team)` / `GetTeamErrorBreakdown(TeamSide team)`
    - 參數: `team` — 隊伍。
    - 回傳: `Dictionary<ActionType,int>` — 各動作的計數。
- `GetScoreTrendData()`
    - 參數: 無。
    - 回傳: `List<(int Time,int HomeScore,int AwayScore)>` — 得分時間序列。
- `GetErrorClusterPoints(TeamSide team, int windowSize = 5)`
    - 參數: `team` — 隊伍；`windowSize` — 滑動視窗大小 (>0)。
    - 回傳: `List<ErrorClusterInfo>` — 失誤集群清單。

**CsvExporter**
- `ExportEventsToCSV(EventManager eventManager, Match match, string filePath)`
    - 參數: `eventManager`、`match`、`filePath`（輸出路徑）。
    - 回傳: `bool` — 匯出成功為 `true`，失敗為 `false`（並印出錯誤資訊）。
- `ExportStatisticsToCSV(StatisticsEngine statistics, Match match, EventManager eventManager, string filePath)`
    - 參數: 同上。
    - 回傳: `bool`。

**Models（快速說明）**
- `GameEvent` 屬性: `Timestamp`、`PlayerId`、`Action`、`Score`、`Team`、`SetNumber`、`Notes`。
    - 建構子: `GameEvent()` / `GameEvent(int playerId, ActionType action, string score, TeamSide team, int setNumber = 1)`。
- `Player` 建構子與屬性: `JerseyNumber`、`Name`、`Position`、`Height`、`IsActive`、`Team`。
- `Team` 方法: `AddPlayer(Player)`、`GetPlayerByNumber(int)`、`GetCurrentSetScore(int)`、`UpdateCurrentSetScore(int,int)`、`StartNewSet(int)`、`GetActivePlayers()`。
- `Match` 方法: `StartMatch()`、`PauseMatch()`、`ResumeMatch()`、`FinishMatch()`、`GetCurrentScore()`。
- `ErrorClusterInfo` 屬性: `StartTimestamp`、`EndTimestamp`、`GlobalEventStartIndex`、`ErrorCount`、`WindowSize`、`Duration`。

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
 - 目前核心邏輯已完成，UI 層（例如 WinForms）為後續整合項目。
 - 專案不依賴外部第三方套件，主要使用 .NET 標準函式庫。
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
