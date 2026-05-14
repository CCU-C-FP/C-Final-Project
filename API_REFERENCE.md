# API 快速參考

## EventManager - 事件管理

```csharp
EventManager manager = new EventManager();

// 新增事件
manager.AddEvent(new GameEvent(...));

// 撤銷/重做
manager.Undo();                          // 撤銷上一個事件
manager.Redo();                          // 重做上一個被撤銷的事件
bool canUndo = manager.CanUndo();        // 是否可撤銷
bool canRedo = manager.CanRedo();        // 是否可重做

// 查詢事件
List<GameEvent> all = manager.GetAllEvents();
List<GameEvent> byPlayer = manager.GetEventsByPlayer(1);
List<GameEvent> byAction = manager.GetEventsByAction(ActionType.AttackSuccess);
List<GameEvent> bySet = manager.GetEventsBySet(1);

// 狀態
int count = manager.GetEventCount();
manager.ClearAllEvents();

// 事件訂閱
manager.EventAdded += (sender, evt) => { };
manager.EventUndone += (sender, evt) => { };
manager.EventRedone += (sender, evt) => { };
```

## ScoringService - 評分管理

```csharp
ScoringService scoring = new ScoringService(match, eventManager);

// 處理事件並自動計分
scoring.ProcessGameEvent(gameEvent);

// 手動設定比分
scoring.SetTeamScore(TeamSide.Home, 15);

// 查詢比分
string currentScore = scoring.GetCurrentScore();        // "15-12"
string detailedScore = scoring.GetDetailedScore();      // "25-22 15-12"

// 事件訂閱
scoring.ScoreUpdated += (sender, score) => { };
scoring.SetFinished += (sender, winner) => { };         // 0=Home, 1=Away
scoring.MatchFinished += (sender, winner) => { };       // TeamSide.Home/Away
```

## StatisticsEngine - 統計分析

```csharp
StatisticsEngine stats = new StatisticsEngine(eventManager, match);

// 個人統計
double attackRate = stats.GetPlayerAttackSuccessRate(1);    // 66.67%
double serveRate = stats.GetPlayerServeSuccessRate(1);      // 100.00%
int scores = stats.GetPlayerScoresTotals(1, TeamSide.Home); // 5
int errors = stats.GetPlayerErrorCount(1);                   // 2

// 隊伍統計
double homeAttack = stats.GetTeamAttackSuccessRate(TeamSide.Home);
double homeServe = stats.GetTeamServeSuccessRate(TeamSide.Home);

// 得分來源分析
var breakdown = stats.GetTeamScoringBreakdown(TeamSide.Home);
int attackScores = breakdown[ActionType.AttackSuccess];      // 5
int blockScores = breakdown[ActionType.BlockSuccess];        // 2
int serveScores = breakdown[ActionType.ServeSuccess];        // 1

// 失誤統計
var errors = stats.GetTeamErrorBreakdown(TeamSide.Home);
int attackErrors = errors[ActionType.AttackFault];          // 3
int serveErrors = errors[ActionType.ServeFault];            // 1

// 趨勢分析
var trendData = stats.GetScoreTrendData();  // 返回 List<(int Time, int HomeScore, int AwayScore)>
foreach (var (time, homeScore, awayScore) in trendData) {
    Console.WriteLine($"回合 {time}: {homeScore} - {awayScore}");
}

// 失誤密集點
var clusters = stats.GetErrorClusterPoints(TeamSide.Home, windowSize: 5);

// 生成報告
string report = stats.GenerateStatisticsReport();
```

## CsvExporter - 資料導出

```csharp
// 導出事件
bool success = CsvExporter.ExportEventsToCSV(
    eventManager, 
    match, 
    "C:\\events.csv"
);

// 導出統計
bool success = CsvExporter.ExportStatisticsToCSV(
    statistics, 
    match, 
    eventManager, 
    "C:\\statistics.csv"
);
```

## 數據模型

### ActionType 
```csharp
// 發球
ActionType.ServeSuccess       // 發球成功
ActionType.ServeFault         // 發球失誤

// 攻擊
ActionType.AttackSuccess      // 攻擊得分
ActionType.AttackFault        // 攻擊失誤
ActionType.AttackBlocked      // 被攔網
ActionType.AttackOutOfBounds  // 出界

// 攔網
ActionType.BlockSuccess       // 攔網成功
ActionType.BlockFault         // 攔網失誤

// 防守
ActionType.ReceiveSuccess     // 接球成功
ActionType.ReceiveFault       // 接球失誤

// 傳球
ActionType.TossSuccess        // 傳球成功
ActionType.TossFault          // 傳球失誤

// 其他
ActionType.TeamScore          // 團隊得分
ActionType.Substitution       // 替補
ActionType.Timeout            // 暫停
ActionType.TechnicalFault     // 技術犯規
ActionType.Other              // 其他
```

### TeamSide 
```csharp
TeamSide.Home   // 主隊
TeamSide.Away   // 客隊
```

### GameEvent (類別)
```csharp
public class GameEvent {
    public DateTime Timestamp { get; set; }    // 事件時刻
    public int PlayerId { get; set; }          // 球員背號 (0=隊伍)
    public ActionType Action { get; set; }     // 動作類型
    public string Score { get; set; }          // 比分 "25-22"
    public TeamSide Team { get; set; }         // 隊伍
    public int SetNumber { get; set; }         // 局數
    public string Notes { get; set; }          // 備註
}

// 建構子
GameEvent evt = new GameEvent();
GameEvent evt = new GameEvent(1, ActionType.AttackSuccess, "1-0", TeamSide.Home, 1);
```

### Player (類別)
```csharp
public class Player {
    public int JerseyNumber { get; set; }      // 背號
    public string Name { get; set; }           // 姓名
    public string Position { get; set; }       // 位置
    public int Height { get; set; }            // 身高
    public bool IsActive { get; set; }         // 是否在場
    public TeamSide Team { get; set; }         // 隊伍
}

// 建構子
Player p = new Player(1, "王小明", "主攻", TeamSide.Home);
```

### Team (類別)
```csharp
public class Team {
    public TeamSide Side { get; set; }         // 隊伍標識
    public string TeamName { get; set; }       // 隊伍名稱
    public List<Player> Players { get; set; }  // 球員列表
    public Dictionary<int, int> SetScores { get; set; }  // 各局比分
    public int SetsWon { get; set; }           // 贏得局數
}

// 建構子
Team home = new Team(TeamSide.Home, "台北虎隊");

// 方法
home.AddPlayer(player);                        // 新增球員
Player? p = home.GetPlayerByNumber(1);         // 查詢球員
int score = home.GetCurrentSetScore(1);        // 取得第1局分數
home.UpdateCurrentSetScore(1, 15);             // 設定第1局分數
home.StartNewSet(2);                           // 開始第2局
List<Player> active = home.GetActivePlayers(); // 取得在場球員
```

### Match (類別)
```csharp
public class Match {
    public Team HomeTeam { get; set; }         // 主隊
    public Team AwayTeam { get; set; }         // 客隊
    public DateTime StartTime { get; set; }    // 開始時間
    public MatchStatus Status { get; set; }    // 比賽狀態
    public int CurrentSetNumber { get; set; }  // 目前局數
    public string Venue { get; set; }          // 場地
}

// 建構子
Match match = new Match(homeTeam, awayTeam, "中正紀念堂");

// 方法
match.StartMatch();                            // 開始比賽
match.PauseMatch();                            // 暫停比賽
match.ResumeMatch();                           // 繼續比賽
match.FinishMatch();                           // 結束比賽
string score = match.GetCurrentScore();        // 取得比分 "15-12"
```

---

## 完整示例

```csharp
// 1. 初始化
var homeTeam = new Team(TeamSide.Home, "主隊");
homeTeam.AddPlayer(new Player(1, "球員1", "主攻", TeamSide.Home));
homeTeam.AddPlayer(new Player(2, "球員2", "邊攻", TeamSide.Home));

var awayTeam = new Team(TeamSide.Away, "客隊");
awayTeam.AddPlayer(new Player(1, "球員3", "主攻", TeamSide.Away));
awayTeam.AddPlayer(new Player(2, "球員4", "邊攻", TeamSide.Away));

var match = new Match(homeTeam, awayTeam);
var eventManager = new EventManager();
var scoring = new ScoringService(match, eventManager);
var stats = new StatisticsEngine(eventManager, match);

// 2. 訂閱事件
scoring.ScoreUpdated += (_, score) => Console.WriteLine($"比分: {score}");
eventManager.EventAdded += (_, evt) => Console.WriteLine($"事件: {evt}");

// 3. 記錄事件
match.StartMatch();
var evt1 = new GameEvent(1, ActionType.ServeSuccess, "0-0", TeamSide.Home, 1);
eventManager.AddEvent(evt1);
scoring.ProcessGameEvent(evt1);  // 比分: 1-0

var evt2 = new GameEvent(2, ActionType.AttackSuccess, "1-0", TeamSide.Away, 1);
eventManager.AddEvent(evt2);
scoring.ProcessGameEvent(evt2);  // 比分: 1-1

// 4. 查詢統計
Console.WriteLine($"主隊攻擊成功率: {stats.GetTeamAttackSuccessRate(TeamSide.Home):F2}%");

// 5. 導出數據
CsvExporter.ExportEventsToCSV(eventManager, match, "events.csv");
CsvExporter.ExportStatisticsToCSV(stats, match, eventManager, "stats.csv");
```
