namespace MyProject.Models
{
    /// 球員資料類別
    /// 代表排球隊伍中的單一球員
    
    public class Player
    {
        /// 球員背號（唯一識別符）
        public int JerseyNumber { get; set; }

        /// 球員姓名
        public string Name { get; set; }

        /// 球員位置（如：主攻、舉球、二傳、攔網手等）
        public string Position { get; set; }

        /// 球員高度（公分）
        public int Height { get; set; }


        /// 球員當前狀態：是否在場上
        public bool IsActive { get; set; }

        /// 所屬隊伍
        public TeamSide Team { get; set; }

        /// 建立新球員實例
        public Player(int jerseyNumber, string name, string position, TeamSide team)
        {
            JerseyNumber = jerseyNumber;
            Name = name;
            Position = position;
            Team = team;
            IsActive = true;
        }

        
        /// 無參數建構子
        public Player()
        {
            Name = string.Empty;
            Position = string.Empty;
            IsActive = true;
        }

        public override string ToString()
        {
            return $"#{JerseyNumber} {Name} ({Position}) - {(IsActive ? "在場" : "替補")}";
        }
    }
}
