namespace MyProject.Models
{
    
    /// 代表排球比賽中的單一場次
    /// 管理兩支隊伍、事件記錄和比賽狀態
   
    public class Match
    {
    
        /// 主隊
        
        public Team HomeTeam { get; set; }

        /// 客隊
        public Team AwayTeam { get; set; }

        /// 比賽開始時間
        public DateTime StartTime { get; set; }

        
        /// 比賽狀態
        public MatchStatus Status { get; set; }

        /// 目前局數
        public int CurrentSetNumber { get; set; }

        
        /// 比賽地點（場地名稱）
        public string Venue { get; set; }

        /// 建立新比賽
        public Match(Team homeTeam, Team awayTeam, string venue = "")
        {
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            Venue = venue;
            StartTime = DateTime.Now;
            Status = MatchStatus.NotStarted;
            CurrentSetNumber = 1;
        }

        
        /// 開始比賽
        public void StartMatch()
        {
            Status = MatchStatus.InProgress;
            StartTime = DateTime.Now;
        }

        /// 暫停比賽
        public void PauseMatch()
        {
            if (Status == MatchStatus.InProgress)
            {
                Status = MatchStatus.Paused;
            }
        }

        /// 繼續比賽
        public void ResumeMatch()
        {
            if (Status == MatchStatus.Paused)
            {
                Status = MatchStatus.InProgress;
            }
        }

        /// 結束比賽
        public void FinishMatch()
        {
            Status = MatchStatus.Finished;
        }

        /// 獲取目前比分字串
        public string GetCurrentScore()
        {
            int homeScore = HomeTeam.GetCurrentSetScore(CurrentSetNumber);
            int awayScore = AwayTeam.GetCurrentSetScore(CurrentSetNumber);
            return $"{homeScore}-{awayScore}";
        }

        public override string ToString()
        {
            return $"{HomeTeam.TeamName} vs {AwayTeam.TeamName} - 局: {CurrentSetNumber} - 狀態: {Status}";
        }
    }
}
