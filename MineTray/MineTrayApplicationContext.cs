using MineTray.Models;
using MineTray.Services;
using MineTray.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MineTray
{

    public class MineTrayApplicationContext : ApplicationContext
    {
#nullable disable
        // コンストラクタで初期化されるフィールド - null許容を無効化
        private NotifyIcon _notifyIcon;

        // === 分離されたタイマー ===
        private Timer _pollTimer;       // サーバーPing用 (低速、60秒)
        private Timer _animationTimer;  // アイコンローテーション用 (高速、ユーザー定義)

        // === サービス ===
        private IconService _iconService;
        private NotificationService _notificationService;

        private MinecraftServerPinger _pinger;
        private SkinManager _skinManager;
        private AppSettings _settings;
        private List<PlayerHistoryItem> _playerHistory;
#nullable restore

        private PlayerListForm? _playerListForm;

        // 設定・データ

        private int _pollInterval = 60000;

        private MinecraftServerStatus? _lastStatus;

        // === アニメーション用共有データ ===
        private List<Image> _currentSkins = new();  // Pollで更新、アニメーションで参照
        private int _rotationIndex = 0;

        public static Icon? MainIcon { get; private set; } // フォームで共有

        public MineTrayApplicationContext()
        {
            _settings = AppSettings.Load();
            _playerHistory = PlayerHistoryManager.Load();

            // Services
            _iconService = new IconService();
            MainIcon = _iconService.MainIcon;

            _notificationService = new NotificationService();
            _notificationService.NotificationsEnabled = _settings.NotificationsEnabled;
            _notificationService.OnNotify += (title, msg) => Notify(title, msg, ToolTipIcon.Info);

            _pinger = new MinecraftServerPinger();
            _skinManager = new SkinManager();

            _playerListForm = new PlayerListForm(new List<PlayerHistoryItem>(), _skinManager);

            InitializeTrayIcon();
            InitializeTimers();

            // 自動起動設定を適用
            SetAutoStart(_settings.AutoStartEnabled);

            // 初回ポーリング
            // 注: Task.Runでラップするとスレッドプール上で実行されUIのSynchronizationContextを失うため、
            // コンストラクタ(UIスレッド)のコンテキストを維持できるよう直接awaitする。
            _ = RunInitialPollAsync();
        }

        private async Task RunInitialPollAsync()
        {
            await Task.Delay(1000);
            if (_notifyIcon.Visible)
            {
                TryPoll(); // 安全なラッパーを使用
            }
        }

        /// <summary>
        /// Windows起動時の自動起動を設定/解除します。
        /// </summary>
        private void SetAutoStart(bool enable)
        {
            try
            {
                const string appName = "MineTray";

                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return;

                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null) return;

                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetAutoStart] エラー: {ex.Message}");
            }
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = _iconService.MainIcon ?? SystemIcons.Application,
                Visible = true,
                Text = "MineTray: Initializing..."
            };

            UpdateContextMenu();

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                   ShowPlayerList();
                }
            };
        }

        private void UpdateContextMenu()
        {
            // GDI+/Component破棄漏れを防ぐため、置き換え前の古いメニューを保持しておき後で破棄する
            var oldMenu = _notifyIcon.ContextMenuStrip;

            var contextMenu = new ContextMenuStrip();
            contextMenu.RenderMode = ToolStripRenderMode.System;
            bool isJa = _settings.Language != "en";
            // 設定サブメニュー
            var settingsItem = new ToolStripMenuItem(isJa ? "設定" : "Settings");

            // 1. サーバー設定
            var serverSettingsItem = new ToolStripMenuItem(isJa ? "サーバー設定" : "Server Settings");
            serverSettingsItem.Click += (s, e) => ShowSettings();
            settingsItem.DropDownItems.Add(serverSettingsItem);

            // 2. 通知
            var notifItem = new ToolStripMenuItem(isJa ? "通知" : "Notifications");
            notifItem.Checked = _settings.NotificationsEnabled;
            notifItem.Click += (s, e) =>
            {
                _settings.NotificationsEnabled = !_settings.NotificationsEnabled;
                _notificationService.NotificationsEnabled = _settings.NotificationsEnabled;
                _settings.Save();
                UpdateContextMenu();
            };
            settingsItem.DropDownItems.Add(notifItem);

            // 3. Language
            var langItem = new ToolStripMenuItem("言語 (Language)");

            var jaItem = new ToolStripMenuItem("日本語");
            jaItem.Click += (s, e) =>
            {
                if (_settings.Language != "ja")
                {
                    _settings.Language = "ja";
                    _settings.Save();
                    UpdateContextMenu();
                }
            };
            if (_settings.Language == "ja") jaItem.Checked = true;
            langItem.DropDownItems.Add(jaItem);

            var enItem = new ToolStripMenuItem("English");
            enItem.Click += (s, e) =>
            {
                if (_settings.Language != "en")
                {
                    _settings.Language = "en";
                    _settings.Save();
                    UpdateContextMenu();
                }
            };
            if (_settings.Language == "en") enItem.Checked = true;
            langItem.DropDownItems.Add(enItem);

            settingsItem.DropDownItems.Add(langItem);

            // 4. 自動起動
            var autoStartItem = new ToolStripMenuItem(isJa ? "自動起動" : "Auto Start");
            autoStartItem.Checked = _settings.AutoStartEnabled;
            autoStartItem.Click += (s, e) =>
            {
                _settings.AutoStartEnabled = !_settings.AutoStartEnabled;
                SetAutoStart(_settings.AutoStartEnabled);
                _settings.Save();
                UpdateContextMenu();
            };
            settingsItem.DropDownItems.Add(autoStartItem);

            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(isJa ? "更新" : "Refresh", null, (s, e) => TryPoll());

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(isJa ? "終了" : "Exit", null, (s, e) => ExitThread());

            _notifyIcon.ContextMenuStrip = contextMenu;

            oldMenu?.Dispose();
        }

        private void ShowSettings()
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = form.GetSettings();
                _settings.Save();

                // Immediately apply new interval to animation timer
                int newInterval = _settings.RotationInterval;
                if (newInterval < 1000) newInterval = 1000;
                _animationTimer.Interval = newInterval;

                UpdateContextMenu();
                TryPoll();
            }
        }

        private void ShowPlayerList()
        {
            var selected = _settings.GetSelectedServer();
            var filtered = _playerHistory.Where(x => x.ServerAddress == selected.Address).ToList();

            bool isPlayersHidden = false;
            if (_lastStatus != null && _lastStatus.Players != null)
            {
                if (_lastStatus.Players.Online > 0 && (_lastStatus.Players.Sample == null || _lastStatus.Players.Sample.Count == 0))
                {
                    isPlayersHidden = true;
                }
            }

            if (_playerListForm == null || _playerListForm.IsDisposed)
            {
                _playerListForm = new PlayerListForm(filtered, _skinManager);
            }

            if (isPlayersHidden)
            {
                _playerListForm.ShowMessage("サーバーにより非公開");
            }
            else
            {
                _playerListForm.SetDataSource(filtered);
            }

            if (_playerListForm.Visible)
            {
                _playerListForm.Hide();
            }
            else
            {
                var screen = Screen.FromPoint(Cursor.Position);
                var workingArea = screen.WorkingArea;
                int x = workingArea.Right - _playerListForm.Width - 10;
                int y = workingArea.Bottom - _playerListForm.Height - 10;

                _playerListForm.Location = new Point(x, y);
                _playerListForm.Show();
                _playerListForm.Activate();
            }
        }

        private void Notify(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (!_settings.NotificationsEnabled) return;
            _notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        private void InitializeTimers()
        {
            // Polling Timer: Slow, for server data fetch
            _pollTimer = new Timer { Interval = _pollInterval };
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();

            // Animation Timer: Fast, for icon rotation
            int animInterval = _settings.RotationInterval;
            if (animInterval < 1000) animInterval = 1000;

            _animationTimer = new Timer { Interval = animInterval };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start(); // Always running from the start
        }

        // Wrapper for safe polling call from UI events
        private void TryPoll(bool isManual = false) => Poll(isManual);

        private async void Poll(bool isManual)
        {
             try
            {
                var selected = _settings.GetSelectedServer();
                _lastStatus = await _pinger.PingAsync(selected.Address);
                await UpdateDataAsync(isManual);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error: {ex.Message}");
                SetDefaultIcon(offline: true);
                if (isManual) Notify("Error", "Update Failed", ToolTipIcon.Error);
            }
        }

        private void PollTimer_Tick(object? sender, EventArgs? e) => Poll(false);

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentSkins.Count == 0)
            {
                // No skins available
                if (_lastStatus == null)
                {
                    // Offline -> Redstone
                    SetDefaultIcon(offline: true);
                }
                else
                {
                    // Online (0 players) -> Chest/Emerald (icon_online)
                    var onlineIcon = _iconService.OnlineIcon;
                    if (onlineIcon != null)
                    {
                        SetCustomIcon((Icon)onlineIcon.Clone());
                    }
                    else
                    {
                        // Fallback to Main if icon_online is missing
                        SetDefaultIcon(offline: false);
                    }
                }
                return;
            }

            _rotationIndex = (_rotationIndex + 1) % _currentSkins.Count;
            ShowSkinIcon(_rotationIndex);
        }

        /// <summary>
        /// Display a skin image as the tray icon.
        /// </summary>
        private void ShowSkinIcon(int index)
        {
            if (index < 0 || index >= _currentSkins.Count) return;

            var icon = _iconService.CreateIconFromImage(_currentSkins[index]);
            if (icon != null)
            {
                SetCustomIcon(icon);
            }
        }

        private void SetCustomIcon(Icon icon)
        {
            // GDI+ハンドルリークを防ぐため、前のカスタムアイコンの破棄はIconServiceに委譲
            _iconService.SetCurrentCustomIcon(icon);
            _notifyIcon.Icon = icon;
        }

        /// <summary>
        /// プレイヤーデータと_currentSkinsリストを更新します。
        /// アニメーションタイマーは自動的に変更を取得します。
        /// </summary>
        private async Task UpdateDataAsync(bool isManual)
        {
            var selected = _settings.GetSelectedServer();

            // サーバー切替または初回実行を検出し、通知の追跡状態をリセット
            _notificationService.CheckServerChange(selected.Address);

            if (_lastStatus == null)
            {
                _notifyIcon.Text = $"{selected.Alias}: Offline";

                // スキンをクリア、破棄を確保
                foreach(var s in _currentSkins) s.Dispose();
                _currentSkins.Clear();
                _rotationIndex = 0;

                // 現在のサーバーのプレイヤーをオフラインとしてマーク
                foreach(var p in _playerHistory.Where(x => x.ServerAddress == selected.Address))
                {
                    p.IsOnline = false;
                }
                _notificationService.GetLastOnlineIds().Clear();

                if (isManual) Notify("MineTray", "サーバーがオフラインです。", ToolTipIcon.Warning);
                return;
            }

            var currentSample = _lastStatus.Players?.Sample ?? new List<PlayerSample>();
            var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newSkins = new List<Image>();
            bool historyChanged = false;

            // --- 1. 履歴の更新と有効なAPIリクエストのフィルタリング ---
            var playersToFetch = new List<PlayerSample>();

            foreach (var p in currentSample)
            {
                if (string.IsNullOrEmpty(p.Id)) continue;
                if (!Guid.TryParse(p.Id, out _)) continue;

                string cleanName = p.CleanName;
                if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > 25) continue;

                currentIds.Add(p.Id);
                playersToFetch.Add(p);

                var existing = _playerHistory.FirstOrDefault(h => h.Id == p.Id && h.ServerAddress == selected.Address);
                if (existing == null)
                {
                    existing = new PlayerHistoryItem
                    {
                        Id = p.Id,
                        Name = cleanName,
                        ServerAddress = selected.Address
                    };
                    _playerHistory.Add(existing);
                    historyChanged = true;
                }
                else if (existing.Name != cleanName)
                {
                     existing.Name = cleanName;
                     historyChanged = true;
                }

                existing.IsOnline = true;
                existing.LastSeen = DateTime.Now;
            }

            // --- 2. スキンの並列ダウンロード ---
            // Task.WhenAllを使用して並列でスキンを取得
            var skinTasks = playersToFetch.Select(async p =>
            {
                try
                {
                    return await _skinManager.GetSkinImageAsync(p.Id ?? string.Empty);
                }
                catch
                {
                    return null;
                }
            });

            var fetchedImages = await Task.WhenAll(skinTasks);
            foreach (var img in fetchedImages)
            {
                if (img != null) newSkins.Add(img);
            }

            // --- 3. オフラインプレイヤーの処理 ---
            foreach (var p in _playerHistory.Where(x => x.ServerAddress == selected.Address))
            {
                if (!currentIds.Contains(p.Id))
                {
                    if (p.IsOnline)
                    {
                        p.IsOnline = false;
                    }
                }
            }

            if (historyChanged)
            {
                PlayerHistoryManager.Save(_playerHistory);
            }

            // --- 4. スマート参加/退出通知 ---
            int onlinePlayers = _lastStatus.Players?.Online ?? 0;
            _notificationService.ProcessPlayerData(currentIds, onlinePlayers, _playerHistory, selected.Address);

            // 手動更新時のフィードバック
            if (isManual)
            {
                int fetchCount = currentIds.Count;
                if (_lastStatus.Players != null && _lastStatus.Players.Online > fetchCount) fetchCount = _lastStatus.Players.Online;
                Notify("MineTray", $"更新完了: {fetchCount}人がオンライン", ToolTipIcon.Info);
            }

            int onlineCount = _lastStatus.Players?.Online ?? 0;
            int maxCount = _lastStatus.Players?.Max ?? 0;

            string displayTitle = string.IsNullOrWhiteSpace(selected.Alias) ? selected.Address : selected.Alias;
            string tooltipText = $"{displayTitle}: {onlineCount}/{maxCount}";
            if (tooltipText.Length >= 64) tooltipText = tooltipText.Substring(0, 60) + "...";
            _notifyIcon.Text = tooltipText;

            // --- 5. スキンリストを安全に交換 ---
            // メモリリークを防ぐため古いスキンを破棄
            foreach(var s in _currentSkins) s.Dispose();

            _currentSkins = newSkins;

            // リストサイズが変わった場合はインデックスをリセット
            if (_currentSkins.Count > 0 && _rotationIndex >= _currentSkins.Count)
            {
                _rotationIndex = 0;
            }

            // オンラインサーバー（0人）用のアイコンを設定
            var onlineIcon = _iconService.OnlineIcon;
            if (_currentSkins.Count == 0 && onlineIcon != null)
            {
                SetCustomIcon((Icon)onlineIcon.Clone());
            }
        }

        private void SetDefaultIcon(bool offline)
        {
            if (offline)
            {
                var icon = _iconService.OfflineIcon;
                if (icon != null) SetCustomIcon((Icon)icon.Clone());
                else SetFallbackIcon(Color.Gray);
            }
            else
            {
                var icon = _iconService.MainIcon;
                if (icon != null) SetCustomIcon((Icon)icon.Clone());
                else SetFallbackIcon(Color.LightGreen);
            }
        }

        private void SetFallbackIcon(Color color)
        {
            var icon = _iconService.CreateFallbackIcon(color);
            if (icon != null) SetCustomIcon(icon);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pollTimer?.Stop();
                _animationTimer?.Stop();

                _notifyIcon.Visible = false;
                _notifyIcon.ContextMenuStrip?.Dispose();
                _notifyIcon.Dispose();

                _pollTimer?.Dispose();
                _animationTimer?.Dispose();

                _iconService?.Dispose();
                _skinManager?.Dispose();

                // 全てのスキン画像を破棄
                foreach(var s in _currentSkins) s.Dispose();
                _currentSkins.Clear();

                if (_playerListForm != null && !_playerListForm.IsDisposed)
                    _playerListForm.Dispose();

                 PlayerHistoryManager.Save(_playerHistory);
            }
            base.Dispose(disposing);
        }
    }
}
