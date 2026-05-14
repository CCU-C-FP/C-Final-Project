namespace MyProject.Utilities
{
    using MyProject.Models;
    using MyProject.Services;
    using System;
    using System.IO;
    using System.Text;

    /// CSV 導出工具
    /// 將比賽事件和統計數據導出為 CSV 格式
    public class CsvExporter
    {
        /// 將事件清單導出為 CSV 檔案
        public static bool ExportEventsToCSV(EventManager eventManager, Match match, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // 寫入標題
                    writer.WriteLine("時刻,球員背號,動作類型,隊伍,局數,比分,備註");

                    // 寫入事件記錄
                    foreach (var evt in eventManager.GetAllEvents())
                    {
                        string playerInfo = evt.PlayerId == 0 ? "隊伍" : evt.PlayerId.ToString();
                        string line = $"{evt.Timestamp:yyyy-MM-dd HH:mm:ss},{playerInfo},{evt.Action},{evt.Team},{evt.SetNumber},{evt.Score},{EscapeCSV(evt.Notes)}";
                        writer.WriteLine(line);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"導出 CSV 時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// 將統計報告導出為 CSV 檔案
        public static bool ExportStatisticsToCSV(StatisticsEngine statistics, Match match, EventManager eventManager, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // 寫入基本信息
                    writer.WriteLine("排球比賽統計報告");
                    writer.WriteLine($"主隊,{match.HomeTeam.TeamName}");
                    writer.WriteLine($"客隊,{match.AwayTeam.TeamName}");
                    writer.WriteLine($"比賽時間,{match.StartTime:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"比賽狀態,{match.Status}");
                    writer.WriteLine();

                    // 隊伍統計
                    writer.WriteLine("隊伍統計");
                    writer.WriteLine("隊伍,攻擊成功率(%),發球成功率(%)");
                    writer.WriteLine($"{match.HomeTeam.TeamName},{statistics.GetTeamAttackSuccessRate(TeamSide.Home):F2},{statistics.GetTeamServeSuccessRate(TeamSide.Home):F2}");
                    writer.WriteLine($"{match.AwayTeam.TeamName},{statistics.GetTeamAttackSuccessRate(TeamSide.Away):F2},{statistics.GetTeamServeSuccessRate(TeamSide.Away):F2}");
                    writer.WriteLine();

                    // 得分來源統計
                    writer.WriteLine("主隊得分來源");
                    writer.WriteLine("動作類型,次數");
                    var homeScoring = statistics.GetTeamScoringBreakdown(TeamSide.Home);
                    foreach (var kvp in homeScoring)
                    {
                        writer.WriteLine($"{kvp.Key},{kvp.Value}");
                    }
                    writer.WriteLine();

                    writer.WriteLine("客隊得分來源");
                    writer.WriteLine("動作類型,次數");
                    var awayScoring = statistics.GetTeamScoringBreakdown(TeamSide.Away);
                    foreach (var kvp in awayScoring)
                    {
                        writer.WriteLine($"{kvp.Key},{kvp.Value}");
                    }
                    writer.WriteLine();

                    // 球員統計
                    writer.WriteLine("球員統計");
                    writer.WriteLine("隊伍,球員背號,球員名稱,攻擊成功率(%),發球成功率(%),得分貢獻,失誤次數");
                    
                    foreach (var player in match.HomeTeam.Players)
                    {
                        writer.WriteLine($"{match.HomeTeam.TeamName},{player.JerseyNumber},{EscapeCSV(player.Name)}," +
                                       $"{statistics.GetPlayerAttackSuccessRate(player.JerseyNumber, TeamSide.Home):F2}," +
                                       $"{statistics.GetPlayerServeSuccessRate(player.JerseyNumber, TeamSide.Home):F2}," +
                                       $"{statistics.GetPlayerScoresTotals(player.JerseyNumber, TeamSide.Home)}," +
                                       $"{statistics.GetPlayerErrorCount(player.JerseyNumber, TeamSide.Home)}");
                    }

                    foreach (var player in match.AwayTeam.Players)
                    {
                        writer.WriteLine($"{match.AwayTeam.TeamName},{player.JerseyNumber},{EscapeCSV(player.Name)}," +
                                       $"{statistics.GetPlayerAttackSuccessRate(player.JerseyNumber, TeamSide.Away):F2}," +
                                       $"{statistics.GetPlayerServeSuccessRate(player.JerseyNumber, TeamSide.Away):F2}," +
                                       $"{statistics.GetPlayerScoresTotals(player.JerseyNumber, TeamSide.Away)}," +
                                       $"{statistics.GetPlayerErrorCount(player.JerseyNumber, TeamSide.Away)}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"導出統計報告時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// CSV 字段轉義（處理含有逗號或引號的字段）
        private static string EscapeCSV(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
