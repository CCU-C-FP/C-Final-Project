namespace MyProject.Models
{
    
    /// 代表排球比賽中的單一事件
    /// 核心數據物件，用於記錄比賽過程中發生的所有動作
    public class GameEvent
    {
        /// 事件發生的時刻（自動擷取）
        public DateTime Timestamp { get; set; }
        /// 執行動作的球員背號
        /// 0 代表未指定或團隊事件
        public int PlayerId { get; set; }

        /// 動作類型（使用 Enum 確保型別安全）
        public ActionType Action { get; set; }

        /// 執行動作時的比分快照
        /// 格式：例如 "25-22" 表示 Home 隊得 25 分，Away 隊得 22 分
        public string Score { get; set; }

        /// 執行動作的隊伍（Home 或 Away）
        public TeamSide Team { get; set; }

        /// 當前局數（1 表示第一局）
        public int SetNumber { get; set; }

        /// 事件備註（可選）
        /// 例如：攻擊的位置、特殊情況說明
        public string Notes { get; set; }

        /// 建立新事件
        public GameEvent()
        {
            Timestamp = DateTime.Now;
            PlayerId = 0;
            Score = "0-0";
            Notes = string.Empty;
        }

        /// 建立新事件（帶參數）
        public GameEvent(int playerId, ActionType action, string score, TeamSide team, int setNumber = 1)
        {
            Timestamp = DateTime.Now;
            PlayerId = playerId;
            Action = action;
            Score = score;
            Team = team;
            SetNumber = setNumber;
            Notes = string.Empty;
        }

        public override string ToString()
        {
            string playerInfo = PlayerId == 0 ? "隊伍" : $"球員 #{PlayerId}";
            return $"[{Timestamp:HH:mm:ss}] {playerInfo} - {Action} - 比分: {Score}";
        }
    }
}
