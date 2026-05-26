namespace MyProject.Services
{
    using MyProject.Models;

    /// 統計引擎
    /// 根據事件清單計算各種統計指標
    /// 為數據分析和趨勢分析提供基礎數據
    public class StatisticsEngine
    {
        private EventManager _eventManager;
        private Match _match;

        public StatisticsEngine(EventManager eventManager, Match match)
        {
            _eventManager = eventManager;
            _match = match;
        }

        /// 計算球員的攻擊成功率
        /// 同時指定隊伍，避免同背號球員混淆
        public double GetPlayerAttackSuccessRate(int playerId, TeamSide team)
        {
            var attacks = _eventManager.GetEventsByPlayer(playerId)
                .Where(e => e.Team == team &&
                           (e.Action == ActionType.AttackSuccess || 
                            e.Action == ActionType.AttackFault ||
                            e.Action == ActionType.AttackBlocked||
                            e.Action == ActionType.AttackOutOfBounds))
                .ToList();

            if (attacks.Count == 0)
                return 0;

            int successful = attacks.Count(e => e.Action == ActionType.AttackSuccess);
            return (double)successful / attacks.Count * 100;
        }

        /// 計算球員的發球成功率
        /// 同時指定隊伍，避免同背號球員混淆
        public double GetPlayerServeSuccessRate(int playerId, TeamSide team)
        {
            var serves = _eventManager.GetEventsByPlayer(playerId)
                .Where(e => e.Team == team &&
                           (e.Action == ActionType.ServeSuccess || e.Action == ActionType.ServeFault))
                .ToList();

            if (serves.Count == 0)
                return 0;

            int successful = serves.Count(e => e.Action == ActionType.ServeSuccess);
            return (double)successful / serves.Count * 100;
        }

        /// 計算隊伍的攻擊成功率
        public double GetTeamAttackSuccessRate(TeamSide team)
        {
            var attacks = _eventManager.GetAllEvents()
                .Where(e => e.Team == team &&
                           (e.Action == ActionType.AttackSuccess ||
                            e.Action == ActionType.AttackFault ||
                            e.Action == ActionType.AttackBlocked||
                            e.Action == ActionType.AttackOutOfBounds))
                .ToList();

            if (attacks.Count == 0)
                return 0;

            int successful = attacks.Count(e => e.Action == ActionType.AttackSuccess);
            return (double)successful / attacks.Count * 100;
        }

        /// 計算隊伍的發球成功率
        public double GetTeamServeSuccessRate(TeamSide team)
        {
            var serves = _eventManager.GetAllEvents()
                .Where(e => e.Team == team &&
                           (e.Action == ActionType.ServeSuccess || e.Action == ActionType.ServeFault))
                .ToList();

            if (serves.Count == 0)
                return 0;

            int successful = serves.Count(e => e.Action == ActionType.ServeSuccess);
            return (double)successful / serves.Count * 100;
        }

        /// 計算隊伍的得分來源統計
        /// 返回各類型得分的數量
        public Dictionary<ActionType, int> GetTeamScoringBreakdown(TeamSide team)
        {
            var scoringActions = new[] 
            { 
                ActionType.AttackSuccess, 
                ActionType.BlockSuccess, 
                ActionType.ServeSuccess,
                ActionType.TeamScore
            };

            var allEvents = _eventManager.GetAllEvents();
            var breakdown = new Dictionary<ActionType, int>();
            foreach (var action in scoringActions)
            {
                breakdown[action] = allEvents
                    .Count(e => e.Team == team && e.Action == action);
            }
            return breakdown;
        }

        /// 計算隊伍的失誤來源統計
        public Dictionary<ActionType, int> GetTeamErrorBreakdown(TeamSide team)
        {
            var errorActions = new[]
            {
                ActionType.AttackFault,
                ActionType.ServeFault,
                ActionType.BlockFault,
                ActionType.ReceiveFault,
                ActionType.TossFault,
                ActionType.AttackOutOfBounds
            };

            var allEvents = _eventManager.GetAllEvents();
            var breakdown = new Dictionary<ActionType, int>();
            foreach (var action in errorActions)
            {
                breakdown[action] = allEvents
                    .Count(e => e.Team == team && e.Action == action);
            }
            return breakdown;
        }

        /// 計算球員的得分貢獻
        public int GetPlayerScoresTotals(int playerId, TeamSide team)
        {
            var scoringActions = new[] 
            { 
                ActionType.AttackSuccess, 
                ActionType.BlockSuccess,
                ActionType.ServeSuccess
            };

            return _eventManager.GetEventsByPlayer(playerId)
                .Count(e => e.Team == team && scoringActions.Contains(e.Action));
        }

         
        /// 計算球員的失誤次數
        /// 同時指定隊伍，避免同背號球員混淆
        /// </summary>
        public int GetPlayerErrorCount(int playerId, TeamSide team)
        {
            var errorActions = new[]
            {
                ActionType.AttackFault,
                ActionType.ServeFault,
                ActionType.BlockFault,
                ActionType.ReceiveFault,
                ActionType.TossFault,
                ActionType.AttackOutOfBounds
                
            };

            return _eventManager.GetEventsByPlayer(playerId)
                .Count(e => e.Team == team && errorActions.Contains(e.Action));
        }

        /// 取得各局的得分進度曲線（用於趨勢分析）
        public List<(int Time, int HomeScore, int AwayScore)> GetScoreTrendData()
        {
            var trendData = new List<(int Time, int HomeScore, int AwayScore)>();
            int homeScore = 0;
            int awayScore = 0;
            int time = 0;
            int? currentSetNumber = null;


           foreach (var evt in _eventManager.GetAllEvents()
                 .OrderBy(e => e.SetNumber)
                 .ThenBy(e => e.Timestamp))
             {
                 if (currentSetNumber != evt.SetNumber)
                 {
                     currentSetNumber = evt.SetNumber;
                     homeScore = 0;
                     awayScore = 0;
                     time = 0;
                 }

                bool homeScored = false;
                bool awayScored = false;

                // 判定得分
                switch (evt.Action)
                {
                    case ActionType.AttackSuccess:
                    case ActionType.BlockSuccess:
                    case ActionType.ServeSuccess:
                    case ActionType.TeamScore:
                        if (evt.Team == TeamSide.Home) homeScored = true;
                        else awayScored = true;
                        break;

                    case ActionType.AttackFault:
                    case ActionType.BlockFault:
                    case ActionType.ServeFault:
                    case ActionType.ReceiveFault:
                    case ActionType.TossFault:
                    case ActionType.AttackOutOfBounds:
                        if (evt.Team == TeamSide.Home) awayScored = true;
                        else homeScored = true;
                        break;
                }

                if (homeScored) homeScore++;
                if (awayScored) awayScore++;

                trendData.Add((time++, homeScore, awayScore));
            }

            return trendData;
        }

         
        /// 取得失誤密集點（連續失誤集群）
        /// 分析隊伍事件中是否存在集中的失誤時期
        /// </summary>
        /// <param name="team">分析的隊伍</param>
        /// <param name="windowSize">滑動視窗大小（預設 5 個事件）</param>
        /// <returns>包含時間戳和全域索引的失誤集群清單</returns>
        public List<ErrorClusterInfo> GetErrorClusterPoints(TeamSide team, int windowSize = 5)
        {
            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "視窗大小必須大於 0");

            var allEvents = _eventManager.GetAllEvents();
            var teamEvents = allEvents
                .Where(e => e.Team == team)
                .ToList();

            var clusters = new List<ErrorClusterInfo>();
            var errorActions = new[]
            {
                ActionType.AttackFault,
                ActionType.ServeFault,
                ActionType.BlockFault,
                ActionType.ReceiveFault,
                ActionType.TossFault,
                ActionType.AttackOutOfBounds
                
            };

            for (int i = 0; i <= teamEvents.Count - windowSize; i++)
            {
                var windowEvents = teamEvents
                    .Skip(i)
                    .Take(windowSize)
                    .ToList();

                int errorCount = windowEvents
                    .Count(e => errorActions.Contains(e.Action));

                // 如果在窗口內有 3 個或以上的失誤，標記為密集點
                if (errorCount >= 3)
                {
                    // 取得此窗口中第一個和最後一個事件的時間戳
                    var startEvent = windowEvents.First();
                    var endEvent = windowEvents.Last();

                    // 計算全域事件索引（在完整事件清單中的位置）
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

        /// 生成統計報告（文字格式）
        public string GenerateStatisticsReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 比賽統計報告 ===");
            report.AppendLine($"比賽: {_match.HomeTeam.TeamName} vs {_match.AwayTeam.TeamName}");
            report.AppendLine();

            report.AppendLine("主隊統計:");
            report.AppendLine($"  攻擊成功率: {GetTeamAttackSuccessRate(TeamSide.Home):F2}%");
            report.AppendLine($"  發球成功率: {GetTeamServeSuccessRate(TeamSide.Home):F2}%");
            var homeBreakdown = GetTeamScoringBreakdown(TeamSide.Home);
            report.AppendLine($"  得分來源: 攻擊得分 {homeBreakdown[ActionType.AttackSuccess]}, " +
                             $"攔網得分 {homeBreakdown[ActionType.BlockSuccess]}, " +
                             $"發球得分 {homeBreakdown[ActionType.ServeSuccess]}");
            report.AppendLine();

            report.AppendLine("客隊統計:");
            report.AppendLine($"  攻擊成功率: {GetTeamAttackSuccessRate(TeamSide.Away):F2}%");
            report.AppendLine($"  發球成功率: {GetTeamServeSuccessRate(TeamSide.Away):F2}%");
            var awayBreakdown = GetTeamScoringBreakdown(TeamSide.Away);
            report.AppendLine($"  得分來源: 攻擊得分 {awayBreakdown[ActionType.AttackSuccess]}, " +
                             $"攔網得分 {awayBreakdown[ActionType.BlockSuccess]}, " +
                             $"發球得分 {awayBreakdown[ActionType.ServeSuccess]}");
            report.AppendLine();

            report.AppendLine($"總事件數: {_eventManager.GetEventCount()}");

            return report.ToString();
        }
    }
}
