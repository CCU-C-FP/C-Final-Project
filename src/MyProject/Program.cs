using MyProject.Models;
using MyProject.Services;
using MyProject.Utilities;

namespace MyProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // ========== 演示排球戰術數據記錄系統 ==========
            Console.WriteLine("=== 排球戰術數據記錄系統 (Volleyball Scouting & Trend Analyzer) ===\n");

            // 1. 建立隊伍和球員
            Console.WriteLine("[第 1 步] 建立隊伍和球員...");
            Team homeTeam = CreateHomeTeam();
            Team awayTeam = CreateAwayTeam();
            Console.WriteLine($"✓ 主隊: {homeTeam.TeamName} ({homeTeam.Players.Count} 名球員)");
            Console.WriteLine($"✓ 客隊: {awayTeam.TeamName} ({awayTeam.Players.Count} 名球員)\n");

            // 2. 建立比賽
            Console.WriteLine("[第 2 步] 建立比賽...");
            Match match = new Match(homeTeam, awayTeam, "中正紀念堂排球館");
            match.StartMatch();
            Console.WriteLine($"✓ 比賽開始: {match}\n");

            // 3. 建立服務層
            Console.WriteLine("[第 3 步] 初始化服務層...");
            EventManager eventManager = new EventManager();
            ScoringService scoringService = new ScoringService(match, eventManager);
            StatisticsEngine statsEngine = new StatisticsEngine(eventManager, match);

            // 訂閱事件通知（演示解耦設計）
            eventManager.EventAdded += (sender, evt) => 
                Console.WriteLine($"  → 事件已記錄: {evt}");
            
            scoringService.ScoreUpdated += (sender, score) =>
                Console.WriteLine($"  ✓ 比分更新: {score}");

            Console.WriteLine("✓ 服務層初始化完成\n");

            // 4. 記錄一系列比賽事件（演示雙軌制計分）
            Console.WriteLine("[第 4 步] 記錄比賽事件 (模擬前 10 個回合)...\n");
            SimulateMatchEvents(match, eventManager, scoringService);

            // 5. 展示撤銷/重做功能
            Console.WriteLine("\n[第 5 步] 演示撤銷功能...");
            if (eventManager.CanUndo())
            {
                Console.WriteLine("↶ 撤銷上一個事件...");
                eventManager.Undo();
                Console.WriteLine("✓ 事件已撤銷\n");
            }

            // 6. 統計分析
            Console.WriteLine("[第 6 步] 統計分析結果...\n");
            DisplayStatistics(statsEngine, match);

            // 7. 導出數據
            Console.WriteLine("\n[第 7 步] 導出數據...");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string exportDirectory = !string.IsNullOrWhiteSpace(desktopPath) && Directory.Exists(desktopPath)
                ? desktopPath
                : Directory.GetCurrentDirectory();

            Directory.CreateDirectory(exportDirectory);

            string eventsCsvPath = Path.Combine(exportDirectory, "volleyball_events.csv");
            string statsCsvPath = Path.Combine(exportDirectory, "volleyball_statistics.csv");

            if (CsvExporter.ExportEventsToCSV(eventManager, match, eventsCsvPath))
                Console.WriteLine($"✓ 事件資料已導出至: {eventsCsvPath}");
            
            if (CsvExporter.ExportStatisticsToCSV(statsEngine, match, eventManager, statsCsvPath))
                Console.WriteLine($"✓ 統計資料已導出至: {statsCsvPath}");

            Console.WriteLine("\n=== 演示完成 ===");
            Console.WriteLine("按任意鍵結束...");
            Console.ReadKey();
        }

        static Team CreateHomeTeam()
        {
            Team homeTeam = new Team(TeamSide.Home, "台北虎隊");
            homeTeam.AddPlayer(new Player(1, "陳明昊", "主攻", TeamSide.Home) { Height = 188 });
            homeTeam.AddPlayer(new Player(2, "李威廷", "邊攻", TeamSide.Home) { Height = 186 });
            homeTeam.AddPlayer(new Player(3, "王建宏", "接應", TeamSide.Home) { Height = 180 });
            homeTeam.AddPlayer(new Player(4, "劉家豪", "二傳", TeamSide.Home) { Height = 178 });
            homeTeam.AddPlayer(new Player(5, "黃子昱", "舞步", TeamSide.Home) { Height = 182 });
            homeTeam.AddPlayer(new Player(6, "蔡明哲", "舞步", TeamSide.Home) { Height = 185 });
            return homeTeam;
        }

        static Team CreateAwayTeam()
        {
            Team awayTeam = new Team(TeamSide.Away, "高雄鷹隊");
            awayTeam.AddPlayer(new Player(1, "吳昆遠", "主攻", TeamSide.Away) { Height = 190 });
            awayTeam.AddPlayer(new Player(2, "張駿昊", "邊攻", TeamSide.Away) { Height = 187 });
            awayTeam.AddPlayer(new Player(3, "鄒昀庭", "接應", TeamSide.Away) { Height = 182 });
            awayTeam.AddPlayer(new Player(4, "何明軒", "二傳", TeamSide.Away) { Height = 176 });
            awayTeam.AddPlayer(new Player(5, "馬浩鈞", "舞步", TeamSide.Away) { Height = 184 });
            awayTeam.AddPlayer(new Player(6, "邱柏燁", "舞步", TeamSide.Away) { Height = 183 });
            return awayTeam;
        }

        static void SimulateMatchEvents(Match match, EventManager eventManager, ScoringService scoringService)
        {
            var events = new (int playerId, ActionType action, TeamSide team)[]
            {
                (1, ActionType.ServeSuccess, TeamSide.Home),      // 主隊發球成功
                (2, ActionType.AttackSuccess, TeamSide.Away),     // 客隊攻擊得分
                (2, ActionType.ServeSuccess, TeamSide.Away),      // 客隊發球成功
                (3, ActionType.AttackSuccess, TeamSide.Home),     // 主隊攻擊得分
                (1, ActionType.AttackFault, TeamSide.Home),       // 主隊攻擊失誤 → 客隊得分
                (3, ActionType.ServeSuccess, TeamSide.Away),      // 客隊發球成功
                (4, ActionType.BlockSuccess, TeamSide.Home),      // 主隊攔網得分
                (2, ActionType.AttackSuccess, TeamSide.Away),     // 客隊攻擊得分
                (1, ActionType.AttackSuccess, TeamSide.Home),     // 主隊攻擊得分
                (5, ActionType.ServeFault, TeamSide.Away),        // 客隊發球失誤 → 主隊得分
            };

            foreach (var (playerId, action, team) in events)
            {
                GameEvent evt = new GameEvent(playerId, action, match.GetCurrentScore(), team, match.CurrentSetNumber);
                eventManager.AddEvent(evt);
                scoringService.ProcessGameEvent(evt);
                
                System.Threading.Thread.Sleep(200); // 稍微延遲以顯示進度
            }
        }

        static void DisplayStatistics(StatisticsEngine statsEngine, Match match)
        {
            Console.WriteLine("── 隊伍統計 ──");
            Console.WriteLine($"主隊 ({match.HomeTeam.TeamName}):");
            Console.WriteLine($"  攻擊成功率: {statsEngine.GetTeamAttackSuccessRate(TeamSide.Home):F2}%");
            Console.WriteLine($"  發球成功率: {statsEngine.GetTeamServeSuccessRate(TeamSide.Home):F2}%");

            Console.WriteLine($"\n客隊 ({match.AwayTeam.TeamName}):");
            Console.WriteLine($"  攻擊成功率: {statsEngine.GetTeamAttackSuccessRate(TeamSide.Away):F2}%");
            Console.WriteLine($"  發球成功率: {statsEngine.GetTeamServeSuccessRate(TeamSide.Away):F2}%");

            Console.WriteLine("\n── 球員個人統計（前 3 名主要球員）──");
            if (match.HomeTeam.Players.Count > 0)
            {
                var player1 = match.HomeTeam.Players[0];
                Console.WriteLine($"主隊 - {player1.Name} (#{player1.JerseyNumber}):");
                Console.WriteLine($"  攻擊成功率: {statsEngine.GetPlayerAttackSuccessRate(player1.JerseyNumber, TeamSide.Home):F2}%");
                Console.WriteLine($"  發球成功率: {statsEngine.GetPlayerServeSuccessRate(player1.JerseyNumber, TeamSide.Home):F2}%");
                Console.WriteLine($"  失誤次數: {statsEngine.GetPlayerErrorCount(player1.JerseyNumber, TeamSide.Home)}");
            }

            if (match.AwayTeam.Players.Count > 0)
            {
                var player2 = match.AwayTeam.Players[1];
                Console.WriteLine($"\n客隊 - {player2.Name} (#{player2.JerseyNumber}):");
                Console.WriteLine($"  攻擊成功率: {statsEngine.GetPlayerAttackSuccessRate(player2.JerseyNumber, TeamSide.Away):F2}%");
                Console.WriteLine($"  發球成功率: {statsEngine.GetPlayerServeSuccessRate(player2.JerseyNumber, TeamSide.Away):F2}%");
                Console.WriteLine($"  失誤次數: {statsEngine.GetPlayerErrorCount(player2.JerseyNumber, TeamSide.Away)}");
            }

            Console.WriteLine("\n── 得分來源分析 ──");
            var homeScoring = statsEngine.GetTeamScoringBreakdown(TeamSide.Home);
            Console.WriteLine($"主隊得分來源: 攻擊 {homeScoring[ActionType.AttackSuccess]} | " +
                            $"攔網 {homeScoring[ActionType.BlockSuccess]} | " +
                            $"發球 {homeScoring[ActionType.ServeSuccess]}");

            var awayScoring = statsEngine.GetTeamScoringBreakdown(TeamSide.Away);
            Console.WriteLine($"客隊得分來源: 攻擊 {awayScoring[ActionType.AttackSuccess]} | " +
                            $"攔網 {awayScoring[ActionType.BlockSuccess]} | " +
                            $"發球 {awayScoring[ActionType.ServeSuccess]}");

            Console.WriteLine("\n── 趨勢數據 ──");
            var trendData = statsEngine.GetScoreTrendData();
            Console.WriteLine("比分進度 (前 5 回合):");
            foreach (var (time, homeScore, awayScore) in trendData.Take(5))
            {
                Console.WriteLine($"  回合 {time}: {homeScore} - {awayScore}");
            }
        }
    }
}

