using MineTray.Models;
using MineTray.Services;

namespace MineTray.Tests
{
    /// <summary>
    /// NotificationServiceの参加/退出検出ロジックのテスト。
    /// </summary>
    public class NotificationServiceTests
    {
        private static HashSet<string> Ids(params string[] ids) =>
            new(ids, StringComparer.OrdinalIgnoreCase);

        [Fact]
        public void FirstPoll_DoesNotNotify()
        {
            var service = new NotificationService();
            var notifications = new List<(string Title, string Message)>();
            service.OnNotify += (t, m) => notifications.Add((t, m));

            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids("id1"), 1, history, "addr");

            Assert.Empty(notifications);
        }

        [Fact]
        public void PlayerJoins_OnSubsequentPoll_Notifies()
        {
            var service = new NotificationService();
            var notifications = new List<(string Title, string Message)>();
            service.OnNotify += (t, m) => notifications.Add((t, m));

            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" },
                new() { Id = "id2", Name = "Bob", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids("id1"), 1, history, "addr"); // 初回: Aliceのみ
            service.ProcessPlayerData(Ids("id1", "id2"), 2, history, "addr"); // Bobが参加

            var notification = Assert.Single(notifications);
            Assert.Contains("Bob", notification.Message);
            Assert.Contains("参加", notification.Message);
        }

        [Fact]
        public void PlayerLeaves_OnSmallServer_Notifies()
        {
            var service = new NotificationService();
            var notifications = new List<(string Title, string Message)>();
            service.OnNotify += (t, m) => notifications.Add((t, m));

            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" },
                new() { Id = "id2", Name = "Bob", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids("id1", "id2"), 2, history, "addr"); // 初回: 両方在線
            service.ProcessPlayerData(Ids("id1"), 1, history, "addr"); // Bobが退出（サンプルが全員をカバー＝小規模サーバー）

            var notification = Assert.Single(notifications);
            Assert.Contains("Bob", notification.Message);
            Assert.Contains("退出", notification.Message);
        }

        [Fact]
        public void NotificationsDisabled_SuppressesNotification()
        {
            var service = new NotificationService { NotificationsEnabled = false };
            var notifications = new List<(string Title, string Message)>();
            service.OnNotify += (t, m) => notifications.Add((t, m));

            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids(), 0, history, "addr");
            service.ProcessPlayerData(Ids("id1"), 1, history, "addr"); // 参加が発生するはずだが通知は抑制される

            Assert.Empty(notifications);
        }

        [Fact]
        public void CheckServerChange_ResetsTrackingState()
        {
            var service = new NotificationService();
            var notifications = new List<(string Title, string Message)>();
            service.OnNotify += (t, m) => notifications.Add((t, m));

            var historyAddr1 = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr1" }
            };
            service.ProcessPlayerData(Ids("id1"), 1, historyAddr1, "addr1"); // addr1での初回ポール

            // サーバー切替: 追跡状態がリセットされ、次のポールが再び「初回」扱いになるべき
            service.CheckServerChange("addr2");

            var historyAddr2 = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr2" }
            };
            service.ProcessPlayerData(Ids("id1"), 1, historyAddr2, "addr2");

            Assert.Empty(notifications);
        }

        [Fact]
        public void GetLastOnlineIds_ReturnsLiveMutableReference()
        {
            var service = new NotificationService();
            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids("id1"), 1, history, "addr");
            Assert.Contains("id1", service.GetLastOnlineIds());

            // MineTrayApplicationContextはオフライン検知時にこの参照を直接Clear()する
            service.GetLastOnlineIds().Clear();
            Assert.Empty(service.GetLastOnlineIds());
        }

        [Fact]
        public void Reset_ClearsAllTrackingState()
        {
            var service = new NotificationService();
            var history = new List<PlayerHistoryItem>
            {
                new() { Id = "id1", Name = "Alice", ServerAddress = "addr" }
            };

            service.ProcessPlayerData(Ids("id1"), 1, history, "addr");
            Assert.NotEmpty(service.GetLastOnlineIds());

            service.Reset();

            Assert.Empty(service.GetLastOnlineIds());
        }
    }
}
