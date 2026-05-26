namespace MyProject.Services
{
    
    /// 失誤密集點信息
    /// 表示一個時間窗口內出現的失誤集群，包含時間戳和事件索引以供調用端使用
  
    public class ErrorClusterInfo
    {
        
        /// 密集點窗口起始時刻（第一個事件的時間戳）
        
        public DateTime StartTimestamp { get; set; }

       
        /// 密集點窗口結束時刻（最後一個事件的時間戳）
        
        public DateTime EndTimestamp { get; set; }

        
        /// 該窗口在全域事件序列中的起始索引
        /// 可用於在事件清單中定位該集群
        
        public int GlobalEventStartIndex { get; set; }

        
        /// 該窗口內的失誤數量
        
        public int ErrorCount { get; set; }

        
        /// 該窗口的視窗大小
       
        public int WindowSize { get; set; }

        
        /// 建立新的失誤密集點信息
      
        public ErrorClusterInfo(
            DateTime startTimestamp,
            DateTime endTimestamp,
            int globalEventStartIndex,
            int errorCount,
            int windowSize)
        {
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            GlobalEventStartIndex = globalEventStartIndex;
            ErrorCount = errorCount;
            WindowSize = windowSize;
        }

       
        /// 密集點持續時間
       
        public TimeSpan Duration => EndTimestamp - StartTimestamp;

      
        /// 友善的字串表示
        
        public override string ToString()
        {
            return $"失誤密集 | 時間: {StartTimestamp:HH:mm:ss} - {EndTimestamp:HH:mm:ss}" +
                   $" | 失誤數: {ErrorCount}/{WindowSize} | 全域索引: {GlobalEventStartIndex}";
        }
    }
}
