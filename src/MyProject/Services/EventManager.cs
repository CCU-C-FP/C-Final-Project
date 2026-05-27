namespace MyProject.Services
{
    using MyProject.Models;

     
    /// 事件管理器
    /// 負責維護比賽事件清單、事件新增、撤銷功能
    /// 支援事件通知機制（委派事件），與 UI 層解耦
    public class EventManager
    {
         
        /// 事件清單
        private List<GameEvent> _events;

         
        /// 撤銷棧 - 保存已撤銷的事件供重做使用
        private Stack<GameEvent> _undoStack;

         
        /// 重做棧
        private Stack<GameEvent> _redoStack;

         
        /// 事件發生時的委派通知
        /// 供 UI 層訂閱，實現解耦通知
        public event EventHandler<GameEvent>? EventAdded;
        public event EventHandler<GameEvent>? EventUndone;
        public event EventHandler<GameEvent>? EventRedone;
         
        /// 當所有事件被清除時通知訂閱者（例如讓 ScoringService 重置分數）
        public event EventHandler? EventsCleared;

         
        /// 建立新的事件管理器
        public EventManager()
        {
            _events = new List<GameEvent>();
            _undoStack = new Stack<GameEvent>();
            _redoStack = new Stack<GameEvent>();
        }

         
        /// 新增事件
        /// </summary>
        public void AddEvent(GameEvent gameEvent)
        {
            if (gameEvent != null)
            {
                _events.Add(gameEvent);
                _undoStack.Push(gameEvent);
                _redoStack.Clear(); // 新增事件時清空重做棧

                // 觸發事件通知 UI 層
                EventAdded?.Invoke(this, gameEvent);
            }
        }

         
        /// 撤銷上一個事件
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count > 0)
            {
                GameEvent undoneEvent = _undoStack.Pop();
                _events.Remove(undoneEvent);
                _redoStack.Push(undoneEvent);

                EventUndone?.Invoke(this, undoneEvent);
                return true;
            }
            return false;
        }

        /// 重做上一個被撤銷的事件
        public bool Redo()
        {
            if (_redoStack.Count > 0)
            {
                GameEvent redoneEvent = _redoStack.Pop();
                _events.Add(redoneEvent);
                _undoStack.Push(redoneEvent);

                EventRedone?.Invoke(this, redoneEvent);
                return true;
            }
            return false;
        }

        /// 取得所有事件
        public List<GameEvent> GetAllEvents()
        {
            return new List<GameEvent>(_events); // 返回副本以保護內部狀態
        }

        /// 取得特定球員的事件
        public List<GameEvent> GetEventsByPlayer(int playerId)
        {
            return _events.Where(e => e.PlayerId == playerId).ToList();
        }

        /// 取得特定動作類型的事件
        public List<GameEvent> GetEventsByAction(ActionType action)
        {
            return _events.Where(e => e.Action == action).ToList();
        }

        /// 取得特定局數的事件
        public List<GameEvent> GetEventsBySet(int setNumber)
        {
            return _events.Where(e => e.SetNumber == setNumber).ToList();
        }

        /// 清空所有事件
        public void ClearAllEvents()
        {
            _events.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
            // 通知所有訂閱者事件歷史已被清空
            EventsCleared?.Invoke(this, System.EventArgs.Empty);
        }

        /// 取得事件總數
        public int GetEventCount()
        {
            return _events.Count;
        }

        /// 是否可以撤銷
        public bool CanUndo()
        {
            return _undoStack.Count > 0;
        }

        /// 是否可以重做
        public bool CanRedo()
        {
            return _redoStack.Count > 0;
        }
    }
}
