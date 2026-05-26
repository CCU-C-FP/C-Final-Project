namespace MyProject.Models
{
    
    public enum ActionType
    {
        // 發球相關
        ServeSuccess,          // 發球成功
        ServeFault,            // 發球失誤

        // 攻擊相關
        AttackSuccess,         // 攻擊得分
        AttackFault,           // 攻擊失誤
        AttackBlocked,         // 攻擊被攔網
        AttackOutOfBounds,     // 攻擊出界

        // 攔網相關
        BlockSuccess,          // 攔網成功（得分）
        BlockFault,            // 攔網失誤

        // 接球/防守相關
        ReceiveSuccess,        // 接球成功
        ReceiveFault,          // 接球失誤（失分）

        // 舉球相關
        TossSuccess,           // 舉球成功
        TossFault,             // 舉球失誤

        // 綜合事件
        TeamScore,             // 團隊得分（球員背號為0）
        Substitution,          // 替補（球員背號為替補上場者的編號）
        Timeout,               // 暫停
        TechnicalFault,        // 技術犯規
        Other                  // 其他
    }

//  比賽狀態
    public enum MatchStatus
    {
        NotStarted,
        InProgress,
        Paused,
        Finished
    }

    
    // 隊伍標識
    public enum TeamSide
    {
        Home,
        Away
    }
}
