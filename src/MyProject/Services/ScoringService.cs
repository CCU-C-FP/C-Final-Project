namespace MyProject.Services
{
    using MyProject.Models;

    
    /// 評分服務
    /// 根據比賽事件自動計算和更新比分
    /// 實現雙軌制計分規則（25分先勝制，但需差2分）
    
    public class ScoringService
    {
        
        /// 當前比賽
        private Match _match;

        /// 事件管理器
        private EventManager _eventManager;

        
        /// 局數結束時的通知
        public event EventHandler<int>? SetFinished; // 參數為獲勝隊伍標識 (0=Home, 1=Away)

        /// 比賽結束時的通知
        public event EventHandler<TeamSide>? MatchFinished; // 參數為獲勝隊伍

        /// 比分更新時的通知
        public event EventHandler<string>? ScoreUpdated; // 參數為新的比分字串

        public ScoringService(Match match, EventManager eventManager)
        {
            _match = match;
            _eventManager = eventManager;
        }

        /// 根據比賽事件更新比分
        public void ProcessGameEvent(GameEvent gameEvent)
        {
            if (gameEvent == null || _match == null)
                return;

            bool homeTeamScored = false;
            bool awayTeamScored = false;

            // 根據動作類型判定是否得分
            switch (gameEvent.Action)
            {
                // 主隊得分的情況
                case ActionType.AttackSuccess:
                case ActionType.BlockSuccess:
                case ActionType.ServeSuccess:
                    if (gameEvent.Team == TeamSide.Home)
                        homeTeamScored = true;
                    else
                        awayTeamScored = true;
                    break;

                // 客隊得分的情況（主隊失誤）
                case ActionType.AttackFault:
                case ActionType.BlockFault:
                case ActionType.ServeFault:
                case ActionType.ReceiveFault:
                case ActionType.TossFault:
                    if (gameEvent.Team == TeamSide.Home)
                        awayTeamScored = true;
                    else
                        homeTeamScored = true;
                    break;

                case ActionType.TeamScore:
                    // 明確指定的隊伍得分
                    if (gameEvent.Team == TeamSide.Home)
                        homeTeamScored = true;
                    else
                        awayTeamScored = true;
                    break;

                case ActionType.Timeout:
                case ActionType.Substitution:
                case ActionType.TechnicalFault:
                case ActionType.Other:
                    // 這些動作不直接影響比分
                    break;
            }

            // 更新比分
            if (homeTeamScored)
            {
                IncrementTeamScore(TeamSide.Home);
            }
            if (awayTeamScored)
            {
                IncrementTeamScore(TeamSide.Away);
            }

            // 檢查局是否結束
            CheckSetCompletion();

            // 通知 UI 層比分已更新
            ScoreUpdated?.Invoke(this, _match.GetCurrentScore());
        }

        /// 增加隊伍比分
        private void IncrementTeamScore(TeamSide team)
        {
            Team targetTeam = team == TeamSide.Home ? _match.HomeTeam : _match.AwayTeam;
            int currentSet = _match.CurrentSetNumber;
            int currentScore = targetTeam.GetCurrentSetScore(currentSet);
            targetTeam.UpdateCurrentSetScore(currentSet, currentScore + 1);
        }

        /// 直接設定隊伍比分（用於手動調整）

        public void SetTeamScore(TeamSide team, int score)
        {
            Team targetTeam = team == TeamSide.Home ? _match.HomeTeam : _match.AwayTeam;
            int currentSet = _match.CurrentSetNumber;
            targetTeam.UpdateCurrentSetScore(currentSet, score);
            
            CheckSetCompletion();
            ScoreUpdated?.Invoke(this, _match.GetCurrentScore());
        }

        /// 檢查目前局是否結束
        /// 雙軌制規則：25分先勝制，需差2分
        private void CheckSetCompletion()
        {
            int homeScore = _match.HomeTeam.GetCurrentSetScore(_match.CurrentSetNumber);
            int awayScore = _match.AwayTeam.GetCurrentSetScore(_match.CurrentSetNumber);

            bool setFinished = false;
            TeamSide setWinner = TeamSide.Home;

            // 檢查是否達到 25 分且領先 2 分
            if (homeScore >= 25 && homeScore - awayScore >= 2)
            {
                setFinished = true;
                setWinner = TeamSide.Home;
            }
            else if (awayScore >= 25 && awayScore - homeScore >= 2)
            {
                setFinished = true;
                setWinner = TeamSide.Away;
            }

            if (setFinished)
            {
                OnSetFinished(setWinner);
            }
        }


        /// 局結束處理
        private void OnSetFinished(TeamSide setWinner)
        {
            // 更新局數獲勝計數
            if (setWinner == TeamSide.Home)
            {
                _match.HomeTeam.SetsWon++;
            }
            else
            {
                _match.AwayTeam.SetsWon++;
            }

            SetFinished?.Invoke(this, setWinner == TeamSide.Home ? 0 : 1);

            // 檢查比賽是否結束
            CheckMatchCompletion();
        }

        /// 檢查比賽是否結束
        /// 三先勝制：先贏2局者獲勝
        private void CheckMatchCompletion()
        {
            if (_match.HomeTeam.SetsWon >= 2 || _match.AwayTeam.SetsWon >= 2)
            {
                // 判斷贏家是誰
                TeamSide matchWinner = _match.HomeTeam.SetsWon >= 2 ? TeamSide.Home : TeamSide.Away;
                
                _match.FinishMatch();
                
                // 觸發比賽結束事件
                MatchFinished?.Invoke(this, matchWinner);
            }
            else
            {
                // 尚未有人贏得 2 局，繼續開始下一局
                _match.CurrentSetNumber++;
                _match.HomeTeam.StartNewSet(_match.CurrentSetNumber);
                _match.AwayTeam.StartNewSet(_match.CurrentSetNumber);
            }
        }

        /// 取得目前比分
        public string GetCurrentScore()
        {
            return _match.GetCurrentScore();
        }

        /// 取得詳細的局數比分
        public string GetDetailedScore()
        {
            string homeSetScores = string.Join("-", _match.HomeTeam.SetScores.Values);
            string awaySetScores = string.Join("-", _match.AwayTeam.SetScores.Values);
            return $"主隊: {homeSetScores} | 客隊: {awaySetScores}";
        }
    }
}
