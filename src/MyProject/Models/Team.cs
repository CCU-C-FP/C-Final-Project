namespace MyProject.Models
{
    /// 代表排球隊伍
    /// 管理隊伍的球員、隊伍名稱和當前比分
    public class Team
    {
        /// 隊伍標識（Home 或 Away）
        public TeamSide Side { get; set; }

        
        /// 隊伍名稱
        public string TeamName { get; set; }

      
        /// 隊伍中的球員列表
        public List<Player> Players { get; set; }

        
        /// 隊伍當前比分（雙軌制）
        /// Key: 局數，Value: 該局的得分
        public Dictionary<int, int> SetScores { get; set; }

        /// 隊伍贏得的局數
        public int SetsWon { get; set; }

        
        /// 建立新隊伍
        public Team(TeamSide side, string teamName)
        {
            Side = side;
            TeamName = teamName;
            Players = new List<Player>();
            SetScores = new Dictionary<int, int>();
            SetsWon = 0;

            // 初始化第一局的比分
            SetScores[1] = 0;
        }


        /// 新增球員到隊伍
        public void AddPlayer(Player player)
        {
            if (player != null && !Players.Any(p => p.JerseyNumber == player.JerseyNumber))
            {
                player.Team = Side;
                Players.Add(player);
            }
        }

        
        /// 根據背號取得球員
        public Player? GetPlayerByNumber(int jerseyNumber)
        {
            return Players.FirstOrDefault(p => p.JerseyNumber == jerseyNumber);
        }


        /// 取得目前局數的得分
        public int GetCurrentSetScore(int currentSetNumber)
        {
            return SetScores.ContainsKey(currentSetNumber) ? SetScores[currentSetNumber] : 0;
        }

        /// 更新目前局數的得分
        public void UpdateCurrentSetScore(int currentSetNumber, int newScore)
        {
            if (!SetScores.ContainsKey(currentSetNumber))
            {
                SetScores[currentSetNumber] = 0;
            }
            SetScores[currentSetNumber] = newScore;
        }

        /// 開始新局
        public void StartNewSet(int nextSetNumber)
        {
            if (!SetScores.ContainsKey(nextSetNumber))
            {
                SetScores[nextSetNumber] = 0;
            }
        }


        /// 取得所有在場球員
        
        public List<Player> GetActivePlayers()
        {
            return Players.Where(p => p.IsActive).ToList();
        }

        public override string ToString()
        {
           string scoreStr = string.Join("-", SetScores.OrderBy(score => score.Key).Select(score => score.Value));
            return $"{TeamName} ({Side}) - 局數: {scoreStr} - 贏得: {SetsWon}";
        }
    }
}
