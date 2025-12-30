using MineTray.Models;

namespace MineTray.Services
{
    /// <summary>
    /// スマートな参加/退出通知を処理します（レート制限とプレイヤー追跡機能付き）。
    /// </summary>
    public class NotificationService
    {
        private HashSet<string> _lastOnlineIds = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> _uuidLastSeenPoll = new(StringComparer.OrdinalIgnoreCase);
        private int _pollCount = 0;
        private bool _isFirstPoll = true;
        private string _lastServerAddress = string.Empty;

        private const int LEAVE_THRESHOLD_POLLS = 5; // 5ポール = 約5分

        /// <summary>
        /// 通知を表示すべき時に発生するイベント。
        /// </summary>
        public event Action<string, string>? OnNotify;

        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// 追跡状態をリセットします（サーバー切替時など）。
        /// </summary>
        public void Reset()
        {
            _isFirstPoll = true;
            _lastOnlineIds.Clear();
            _uuidLastSeenPoll.Clear();
            _pollCount = 0;
        }

        /// <summary>
        /// サーバーが変更されたかチェックし、変更があれば追跡をリセットします。
        /// </summary>
        public void CheckServerChange(string currentAddress)
        {
            if (_lastServerAddress != currentAddress)
            {
                _lastServerAddress = currentAddress;
                Reset();
            }
        }

        /// <summary>
        /// プレイヤーデータを処理し、参加/退出の通知を生成します。
        /// </summary>
        public void ProcessPlayerData(
            HashSet<string> currentIds, 
            int onlinePlayerCount,
            List<PlayerHistoryItem> playerHistory,
            string serverAddress)
        {
            _pollCount++;
            
            int samplePlayers = currentIds.Count;
            bool isSmallServer = (samplePlayers >= onlinePlayerCount && onlinePlayerCount > 0);
            
            if (!_isFirstPoll && NotificationsEnabled)
            {
                var joinedNames = new List<string>();
                var leftNames = new List<string>();

                // 参加検出: 現在のサンプルにあり、以前見たことがないUUID
                foreach (var id in currentIds)
                {
                    if (!_uuidLastSeenPoll.ContainsKey(id))
                    {
                        var p = playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == serverAddress);
                        if (p != null) joinedNames.Add(p.Name);
                    }
                }

                // 退出検出
                if (isSmallServer)
                {
                    // 小規模サーバー: 直接検出（完全なリストがあるため）
                    foreach (var id in _lastOnlineIds)
                    {
                        if (!currentIds.Contains(id))
                        {
                            var p = playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == serverAddress);
                            if (p != null) leftNames.Add(p.Name);
                        }
                    }
                }
                else
                {
                    // 大規模サーバー: 時間ベースの検出
                    var expiredIds = _uuidLastSeenPoll
                        .Where(kv => _pollCount - kv.Value > LEAVE_THRESHOLD_POLLS)
                        .Select(kv => kv.Key)
                        .ToList();
                    
                    foreach (var id in expiredIds)
                    {
                        var p = playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == serverAddress);
                        if (p != null) leftNames.Add(p.Name);
                        _uuidLastSeenPoll.Remove(id); // クリーンアップ
                    }
                }

                // 通知メッセージの構築
                if (joinedNames.Count > 0 || leftNames.Count > 0)
                {
                    string msg = "";

                    if (joinedNames.Count == 1) msg += $"{joinedNames[0]} が参加";
                    else if (joinedNames.Count > 1) msg += $"{joinedNames.Count}人が参加";

                    if (joinedNames.Count > 0 && leftNames.Count > 0) msg += "、";

                    if (leftNames.Count == 1) msg += $"{leftNames[0]} が退出";
                    else if (leftNames.Count > 1) msg += $"{leftNames.Count}人が退出";
                    
                    if (!string.IsNullOrEmpty(msg))
                    {
                        OnNotify?.Invoke("MineTray", msg + "しました。");
                    }
                }
            }
            
            // 追跡データを更新
            foreach (var id in currentIds)
            {
                _uuidLastSeenPoll[id] = _pollCount;
                _lastOnlineIds.Add(id);
            }
            
            _isFirstPoll = false;
        }

        /// <summary>
        /// 小規模サーバーの退出検出用に現在のオンラインIDを取得します。
        /// </summary>
        public HashSet<string> GetLastOnlineIds() => _lastOnlineIds;
    }
}
